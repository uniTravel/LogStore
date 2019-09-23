namespace LogStore.Transport

open System
open System.IO
open System.Net
open System.Net.Sockets
open LogStore.Common.Utils

type internal State = {
    Connections: int
    AdjustedSize: int
    BufferSize: int
    Backlog: int
    HostEndPoint: IPEndPoint
    ReceiveTimeout: int
}

type Empty = NoItems

type Standby = {
    Agent: PoolAgent<byte[]>
    State: State
}

type ServerArea = {
    Agent: PoolAgent<byte[]>
    State: State
    Listener: TcpListener
    Streams: NetworkStream list
}

type Server =
    | Initial of Empty
    | Standby of Standby
    | Active of ServerArea

module Server =

    let rec processReceive (netStream: NetworkStream) (writeTo: byte[] -> unit) (buffer: byte[]) =
        try
            if netStream.DataAvailable then
                let q = netStream.Read (buffer, 0, buffer.Length)
                printfn "服务端接收数据，长度%d" q
                processReceive netStream writeTo buffer
            else
                printfn "服务端收到数据，长度%d" 1
            // processReceive netStream writeTo buffer
        with _ ->
            netStream.Close ()

    let initServer (cfg: ServerConfig) : Server =
        let res = List.replicate cfg.Connections <| Array.zeroCreate<byte> cfg.BufferSize
        let state = {
            Connections = cfg.Connections
            AdjustedSize = 0
            BufferSize = cfg.BufferSize
            Backlog = cfg.Backlog
            HostEndPoint = cfg.HostEndPoint
            ReceiveTimeout = cfg.ReceiveTimeout
        }
        Standby { Agent = new PoolAgent<byte[]> (res, 3.0); State = state }

    let startListen (standby: Standby) (listener: TcpListener) : Server =
        listener.Start standby.State.Backlog
        Active { Agent = standby.Agent; State = standby.State; Listener = listener; Streams = List.Empty }

    let startAccept (server: ServerArea) (netStream: NetworkStream) (cfg: ServerConfig) : Server =
        let { Agent = agent; Streams = streams } = server
        netStream :: streams |> ignore
        agent.AsyncAction <| processReceive netStream cfg.WriteTo |> Async.Start
        Active server

    let adjust (server: ServerArea) (size: int) : Server =
        match size with
        | s when s > 0 ->
            let res = List.replicate s <| Array.zeroCreate<byte> server.State.BufferSize
            server.Agent.IncreaseSize res |> ignore
        | 0 -> ()
        | s -> server.Agent.DecreaseSize -s |> ignore
        let state = { server.State with AdjustedSize = server.Agent.AdjustedSize }
        Active { server with State = state }

    let stopServer (server: ServerArea) () : Server =
        let { Agent = agent; State = state; Listener = listener; Streams = streams } = server
        if state.AdjustedSize > 0 then agent.DecreaseSize state.AdjustedSize |> ignore
        streams |> List.iter (fun ns -> ns.Close ())
        listener.Stop ()
        Standby { Agent = agent; State = { state with AdjustedSize = 0 } }

    //#region 根据状态控制

    type Empty with
        member __.InitServer = initServer

    type Standby with
        member this.StartListen = startListen this

    type ServerArea with
        member this.StartAccept = startAccept this
        member this.Adjust = adjust this
        member this.StopServer = stopServer this

    let init (server: Server) =
        match server with
        | Initial state -> state.InitServer
        | state -> failwithf "状态为%A，只有初始状态才能执行初始化操作。" state

    let listen (server: Server) =
        match server with
        | Standby state -> state.StartListen
        | state -> failwithf "状态为%A，只有Standby状态才能开始侦听Socket连接。" state

    let accept (server: Server) =
        match server with
        | Active state -> state.StartAccept
        | state -> failwithf "状态为%A，只有Active状态才能接受Socket连接。" state

    let resize (server: Server) =
        match server with
        | Active state -> state.Adjust
        | state -> failwithf "状态为%A，只有Active状态才能调整Socket服务器的吞吐限制。" state

    let getState (server: Server) () =
        match server with
        | Standby state -> state.State
        | Active state -> state.State
        | Initial _ -> failwith "初始状态无法获取Socket服务器状态。"

    let stop (server: Server) =
        match server with
        | Active state -> state.StopServer
        | state -> failwithf "状态为%A，只有Active状态才能停止Socket服务器。" state

    //#endregion

    let newServer = Initial NoItems

type Server with
    member this.Init = Server.init this
    member this.Listen = Server.listen this
    member this.Accept = Server.accept this
    member this.Resize = Server.resize this
    member this.GetState = Server.getState this
    member this.Stop = Server.stop this