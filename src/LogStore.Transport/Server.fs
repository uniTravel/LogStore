namespace LogStore.Transport

open System
open System.IO
open System.Net.Sockets
open System.Threading

type State = {
    Id: Guid
    Socket: Socket
}

type ServerHandler = {
    Writer: BinaryWriter
    Reader: BinaryReader
    SockerCancellation: CancellationTokenSource
}

type Accepted = State * ServerHandler

type Empty = NoItems

type Standby = {
    Listener: TcpListener
}

type ServerArea = {
    Listener: TcpListener
    ServerCancellation: CancellationTokenSource
    SocketMap: Ref<Map<Guid, Accepted>>
}

type Server =
    | Initial of Empty
    | Standby of Standby
    | Active of ServerArea

module Server =

    let processReceive (_, handler: ServerHandler) (cfg: ServerConfig) = async {
        let { Writer = bw; Reader = br } = handler
        while true do cfg.Handler cfg.DataHandler br bw
    }

    let acceptAgent (server: ServerArea) (cfg: ServerConfig) =
        MailboxProcessor<Socket>.Start <| fun inbox ->
            let rec loop () = async {
                let! socket = inbox.Receive ()
                let id = Guid.NewGuid ()
                let state = { Id = id; Socket = socket }
                let netStream = new NetworkStream (socket, true)
                let stream = new BufferedStream (netStream, cfg.BufferSize)
                let bw = new BinaryWriter (stream)
                let br = new BinaryReader (stream)
                let socketCancellation = new CancellationTokenSource ()
                let handler = { Writer = bw; Reader = br; SockerCancellation = socketCancellation }
                let accepted = (state, handler)
                server.SocketMap := (!server.SocketMap).Add (id, accepted)
                Async.Start (processReceive accepted cfg, socketCancellation.Token)
                return! loop ()
            }
            loop ()

    let processAccept (server: ServerArea)  (cfg: ServerConfig) = async {
        while true do
            match server.Listener.Pending () with
            | false -> do! Async.Sleep 10
            | true ->
                let socket = server.Listener.AcceptSocket ()
                (acceptAgent server cfg).Post socket
    }

    let closeAccepted (_, handler: ServerHandler) : unit =
        try
            handler.SockerCancellation.Cancel ()
            handler.Writer.Close ()
            handler.Reader.Close ()
        with _ -> ()

    let initServer (cfg: ServerConfig) : Server =
        let listener = TcpListener cfg.HostEndPoint
        Standby { Listener = listener }

    let startServer (standby: Standby) (cfg: ServerConfig) (serverCancellation: CancellationTokenSource) : Server =
        let server = { Listener = standby.Listener; ServerCancellation = serverCancellation; SocketMap = ref Map.empty }
        server.Listener.Start cfg.Backlog
        Async.Start (processAccept server cfg, serverCancellation.Token)
        Active server

    let serverState (server: ServerArea) () : State list =
        !server.SocketMap |> Map.toList |> List.unzip |> snd |> List.unzip |> fst

    let disconnect (server: ServerArea) (id: Guid) : Server =
        match (!server.SocketMap).TryFind id with
        | Some accepted ->
            closeAccepted accepted
            server.SocketMap := (!server.SocketMap).Remove id
            Active server
        | None -> failwithf "未找到ID为%A的Socket" id

    let stopServer (server: ServerArea) () : Server =
        let { Listener = listener; SocketMap = sockets; ServerCancellation = serverCancellation } = server
        serverCancellation.Cancel ()
        !sockets |> Map.iter (fun _ accepted -> closeAccepted accepted)
        listener.Stop ()
        Standby { Listener = listener }

    //#region 根据状态控制

    type Empty with
        member __.InitServer = initServer

    type Standby with
        member this.StartServer = startServer this

    type ServerArea with
        member this.ServerState = serverState this
        member this.Disconnect = disconnect this
        member this.StopServer = stopServer this

    let init (server: Server) =
        match server with
        | Initial state -> state.InitServer
        | state -> failwithf "状态为%A，只有初始状态才能执行初始化操作。" state

    let start (server: Server) =
        match server with
        | Standby state -> state.StartServer
        | state -> failwithf "状态为%A，只有Standby状态才能启动Socket服务器。" state

    let getState (server: Server) =
        match server with
        | Active state -> state.ServerState
        | state -> failwithf "状态为%A，只有Active状态才能获取服务器状态。" state

    let closeSocket (server: Server) =
        match server with
        | Active state -> state.Disconnect
        | state -> failwithf "状态为%A，只有Active状态才能手工关闭Socket连接。" state

    let stop (server: Server) =
        match server with
        | Active state -> state.StopServer
        | state -> failwithf "状态为%A，只有Active状态才能停止Socket服务器。" state

    //#endregion

    let newServer = Initial NoItems

type Server with
    member this.Init = Server.init this
    member this.Start = Server.start this
    member this.GetState = Server.getState this
    member this.CloseSocket = Server.closeSocket this
    member this.Stop = Server.stop this