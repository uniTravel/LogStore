namespace LogStore.Transport

open System.IO
open System.Net.Sockets
open System.Threading
open LogStore.Common.Utils

type ServerToken = {
    Socket: Socket
    Data: Ref<byte array>
}

type Empty = NoItems

type Standby = {
    Agent: PoolAgent<SocketAsyncEventArgs>
    Accepted: Semaphore
}

type ServerArea = {
    Agent: PoolAgent<SocketAsyncEventArgs>
    Accepted: Semaphore
    Listener: Socket
}

type Server =
    | Initial of Empty
    | Standby of Standby
    | Active of ServerArea

module Server =

    let closeClientSocket (e: SocketAsyncEventArgs) (accepted: Semaphore) =
        let token = e.UserToken :?> ServerToken
        try token.Socket.Shutdown SocketShutdown.Send with _ -> ()
        token.Socket.Close ()
        accepted.Release () |> ignore

    let processSend (e: SocketAsyncEventArgs) =
        match e.SocketError with
        | SocketError.Success -> ()
        | err -> printfn "服务端发送数据错误：%A" err

    let rec processReceive (e: SocketAsyncEventArgs) (accepted: Semaphore) =
        if e.BytesTransferred > 0 && e.SocketError = SocketError.Success then
            let { Socket = socket; Data = data } = e.UserToken :?> ServerToken
            data := Array.append !data e.Buffer.[0 .. e.BytesTransferred - 1]
            let willRaiseEvent = socket.ReceiveAsync e
            if not willRaiseEvent then processReceive e accepted
            else
                printfn "服务端接收数据，长度%d" (!data).Length
                closeClientSocket e accepted

    let initServer (cfg: ServerConfig) : Server =
        let receiveBuffer = BufferManager (cfg.BufferSize * cfg.MaxConnections, cfg.BufferSize)
        let accepted = new Semaphore (cfg.MaxConnections, cfg.MaxConnections)
        let res =
            [ 1 .. cfg.MaxConnections ]
            |> List.map (fun _ ->
                let readEventArg = new SocketAsyncEventArgs ()
                readEventArg.Completed.Add (fun e ->
                    match e.LastOperation with
                    | SocketAsyncOperation.Receive -> processReceive e accepted
                    | SocketAsyncOperation.Send -> processSend e
                    | op -> failwithf "%A 不是接收或者发送操作。" op
                )
                receiveBuffer.SetBuffer readEventArg |> ignore
                readEventArg
            )
        Standby { Agent = new PoolAgent<SocketAsyncEventArgs> (res, 3.0); Accepted = accepted }

    let rec startServer (standby: Standby) (cfg: ServerConfig) : Server =
        let listener = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        listener.Bind cfg.HostEndPoint
        listener.Listen cfg.Backlog
        let acceptEventArg = new SocketAsyncEventArgs ()
        let area = { Agent = standby.Agent; Accepted = standby.Accepted; Listener = listener }
        acceptEventArg.Completed.Add <| processAccept area cfg
        startAccept area cfg acceptEventArg
    and startAccept (server: ServerArea) (cfg: ServerConfig) (acceptEventArg: SocketAsyncEventArgs) =
        let { Accepted = accepted; Listener = listener } = server
        acceptEventArg.AcceptSocket <- null
        accepted.WaitOne () |> ignore
        let willRaiseEvent = listener.AcceptAsync acceptEventArg
        if not willRaiseEvent then processAccept server cfg acceptEventArg
        Active server
    and processAccept (server: ServerArea) (cfg: ServerConfig) (acceptEventArg: SocketAsyncEventArgs) =
        server.Agent.Action <| fun readEventArg ->
            readEventArg.UserToken <- { Socket = acceptEventArg.AcceptSocket; Data = ref Array.empty }
            let willRaiseEvent = acceptEventArg.AcceptSocket.ReceiveAsync readEventArg
            if not willRaiseEvent then processReceive readEventArg server.Accepted
        startAccept server cfg acceptEventArg |> ignore

    let stopServer (server: ServerArea) () : unit =
        let { Agent = agent; Listener = listener } = server
        agent.Close <| Some (fun e ->
            if not (isNull e.AcceptSocket) then
                e.AcceptSocket.Shutdown SocketShutdown.Both
                e.AcceptSocket.Close ()
        )
        try listener.Shutdown SocketShutdown.Both
        with _ -> ()
        listener.Close ()

    //#region 根据状态控制

    type Empty with
        member __.InitServer = initServer

    type Standby with
        member this.StartServer = startServer this

    type ServerArea with
        member this.StopServer = stopServer this

    let init (server: Server) =
        match server with
        | Initial state -> state.InitServer
        | state -> failwithf "状态为%A，只有初始状态才能执行初始化操作。" state

    let start (server: Server) =
        match server with
        | Standby state -> state.StartServer
        | state -> failwithf "状态为%A，只有Standby状态才能启动Socket服务器。" state

    let stop (server: Server) =
        match server with
        | Active state -> state.StopServer
        | state -> failwithf "状态为%A，只有Active状态才能停止Socket服务器。" state

    //#endregion

    let newServer = Initial NoItems

type Server with
    member this.Init = Server.init this
    member this.Start = Server.start this
    member this.Stop = Server.stop this