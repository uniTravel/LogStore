namespace LogStore.Data

open System.IO

[<RequireQualifiedAccess>]
module internal ChunkSeek =

    /// <summary>自由长度数据搜寻写入位置
    /// </summary>
    /// <param name="br">用于读取数据的资源。</param>
    /// <param name="toWrite">写入位置。</param>
    val freeSeek : BinaryReader -> int -> int

    /// <summary>固定长度数据搜寻写入位置
    /// </summary>
    /// <param name="fixedLength">固定长度。</param>
    /// <param name="br">用于读取数据的资源。</param>
    /// <param name="toWrite">写入位置。</param>
    val fixedSeek : int -> BinaryReader -> int -> int