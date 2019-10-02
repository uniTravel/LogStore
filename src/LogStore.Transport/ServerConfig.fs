namespace LogStore.Transport

open System.IO
open System.Net

[<Sealed>]
type ServerConfig (bufferSize, backlog, hostEndPoint, timeout, handler, dataHandler) =
    member __.BufferSize : int = bufferSize
    member __.Backlog : int = backlog
    member __.HostEndPoint : IPEndPoint = hostEndPoint
    member __.Timeout : int64 option = timeout
    member __.Handler : (byte[] -> byte[]) -> BinaryReader -> BinaryWriter -> unit = handler
    member __.DataHandler : byte[] -> byte[] = dataHandler