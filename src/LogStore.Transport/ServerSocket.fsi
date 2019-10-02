namespace LogStore.Transport

open System

[<Sealed>]
type internal ServerSocket =

    interface IDisposable

    /// <summary>构造函数
    /// </summary>
    /// <param name="config">SocketServer配置。</param>
    new : ServerConfig -> ServerSocket

    /// <summary>初始化Socket服务器
    /// </summary>
    member Init : unit -> unit

    /// <summary>启动Socket服务器
    /// </summary>
    member Start : unit -> unit

    /// <summary>获取Socket服务器状态
    /// </summary>
    /// <returns>Socket服务器状态。</returns>
    member GetState : unit -> State list

    /// <summary>手工关闭Socket连接
    /// <para>未配置服务端超时，才可以手工关闭连接。</para>
    /// </summary>
    /// <param name="id">Socket连接的Guid。</param>
    member CloseSocket : Guid -> unit

    /// <summary>停止Socket服务器
    /// </summary>
    member Stop : unit -> unit