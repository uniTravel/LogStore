module Socket

open System.IO
open System.Net
open System.Threading
open System.Text
open Expecto
open LogStore.Data
open LogStore.Transport

let port = 65000
let hostName = Dns.GetHostName ()
let address = (Dns.GetHostAddresses hostName).[3]
let hostEndPoint = IPEndPoint (address, port)
let serverHandler = SocketTransport.defaultServerHandler
let dataHandler (data: byte[]) : byte[] =
    let stream = new MemoryStream 8192
    let bw = new BinaryWriter (stream)
    bw.Write data
    data
let clientSender = SocketTransport.defaultClientSender
let clientReceiver = SocketTransport.defaultClientReceiver
let private serverConfig = ServerConfig (1024, 10, hostEndPoint, None, serverHandler, dataHandler)
let private clientConfig = ClientConfig (1024, hostEndPoint, clientSender, clientReceiver)
let private server = new ServerSocket (serverConfig)

[<Tests>]
let tests =
    testList "Socket通讯" [
        testCase "服务端" <| fun _ ->
            server.Init ()
            server.Start ()
            Thread.Sleep 2000
        testCase "客户端1" <| fun _ ->
            let client = new ClientSocket (clientConfig)
            client.Send <| Encoding.UTF8.GetBytes "This is test" |> ignore
            client.Send <| Encoding.UTF8.GetBytes "This is another test" |> ignore
        testCase "客户端2" <| fun _ ->
            let client = new ClientSocket (clientConfig)
            client.Send <| Encoding.UTF8.GetBytes "This is second test" |> ignore
        testCase "客户端3" <| fun _ ->
            let client = new ClientSocket (clientConfig)
            client.Send <| Encoding.UTF8.GetBytes "This is third test" |> ignore
    ]
    |> testLabel "LogStore.Transport"