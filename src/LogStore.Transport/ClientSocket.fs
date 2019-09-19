namespace LogStore.Transport

open System
open System.IO

[<Sealed>]
type ClientSocket (config: ClientConfig) =

    let client = Client.connect config.MaxConnections config.BufferSize config.HostEndPoint

    interface IDisposable with
        member __.Dispose () =
            failwith ""

    member __.Send (writeTo: BinaryWriter -> unit) : unit =
        client.Send writeTo config