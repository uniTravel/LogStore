namespace LogStore.Transport

open System.IO
open System.Net

/// <summary>SocketClient配置
/// </summary>
[<Sealed>]
type internal ClientConfig =

    /// <summary>构造函数
    /// </summary>
    /// <param name="maxConnections">最大Socket连接数。</param>
    /// <param name="bufferSize">缓存大小。</param>
    /// <param name="hostEndPoint">服务端的终结点。</param>
    /// <param name="write">生成发送数据的函数。</param>
    new : int * int * IPEndPoint * (byte[] -> BinaryWriter -> unit) -> ClientConfig

    /// <summary>最大Socket连接数
    /// </summary>
    member MaxConnections : int

    /// <summary>缓存大小
    /// </summary>
    member BufferSize : int

    /// <summary>服务端的终结点
    /// </summary>
    member HostEndPoint : IPEndPoint

    /// <summary>生成发送数据包的函数
    /// <para>在数据基础上添加协议，用于解析数据。</para>
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <param name="bw">二进制流写入。</param>
    member Write : (byte[] -> BinaryWriter -> unit)