namespace LogStore.Transport

open System.IO
open System.Net
open System.Net.Sockets
open LogStore.Common.Utils

type ClientToken = {
    Stream: BufferedStream
    Writer: BinaryWriter
    Reader: BinaryReader
}

type Client = Active of PoolAgent<ClientToken>

module Client =

    let processSend (cfg: ClientConfig) (data: byte[]) (token: ClientToken) =
        try
            cfg.Write data token.Writer
            token.Stream.Flush ()
        with _ -> reraise ()

    let processReceive (cfg: ClientConfig) (token: ClientToken) =
        ()

    let connect (maxConnections: int) (bufferSize: int) (hostEndPoint: IPEndPoint) : Client =
        let client = new TcpClient ()
        client.Connect hostEndPoint
        let netStream = client.GetStream ()
        let res =
            [ 1 .. maxConnections ]
            |> List.map (fun _ ->
                let stream = new BufferedStream (netStream, bufferSize)
                let bw = new BinaryWriter (stream)
                let br = new BinaryReader (stream)
                { Stream = stream; Writer = bw; Reader = br }
            )
        Active <| new PoolAgent<ClientToken> (res, 3.0)

    //#region 根据状态控制

    let disconnect (Active agent) () : unit=
        agent.Close <| Some (fun token -> token.Stream.Close ())

    let send (Active agent) (data: byte[]) (cfg: ClientConfig) : unit =
        agent.Action <| fun token ->
            if token.Stream.CanWrite then processSend cfg data token
            if token.Stream.CanRead then processReceive cfg token

    let sendAsync (Active agent) (data: byte[]) (cfg: ClientConfig) : Async<unit> =
        agent.AsyncAction <| fun token ->
            if token.Stream.CanWrite then processSend cfg data token
            if token.Stream.CanRead then processReceive cfg token

    //#endregion

type Client with
    member this.Disconnect = Client.disconnect this
    member this.Send = Client.send this
    member this.SendAsync = Client.sendAsync this