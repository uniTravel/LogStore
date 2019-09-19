namespace LogStore.Transport

open System

[<Sealed>]
type internal ServerSocket =

    interface IDisposable

    /// <summary>构造函数
    /// </summary>
    /// <param name="config">SockerServer配置。</param>
    new : ServerConfig -> ServerSocket

    /// <summary>初始化Socket服务器
    /// </summary>
    member Init : unit -> unit

    /// <summary>启动Socket服务器
    /// </summary>
    member Start : unit -> unit