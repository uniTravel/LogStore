namespace LogStore.Core

open System
open System.IO

type WriteCommand =
    | Write of (BinaryWriter -> unit) * AsyncReplyChannel<Result<bool, string>>
    | SwitchWriter
    | StopWrite

type ReadCommand =
    | Read of int64 * AsyncReplyChannel<Result<Reader, string>>
    | SwitchReaders
    | StopRead

type ScavengeCommand =
    | Reader of Reader
    | StopScavenge

[<Sealed>]
type ChunkManager (config: ChunkConfig) =

    let internalAppend =
        match config.LogMode with
        | Free -> Chunk.freeAppend
        | Fixed l -> Chunk.fixedAppend l

    let internalRead =
        match config.LogMode with
        | Free -> Chunk.freeRead
        | Fixed l -> Chunk.fixedRead l

    let (pos, db) = ChunkDB.openDB config

    let mutable db = db

    let mutable checkpoint = pos

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
            let rec loop (oldWriter: Writer) = async {
                match! inbox.Receive () with
                | Write (writeTo, channel) ->
                    try
                        let newPos = Chunk.append internalAppend writeTo checkpoint oldWriter
                        match newPos with
                        | pos when pos > checkpoint ->
                            checkpoint <- pos
                            channel.Reply <| Ok true
                        | _ -> channel.Reply <| Ok false
                        return! loop oldWriter
                    with ex ->
                        channel.Reply <| Error ex.Message
                        return! loop oldWriter
                | SwitchWriter ->
                    checkpoint <- int64 db.ChunkNumber * config.ChunkSize
                    return! loop db.Writer
                | StopWrite -> return ()
            }
            loop db.Writer

    let readAgent =
        MailboxProcessor<ReadCommand>.Start <| fun inbox ->
            let rec loop (readers: Reader array) = async {
                match! inbox.Receive () with
                | Read (globalPos, channel) ->
                    if globalPos > checkpoint then
                        channel.Reply <| Error (sprintf "读取位置越界，当前写入位置为%d。" checkpoint)
                        return! loop readers
                    let chunkNum = int <| globalPos / config.ChunkSize
                    channel.Reply <| Ok readers.[chunkNum]
                    return! loop readers
                | SwitchReaders -> return! loop db.Readers
                | StopRead -> return ()
            }
            loop db.Readers

    let rec append writeTo =
        match writeAgent.PostAndReply <| fun channel -> Write (writeTo, channel) with
        | Ok reply ->
            match reply with
            | true -> checkpoint
            | false ->
                let (unloadReader, oldReader, newDB) = db.Complete config
                db <- newDB
                writeAgent.Post SwitchWriter
                readAgent.Post SwitchReaders
                scavengeAgent.Post <| Reader oldReader
                match unloadReader with
                | Some reader -> scavengeAgent.Post <| Reader reader
                | None -> ()
                append writeTo
        | Error err -> invalidOp err

    interface IDisposable with
        member __.Dispose () =
            writeAgent.Post StopWrite
            readAgent.Post StopRead
            scavengeAgent.Post StopScavenge
            db.Close ()

    member __.Append writeTo = append writeTo

    member __.Read globalPos readFrom =
        async {
            match! readAgent.PostAndAsyncReply <| fun channel -> Read (globalPos, channel) with
            | Ok reader -> return! Chunk.read internalRead readFrom reader globalPos
            | Error err -> invalidOp err
        }