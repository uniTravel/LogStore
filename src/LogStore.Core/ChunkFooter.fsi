namespace LogStore.Core

open System.Security.Cryptography

type internal ChunkFooter

[<RequireQualifiedAccess>]
module internal ChunkFooter =
    val size : int64
    val private checksumSize : int64
    val Default : ChunkFooter
    val complete : int64 -> MD5 -> ChunkFooter
    val fromStream : OriginStream -> ChunkFooter
    val accHash : MD5 -> OriginStream -> unit

[<Sealed>]
type internal ChunkFooter with
    member IsCompleted : bool
    member DataSize : int64
    member MD5Hash : byte array
    member AsByteArray : byte array