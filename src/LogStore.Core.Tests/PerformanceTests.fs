module Performance.Tests

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open Expecto

let private sw = Stopwatch ()
let private after (ts : TimeSpan) testName =
    sw.Stop ()
    let elapsed = sw.ElapsedMilliseconds
    printfn "%s：开始时间 %O | 结束时间 %O | 耗时 %d 毫秒" testName ts DateTime.Now.TimeOfDay elapsed
let private go () = after DateTime.Now.TimeOfDay

[<Tests>]
let testsMD5 =
    testSequenced <| testList "MD5性能测试" [
        let withArg f () =
            let filename = @"D:\UC\LogStore\test.exe"
            let fs = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            let buffer = Array.zeroCreate <| 8 * 1024
            let md5 = MD5.Create ()
            sw.Restart () |> go |> f buffer fs md5
            fs.Close ()
            md5.Clear ()
        yield! testFixture withArg [
            "文件流", fun _ fs md5 finish ->
                md5.ComputeHash (fs) |> ignore
                finish "文件流"
            "MD5", fun buffer fs md5 finish ->
                let mutable toRead = int32 fs.Length
                while toRead > 0 do
                    let read = fs.Read (buffer, 0, buffer.Length)
                    md5.TransformBlock (buffer, 0, read, null, 0) |> ignore
                    match read with
                    | 0 -> toRead <- 0
                    | _ -> toRead <- toRead - read
                md5.TransformFinalBlock (Array.empty, 0, 0) |> ignore
                finish "MD5"
            "递归", fun buffer fs md5 finish ->
                let b = buffer.Length
                let rec trans toRead =
                    match toRead with
                    | read when read <= b ->
                        fs.Read (buffer, 0, read) |> ignore
                        md5.TransformBlock (buffer, 0, read, null, 0)
                    | _ ->
                        fs.Read(buffer, 0, b) |> ignore
                        md5.TransformBlock (buffer, 0, b, null, 0) |> ignore
                        trans <| toRead - b
                trans (int32 fs.Length) |> ignore
                md5.TransformFinalBlock (Array.empty, 0, 0) |> ignore
                finish "递归"
        ]
    ]
    |> testLabel "LogStore.Core"