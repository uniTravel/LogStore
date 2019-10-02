namespace LogStore.Transport

open System
open System.Net.Sockets
open System.Threading

/// <summary>Socket状态
/// </summary>
/// <typeparam name="Id">Socket的ID。</typeparam>
/// <typeparam name="Socket">Socket。</typeparam>
type internal State = {
    Id: Guid
    Socket: Socket
}

/// <summary>Socket服务器
/// <para>可区分联合，表示Socket服务器的状态。</para>
/// </summary>
/// <typeparam name="Initial">初始状态。</typeparam>
/// <typeparam name="Standby">待用状态。</typeparam>
/// <typeparam name="Active">活动状态。</typeparam>
type internal Server

[<RequireQualifiedAccess>]
module internal Server =
    val newServer : Server

[<Sealed>]
type internal Server with

    /// <summary>初始化Socket服务器
    /// <para>设置缓存区。</para>
    /// </summary>
    /// <param name="cfg">SocketServer配置。</param>
    /// <returns>Standby状态的Socket服务器。</returns>
    member Init : (ServerConfig -> Server)

    /// <summary>启动Socket服务器
    /// </summary>
    /// <param name="cfg">SocketServer配置。</param>
    /// <param name="cancellationToken">取消侦听的凭据。</param>
    /// <returns>Active状态的Socket服务器。</returns>
    member Start : (ServerConfig -> CancellationTokenSource -> Server)

    /// <summary>获取Socket服务器状态
    /// </summary>
    /// <returns>Socket服务器状态。</returns>
    member GetState : (unit -> State list)

    /// <summary>手工关闭Socket连接
    /// </summary>
    /// <param name="id">Socket连接的Guid。</param>
    member CloseSocket : (Guid -> Server)

    /// <summary>停止Socket服务器
    /// </summary>
    /// <returns>Standby状态的Socket服务器。</returns>
    member Stop : (unit -> Server)