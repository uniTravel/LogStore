namespace LogStore.Transport

open System.Net.Sockets

[<Sealed>]
type internal BufferManager =

    /// <summary>构造函数
    /// </summary>
    /// <param name="totalBytes">缓存池管理的字节数。</param>
    /// <param name="bufferSize">缓存大小。</param>
    new : int * int -> BufferManager

    /// <summary>初始化缓存池
    /// </summary>
    member InitBuffer : unit -> unit

    /// <summary>分配缓存
    /// <para>从缓存池向特定的SocketAsyncEventArgs对象分配缓存。</para>
    /// </summary>
    member SetBuffer : SocketAsyncEventArgs -> bool

    /// <summary>释放缓存
    /// <para>从特定的SocketAsyncEventArgs对象移除缓存，返回缓存池。</para>
    /// </summary>
    member FreeBuffer : SocketAsyncEventArgs -> unit