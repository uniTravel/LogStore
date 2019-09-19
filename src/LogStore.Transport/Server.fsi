namespace LogStore.Transport

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
    /// <para>1、划出一大块缓存区。</para>
    /// <para>2、建立SocketAsyncEventArgs资源池。</para>
    /// </summary>
    /// <param name="cfg">SockerServer配置。</param>
    /// <returns>Standby状态的Socket服务器。</returns>
    member Init : (ServerConfig -> Server)

    /// <summary>启动Socket服务器
    /// </summary>
    /// <param name="cfg">SockerServer配置。</param>
    /// <returns>Active状态的Socket服务器。</returns>
    member Start : (ServerConfig -> Server)

    /// <summary>停止Socket服务器
    /// </summary>
    member Stop : (unit -> unit)
