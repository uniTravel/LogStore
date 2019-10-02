namespace LogStore.Transport

open System.IO
open System.Net

[<Sealed>]
type ClientConfig (bufferSize, hostEndPoint, sender, receiver) =
    member __.BufferSize : int = bufferSize
    member __.HostEndPoint : IPEndPoint = hostEndPoint
    member __.Sender : byte[] -> BinaryWriter -> unit = sender
    member __.Receiver : BinaryReader -> byte[] = receiver