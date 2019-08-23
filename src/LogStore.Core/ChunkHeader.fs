namespace LogStore.Core

open System
open System.IO

type ChunkHeader = {
    _chunkNumber: int
    _chunkSize: int64
    _chunkId: Guid
} with
    member this.ChunkNumber = this._chunkNumber
    member this.ChunkSize = this._chunkSize
    member this.ChunkId = this._chunkId

module ChunkHeader =

    let size = 128L

    let create index chunkSize =
        { _chunkNumber = index; _chunkSize = chunkSize; _chunkId = Guid.NewGuid () }

    let fromStream (OriginStream stream) =
        let reader = new BinaryReader(stream)
        stream.Seek (0L, SeekOrigin.Begin) |> ignore
        let chunkNumber = reader.ReadInt32 ()
        let chunkSize = reader.ReadInt64 ()
        let chunkId = Guid (reader.ReadBytes (16))
        { _chunkNumber = chunkNumber; _chunkSize = chunkSize; _chunkId = chunkId }

    let asByteArray (chunkHeader: ChunkHeader) =
        let array = Array.zeroCreate <| int size
        use memStream = new MemoryStream (array)
        use writer = new BinaryWriter (memStream)
        writer.Write (chunkHeader._chunkNumber)
        writer.Write (chunkHeader._chunkSize)
        writer.Write (chunkHeader._chunkId.ToByteArray ())
        array

type ChunkHeader with
    member this.AsByteArray = ChunkHeader.asByteArray this