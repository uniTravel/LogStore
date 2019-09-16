namespace LogStore.Data

open System.IO

[<RequireQualifiedAccess>]
module internal ChunkWriter =

    /// <summary>写入自由长度数据
    /// </summary>
    /// <param name="writeTo">写入缓存的函数。</param>
    /// <param name="bs">用于写入数据的缓存。</param>
    /// <param name="bw">用于写入缓存的BinaryWriter。</param>
    /// <returns>写入数据的长度。</returns>
    val freeAppend : (BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64

    /// <summary>写入固定长度数据
    /// </summary>
    /// <param name="fixedLength">固定的数据长度。</param>
    /// <param name="writeTo">写入缓存的函数。</param>
    /// <param name="bs">用于写入数据的缓存。</param>
    /// <param name="bw">用于写入缓存的BinaryWriter。</param>
    /// <returns>写入数据的长度。</returns>
    val fixedAppend : int -> (BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64
