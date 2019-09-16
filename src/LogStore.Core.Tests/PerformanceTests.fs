module Performance

open System
open System.IO
open Expecto
open LogStore.Core
open LogStore.Data

let path = @"D:\UC\LogStore\\TestCase\ChunkManager\Jour"
let prefix = "Jour"
let length = 6
let chunkSize = int64 <| 8 * 1024 * 1024
let maxCacheSize = 10
let readerCount = 9
let writer = ChunkWriter.freeAppend
let reader = ChunkReader.freeRead
let seek = ChunkSeek.freeSeek

let private config = ChunkConfig (path, prefix, length, chunkSize, maxCacheSize, readerCount, writer, reader, seek)
if not <| Directory.Exists path then Directory.CreateDirectory path |> ignore

[<PTests>]
let tests =
    testSequenced <| testList "Chunk管理性能" [
        let withArgs f () =
            Directory.EnumerateFiles path |> Seq.iter (fun file ->
                File.SetAttributes (file, FileAttributes.NotContentIndexed)
                File.Delete file
            )
            ChunkDB.init config
            use manager = new ChunkManager (config)
            go "Chunk管理性能" |> f manager
        yield! testFixture withArgs [
            "写入万条4K左右数据。", fun manager finish ->
                let random = Random ()
                let mutable pos = 0L
                for i in 1 .. 10000 do
                    let length = random.Next (4000, 4200)
                    pos <- manager.Append <| fun bw ->
                        bw.Write i
                        sprintf "%0*d" length i |> bw.Write
                printfn "写入数据量：%d" pos
                finish 1
            "写入千条40K左右数据。", fun manager finish ->
                let random = Random ()
                let mutable pos = 0L
                for i in 1 .. 1000 do
                    let length = random.Next (40000, 41000)
                    pos <- manager.Append <| fun bw ->
                        bw.Write i
                        sprintf "%0*d" length i |> bw.Write
                printfn "写入数据量：%d" pos
                finish 2
        ]
    ]
    |> testLabel "LogStore.Core"
