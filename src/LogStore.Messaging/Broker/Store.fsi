namespace LogStore.Messaging.Broker

/// <summary>消息库
/// <para>单一状态的可区分联合，表示消息库的状态。</para>
/// </summary>
/// <typeparam name="Active">活动状态。</typeparam>
type internal Store

[<RequireQualifiedAccess>]
module internal Store =

    val newStore : Store

[<Sealed>]
type internal Store with

    member Init : (unit -> Store)