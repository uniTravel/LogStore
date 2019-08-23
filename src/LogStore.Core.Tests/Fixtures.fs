[<AutoOpen>]
module Fixture

open System
open System.Diagnostics

let private sw = Stopwatch ()

let private after1 (ts : TimeSpan) testName idx =
    sw.Stop ()
    let elapsed = sw.Elapsed.TotalMilliseconds
    printfn "%s-%O：开始 %O | 结束 %O | 耗时 %.3f 毫秒" testName idx ts DateTime.Now.TimeOfDay elapsed

let private after2 (ts : TimeSpan) testName idx =
    sw.Stop ()
    let elapsed = sw.Elapsed.TotalMilliseconds
    printfn "%s-%O：开始 %O | 结束 %O | 耗时 %.3f 毫秒" testName idx ts DateTime.Now.TimeOfDay elapsed
    elapsed

let go testName =
    sw.Restart ()
    after1 DateTime.Now.TimeOfDay testName

let goElapsed testName =
    sw.Restart ()
    after2 DateTime.Now.TimeOfDay testName