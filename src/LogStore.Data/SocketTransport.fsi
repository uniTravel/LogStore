namespace LogStore.Data

open System.IO

[<RequireQualifiedAccess>]
module internal SocketTransport =

    /// <summary>服务端解析数据
    /// <para>数据前面记录int型数据长度。</para>
    /// <para>1、从Socket读取数据。</para>
    /// <para>2、处理数据，生成反馈。</para>
    /// </summary>
    /// <param name="dataHandler">处理数据的函数。</param>
    /// <param name="br">从数据流读取。</param>
    /// <param name="bw">写入数据流，反馈给客户端。</param>
    val defaultServerHandler : (byte[] -> byte[]) -> BinaryReader -> BinaryWriter -> unit

    /// <summary>客户端生成发送数据包
    /// <para>数据前面记录int型数据长度。</para>
    /// </summary>
    /// <param name="data">待发送的数据。</param>
    /// <param name="bw">二进制流写入。</param>
    val defaultClientSender : byte[] -> BinaryWriter -> unit

    /// <summary>客户端接收反馈
    /// <para>数据前面记录int型数据长度。</para>
    /// </summary>
    /// <param name="br">从数据流读取。</param>
    /// <returns>服务端的反馈。</returns>
    val defaultClientReceiver : BinaryReader -> byte[]