namespace LogStore.Transport

open System.Net
open System.Net.Sockets

/// <summary>Socket服务器
/// <para>可区分联合，表示Socket服务器的状态。</para>
/// </summary>
/// <typeparam name="Initial">初始状态。</typeparam>
/// <typeparam name="Standby">待用状态。</typeparam>
/// <typeparam name="Active">活动状态。</typeparam>
type internal Server

/// <summary>Socket服务器状态
/// </summary>
/// <typeparam name="MaxConnections">最大并发Socket连接数。</typeparam>
/// <typeparam name="AdjustedSize">调整的Socket连接数，该数不小于0。</typeparam>
/// <typeparam name="BufferSize">缓存大小。</typeparam>
/// <typeparam name="Backlog">请求的积压限度。</typeparam>
/// <typeparam name="HostEndPoint">服务端的终结点。</typeparam>
/// <typeparam name="ReceiveTimeout">Socket接收超时。</typeparam>
type internal State = {
    MaxConnections: int
    AdjustedSize: int
    BufferSize: int
    Backlog: int
    HostEndPoint: IPEndPoint
    ReceiveTimeout: int
}

[<RequireQualifiedAccess>]
module internal Server =
    val newServer : Server

[<Sealed>]
type internal Server with

    /// <summary>初始化Socket服务器
    /// <para>1、划出一大块缓存区。</para>
    /// <para>2、建立SocketAsyncEventArgs资源池。</para>
    /// </summary>
    /// <param name="cfg">SockerServer配置。</param>
    /// <returns>Standby状态的Socket服务器。</returns>
    member Init : (ServerConfig -> Server)

    /// <summary>侦听Socket连接
    /// </summary>
    /// <param name="listener">侦听器。</param>
    /// <returns>Active状态的Socket服务器。</returns>
    member Listen : (TcpListener -> Server)

    /// <summary>接受Socket连接
    /// </summary>
    /// <param name="netStream">Socker网络流。</param>
    /// <param name="cfg">SockerServer配置。</param>
    /// <returns>Active状态的Socket服务器。</returns>
    member Accept : (NetworkStream -> ServerConfig -> Server)

    /// <summary>调整Socket服务器的吞吐限制
    /// </summary>
    /// <param name="size">调节的资源数量，正数调增，负数调减。</param>
    /// <returns>Active状态的Socket服务器。</returns>
    member Resize : (int -> Server)

    /// <summary>获取Socket服务器状态
    /// </summary>
    /// <returns>Socket服务器状态。</returns>
    member GetState : (unit -> State)

    /// <summary>停止Socket服务器
    /// </summary>
    /// <returns>Standby状态的Socket服务器。</returns>
    member Stop : (unit -> Server)