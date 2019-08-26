namespace LogStore.Core

open System.IO

type internal ValidFileName

type internal Chunk

type internal Reader

type internal Writer

type internal WorkArea = {
    ChunkNum: int
    Writer: Writer
    Readers: Reader array
}

[<RequireQualifiedAccess>]
module internal Chunk =

    /// <summary>验证Chunk文件名是否合法
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    /// <param name="input">待验证的Chunk文件名列表。</param>
    /// <returns>可选的合法Chunk文件名。</returns>
    val validateName : ChunkConfig -> string list -> ValidFileName list

    /// <summary>MD5验证
    /// </summary>
    /// <param name="chunk">已完成的Chunk。</param>
    /// <returns>验证成功与否。</returns>
    val validateChecksum : Chunk -> bool

    /// <summary>对比文件名与相应的Chunk内容
    /// <para>对比文件名中包含的Index与Chunk内的ChunkNumber。</para>
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    /// <param name="filename">合法的Chunk文件名。</param>
    /// <param name="index">ChunkNumber。</param>
    /// <returns>是否一致。</returns>
    val compare : ChunkConfig -> ValidFileName -> int -> bool

    /// <summary>从合法的文件生成Chunk
    /// </summary>
    /// <param name="filename">合法的Chunk文件名。</param>
    /// <returns>Chunk。</returns>
    val fromFile : ValidFileName -> Chunk

    /// <summary>创建Reader
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    /// <param name="chunks">已完成的Chunk列表。</param>
    /// <param name="readers">Reader数组。</param>
    val buildReaders : ChunkConfig -> Reader array -> Chunk list -> unit

    /// <summary>创建Writer
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    /// <param name="chunk">当前的Chunk（未完成）。</param>
    /// <returns>当前的GlobalPosition * Writer * Reader。</returns>
    val buildWriter : ChunkConfig -> Chunk -> (int64 * Writer * Reader)

    /// <summary>初始化Chunk
    /// <para>创建ChunkNumber为零的Chunk。</para>
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    val create : ChunkConfig -> unit

    /// <summary>关闭Chunk
    /// </summary>
    /// <param name="chunk">Chunk。</param>
    val close : Chunk -> unit

    /// <summary>关闭Reader
    /// <para>关闭Reader后还需关闭Chunk。</para>
    /// </summary>
    /// <param name="reader">Reader。</param>
    val closeReader : Reader -> unit

    /// <summary>关闭Writer
    /// <para>关闭Writer后还需关闭Chunk。</para>
    /// </summary>
    /// <param name="writer">Writer。</param>
    val closeWriter : Writer -> unit

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

    /// <summary>写入数据
    /// <para>1、先写入缓存，然后写入Chunk。</para>
    /// <para>2、有问题的不写入，返回的位置仍为原来的全局位置。</para>
    /// </summary>
    /// <param name="internalAppend">内部的写入数据函数。</param>
    /// <param name="writeTo">写入缓存的函数。</param>
    /// <param name="oldPos">当前写入流的全局位置。</param>
    /// <param name="writer">Writer。</param>
    /// <returns>写入之后的全局位置。</returns>
    val append : ((BinaryWriter -> unit) -> MemoryStream -> BinaryWriter -> int64) -> (BinaryWriter -> unit) -> int64 -> Writer -> int64

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

    /// <summary>读取数据
    /// </summary>
    /// <param name="internalRead">内部的读取函数。</param>
    /// <param name="readFrom">从二进制流读取数据的函数。</param>
    /// <param name="reader">Reader。</param>
    /// <param name="globalPos">读取数据的全局位置。</param>
    val read : ((BinaryReader -> unit) -> int64 -> BinaryReader -> Async<unit>) -> (BinaryReader -> unit) -> Reader -> int64 -> Async<unit>

    /// <summary>完成一个Chunk，并添加新Chunk
    /// </summary>
    /// <param name="cfg">ChunkConfig配置。</param>
    /// <param name="workarea">当前工作区。</param>
    /// <returns>超过缓存限制、需要卸载的Reader，完成的Reader，完成后的工作区。</returns>
    val complete : ChunkConfig -> WorkArea -> (Reader option * Reader * WorkArea)

[<Sealed>]
type internal Chunk with
    member Header : ChunkHeader
    member Footer : ChunkFooter