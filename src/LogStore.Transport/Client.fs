namespace LogStore.Transport

open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open LogStore.Common.Utils

type ClientToken = {
    Socket: Socket
    Stream: MemoryStream
    Writer: BinaryWriter
    Reader: BinaryReader
}

type ClientArea = {
    Agent: PoolAgent<SocketAsyncEventArgs>
    Sender: Socket
}

type Client = Active of ClientArea

module Client =

    let rec processReceive (e: SocketAsyncEventArgs) =
        if e.BytesTransferred > 0 && e.SocketError = SocketError.Success then
            let token = e.UserToken :?> Socket
            printfn "客户端接收数据，长度%d" e.BytesTransferred
            let willRaiseEvent = token.ReceiveAsync e
            if not willRaiseEvent then processReceive e

    let processSend (e: SocketAsyncEventArgs) =
        if e.SocketError <> SocketError.Success then
            failwith "Not Implemented"

    let ioCompleted (e: SocketAsyncEventArgs) =
        match e.LastOperation with
        | SocketAsyncOperation.Receive -> processReceive e
        | SocketAsyncOperation.Send -> processSend e
        | op -> failwithf "%A 不是接收或者发送的操作。" op

    let connect (maxConnections: int) (bufferSize: int) (hostEndPoint: IPEndPoint) : Client =
        let bufferManager = BufferManager (bufferSize * 2, bufferSize)
        let autoConnectEvent = new AutoResetEvent false
        let sender = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        let res =
            [ 1 .. maxConnections ]
            |> List.map (fun _ ->
                let sendEventArg = new SocketAsyncEventArgs ()
                let socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                let stream = new MemoryStream 8192
                let bw = new BinaryWriter (stream)
                let br = new BinaryReader (stream)
                sendEventArg.UserToken <- { Socket = socket; Stream = stream; Writer = bw; Reader = br }
                sendEventArg.Completed.Add ioCompleted
                sendEventArg.RemoteEndPoint <- hostEndPoint
                sendEventArg
            )
        let connectEventArg = new SocketAsyncEventArgs ()
        connectEventArg.UserToken <- sender
        connectEventArg.RemoteEndPoint <- hostEndPoint
        connectEventArg.Completed.Add (fun e ->
            autoConnectEvent.Set () |> ignore
            if e.SocketError = SocketError.Success then
                bufferManager.InitBuffer ()
                let receiveEventArg = new SocketAsyncEventArgs ()
                receiveEventArg.Completed.Add ioCompleted
                receiveEventArg.UserToken <- connectEventArg.UserToken
                bufferManager.SetBuffer receiveEventArg |> ignore
                let willRaiseEvent = connectEventArg.ConnectSocket.ReceiveAsync receiveEventArg
                if not willRaiseEvent then processReceive receiveEventArg
        )
        sender.ConnectAsync connectEventArg |> ignore
        autoConnectEvent.WaitOne () |> ignore
        Active { Agent = new PoolAgent<SocketAsyncEventArgs> (res, 3.0); Sender = sender }

    //#region 根据状态控制

    let disconnect (Active client) () : unit=
        failwith ""

    let send (Active client) (writeTo: BinaryWriter -> unit) (cfg: ClientConfig) : unit =
        let { Agent = agent; Sender = sender } = client
        agent.Action <| fun sendEventArg ->
            let token = sendEventArg.UserToken :?> ClientToken
            let token = {token with Socket = sender }
            token.Stream.SetLength 0L
            sendEventArg.UserToken <- token
            cfg.Write writeTo token.Writer
            token.Stream.Position <- 0L
            let data = token.Reader.ReadBytes <| int token.Stream.Length
            sendEventArg.SetBuffer (data, 0, data.Length)
            printfn "客户端发送数据，长度%d" data.Length
            sender.SendAsync sendEventArg |> ignore

    //#endregion

type Client with
    member this.Disconnect = Client.disconnect this
    member this.Send = Client.send this