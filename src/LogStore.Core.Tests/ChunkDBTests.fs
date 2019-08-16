module ChunkFile.Tests

open System
open System.IO
open System.Diagnostics
open Expecto
open LogStore.Core

let path = @"D:\UC\LogStore\Jour"
let prefix = "Jour"
let length = 6
let chunkSize = int64 <| 16 * 1024 * 1024
let maxCacheSize = 10
let readerCount = 9

let private sw = Stopwatch ()
let private after (ts : TimeSpan) testName =
    sw.Stop ()
    let elapsed = sw.ElapsedMilliseconds
    printfn @"%s：开始时间 %O | 结束时间 %O | 耗时 %d 毫秒" testName ts DateTime.Now.TimeOfDay elapsed
let private go () = after DateTime.Now.TimeOfDay

let private config = ChunkConfig (path, prefix, length, chunkSize, maxCacheSize, readerCount)

[<Tests>]
let tests =
    testList "Chunk库测试" [
        testCase "" <| fun _ ->
            Directory.EnumerateFiles path |> Seq.iter File.Delete
            let finish = sw.Restart () |> go
            ChunkDB.init config
            finish "测试1"
    ]
    |> testLabel "LogStore.Core"