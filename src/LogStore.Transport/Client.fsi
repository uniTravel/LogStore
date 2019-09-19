namespace LogStore.Transport

open System.IO
open System.Net

/// <summary>Socket客户端
/// <para>单一状态的可区分联合，表示Socket客户端的状态。</para>
/// </summary>
/// <typeparam name="Active">活动状态。</typeparam>
type internal Client

[<RequireQualifiedAccess>]
module internal Client =

    /// <summary>连接到Socket服务器
    /// </summary>
    /// <param name="maxConnections">最大Socket连接数。</param>
    /// <param name="bufferSize">缓存大小。</param>
    /// <param name="hostEndPoint">Socket服务器终结点。</param>
    /// <returns>Active状态的Socket客户端。</returns>
    val connect : int -> int -> IPEndPoint -> Client

[<Sealed>]
type internal Client with

    /// <summary>断开到Socket服务器的连接
    /// </summary>
    member Disconnect : (unit -> unit)

    /// <summary>发送数据
    /// </summary>
    /// <param name="writeTo">写入数据的函数。</param>
    /// <param name="cfg">SockerClient配置。</param>
    member Send : ((BinaryWriter -> unit) -> ClientConfig -> unit)