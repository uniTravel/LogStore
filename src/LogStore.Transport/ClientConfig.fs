namespace LogStore.Transport

open System.IO
open System.Net

[<Sealed>]
type ClientConfig (maxConnections, bufferSize, hostEndPoint, write) =
    member __.MaxConnections : int = maxConnections
    member __.BufferSize : int = bufferSize
    member __.HostEndPoint : IPEndPoint = hostEndPoint
    member __.Write : byte[] -> BinaryWriter -> unit = write