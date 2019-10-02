namespace LogStore.Transport

open System.IO
open System.Net

/// <summary>SocketClient配置
/// </summary>
[<Sealed>]
type internal ClientConfig =

    /// <summary>构造函数
    /// </summary>
    /// <param name="bufferSize">缓存大小。</param>
    /// <param name="hostEndPoint">服务端的终结点。</param>
    /// <param name="sender">生成发送数据包的函数。</param>
    /// <param name="receiver">接收反馈的函数。</param>
    new :
        int *
        IPEndPoint *
        (byte[] -> BinaryWriter -> unit) *
        (BinaryReader -> byte[]) -> ClientConfig

    /// <summary>缓存大小
    /// </summary>
    member BufferSize : int

    /// <summary>服务端的终结点
    /// </summary>
    member HostEndPoint : IPEndPoint

    /// <summary>客户端生成发送数据包
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <param name="bw">二进制流写入。</param>
    member Sender : (byte[] -> BinaryWriter -> unit)

    /// <summary>客户端接收反馈
    /// </summary>
    /// <param name="br">从数据流读取。</param>
    member Receiver : (BinaryReader -> byte[])