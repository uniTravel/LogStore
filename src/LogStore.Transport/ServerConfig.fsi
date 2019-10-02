namespace LogStore.Transport

open System.IO
open System.Net

/// <summary>SocketServer配置
/// </summary>
[<Sealed>]
type internal ServerConfig =

    /// <summary>构造函数
    /// </summary>
    /// <param name="bufferSize">缓存大小。</param>
    /// <param name="backlog">请求的积压限度。</param>
    /// <param name="hostEndPoint">服务端的终结点。</param>
    /// <param name="timeout">可选的服务端超时（毫秒）。</param>
    /// <param name="handler">解析数据包的函数。</param>
    /// <param name="dataHandler">处理数据的函数。</param>
    new :
        int *
        int *
        IPEndPoint *
        int64 option *
        ((byte[] -> byte[]) -> BinaryReader -> BinaryWriter -> unit) *
        (byte[] -> byte[]) -> ServerConfig

    /// <summary>缓存大小
    /// </summary>
    member BufferSize : int

    /// <summary>请求的积压限度
    /// </summary>
    member Backlog : int

    /// <summary>服务端的终结点
    /// </summary>
    member HostEndPoint : IPEndPoint

    /// <summary>可选的服务端超时（毫秒）
    /// <para>1、如果有设置：超时未收到数据将关闭连接；不能手工关闭连接。</para>
    /// <para>2、如果未设置：可以手工关闭连接。</para>
    /// </summary>
    member Timeout : int64 option

    /// <summary>解析数据包的函数
    /// </summary>
    /// <param name="dataHandler">处理数据的函数。</param>
    /// <param name="br">从数据流读取。</param>
    /// <param name="bw">写入数据流，反馈给消息生产者。</param>
    member Handler : ((byte[] -> byte[]) -> BinaryReader -> BinaryWriter -> unit)

    /// <summary>处理数据的函数
    /// </summary>
    member DataHandler : (byte[] -> byte[])