namespace LogStore.Common.Utils

/// <summary>资源池代理
/// <para>池中的资源具有同样的值。</para>
/// </summary>
/// <typeparam name="'res">泛型的资源类型。</typeparam>
[<Sealed>]
type PoolAgent<'res> =

    /// <summary>构造函数
    /// </summary>
    /// <param name="res">资源实例列表。</param>
    /// <param name="blockSeconds">挂起超过设定的秒数，资源池将阻塞。</param>
    new : 'res list * float -> PoolAgent<'res>

    /// <summary>资源的最小数量
    /// </summary>
    member MinSize : int

    /// <summary>增加的资源数量
    /// </summary>
    member AdjustedSize : int

    /// <summary>同步方式调用资源执行操作（无返回值）
    /// </summary>
    /// <param name="f">需要执行的操作。</param>
    member Action : ('res -> unit) -> unit

    /// <summary>异步方式调用资源执行操作（无返回值）
    /// </summary>
    /// <param name="f">需要执行的操作。</param>
    member AsyncAction : ('res -> unit) -> Async<unit>

    /// <summary>同步方式调用资源执行函数运算
    /// </summary>
    /// <param name="f">需要执行的函数。</param>
    /// <returns>执行无误，返回函数计算结果。</returns>
    member Func : ('res -> 'a) -> 'a

    /// <summary>异步方式调用资源执行函数运算
    /// </summary>
    /// <param name="f">需要执行的函数。</param>
    /// <returns>执行无误，则返回True。</returns>
    member AsyncFunc : ('res -> 'a) -> Async<'a>

    /// <summary>资源池扩容
    /// <para>资源总量不低于资源的最小数量。</para>
    /// </summary>
    /// <param name="res">资源实例列表。</param>
    /// <returns>总的增加数量。</returns>
    member IncreaseSize : 'res list -> int

    /// <summary>资源池缩容
    /// <para>资源总量不低于资源的最小数量。</para>
    /// </summary>
    /// <param name="size">减少的资源数量。</param>
    /// <returns>总的增加数量。</returns>
    member DecreaseSize : int -> int

    /// <summary>关闭资源池
    /// <param name="release">释放资源的操作。</param>
    /// </summary>
    member Close : ('res -> unit) option -> unit