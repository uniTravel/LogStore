namespace LogStore.Transport

open System
open System.Threading
open System.Net.Sockets

type SocketCommand =
    | Init of AsyncReplyChannel<string option>
    | Listen of TcpListener * AsyncReplyChannel<string option>
    | Accept of NetworkStream
    | Resize of int
    | GetState of AsyncReplyChannel<State>
    | Stop of AsyncReplyChannel<string option>

[<Sealed>]
type ServerSocket (config: ServerConfig) =

    let mutable canStart = true

    let listenCancellation = new CancellationTokenSource ()

    let agentCancellation = new CancellationTokenSource ()

    let agent =
        MailboxProcessor<SocketCommand>.Start (fun inbox ->
            let rec loop (server: Server) = async {
                match! inbox.Receive () with
                | Init channel ->
                    try
                        let newServer = server.Init config
                        channel.Reply None
                        return! loop newServer
                    with ex ->
                        channel.Reply <| Some ex.Message
                        return! loop server
                | Listen (listener, channel) ->
                    try
                        let newServer = server.Listen listener
                        channel.Reply None
                        return! loop newServer
                    with ex ->
                        channel.Reply <| Some ex.Message
                        return! loop server
                | Accept netStream ->
                    let newServer = server.Accept netStream config
                    return! loop newServer
                | Resize size ->
                    let newServer = server.Resize size
                    return! loop newServer
                | GetState channel ->
                    channel.Reply <| server.GetState ()
                    return! loop server
                | Stop channel ->
                    try
                        let newServer = server.Stop ()
                        channel.Reply None
                        return! loop newServer
                    with ex ->
                        channel.Reply <| Some ex.Message
                        return! loop server
            }
            loop Server.newServer
        , agentCancellation.Token)

    let startServer = async {
        let listener = TcpListener config.HostEndPoint
        match agent.PostAndReply <| fun channel -> Listen (listener, channel) with
        | Some ex -> invalidOp ex
        | None ->
            while true do
                match listener.Pending () with
                | false -> do! Async.Sleep 10
                | true ->
                    let socket = listener.AcceptSocket ()
                    socket.ReceiveTimeout <- config.ReceiveTimeout
                    let netStream = new NetworkStream (socket, true)
                    agent.Post <| Accept netStream
    }

    let close () =
        if canStart then invalidOp "未启动，无从停止。"
        else
            listenCancellation.Cancel ()
            match agent.PostAndReply Stop with
            | Some ex -> invalidOp ex
            | None -> ()

    interface IDisposable with
        member __.Dispose () =
            close ()
            agentCancellation.Cancel ()

    member __.Init () : unit =
        match agent.PostAndReply Init with
        | Some ex -> invalidOp ex
        | None -> ()

    member __.Start () : unit =
        if canStart then
            Async.Start (startServer, listenCancellation.Token)
            canStart <- false
        else invalidOp "已启动，无法重复启动。"

    member __.Resize (size: int) : unit =
        agent.Post <| Resize size

    member __.GetState () : State =
        agent.PostAndReply GetState

    member __.Stop () : unit =
        close ()