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
    /// <param name="writeTo">写入数据的函数。</param>
    member Send : (BinaryWriter -> unit) -> unit