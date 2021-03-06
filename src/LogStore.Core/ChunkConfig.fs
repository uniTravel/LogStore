namespace LogStore.Core

open System.IO

[<Sealed>]
type ChunkConfig (path, folder, suffix, length, chunkSize, cacheSize, readerCount, writer, reader, seek) =
    member __.Path : string = path
    member __.Folder : string = folder
    member __.Suffix : string = suffix
    member __.Length : int = length
    member __.ChunkSize : int64 = chunkSize
    member __.CacheSize : int = cacheSize
    member __.ReaderCount : int = readerCount
    member __.Writer : ((BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64) = writer
    member __.Reader : ((BinaryReader -> unit) -> int64 -> BinaryReader -> Async<unit>) = reader
    member __.Seek : (BinaryReader -> int -> int) = seek