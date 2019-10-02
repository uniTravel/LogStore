namespace LogStore.Transport

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
    /// <param name="bufferSize">缓存大小。</param>
    /// <param name="hostEndPoint">Socket服务器终结点。</param>
    /// <returns>Active状态的Socket客户端。</returns>
    val connect : int -> IPEndPoint -> Client

[<Sealed>]
type internal Client with

    /// <summary>断开到Socket服务器的连接
    /// </summary>
    member Disconnect : (unit -> unit)

    /// <summary>发送数据
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <param name="cfg">SocketClient配置。</param>
    /// <returns>服务端的反馈。</returns>
    member Send : (byte[] -> ClientConfig -> byte[])

    /// <summary>异步发送数据
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <param name="cfg">SocketClient配置。</param>
    /// <returns>服务端的反馈。</returns>
    member SendAsync : (byte[] -> ClientConfig -> Async<byte[]>)