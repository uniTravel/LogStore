namespace LogStore.Data

open System.IO

[<RequireQualifiedAccess>]
module internal SocketServer =

    /// <summary>检索数据
    /// <para>1、从Socket读取数据。</para>
    /// <para>2、写入目标存储流。</para>
    /// <para>3、数据的前后记录int型数据长度。</para>
    /// </summary>
    /// <param name="br">二进制流读取。</param>
    /// <param name="writeTo">二进制流写入函数。</param>
    val retrieve : BinaryReader -> ((BinaryWriter -> unit) -> int64) -> unit