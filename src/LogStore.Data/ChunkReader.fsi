namespace LogStore.Data

open System.IO

[<RequireQualifiedAccess>]
module internal ChunkReader =

    /// <summary>读取自由长度数据
    /// </summary>
    /// <param name="readFrom">从二进制流读取数据的函数。</param>
    /// <param name="localPos">读取数据的本地位置。</param>
    /// <param name="br">用于读取数据的资源。</param>
    val freeRead : (BinaryReader -> unit) -> int64 -> BinaryReader -> Async<unit>

    /// <summary>读取固定长度数据
    /// </summary>
    /// <param name="fixedLength">固定的数据长度。</param>
    /// <param name="readFrom">从二进制流读取数据的函数。</param>
    /// <param name="localPos">读取数据的本地位置。</param>
    /// <param name="br">用于读取数据的资源。</param>
    val fixedRead : int -> (BinaryReader -> unit) -> int64 -> BinaryReader -> Async<unit>
