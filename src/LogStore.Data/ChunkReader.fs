namespace LogStore.Data

open System.IO

module ChunkReader =

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
        let length = br.ReadInt32 ()
        if length <> fixedLength then failwithf "定长读取异常：数据长度应为%d，但实际为%d。" fixedLength length
        readFrom br
        match br.BaseStream.Position - localPos with
        | l when l - 4L <> int64 fixedLength -> failwithf "定长读取异常：读取长度%d，不匹配固定长度%d" l fixedLength
        | _ -> ()
    }