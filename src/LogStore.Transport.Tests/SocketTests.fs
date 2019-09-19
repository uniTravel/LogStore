module Socket

open System.IO
open System.Net
open Expecto
open LogStore.Data
open LogStore.Transport

let port = 65000
let hostName = Dns.GetHostName ()
let address = (Dns.GetHostAddresses hostName).[2]
let hostEndPoint = IPEndPoint (address, port)
let serverRetrieve = SocketServer.retrieve
let serverWriteTo (writeTo: BinaryWriter -> unit) : int64 =
    let stream = new MemoryStream 8192
    let bw = new BinaryWriter (stream)
    writeTo bw
    stream.Length
let clientWrite = SocketClient.write
let private serverConfig = ServerConfig (2, 1024, 10, hostEndPoint, serverRetrieve, serverWriteTo)
let private clientConfig = ClientConfig (2, 1024, hostEndPoint, clientWrite)
let private server = new ServerSocket (serverConfig)

[<Tests>]
let tests =
    testList "Socket通讯" [
        let withArgs f () =
            go "Socket通讯" |> f
        yield! testFixture withArgs [
            "服务端", fun finish ->
                server.Init ()
                server.Start ()
                finish 1
            "客户端1", fun finish ->
                let client = new ClientSocket (clientConfig)
                client.Send <| fun bw ->
                    bw.Write "This is test"
                finish 2
            "客户端2", fun finish ->
                let client = new ClientSocket (clientConfig)
                client.Send <| fun bw ->
                    bw.Write "This is second test"
                finish 3
            "客户端3", fun finish ->
                let client = new ClientSocket (clientConfig)
                client.Send <| fun bw ->
                    bw.Write "This is third test"
                finish 4
        ]
    ]
    |> testLabel "LogStore.Transport"