namespace LogStore.Transport

open System.IO
open System.Net
open System.Net.Sockets
open Logary
open Logary.Message

type ClientHandler = {
    Writer: BinaryWriter
    Reader: BinaryReader
}

type Client = Active of ClientHandler

module Client =

    let lg = Log.create "LogStore.Transport.Client"

    let processSend (cfg: ClientConfig) (data: byte[]) (handler: ClientHandler) =
        cfg.Sender data handler.Writer

    let processReceive (cfg: ClientConfig) (handler: ClientHandler) =
        cfg.Receiver handler.Reader

    let sending (cfg: ClientConfig) (data: byte[]) (handler: ClientHandler) : byte[] =
        processSend cfg data handler
        processReceive cfg handler

    let connect (bufferSize: int) (hostEndPoint: IPEndPoint) : Client =
        let client = new TcpClient ()
        client.Connect hostEndPoint
        lg.logSimple <| eventInfof "连接到Socket服务器%A。" hostEndPoint
        let netStream = client.GetStream ()
        let buffer = new BufferedStream (netStream, bufferSize)
        let bw = new BinaryWriter (buffer)
        let br = new BinaryReader (buffer)
        Active { Writer = bw; Reader = br }

    //#region 根据状态控制

    let disconnect (Active handler) () : unit=
        handler.Writer.Close ()
        handler.Reader.Close ()

    let send (Active handler) (data: byte[]) (cfg: ClientConfig) : byte[] =
        sending cfg data handler

    let sendAsync (Active handler) (data: byte[]) (cfg: ClientConfig) : Async<byte[]> = async {
        return sending cfg data handler
    }

    //#endregion

type Client with
    member this.Disconnect = Client.disconnect this
    member this.Send = Client.send this
    member this.SendAsync = Client.sendAsync this