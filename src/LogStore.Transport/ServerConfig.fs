namespace LogStore.Transport

open System.IO
open System.Net

[<Sealed>]
type ServerConfig (connections, bufferSize, backlog, hostEndPoint, timeout, writeTo) =
    member __.Connections : int = connections
    member __.BufferSize : int = bufferSize
    member __.Backlog : int = backlog
    member __.HostEndPoint : IPEndPoint = hostEndPoint
    member __.ReceiveTimeout : int = timeout
    member __.WriteTo : byte[] -> unit = writeTo