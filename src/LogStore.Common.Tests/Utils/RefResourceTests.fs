module RefResourcePool.Tests

open System
open System.IO
open System.Diagnostics
open Expecto
open LogStore.Common.Utils

let private sw = Stopwatch ()
let private after (ts : TimeSpan) testName idx =
    sw.Stop ()
    let elapsed = sw.Elapsed.TotalMilliseconds
    printfn "%s%d：开始时间 %O | 结束时间 %O | 耗时 %.3f 毫秒" testName idx ts DateTime.Now.TimeOfDay elapsed
let private go testName () = after DateTime.Now.TimeOfDay testName

let private path = @"D:\UC\LogStore\TestCase\ResourcePool\"

let private createFile filename =
    use temp = File.CreateText(filename)
    temp.WriteLine "test1"
    temp.WriteLine "test2"
    temp.WriteLine "test3"

[<Tests>]
let syncTests =
    testSequencedGroup "Ref Resource Pool" <| testList "同步调用引用" [
        let withAgent f () =
            Directory.EnumerateFiles path |> Seq.iter File.Delete
            let filename = Path.Combine [| path; @"test.txt" |]
            createFile filename
            let sr1 = File.OpenText filename
            let sr2 = File.OpenText filename
            let sr3 = File.OpenText filename
            let agent = new PoolAgent<StreamReader> ([ sr1; sr2; sr3 ], 1.0)
            sw.Restart() |> go "同步调用引用" |> f agent
            agent.Close <| Some (fun x -> x.Close ())
        yield! testFixture withAgent [
            "同步请求资源，运行Action操作。", fun agent finish ->
                let actual = Array.zeroCreate<string> 3
                agent.Action <| fun sr -> actual.[0] <- sr.ReadLine ()
                agent.Action <| fun sr -> actual.[1] <- sr.ReadLine ()
                agent.Action <| fun sr -> actual.[2] <- sr.ReadLine ()
                Expect.allEqual actual "test1" "出现异常"
                finish 1
            "同步请求资源，运行Func函数。", fun agent finish ->
                let r1 = agent.Func <| fun sr -> sr.ReadLine ()
                let r2 = agent.Func <| fun sr -> sr.ReadLine ()
                let r3 = agent.Func <| fun sr -> sr.ReadLine ()
                Expect.allEqual [ r1; r2; r3] "test1" "出现异常"
                finish 2
        ]
    ]
    |> testLabel "LogStore.Common"