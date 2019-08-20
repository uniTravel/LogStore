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
    OriginStream: OriginStream
    ChunkFooter: ChunkFooter
} with
    member this.Header = this.ChunkHeader
    member this.Footer = this.ChunkFooter

type ReaderItem = {
    CachedData: nativeint
    ReaderStream: ReaderStream
    BinaryReader: BinaryReader
}

type Reader = {
    Start: int64
    Agent: PoolAgent<ReaderItem>
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
    Readers: (int * Reader) array
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
        stream.Seek (0L, SeekOrigin.Begin) |> ignore
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
        md5.TransformFinalBlock (Array.empty, 0, 0) |> ignore
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
        fun readerItem ->
            let { CachedData = cd; ReaderStream = ReaderStream rs; BinaryReader = br } = readerItem
            br.Close ()
            rs.Close ()
            Marshal.FreeHGlobal cd
        |> Some
        |> reader.Agent.Close

    //#endregion

    //#region 创建值，包括Chunk、Reader、Writer

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
        { ChunkHeader = ch; OriginStream = os; ChunkFooter = cf }

    let create (cfg: ChunkConfig) : unit =
        if Directory.Exists cfg.Path |> not then failwithf "文件夹%s不存在。" cfg.Path
        if (Directory.GetFiles cfg.Path).Length <> 0 then failwith "Chunk库中已有文件。"
        internalCreate cfg 0 |> ignore

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
        fs.Seek (0L, SeekOrigin.Begin) |> ignore
        trans umTemp buffer toRead

    let locate (fs: FileStream) =
        let rec seek (br: BinaryReader) toWrite =
            let length = br.ReadInt32 ()
            match length with
            | l when l <= 0 -> toWrite
            | l ->
                br.ReadBytes (length) |> ignore
                let suffixLength = br.ReadInt32 ()
                match suffixLength with
                | s when s <> l -> toWrite
                | _ -> seek br <| toWrite + length + 2 * sizeof<int>
        use br = new BinaryReader (fs)
        fs.Seek (ChunkHeader.size, SeekOrigin.Begin) |> ignore
        seek br <| int ChunkHeader.size

    let buildReader (cfg: ChunkConfig) (chunk: Chunk) : Reader =
        let cachedSize = alignedSize chunk.Footer.DataSize
        let cachedData = Marshal.AllocHGlobal (int cachedSize)
        let ptr = NativePtr.ofNativeInt<byte> cachedData
        let (OriginStream fs) = chunk.OriginStream
        File.SetAttributes (fs.Name, FileAttributes.ReadOnly ||| FileAttributes.NotContentIndexed)
        buildCacheArray ptr cachedSize fs <| int cachedSize
        let readerItems =
            [ 1 .. cfg.ReaderCount ]
            |> List.map (fun x ->
                let um = new UnmanagedMemoryStream (ptr, cachedSize)
                let br = new BinaryReader (um)
                { CachedData = cachedData; ReaderStream = ReaderStream um; BinaryReader = br }
            )
        close chunk
        { Start = int64 chunk.Header.ChunkNumber * cfg.ChunkSize; Agent = PoolAgent (readerItems, 3.0) }

    let buildWriter (cfg: ChunkConfig) (chunk: Chunk) : (int * int64 * Writer * Reader) =
        let cachedSize = alignedSize chunk.Header.ChunkSize
        let cachedData = Marshal.AllocHGlobal (int cachedSize)
        let ptr = NativePtr.ofNativeInt<byte> cachedData
        let (OriginStream fs) = chunk.OriginStream
        let start = int64 chunk.Header.ChunkNumber * cfg.ChunkSize
        File.SetAttributes (fs.Name, FileAttributes.NotContentIndexed)
        let pos = locate fs
        buildCacheArray ptr cachedSize fs pos
        let readerItems =
            [ 1 .. cfg.ReaderCount ]
            |> List.map (fun x ->
                let um = new UnmanagedMemoryStream (ptr, cachedSize)
                let br = new BinaryReader (um)
                { CachedData = cachedData; ReaderStream = ReaderStream um; BinaryReader = br }
            )
        let reader = { Start = start; Agent = PoolAgent (readerItems, 3.0) }
        let flush = new FileStream (fs.Name, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, writeBufferSize, FileOptions.SequentialScan)
        let write = new UnmanagedMemoryStream (ptr, cachedSize, cachedSize, FileAccess.ReadWrite)
        flush.Position <- int64 pos
        write.Position <- int64 pos
        let memStream = new MemoryStream (writeBufferSize)
        let md5 = MD5.Create ()
        accHash md5 chunk.OriginStream <| int64 pos
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
        let globalPos = start + int64 pos - ChunkHeader.size
        chunk.Header.ChunkNumber, globalPos, writer, reader

    let complete (cfg: ChunkConfig) { ChunkNum = oldNum; Writer = writer; Readers = readers} : Reader * WorkArea =
        // 写完并刷盘
        let { FlushStream = FlushStream fs; WriterStream = WriterStream ws; MD5 = md5 } = writer
        let dataSize = ws.Position - ChunkHeader.size
        let footer = (ChunkFooter.complete dataSize md5).AsByteArray
        let aligned = alignedSize dataSize
        ws.SetLength aligned
        fs.SetLength aligned
        ws.Seek (-ChunkFooter.size, SeekOrigin.End) |> ignore
        fs.Seek (-ChunkFooter.size, SeekOrigin.End) |> ignore
        ws.Write (footer, 0, footer.Length)
        fs.Write (footer, 0, footer.Length)
        fs.Flush true
        // 切换内存Reader，以对齐DataSize
        let cachedData = Marshal.AllocHGlobal (int aligned)
        let ptr = NativePtr.ofNativeInt<byte> cachedData
        File.SetAttributes (fs.Name, FileAttributes.ReadOnly ||| FileAttributes.NotContentIndexed)
        use umTemp = new UnmanagedMemoryStream (ptr, aligned, aligned, FileAccess.ReadWrite)
        ws.Position <- 0L
        ws.CopyTo umTemp
        let readerItems =
            [ 1 .. cfg.ReaderCount ]
            |> List.map (fun x ->
                let um = new UnmanagedMemoryStream (ptr, aligned)
                let br = new BinaryReader (um)
                { CachedData = cachedData; ReaderStream = ReaderStream um; BinaryReader = br }
            )
        let oldIdx = oldNum % cfg.CacheSize
        let (num, oldReader) = readers.[oldIdx]
        let newReader = { oldReader with Agent = PoolAgent (readerItems, 3.0) }
        readers.[oldIdx] <- num, newReader
        // 创建新的Reader、Writer
        let (header, filename, footer) = internalCreate cfg (oldNum + 1)
        let tempfs = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read, readBufferSize, FileOptions.SequentialScan)
        let chunk = { ChunkHeader = header; OriginStream = OriginStream tempfs; ChunkFooter = footer }
        let (newNum, _, w, r) = buildWriter cfg chunk
        let newIdx = newNum % cfg.CacheSize
        readers.[newIdx] <- newNum, r
        closeWriter writer
        oldReader, { ChunkNum = newNum; Writer = w; Readers = readers}

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
        length + 2 * sizeof<int> |> int64

    let fixedAppend (fixedLength: int) (writeTo: BinaryWriter -> unit) (bs: MemoryStream) (bw: BinaryWriter) =
        bs.SetLength 0L
        writeTo bw
        match int bs.Length with
        | l when l = fixedLength -> int64 fixedLength
        | l -> failwithf "写入长度%d，不等于固定长度%d" l fixedLength

    let append (internalAppend: (BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64) writeTo oldPos writer : int64 =
        let { FlushStream = FlushStream fs; WriterStream = WriterStream ws; BufferStream = Buffer bs; BufferWriter = bw; MD5 = md5; Stop = stop } = writer
        let newPos = oldPos + internalAppend writeTo bs bw
        match newPos with
        | np when np > stop -> oldPos
        | np ->
            let len = int bs.Length
            let buf = bs.GetBuffer ()
            md5.TransformBlock(buffer, 0, len, null, 0) |> ignore
            ws.Write (buf, 0, len)
            fs.Write (buf, 0, len)
            np

    let freeRead (readFrom: BinaryReader -> unit) localPos readerItem =
        let { ReaderStream = ReaderStream rs; BinaryReader = br } = readerItem
        rs.Position <- localPos
        let prefixLength = br.ReadInt32 ()
        if prefixLength <= 0 then failwithf "数据长度应为正值，但实际为%d。" prefixLength
        readFrom br
        let suffixLength = br.ReadInt32 ()
        if prefixLength <> suffixLength then failwithf "前缀长度%d 不等于后缀长度%d。" prefixLength suffixLength

    let fixedRead (fixedLength: int) (readFrom: BinaryReader -> unit) localPos readerItem =
        let { ReaderStream = ReaderStream rs; BinaryReader = br } = readerItem
        rs.Position <- localPos
        readFrom br
        match rs.Position - localPos with
        | l when l <> int64 fixedLength -> failwithf "读取长度%d，不等于固定长度%d" l fixedLength
        | _ -> ()

    let read (internalRead: (BinaryReader -> unit) -> int64 -> ReaderItem -> unit) readFrom (reader: Reader) globalPos = async {
        let localPos = globalPos - reader.Start + ChunkHeader.size
        return! reader.Agent.AsyncAction <| internalRead readFrom localPos
    }

    //#endregion