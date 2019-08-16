namespace LogStore.Core

/// <summary>ChunkConfig配置
/// </summary>
[<Sealed>]
type ChunkConfig =

    /// <summary>构造函数
    /// </summary>
    /// <param name="path">Chunk文件库路径。</param>
    /// <param name="prefix">Chunk文件前缀。</param>
    /// <param name="length">Chunk文件名中索引的位数。</param>
    /// <param name="chunkSize">Chunk文件中数据块的预设大小。</param>
    /// <param name="cacheSize">Chunk缓存数量。</param>
    /// <param name="readerCount">Chunk读取器的资源数量。</param>
    new : string * string * int * int64 * int * int -> ChunkConfig

    /// <summary>Chunk文件库路径
    /// </summary>
    member Path : string

    /// <summary>Chunk文件前缀
    /// </summary>
    member Prefix : string

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