namespace LogStore.Core

type LogMode =
    | Free
    | Fixed of int

[<Sealed>]
type ChunkConfig (path, prefix, length, chunkSize, cacheSize, readerCount, logMode) =
    member __.Path : string = path
    member __.Prefix : string = prefix
    member __.Length : int = length
    member __.ChunkSize : int64 = chunkSize
    member __.CacheSize : int = cacheSize
    member __.ReaderCount : int = readerCount
    member __.LogMode : LogMode = logMode