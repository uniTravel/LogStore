open System
open System.Threading

let data = 1

[<EntryPoint>]
let main argv =
    let mutable counter = 0
    let max = if argv.Length <> 0 then Convert.ToInt32 argv.[0] else -1
    while max = -1 || counter < max do
        counter <- counter + 1
        printfn "Counter：%d" counter
        Thread.Sleep 1000
    0 // return an integer exit code