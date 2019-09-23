namespace LogStore.Transport

open System
open System.IO

[<Sealed>]
type internal ClientSocket =

    interface IDisposable

    /// <summary>构造函数
    /// </summary>
    /// <param name="config">SockerClient配置。</param>
    new : ClientConfig -> ClientSocket

    /// <summary>发送数据
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    member Send : byte[] -> unit

    /// <summary>异步发送数据
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    member SendAsync : byte[] -> Async<unit>