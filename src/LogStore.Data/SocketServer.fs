namespace LogStore.Data

open System.IO

module SocketServer =

    let retrieve (br: BinaryReader) (writeTo: (BinaryWriter -> unit) -> int64) : unit =
        let prefixLength = br.ReadInt32 ()
        let data = br.ReadBytes prefixLength
        let suffixLength = br.ReadInt32 ()
        if prefixLength <> suffixLength then failwithf "读取异常：前缀长度%d 不等于后缀长度%d。" prefixLength suffixLength
        writeTo <| fun bw -> bw.Write data
        |> ignore
        // let toRead = toRetrieve - br.BaseStream.Position
        // if toRead > 0L then retrieve br writeTo toRead
        printfn "服务端接收数据，长度%d" br.BaseStream.Position