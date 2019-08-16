namespace LogStore.Core

open System.IO
open System.Security.Cryptography

type ChunkFooter = {
    _isCompleted: bool
    _dataSize: int64
    _md5Hash: byte array
} with
    member this.IsCompleted = this._isCompleted
    member this.DataSize = this._dataSize
    member this.MD5Hash =this._md5Hash


module ChunkFooter =

    let size = 128L
    let checksumSize = 16L
    let buffer = Array.zeroCreate <| int32 (size - checksumSize)

    let Default = { _isCompleted = false; _dataSize = -1L; _md5Hash = Array.zeroCreate <| int32 checksumSize }

    let fromStream (OriginStream stream) =
        let reader = new BinaryReader (stream)
        stream.Seek (-size, SeekOrigin.End) |> ignore
        let isCompleted = reader.ReadBoolean ()
        let dataSize = reader.ReadInt64 ()
        stream.Seek (-checksumSize, SeekOrigin.End) |> ignore
        let hash = reader.ReadBytes (int32 checksumSize)
        { _isCompleted = isCompleted; _dataSize = dataSize; _md5Hash = hash }

    let accHash (md5: MD5) (OriginStream stream) =
        stream.Seek (-size, SeekOrigin.End) |> ignore
        stream.Read (buffer, 0, buffer.Length) |> ignore
        md5.TransformBlock (buffer, 0, buffer.Length, null, 0) |> ignore

    let asByteArray (chunkFooter: ChunkFooter) =
        let array = Array.zeroCreate <| int32 size
        use memStream = new MemoryStream (array)
        use writer = new BinaryWriter (memStream)
        writer.Write (chunkFooter._isCompleted)
        writer.Write (chunkFooter._dataSize)
        memStream.Position <- size - checksumSize
        writer.Write (chunkFooter._md5Hash)
        array

    let complete dataSize (md5: MD5) =
        let footer = { _isCompleted = true; _dataSize = dataSize; _md5Hash = md5.Hash}
        md5.TransformFinalBlock(asByteArray footer, 0, int (size - checksumSize)) |> ignore
        { footer with _md5Hash = md5.Hash }

type ChunkFooter with
    member this.AsByteArray = ChunkFooter.asByteArray this