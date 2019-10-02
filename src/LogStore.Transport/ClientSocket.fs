namespace LogStore.Transport

open System

[<Sealed>]
type ClientSocket (config: ClientConfig) =

    let client = Client.connect config.BufferSize config.HostEndPoint

    interface IDisposable with
        member __.Dispose () =
            client.Disconnect ()

    member __.Send (data: byte[]) : byte[] =
        client.Send data config

    member __.SendAsync (data: byte[]) : Async<byte[]> =
        client.SendAsync data config