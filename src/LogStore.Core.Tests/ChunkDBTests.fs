module ChunkDB

open System.IO
open System.Text.RegularExpressions
open Expecto
open LogStore.Core

let path = @"D:\UC\LogStore\\TestCase\ChunkDB\Jour"
let prefix = "Jour"
let length = 6
let chunkSize = int64 <| 16 * 1024 * 1024
let maxCacheSize = 10
let readerCount = 9
let logMode = Free

let private config = ChunkConfig (path, prefix, length, chunkSize, maxCacheSize, readerCount, logMode)

[<Tests>]
let initDBTests =
    testSequencedGroup "Chunk DB" <| testList "初始化Chunk库" [
        let withArgs f () =
            go "初始化Chunk库" |> f
        yield! testFixture withArgs [
            "配置的Path不存在。", fun finish ->
                if Directory.Exists path then
                    Directory.EnumerateFiles path |> Seq.iter File.Delete
                    Directory.Delete path
                let f = fun _ -> ChunkDB.init config
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                finish 1
            "配置的Path为空。", fun finish ->
                Directory.CreateDirectory path |> ignore
                ChunkDB.init config
                finish 2
            "配置的Path非空。", fun finish ->
                let f = fun _ -> ChunkDB.init config
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                finish 3
        ]
    ]
    |> testLabel "LogStore.Core"

[<Tests>]
let openDBTests =
    testSequencedGroup "Chunk DB" <| testList "打开Chunk库" [
        let withArgs f () =
            go "打开Chunk库" |> f
        yield! testFixture withArgs [
            "配置的Path不存在。", fun finish ->
                if Directory.Exists path then
                    Directory.EnumerateFiles path |> Seq.iter File.Delete
                    Directory.Delete path
                let f = fun _ -> ChunkDB.openDB config |> ignore
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                finish 1
            "配置的Path为空。", fun finish ->
                Directory.CreateDirectory path |> ignore
                let f = fun _ -> ChunkDB.openDB config |> ignore
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                finish 2
            "Chunk库中有文件，但命名不规范。", fun finish ->
                Directory.EnumerateFiles path |> Seq.iter File.Delete
                ChunkDB.init config
                Directory.EnumerateFiles path
                |> Seq.iter (fun filename ->
                    let pattern = sprintf "\d{%i}" config.Length
                    let replacement = "3$&"
                    let newFileName = Regex.Replace (filename, pattern, replacement)
                    File.Move (filename, newFileName)
                )
                let f = fun _ -> ChunkDB.openDB config |> ignore
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                finish 3
            "Chunk库中有文件，但ChunkNumber不对。", fun finish ->
                Directory.EnumerateFiles path |> Seq.iter File.Delete
                ChunkDB.init config
                Directory.EnumerateFiles path
                |> Seq.iteri (fun i filename ->
                    let pattern = sprintf "\d{%i}" config.Length
                    let replacement = sprintf "%0*i" config.Length (i + 1)
                    let newFileName = Regex.Replace (filename, pattern, replacement)
                    File.Move (filename, newFileName)
                )
                let f = fun _ -> ChunkDB.openDB config |> ignore
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                finish 4
        ]
    ]
    |> testLabel "LogStore.Core"