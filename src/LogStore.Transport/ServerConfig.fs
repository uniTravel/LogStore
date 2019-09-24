namespace LogStore.Transport

open System.IO
open System.Net

[<Sealed>]
type ServerConfig (maxConnections, bufferSize, backlog, hostEndPoint, timeout, writeTo) =
    member __.MaxConnections : int = maxConnections
    member __.BufferSize : int = bufferSize
    member __.Backlog : int = backlog
    member __.HostEndPoint : IPEndPoint = hostEndPoint
    member __.ReceiveTimeout : int = timeout
    member __.WriteTo : byte[] -> unit = writeTo