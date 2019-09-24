namespace LogStore.Transport

open System.IO
open System.Net

/// <summary>SocketServer配置
/// </summary>
[<Sealed>]
type internal ServerConfig =

    /// <summary>构造函数
    /// </summary>
    /// <param name="maxConnections">最大并发Socket连接数。</param>
    /// <param name="bufferSize">缓存大小。</param>
    /// <param name="backlog">请求的积压限度。</param>
    /// <param name="hostEndPoint">服务端的终结点。</param>
    /// <param name="timeout">Socket接收超时。</param>
    /// <param name="writeTo">处理数据的函数。</param>
    new : int * int * int * IPEndPoint * int * (byte[] -> unit) -> ServerConfig

    /// <summary>最大并发Socket连接数
    /// </summary>
    member MaxConnections : int

    /// <summary>缓存大小
    /// </summary>
    member BufferSize : int

    /// <summary>请求的积压限度
    /// </summary>
    member Backlog : int

    /// <summary>Socket接收超时
    /// <para>服务端Socket的接收超时设置，单位为毫秒。</para>
    /// </summary>
    member HostEndPoint : IPEndPoint

    /// <summary>服务端的终结点
    /// </summary>
    member ReceiveTimeout : int

    /// <summary>处理数据的函数
    /// </summary>
    member WriteTo : (byte[] -> unit)