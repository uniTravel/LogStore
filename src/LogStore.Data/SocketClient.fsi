namespace LogStore.Data

open System.IO

[<RequireQualifiedAccess>]
module internal SocketClient =

    /// <summary>生成发送数据包的函数
    /// <para>在数据基础上添加协议，用于解析数据。</para>
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <param name="bw">二进制流写入。</param>
    val write : byte[] -> BinaryWriter ->unit