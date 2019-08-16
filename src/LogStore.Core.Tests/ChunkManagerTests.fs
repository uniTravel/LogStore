module ChunkManager.Tests

open System
open System.Diagnostics
open Expecto

let private sw = Stopwatch ()
let private after (ts : TimeSpan) testName =
    sw.Stop ()
    let elapsed = sw.ElapsedMilliseconds
    printfn @"%s：开始时间 %O | 结束时间 %O | 耗时 %d 毫秒" testName ts DateTime.Now.TimeOfDay elapsed
let private go () = after DateTime.Now.TimeOfDay

[<FTests>]
let tests =
    testList "Chunk管理器测试" [
        testCase "" <| fun _ ->
            let finish = sw.Restart () |> go

            finish "测试1"

    ]
    |> testLabel "LogStore.Core"