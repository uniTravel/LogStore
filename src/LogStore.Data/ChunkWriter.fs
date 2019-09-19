namespace LogStore.Data

open System.IO

module ChunkWriter =

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
        bw.Write fixedLength
        writeTo bw
        match int bs.Length with
        | l when l = fixedLength + sizeof<int> -> bs.Length
        | l -> failwithf "写入长度%d，不匹配固定长度%d" l fixedLength