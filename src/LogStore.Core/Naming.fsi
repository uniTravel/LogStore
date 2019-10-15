namespace LogStore.Core

type internal ValidFileName = ValidFileName of string

[<RequireQualifiedAccess>]
module internal Naming =

    /// <summary>获取Chunk文件名
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    /// <param name="index">Chunk文件名的索引。</param>
    /// <returns>合法的Chunk文件名。</returns>
    val fileName : ChunkConfig -> int -> string

    /// <summary>验证Chunk文件名是否合法
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    /// <param name="input">待验证的Chunk文件名列表。</param>
    /// <returns>可选的合法Chunk文件名。</returns>
    val validateName : ChunkConfig -> string list -> ValidFileName list