module Messaging

open System.Net
open System.Net.Sockets
open Expecto

let port = 65000
let hostName = Dns.GetHostName ()
let address = Dns.GetHostAddresses hostName |> Array.find (fun ad -> ad.AddressFamily = AddressFamily.InterNetwork)
let hostEndPoint = IPEndPoint (address, port)


[<Tests>]
let tests =
    testList "消息队列" [
        testCase "服务端" <| fun _ ->

            printfn ""
    ]
    |> testLabel "LogStore.Messaging"