namespace LogStore.Data

open System.IO

module SocketTransport =

    let defaultServerHandler (dataHandler: byte[] -> byte[]) (br: BinaryReader) (bw: BinaryWriter) : unit =
        let rec read () =
            let length = br.ReadInt32 ()
            let data = br.ReadBytes length
            let result = dataHandler data
            bw.Write result.Length
            bw.Write result
            read ()
        read ()

    let defaultClientSender (data: byte[]) (bw: BinaryWriter) : unit =
        bw.Write data.Length
        bw.Write data

    let defaultClientReceiver (br: BinaryReader) : byte[] =
        let length = br.ReadInt32 ()
        let data = br.ReadBytes length
        data