namespace LogStore.Data

open System.IO
open Logary
open Logary.Message

module SocketTransport =

    let lg = Log.create "LogStore.Data.SocketServer"

    let defaultServerHandler (dataHandler: byte[] -> byte[]) (br: BinaryReader) (bw: BinaryWriter) : unit =
        lg.logSimple <| eventVerbose "开始接收数据。"
        let rec read () =
            let length = br.ReadInt32 ()
            let data = br.ReadBytes length
            lg.logSimple <| eventVerbosef "收到数据%d字节。" data.Length
            let result = dataHandler data
            bw.Write result.Length
            bw.Write result
            read ()
        read ()

    let defaultClientSender (data: byte[]) (bw: BinaryWriter) : unit =
        bw.Write data.Length
        bw.Write data
        lg.logSimple <| eventVerbosef "发送数据%d字节" data.Length

    let defaultClientReceiver (br: BinaryReader) : byte[] =
        let length = br.ReadInt32 ()
        let data = br.ReadBytes length
        lg.logSimple <| eventVerbosef "收到反馈%d" data.Length
        data