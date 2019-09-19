namespace LogStore.Transport

open System.Collections.Generic
open System.Net.Sockets

[<Sealed>]
type BufferManager (totalBytes: int, bufferSize: int) =

    let buffer = lazy ( Array.zeroCreate<byte> totalBytes )

    let mutable currentIndex = 0

    let freeIndexPool = Stack<int> ()

    member __.InitBuffer () : unit =
        buffer.Force () |> ignore

    member __.SetBuffer (args: SocketAsyncEventArgs) : bool =
        if freeIndexPool.Count > 0 then
            args.SetBuffer (buffer.Force (), (freeIndexPool.Pop ()), bufferSize)
            true
        elif totalBytes - bufferSize < currentIndex then
            false
        else
            args.SetBuffer (buffer.Force (), currentIndex, bufferSize)
            currentIndex <- currentIndex + bufferSize
            true

    member __.FreeBuffer (args: SocketAsyncEventArgs) : unit =
        freeIndexPool.Push args.Offset
        args.SetBuffer (null, 0, 0)