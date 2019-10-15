namespace LogStore.Core

open System.IO

/// <summary>ChunkConfig配置
/// </summary>
[<Sealed>]
type internal ChunkConfig =

    /// <summary>构造函数
    /// </summary>
    /// <param name="path">Chunk文件库路径。</param>
    /// <param name="folder">Chunk文件夹。</param>
    /// <param name="suffix">Chunk文件后缀。</param>
    /// <param name="length">Chunk文件名中索引的位数。</param>
    /// <param name="chunkSize">Chunk文件中数据块的预设大小。</param>
    /// <param name="cacheSize">Chunk缓存数量。</param>
    /// <param name="readerCount">Chunk读取器的资源数量。</param>
    /// <param name="writer">写入函数。</param>
    /// <param name="reader">读取函数。</param>
    /// <param name="seek">搜寻函数。</param>
    new :
        string *
        string *
        string *
        int *
        int64 *
        int *
        int *
        ((BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64) *
        ((BinaryReader -> unit) -> int64 -> BinaryReader -> Async<unit>) *
        (BinaryReader -> int -> int) -> ChunkConfig

    /// <summary>Chunk文件库路径
    /// </summary>
    member Path : string

    /// <summary>Chunk文件夹
    /// </summary>
    member Folder : string

    /// <summary>Chunk文件后缀
    /// </summary>
    member Suffix : string

    /// <summary>Chunk文件名中索引的位数
    /// </summary>
    member Length : int

    /// <summary>Chunk文件中数据块的预设大小
    /// </summary>
    member ChunkSize : int64

    /// <summary>Chunk缓存数量
    /// </summary>
    member CacheSize : int

    /// <summary>Chunk读取器的资源数量
    /// </summary>
    member ReaderCount : int

    /// <summary>写入函数
    /// </summary>
    member Writer : ((BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64)

    /// <summary>读取函数
    /// </summary>
    member Reader : ((BinaryReader -> unit) -> int64 -> BinaryReader -> Async<unit>)

    /// <summary>搜寻函数
    /// </summary>
    member Seek : (BinaryReader -> int -> int)