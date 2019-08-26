namespace LogStore.Core
#nowarn "9"

open System.IO
open System.Runtime.InteropServices
open System.Security.Cryptography
open System.Text.RegularExpressions
open Microsoft.FSharp.NativeInterop
open LogStore.Common.Utils

type ValidFileName = ValidFileName of string

type Chunk = {
    ChunkHeader: ChunkHeader
    FileName: string
    OriginStream: OriginStream
    ChunkFooter: ChunkFooter
} with
    member this.Header = this.ChunkHeader
    member this.Footer = this.ChunkFooter

type Reader = {
    CachedData: nativeint option
    FileName: string
    Start: int64
    Agent: PoolAgent<BinaryReader>
}

type Writer = {
    CachedData: nativeint
    FlushStream: FlushStream
    WriterStream: WriterStream
    BufferStream: BufferStream
    BufferWriter: BinaryWriter
    MD5: MD5
    Stop: int64
}

type WorkArea = {
    ChunkNum: int
    Writer: Writer
    Readers: Reader array
}

module Chunk =

    let writeBufferSize = 8192

    let readBufferSize = 8192

    let buffer = Array.zeroCreate readBufferSize

    let fileName (cfg: ChunkConfig) (index: int) =
        Path.Combine (cfg.Path, sprintf "%s%0*i.lsc" cfg.Prefix cfg.Length index)

    let alignedSize dataSize =
        let size = ChunkHeader.size + dataSize + ChunkFooter.size
        let x = size % 4096L
        match x with
        | 0L -> size
        | _ -> (size / 4096L + 1L) * 4096L

    //#region 数据验证

    let accHash (md5: MD5) (OriginStream stream) (count: int64) =
        stream.Position <- 0L
        let rec trans toRead =
            match toRead with
            | read when read <= readBufferSize ->
                stream.Read (buffer, 0, read) |> ignore
                md5.TransformBlock (buffer, 0, read, null, 0) |> ignore
            | _ ->
                stream.Read (buffer, 0, readBufferSize) |> ignore
                md5.TransformBlock (buffer, 0, readBufferSize, null, 0) |> ignore
                trans <| toRead - readBufferSize
        trans <| int32 count

    let validateName (cfg: ChunkConfig) (input: string list) =
        let pattern = sprintf "^%s\d{%i}\.lsc$" cfg.Prefix cfg.Length |> Regex
        let select (elem: string) =
            match elem with
            | name when pattern.IsMatch (Path.GetFileName name) -> Some <| ValidFileName name
            | _ -> None
        input |> List.choose select

    let compare (cfg: ChunkConfig) (ValidFileName filename) (index: int) =
        filename = fileName cfg index

    let validateChecksum (chunk: Chunk) : bool =
        if not chunk.ChunkFooter.IsCompleted
        then failwithf "Chunk：%d 未完成，只能校验已完成的Chunk。" chunk.ChunkHeader.ChunkNumber
        use md5 = MD5.Create ()
        accHash md5 chunk.OriginStream <| ChunkHeader.size + chunk.ChunkFooter.DataSize
        ChunkFooter.accHash md5 chunk.OriginStream
        Array.forall2 ( = ) md5.Hash chunk.ChunkFooter.MD5Hash

    //#endregion

    //#region 关闭资源

    let close (chunk: Chunk) : unit =
        let (OriginStream os) = chunk.OriginStream
        os.Close ()

    let closeWriter (writer: Writer) : unit =
        let { FlushStream = FlushStream fs; WriterStream = WriterStream ws; BufferStream = Buffer bs; BufferWriter = bw; MD5 = md5 } = writer
        bw.Close ()
        bs.Close ()
        ws.Close ()
        fs.Close ()
        md5.Clear ()

    let closeReader (reader: Reader) : unit =
        match reader.CachedData with
        | Some ptr -> Marshal.FreeHGlobal ptr
        | None -> ()
        reader.Agent.Close <| Some (fun (br: BinaryReader) -> br.Close ())

    //#endregion

    //#region 创建Chunk

    let internalCreate (cfg: ChunkConfig) index =
        let md5 = MD5.Create ()
        let header = ChunkHeader.create index cfg.ChunkSize
        let headerByte = header.AsByteArray
        let footer = ChunkFooter.Default
        let footerByte = footer.AsByteArray
        let filename = fileName cfg index
        let tempFilename = sprintf @"%s.tmp" filename
        let tempFile = new FileStream (tempFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None)
        tempFile.SetLength <| alignedSize cfg.ChunkSize
        md5.TransformBlock (headerByte, 0, int ChunkHeader.size, null, 0) |> ignore
        tempFile.Write (headerByte, 0, int ChunkHeader.size)
        tempFile.Seek (-ChunkFooter.size, SeekOrigin.End) |> ignore
        tempFile.Write (footerByte, 0, int ChunkFooter.size)
        tempFile.Flush true
        tempFile.Close ()
        File.Move (tempFilename, filename)
        (header, filename, footer)

    let fromFile (ValidFileName filename) =
        let fs = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read, readBufferSize, FileOptions.SequentialScan)
        let os = OriginStream fs
        let ch = ChunkHeader.fromStream os
        let cf = ChunkFooter.fromStream os
        { ChunkHeader = ch; FileName = filename; OriginStream = os; ChunkFooter = cf }

    let create (cfg: ChunkConfig) : unit =
        if Directory.Exists cfg.Path |> not then failwithf "初始化Chunk库异常：文件夹%s不存在。" cfg.Path
        if (Directory.GetFiles cfg.Path).Length <> 0 then failwithf "初始化Chunk库异常：%s中已有文件。" cfg.Path
        internalCreate cfg 0 |> ignore

    //#endregion

    //#region 创建Reader、Writer

    let buildCacheArray (ptr: nativeptr<byte>) cachedSize (fs: FileStream) toRead =
        let rec trans (um: UnmanagedMemoryStream) (buffer: byte array) toRead =
            match toRead with
            | read when read <= buffer.Length ->
                fs.Read (buffer, 0, read) |> ignore
                um.Write (buffer, 0, read)
            | _ ->
                fs.Read (buffer, 0, buffer.Length) |> ignore
                um.Write (buffer, 0, buffer.Length)
                trans um buffer <| toRead - buffer.Length
        let buffer = Array.zeroCreate <| 64 * 1024
        use umTemp = new UnmanagedMemoryStream (ptr, cachedSize, cachedSize, FileAccess.ReadWrite)
        fs.Position <- 0L
        trans umTemp buffer toRead

    let cacheReader (cfg: ChunkConfig) (readers: Reader array) (chunk: Chunk) =
        let cachedSize = alignedSize chunk.Footer.DataSize
        let cachedData = Marshal.AllocHGlobal (int cachedSize)
        let ptr = NativePtr.ofNativeInt<byte> cachedData
        let (OriginStream fs) = chunk.OriginStream
        buildCacheArray ptr cachedSize fs <| int cachedSize
        let brs = [ 1 .. cfg.ReaderCount ] |> List.map (fun x ->
            let um = new UnmanagedMemoryStream (ptr, cachedSize)
            new BinaryReader (um)
        )
        let start = int64 chunk.Header.ChunkNumber * cfg.ChunkSize
        let reader = { CachedData = Some cachedData; FileName = chunk.FileName; Start = start; Agent = PoolAgent (brs, 3.0) }
        close chunk
        readers.[chunk.Header.ChunkNumber] <- reader

    let fileReader (cfg: ChunkConfig) (readers: Reader array) (chunk: Chunk) =
        let brs = [ 1 .. cfg.ReaderCount ] |> List.map (fun x ->
            let fs = new FileStream (chunk.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, readBufferSize, FileOptions.SequentialScan)
            new BinaryReader (fs)
        )
        let start = int64 chunk.Header.ChunkNumber * cfg.ChunkSize
        let reader = { CachedData = None; FileName = chunk.FileName; Start = start; Agent = PoolAgent (brs, 3.0) }
        close chunk
        readers.[chunk.Header.ChunkNumber] <- reader

    let buildReaders (cfg: ChunkConfig) (readers: Reader array) (chunks: Chunk list) =
        match chunks.Length with
        | len when len < cfg.CacheSize -> chunks |> List.iter (cacheReader cfg readers)
        | _ ->
            let c, f = chunks |> List.splitAt (cfg.CacheSize - 1)
            c |> List.iter (cacheReader cfg readers)
            f |> List.iter (fileReader cfg readers)

    let rec freeSeek (br: BinaryReader) toWrite =
        let length = br.ReadInt32 ()
        match length with
        | l when l <= 0 -> toWrite
        | l ->
            br.ReadBytes length |> ignore
            let suffixLength = br.ReadInt32 ()
            match suffixLength with
            | s when s <> l -> toWrite
            | _ -> freeSeek br <| toWrite + length + 2 * sizeof<int>

    let rec fixedSeek (fixedLength: int) (br: BinaryReader) toWrite =
        br.ReadBytes fixedLength |> ignore
        let length = br.ReadInt32 ()
        match length with
        | l when l = fixedLength -> fixedSeek fixedLength br <| toWrite + fixedLength + sizeof<int>
        | _ -> toWrite

    let cacheWriter (ptr: nativeptr<byte>) cachedSize (chunk: Chunk) (md5: MD5) internalSeek =
        let (OriginStream fs) = chunk.OriginStream
        use br = new BinaryReader (fs)
        fs.Position <- ChunkHeader.size
        let pos = int ChunkHeader.size |> internalSeek br
        buildCacheArray ptr cachedSize fs pos
        accHash md5 chunk.OriginStream <| int64 pos
        int64 pos

    let buildWriter (cfg: ChunkConfig) (chunk: Chunk) : (int64 * Writer * Reader) =
        let start = int64 chunk.Header.ChunkNumber * cfg.ChunkSize
        let cachedSize = alignedSize chunk.Header.ChunkSize
        let cachedData = Marshal.AllocHGlobal (int cachedSize)
        let ptr = NativePtr.ofNativeInt<byte> cachedData
        let md5 = MD5.Create ()
        let pos =
            match cfg.LogMode with
            | Free -> cacheWriter ptr cachedSize chunk md5 freeSeek
            | Fixed l -> cacheWriter ptr cachedSize chunk md5 <| fixedSeek l
        File.SetAttributes (chunk.FileName, FileAttributes.NotContentIndexed)
        let brs =
            [ 1 .. cfg.ReaderCount ]
            |> List.map (fun x ->
                let um = new UnmanagedMemoryStream (ptr, cachedSize)
                new BinaryReader (um)
            )
        let reader = { CachedData = Some cachedData; FileName = chunk.FileName; Start = start; Agent = PoolAgent (brs, 3.0) }
        let flush = new FileStream (chunk.FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, writeBufferSize, FileOptions.SequentialScan)
        let write = new UnmanagedMemoryStream (ptr, cachedSize, cachedSize, FileAccess.ReadWrite)
        flush.Position <- pos
        write.Position <- pos
        let memStream = new MemoryStream (writeBufferSize)
        let writer = {
            CachedData = cachedData
            FlushStream = FlushStream flush
            WriterStream = WriterStream write
            BufferStream = Buffer memStream
            BufferWriter = new BinaryWriter (memStream)
            MD5 = md5
            Stop = start + cfg.ChunkSize
        }
        close chunk
        let globalPos = start + pos - ChunkHeader.size
        globalPos, writer, reader

    let complete (cfg: ChunkConfig) { ChunkNum = oldNum; Writer = writer; Readers = readers} : Reader option * Reader * WorkArea =
        // 写完并刷盘
        let { FlushStream = FlushStream flushstream; WriterStream = WriterStream ws; MD5 = md5 } = writer
        let dataSize = ws.Position - ChunkHeader.size
        let footer = (ChunkFooter.complete dataSize md5).AsByteArray
        let aligned = alignedSize dataSize
        ws.SetLength aligned
        flushstream.SetLength aligned
        ws.Seek (-ChunkFooter.size, SeekOrigin.End) |> ignore
        flushstream.Seek (-ChunkFooter.size, SeekOrigin.End) |> ignore
        ws.Write (footer, 0, footer.Length)
        flushstream.Write (footer, 0, footer.Length)
        flushstream.Flush true
        File.SetAttributes (flushstream.Name, FileAttributes.ReadOnly ||| FileAttributes.NotContentIndexed)
        // 切换内存Reader，以对齐DataSize
        let cachedData = Marshal.AllocHGlobal (int aligned)
        let ptr = NativePtr.ofNativeInt<byte> cachedData
        use umTemp = new UnmanagedMemoryStream (ptr, aligned, aligned, FileAccess.ReadWrite)
        ws.Position <- 0L
        ws.CopyTo umTemp
        let brs =
            [ 1 .. cfg.ReaderCount ]
            |> List.map (fun x ->
                let um = new UnmanagedMemoryStream (ptr, aligned)
                new BinaryReader (um)
            )
        let oldReader = readers.[oldNum]
        let updatedReader = { oldReader with CachedData = Some cachedData; Agent = PoolAgent (brs, 3.0) }
        readers.[oldNum] <- updatedReader
        // 创建新的Reader、Writer
        let newNum = oldNum + 1
        let (header, filename, footer) = internalCreate cfg newNum
        let tempfs = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read, readBufferSize, FileOptions.SequentialScan)
        let chunk = { ChunkHeader = header; FileName = filename; OriginStream = OriginStream tempfs; ChunkFooter = footer }
        let (_, newWriter, newReader) = buildWriter cfg chunk
        readers.[newNum] <- newReader
        closeWriter writer
        // 处理超过缓存数量限制的情形
        match newNum with
        | num when num > cfg.CacheSize ->
            let idx = num - cfg.CacheSize - 1
            let reader = readers.[idx]
            let brs = [ 1 .. cfg.ReaderCount ] |> List.map (fun x ->
                let fs = new FileStream (reader.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, readBufferSize, FileOptions.SequentialScan)
                new BinaryReader (fs)
            )
            readers.[idx] <- { reader with CachedData = None; Agent = PoolAgent (brs, 3.0) }
            Some reader, oldReader, { ChunkNum = newNum; Writer = newWriter; Readers = readers}
        | _ -> None, oldReader, { ChunkNum = newNum; Writer = newWriter; Readers = readers}

    //#endregion

    //#region 读写操作

    let freeAppend (writeTo: BinaryWriter -> unit) (bs: MemoryStream) (bw: BinaryWriter) =
        bs.SetLength 4L
        bs.Position <- 4L
        writeTo bw
        let length = int bs.Length - 4
        bw.Write length
        bs.Position <- 0L
        bw.Write length
        bs.Length

    let fixedAppend (fixedLength: int) (writeTo: BinaryWriter -> unit) (bs: MemoryStream) (bw: BinaryWriter) =
        bs.SetLength 0L
        writeTo bw
        bw.Write fixedLength
        match int bs.Length with
        | l when l = fixedLength + sizeof<int> -> bs.Length
        | l -> failwithf "写入长度%d，不匹配固定长度%d" l fixedLength

    let append (internalAppend: (BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64) writeTo oldPos writer : int64 =
        let { FlushStream = FlushStream fs; WriterStream = WriterStream ws; BufferStream = Buffer bs; BufferWriter = bw; MD5 = md5; Stop = stop } = writer
        let newPos = oldPos + internalAppend writeTo bs bw
        match newPos with
        | np when np > stop -> oldPos
        | np ->
            let len = int bs.Length
            let buf = bs.GetBuffer ()
            md5.TransformBlock(buf, 0, len, null, 0) |> ignore
            ws.Write (buf, 0, len)
            fs.Write (buf, 0, len)
            np

    let freeRead (readFrom: BinaryReader -> unit) localPos (br: BinaryReader) = async {
        br.BaseStream.Position <- localPos
        let prefixLength = br.ReadInt32 ()
        if prefixLength <= 0 then failwithf "自由读取异常：数据长度应为正值，但实际为%d。" prefixLength
        readFrom br
        let suffixLength = br.ReadInt32 ()
        if prefixLength <> suffixLength then failwithf "自由读取异常：前缀长度%d 不等于后缀长度%d。" prefixLength suffixLength
    }

    let fixedRead (fixedLength: int) (readFrom: BinaryReader -> unit) localPos (br: BinaryReader) = async {
        br.BaseStream.Position <- localPos
        readFrom br
        let length = br.ReadInt32 ()
        if length <> fixedLength then failwithf "定长读取异常：数据长度应为%d，但实际为%d。" fixedLength length
        match br.BaseStream.Position - localPos with
        | l when l - 4L <> int64 fixedLength -> failwithf "定长读取异常：读取长度%d，不匹配固定长度%d" l fixedLength
        | _ -> ()
    }

    let read (internalRead: (BinaryReader -> unit) -> int64 -> BinaryReader -> Async<unit>) readFrom (reader: Reader) globalPos = async {
        let localPos = globalPos - reader.Start + ChunkHeader.size
        let! result = reader.Agent.AsyncFunc <| internalRead readFrom localPos
        return! result
    }

    //#endregion