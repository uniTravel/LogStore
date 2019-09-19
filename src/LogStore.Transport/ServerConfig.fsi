namespace LogStore.Transport

open System.IO
open System.Net

/// <summary>SocketServer配置
/// </summary>
[<Sealed>]
type internal ServerConfig =

    /// <summary>构造函数
    /// </summary>
    /// <param name="maxConnections">最大Socket连接数。</param>
    /// <param name="bufferSize">缓存大小。</param>
    /// <param name="backlog">请求的积压限度。</param>
    /// <param name="hostEndPoint">服务端的终结点。</param>
    /// <param name="retrieve">检索数据的函数。</param>
    /// <param name="writeTo">存储数据的函数。</param>
    new :
        int *
        int *
        int *
        IPEndPoint *
        (BinaryReader -> ((BinaryWriter -> unit) -> int64) -> unit) *
        ((BinaryWriter -> unit) -> int64) -> ServerConfig

    /// <summary>最大Socket连接数
    /// </summary>
    member MaxConnections : int

    /// <summary>缓存大小
    /// </summary>
    member BufferSize : int

    /// <summary>请求的积压限度
    /// </summary>
    member Backlog : int

    /// <summary>服务端的终结点
    /// </summary>
    member HostEndPoint : IPEndPoint

    /// <summary>检索数据的函数
    /// </summary>
    /// <param name="br">二进制流读取。</param>
    /// <param name="writeTo">二进制流写入函数。</param>
    member Retrieve : (BinaryReader -> ((BinaryWriter -> unit) -> int64) -> unit)

    /// <summary>存储数据的函数
    /// </summary>
    member WriteTo : ((BinaryWriter -> unit) -> int64)