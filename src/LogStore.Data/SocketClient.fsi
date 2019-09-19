namespace LogStore.Data

open System.IO

[<RequireQualifiedAccess>]
module internal SocketClient =

    /// <summary>生成待发送数据
    /// </summary>
    /// <param name="writeTo">写入数据的函数。</param>
    /// <param name="bw">二进制流写入。</param>
    val write : (BinaryWriter -> unit) -> BinaryWriter ->unit