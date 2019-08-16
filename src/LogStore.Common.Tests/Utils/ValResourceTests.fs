module ValResourcePool.Tests

open System
open System.Threading
open System.Diagnostics
open Expecto
open LogStore.Common.Utils

let private ac (ms: int) x =
    x |> ignore
    Thread.Sleep ms

let private add (ms: int) x =
    let result = x + 1
    Thread.Sleep ms
    result

let private sw = Stopwatch ()
let private after (ts: TimeSpan) testName idx =
    sw.Stop ()
    let elapsed = sw.Elapsed.TotalMilliseconds
    printfn "%s%d：开始时间 %O | 结束时间 %O | 耗时 %.3f 毫秒" testName idx ts DateTime.Now.TimeOfDay elapsed
    elapsed
let private go testName () = after DateTime.Now.TimeOfDay testName

let private parallelTask args l r =
    List.map (l >> r) args
    |> Async.Parallel

[<Tests>]
let syncTests =
    testSequencedGroup "Val Resource Pool" <| testList "同步调用值" [
        let withAgent f () =
            let agent = new PoolAgent<int> ([ 1; 1 ], 1.0)
            sw.Restart () |> go "同步调用值" |> f agent
            agent.Close None
        yield! testFixture withAgent [
            "同步请求资源，运行Action操作。", fun agent finish ->
                (ac >> agent.Action) 100
                (ac >> agent.Action) 200
                (ac >> agent.Action) 300
                let elapsed = finish 1
                Expect.floatGreaterThanOrClose Accuracy.medium elapsed 600.0 "三步操作并非串行。"
            "同步请求资源，运行Func函数。", fun agent finish ->
                let r1 = (add >> agent.Func) 100
                let r2 = (add >> agent.Func) 200
                let r3 = (add >> agent.Func) 300
                let elapsed = finish 2
                Expect.equal r1 2 "第一步计算不成功。"
                Expect.equal r2 2 "第二步计算不成功。"
                Expect.equal r3 2 "第三步计算不成功。"
                Expect.floatGreaterThanOrClose Accuracy.medium elapsed 600.0 "三步计算并非串行。"
        ]
    ]
    |> testLabel "LogStore.Common"

[<Tests>]
let asyncTests =
    testSequencedGroup "Val Resource Pool" <| testList "异步调用值" [
        let withAgent f () =
            let agent = new PoolAgent<int> ([ 1; 1 ], 1.0)
            sw.Restart () |> go "异步调用值" |> f agent
            agent.Close None
        yield! testFixture withAgent [
            "异步并发请求资源，运行Action操作。", fun agent finish ->
                let nested = async {
                    let! child = Async.StartChild <| parallelTask [ 1000; 1000 ] ac agent.AsyncAction
                    do! Async.Sleep 900
                    let! _ = parallelTask [ 1000; 1000 ] ac agent.AsyncAction
                    let! _ = child
                    ()
                }
                Async.RunSynchronously nested
                let elapsed = finish 1
                Expect.floatLessThanOrClose Accuracy.medium elapsed 2100.0 "未能在预期时间内完成。"
            "异步并发请求资源，运行Func函数。", fun agent finish ->
                let nested = async {
                    let! child = Async.StartChild <| parallelTask [ 1000; 1000 ] add agent.AsyncFunc
                    do! Async.Sleep 900
                    let! _ = parallelTask [ 1000; 1000 ] add agent.AsyncFunc
                    let! _ = child
                    ()
                }
                Async.RunSynchronously nested
                let elapsed = finish 2
                Expect.floatLessThanOrClose Accuracy.medium elapsed 2100.0 "未能在预期时间内完成。"
            "请求资源时，资源池挂起时间超限。", fun agent finish ->
                let nested = async{
                    let! _ = Async.StartChild <| parallelTask [ 3000; 1500; 2000 ] add agent.AsyncFunc
                    do! Async.Sleep 1100
                    let! _ = (add >> agent.AsyncFunc) 100
                    ()
                }
                let f = fun _ -> Async.RunSynchronously <| nested
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                finish 3 |> ignore
        ]
    ]
    |> testLabel "LogStore.Common"

[<Tests>]
let controlTests =
    testSequencedGroup "Val Resource Pool" <| testList "值资源池控制" [
        let withAgent f () =
            let agent = new PoolAgent<int> ([ 1; 1 ], 1.0)
            sw.Restart () |> go "值资源池控制" |> f agent
            agent.Close None
        yield! testFixture withAgent [
            "资源池扩容。", fun agent finish ->
                agent.IncreaseSize [ 1; 1; 1 ] |> ignore
                Expect.equal agent.AdjustedSize 3 "资源池的扩容量应为3。"
                finish 1 |> ignore
            "资源池扩容，缩容量小于可缩容量。", fun agent finish ->
                agent.IncreaseSize [ 1; 1; 1 ] |> ignore
                agent.DecreaseSize 2 |> ignore
                Expect.equal agent.AdjustedSize 1 "资源池的扩容量应为1。"
                finish 2 |> ignore
            "资源池扩容，缩容量大于可缩容量。", fun agent finish ->
                agent.IncreaseSize [ 1; 1; 1 ] |> ignore
                agent.DecreaseSize 5 |> ignore
                Expect.equal agent.AdjustedSize 0 "资源池的扩容量应为0。"
                finish 3 |> ignore
            "资源池处于未扩容状态，请求缩容。", fun agent finish ->
                let f = fun _ -> agent.DecreaseSize 1 |> ignore
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                Expect.equal agent.AdjustedSize 0 "资源池的扩容量应为0。"
                finish 4 |> ignore
            "资源池处于已扩容、但资源均被占用，请求缩容。", fun agent finish ->
                let nested = async {
                    agent.IncreaseSize [ 1 ] |> ignore
                    let! child = Async.StartChild <| parallelTask [ 1000; 1000; 1000 ] ac agent.AsyncAction
                    do! Async.Sleep 1
                    agent.DecreaseSize 1 |> ignore
                    let! _ = child
                    ()
                }
                let f = fun _ -> Async.RunSynchronously <| nested
                Expect.throwsC f (fun ex -> printfn "出现异常：%s" ex.Message)
                Expect.equal agent.AdjustedSize 1 "资源池的扩容量应为1。"
                finish 5 |> ignore
        ]
    ]
    |> testLabel "LogStore.Common"