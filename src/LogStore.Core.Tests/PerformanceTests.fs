module Performance

open System
open System.IO
open Expecto
open LogStore.Core

let path = @"D:\UC\LogStore\\TestCase\ChunkManager\Jour"
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
    testSequenced <| testList "Chunk管理性能" [
        let withArgs f () =
            let manager = new ChunkManager (config)
            go "Chunk管理性能" |> f manager
            (manager :> IDisposable).Dispose ()
        yield! testFixture withArgs [
            "写入第一个数据。", fun manager finish ->
                let random = Random ()
                let mutable pos = 0L
                for i in 1 .. 1 do
                    let length = random.Next (4000, 5000)
                    pos <- manager.Append <| fun bw ->
                        bw.Write i
                        sprintf "%0*d" length i |> bw.Write
                printfn "%d" pos
                // Expect.equal pos 12L "写入位置不正确"
                finish 1
        ]
    ]
    |> testLabel "LogStore.Core"
