namespace LogStore.Transport

open System.IO
open System.Net

[<Sealed>]
type ServerConfig (maxConnections, bufferSize, backlog, hostEndPoint, retrieve, writeTo) =
    member __.MaxConnections : int = maxConnections
    member __.BufferSize : int = bufferSize
    member __.Backlog : int = backlog
    member __.HostEndPoint : IPEndPoint = hostEndPoint
    member __.Retrieve : BinaryReader -> ((BinaryWriter -> unit) -> int64) -> unit = retrieve
    member __.WriteTo : (BinaryWriter -> unit) -> int64 = writeTo