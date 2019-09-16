namespace LogStore.Core

open System
open System.IO

[<Sealed>]
type internal ChunkManager =

    interface IDisposable

    /// <summary>构造函数
    /// </summary>
    /// <param name="config">ChunkConfig配置。</param>
    new : ChunkConfig -> ChunkManager

    /// <summary>写入数据
    /// <para>先写入缓存，然后写入Chunk。</para>
    /// </summary>
    /// <param name="writeTo">写入缓存的函数。</param>
    /// <returns>写入的全局位置。</returns>
    member Append : (BinaryWriter -> unit) -> int64

    /// <summary>读取数据
    /// </summary>
    /// <param name="globalPos">读取数据的全局位置。</param>
    /// <param name="readFrom">从二进制流读取数据的函数。</param>
    member Read : int64 -> (BinaryReader -> unit) -> Async<unit>