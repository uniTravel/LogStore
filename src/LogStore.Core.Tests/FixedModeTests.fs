module FixedMode

open System.IO
open Expecto
open LogStore.Core
open LogStore.Data

let area = @"D:\UC\LogStore\TestCase\FixedMode"
let folder = "Log"
let suffix = "log"
let length = 6
let chunkSize = int64 <| 8 * 1024 * 1024
let maxCacheSize = 10
let readerCount = 9
let writer = ChunkWriter.fixedAppend 8
let reader = ChunkReader.fixedRead 8
let seek = ChunkSeek.fixedSeek 8
let path = Path.Combine (area, folder)

let private config = ChunkConfig (area, folder, suffix, length, chunkSize, maxCacheSize, readerCount, writer, reader, seek)
if Directory.Exists path then
    Directory.EnumerateFiles path |> Seq.iter (fun file ->
        File.SetAttributes (file, FileAttributes.NotContentIndexed)
        File.Delete file
    )
else Directory.CreateDirectory path |> ignore
ChunkDB.init config

[<Tests>]
let tests =
    testSequenced <| testList "定长Chunk管理" [
        let withArgs f () =
            use manager = new ChunkManager (config)
            go "定长Chunk管理" |> f manager
        yield! testFixture withArgs [
            "写入第一个数据：1L。", fun manager finish ->
                let pos = manager.Append <| fun bw -> bw.Write 1L
                Expect.equal pos 12L "写入位置不正确"
                let read = manager.Read 0L <| fun br ->
                    let data = br.ReadInt64 ()
                    Expect.equal data 1L "读取的数据不对"
                Async.RunSynchronously read
                finish 1
            "写入第二个数据：2L。", fun manager finish ->
                let pos = manager.Append <| fun bw -> bw.Write 2L
                Expect.equal pos 24L "写入位置不正确"
                let read = manager.Read 12L <| fun br ->
                    let data = br.ReadInt64 ()
                    Expect.equal data 2L "读取的数据不对"
                Async.RunSynchronously read
                finish 2
            "写入第三个数据：3L。", fun manager finish ->
                let pos = manager.Append <| fun bw -> bw.Write 3L
                Expect.equal pos 36L "写入位置不正确"
                let read = manager.Read 24L <| fun br ->
                    let data = br.ReadInt64 ()
                    Expect.equal data 3L "读取的数据不对"
                Async.RunSynchronously read
                finish 3
            "读取数据，但读取位置超过最后写入位置。", fun manager finish ->
                let read = manager.Read 44L <| fun br -> br.ReadInt64 () |> ignore
                let f = fun _ -> Async.RunSynchronously read
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 4
            "读取第三个数据，但读取位置不对。", fun manager finish ->
                let read = manager.Read 26L <| fun br -> br.ReadInt64 () |> ignore
                let f = fun _ -> Async.RunSynchronously read
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 5
        ]
    ]
    |> testLabel "LogStore.Core"