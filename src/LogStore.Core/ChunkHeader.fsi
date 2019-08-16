namespace LogStore.Core

open System

type internal ChunkHeader

[<RequireQualifiedAccess>]
module internal ChunkHeader =
    val size : int64
    val create : int -> int64 -> ChunkHeader
    val fromStream : OriginStream -> ChunkHeader

[<Sealed>]
type internal ChunkHeader with
    member ChunkNumber : int
    member ChunkSize : int64
    member ChunkId : Guid
    member AsByteArray : byte array