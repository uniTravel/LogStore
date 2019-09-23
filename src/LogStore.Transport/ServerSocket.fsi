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

    /// <summary>调整Socket服务器的吞吐限制
    /// <para>1、在配置的Socket连接数基础上调整连接数限制。</para>
    /// <para>2、调整后的连接数总量不得低于配置的连接数。</para>
    /// </summary>
    /// <param name="size">调节的资源数量，正数调增，负数调减。</param>
    member Resize : int -> unit

    /// <summary>获取Socket服务器状态
    /// </summary>
    /// <returns>Socket服务器状态。</returns>
    member GetState : unit -> State

    /// <summary>停止Socket服务器
    /// </summary>
    member Stop : unit -> unit