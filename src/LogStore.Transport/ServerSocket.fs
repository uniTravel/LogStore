namespace LogStore.Transport

open System
open System.Threading

type ServerCommand =
    | Init of AsyncReplyChannel<string option>
    | Start of AsyncReplyChannel<string option>
    | GetState of AsyncReplyChannel<State list>
    | CloseSocket of Guid * AsyncReplyChannel<string option>
    | Stop of AsyncReplyChannel<string option>

[<Sealed>]
type ServerSocket (config: ServerConfig) =

    let agentCancellation = new CancellationTokenSource ()

    let agent =
        MailboxProcessor<ServerCommand>.Start (fun inbox ->
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
                | Start channel ->
                    try
                        let newServer = server.Start config <| new CancellationTokenSource ()
                        channel.Reply None
                        return! loop newServer
                    with ex ->
                        channel.Reply <| Some ex.Message
                        return! loop server
                | GetState channel ->
                    channel.Reply <| server.GetState ()
                    return! loop server
                | CloseSocket (id, channel) ->
                    try
                        let newServer = server.CloseSocket id
                        channel.Reply None
                        return! loop newServer
                    with ex ->
                        channel.Reply <| Some ex.Message
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

    interface IDisposable with
        member __.Dispose () =
            match agent.PostAndReply Stop with
            | Some ex -> invalidOp ex
            | None -> ()
            agentCancellation.Cancel ()

    member __.Init () : unit =
        match agent.PostAndReply Init with
        | Some ex -> invalidOp ex
        | None -> ()

    member __.Start () : unit =
        match agent.PostAndReply Start with
        | Some ex -> invalidOp ex
        | None -> ()

    member __.GetState () : State list =
        agent.PostAndReply GetState

    member __.CloseSocket (id: Guid) : unit =
        match config.Timeout with
        | Some timeout -> failwithf "配置了%d毫秒超时，不能手工关闭连接" timeout
        | None ->
            match agent.PostAndReply <| fun channel -> CloseSocket (id, channel) with
            | Some ex -> invalidOp ex
            | None -> ()

    member __.Stop () : unit =
        match agent.PostAndReply Stop with
        | Some ex -> invalidOp ex
        | None -> ()