namespace LogStore.Transport

open System
open System.IO

[<Sealed>]
type ClientSocket (config: ClientConfig) =

    let client = Client.connect config.MaxConnections config.BufferSize config.HostEndPoint

    interface IDisposable with
        member __.Dispose () =
            client.Disconnect ()

    member __.Send (data: byte[]) : unit =
        client.Send data config

    member __.SendAsync (data: byte[]) : Async<unit> =
        client.SendAsync data config