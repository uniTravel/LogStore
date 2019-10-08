module FreeMode

open System.IO
open Expecto
open LogStore.Core
open LogStore.Data

let path = @"D:\UC\LogStore\\TestCase\FreeMode\Jour"
let prefix = "Jour"
let length = 6
let chunkSize = int64 <| 8 * 1024 * 1024
let maxCacheSize = 10
let readerCount = 9
let writer = ChunkWriter.freeAppend
let reader = ChunkReader.freeRead
let seek = ChunkSeek.freeSeek

let private config = ChunkConfig (path, prefix, length, chunkSize, maxCacheSize, readerCount, writer, reader, seek)
if Directory.Exists path then
    Directory.EnumerateFiles path |> Seq.iter (fun file ->
        File.SetAttributes (file, FileAttributes.NotContentIndexed)
        File.Delete file
    )
else Directory.CreateDirectory path |> ignore
ChunkDB.init config

[<Tests>]
let tests =
    testSequenced <| testList "自由Chunk管理" [
        let withArgs f () =
            use manager = new ChunkManager (config)
            go "自由Chunk管理" |> f manager
        yield! testFixture withArgs [
            "写入第一个数据：100。", fun manager finish ->
                let pos = manager.Append <| fun bw -> bw.Write 100
                Expect.equal pos 12L "写入位置不正确"
                let read = manager.Read 0L <| fun br ->
                    let data = br.ReadInt32 ()
                    Expect.equal data 100 "读取的数据不对"
                Async.RunSynchronously read
                finish 1
            "写入第二个数据：200L。", fun manager finish ->
                let pos = manager.Append <| fun bw -> bw.Write 200L
                Expect.equal pos 28L "写入位置不正确"
                let read = manager.Read 12L <| fun br ->
                    let data = br.ReadInt64 ()
                    Expect.equal data 200L "读取的数据不对"
                Async.RunSynchronously read
                finish 2
            "写入第三个数据：test。", fun manager finish ->
                let pos = manager.Append <| fun bw -> bw.Write "test"
                Expect.equal pos 41L "写入位置不正确"
                let read = manager.Read 28L <| fun br ->
                    let data = br.ReadString ()
                    Expect.equal data "test" "读取的数据不对"
                Async.RunSynchronously read
                finish 3
            "读取数据，但读取位置超过最后写入位置。", fun manager finish ->
                let read = manager.Read 44L <| fun br -> br.ReadString () |> ignore
                let f = fun _ -> Async.RunSynchronously read
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 4
            "读取第三个数据，但读取位置不对。", fun manager finish ->
                let read = manager.Read 26L <| fun br -> br.ReadString () |> ignore
                let f = fun _ -> Async.RunSynchronously read
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 5
        ]
    ]
    |> testLabel "LogStore.Core"
