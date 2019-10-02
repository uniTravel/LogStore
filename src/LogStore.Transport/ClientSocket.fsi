namespace LogStore.Transport

open System

[<Sealed>]
type internal ClientSocket =

    interface IDisposable

    /// <summary>构造函数
    /// </summary>
    /// <param name="config">SocketClient配置。</param>
    new : ClientConfig -> ClientSocket

    /// <summary>发送数据
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <returns>服务端的反馈。</returns>
    member Send : byte[] -> byte[]

    /// <summary>异步发送数据
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <returns>服务端的反馈。</returns>
    member SendAsync : byte[] -> Async<byte[]>