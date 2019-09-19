namespace LogStore.Transport

open System

type SocketCommand =
    | Init of AsyncReplyChannel<string option>
    | Start of AsyncReplyChannel<string option>
    | Stop of AsyncReplyChannel<string option>

[<Sealed>]
type ServerSocket (config: ServerConfig) =

    let agent =
        MailboxProcessor<SocketCommand>.Start <| fun inbox ->
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
                        let newServer = server.Start config
                        channel.Reply None
                        return! loop newServer
                    with ex ->
                        channel.Reply <| Some ex.Message
                        return! loop server
                | Stop channel ->
                    try
                        server.Stop ()
                        channel.Reply None
                        return ()
                    with ex ->
                        channel.Reply <| Some ex.Message
                        return! loop server
            }
            loop Server.newServer

    interface IDisposable with
        member __.Dispose () =
            match agent.PostAndReply Stop with
            | Some ex -> invalidOp ex
            | None -> ()

    member __.Init () : unit =
        match agent.PostAndReply Init with
        | Some ex -> invalidOp ex
        | None -> ()

    member __.Start () : unit =
        match agent.PostAndReply Start with
        | Some ex -> invalidOp ex
        | None -> ()