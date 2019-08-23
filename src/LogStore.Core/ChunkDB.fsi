namespace LogStore.Core

/// <summary>Chunk文件库
/// <para>单一选项的可区分联合，表示Chunk库的状态。</para>
/// </summary>
/// <typeparam name="Active">活动状态，可读写。</typeparam>
type internal DB

[<RequireQualifiedAccess>]
module internal ChunkDB =

    /// <summary>初始化Chunk文件库
    /// </summary>
    val init : ChunkConfig -> unit

    /// <summary>打开Chunk文件库
    /// </summary>
    /// <returns>当前的GlobalPosition * DB。</returns>
    val openDB : ChunkConfig -> (int64 * DB)

[<Sealed>]
type internal DB with

    /// <summary>关闭Chunk库
    /// </summary>
    member Close : (unit -> unit)

    /// <summary>完成一个Chunk，并添加新Chunk
    /// </summary>
    /// <returns>超过缓存限制、需要卸载的Reader，原来的最后一个Reader，新的DB。</returns>
    member Complete : (ChunkConfig -> Reader option * Reader * DB)

    /// <summary>当前的ChunkNumber
    /// </summary>
    member ChunkNumber : int

    /// <summary>获取写的Chunk
    /// </summary>
    member Writer : Writer

    /// <summary>获取读的Chunk集合
    /// </summary>
    member Readers : Reader array