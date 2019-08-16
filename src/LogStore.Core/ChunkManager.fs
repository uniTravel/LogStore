namespace LogStore.Core

open System
open System.IO

type WriteCommand =
    | Write of (BinaryWriter -> unit) * AsyncReplyChannel<Result<int64 option, exn>>
    | SwitchWriter
    | StopWrite

type ReadCommand =
    | Read of int64 * AsyncReplyChannel<Result<Reader, exn>>
    | SwitchReaders
    | StopRead

type ScavengeCommand =
    | Reader of Reader
    | StopScavenge

[<Sealed>]
type ChunkManager (config: ChunkConfig) =

    let (pos, db) = ChunkDB.openDB config

    let mutable db = db

    let scavengeAgent =
        MailboxProcessor<ScavengeCommand>.Start <| fun inbox ->
            let rec loop () = async {
                match! inbox.Receive () with
                | Reader reader ->
                    Chunk.closeReader reader
                | StopScavenge -> return ()
                return! loop ()
            }
            loop ()

    let writeAgent =
        MailboxProcessor<WriteCommand>.Start <| fun inbox ->
            let rec loop (oldPos:int64, oldWriter: Writer) = async {
                match! inbox.Receive () with
                | Write (writeTo, channel) ->
                    try
                        let newPos = Chunk.append writeTo oldPos oldWriter
                        match newPos with
                        | pos when pos > oldPos -> channel.Reply <| Ok (Some pos)
                        | _ -> channel.Reply <| Ok None
                        return! loop (newPos, oldWriter)
                    with ex ->
                        channel.Reply <| Error ex
                        return! loop (oldPos, oldWriter)
                | SwitchWriter ->
                    let beginPos = int64 db.ChunkNumber * config.ChunkSize
                    return! loop (beginPos, db.Writer)
                | StopWrite -> return ()
            }
            loop (pos, db.Writer)

    let readAgent =
        MailboxProcessor<ReadCommand>.Start <| fun inbox ->
            let rec loop (currentIdx: int, readers: (int * Reader) array) = async {
                match! inbox.Receive () with
                | Read (globalPos, channel) ->
                    let size = readers.Length
                    let chunkNum = int <| globalPos / config.ChunkSize
                    if chunkNum > currentIdx || chunkNum < currentIdx - size then
                        channel.Reply <| Error (failwithf "位置越界，当前ChunkNumber为%d，最大缓存Chunk数量为%d。" currentIdx size)
                        return! loop (currentIdx, readers)
                    let idx = chunkNum % config.CacheSize
                    match readers.[idx] with
                    | (num, reader) when chunkNum = num -> channel.Reply <| Ok reader
                    | _ -> channel.Reply <| Error (failwithf "ChunkNumber%d无法取到Reader。" chunkNum)
                    return! loop (currentIdx, readers)
                | SwitchReaders -> return! loop db.Readers
                | StopRead -> return ()
            }
            loop db.Readers

    let rec append writeTo =
        match writeAgent.PostAndReply <| fun channel -> Write (writeTo, channel) with
        | Ok reply ->
            match reply with
            | Some pos -> pos
            | None ->
                let (oldReader, newDB) = db.Complete config
                db <- newDB
                writeAgent.Post SwitchWriter
                readAgent.Post SwitchReaders
                scavengeAgent.Post <| Reader oldReader
                append writeTo
        | Error ex -> raise ex

    interface IDisposable with
        member __.Dispose () =
            writeAgent.Post StopWrite
            readAgent.Post StopRead
            scavengeAgent.Post StopScavenge
            db.Close ()

    member __.Append writeTo = append writeTo

    member __.Read readFrom globalPos =
        async {
            match! readAgent.PostAndAsyncReply <| fun channel -> Read (globalPos, channel) with
            | Ok reader -> return! Chunk.read readFrom reader globalPos
            | Error ex -> return raise ex
        }