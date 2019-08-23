module FreeMode

open System
open System.IO
open Expecto
open LogStore.Core

let path = @"D:\UC\LogStore\\TestCase\FreeMode\Jour"
let prefix = "Jour"
let length = 6
let chunkSize = int64 <| 8 * 1024 * 1024
let maxCacheSize = 10
let readerCount = 9

let private config = ChunkConfig (path, prefix, length, chunkSize, maxCacheSize, readerCount, Free)
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
            let manager = new ChunkManager (config)
            go "自由Chunk管理" |> f manager
            (manager :> IDisposable).Dispose ()
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
        ]
    ]
    |> testLabel "LogStore.Core"
