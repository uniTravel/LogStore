namespace LogStore.Common.Utils

open System

type Channel<'res> = AsyncReplyChannel<Result<'res, string>>

type PendingItem<'res> = Channel<'res> * DateTime

type Empty<'res> = NoItems

type Active<'res> = {
    _front: 'res list
    _rear: 'res list
}

type Pending<'res> = {
    _front: PendingItem<'res> list
    _rear: PendingItem<'res> list
}

type Pool<'res> =
    | Initial of Empty<'res>
    | Empty of Empty<'res>
    | Active of Active<'res>
    | Pending of Pending<'res>
    | Blocked of Pending<'res>

module ResourcePool =

    let okState<'res> res (state: Pool<'res>) (channel: Channel<'res>) =
        channel.Reply <| Ok res
        state

    let errorState<'res> msg (state: Pool<'res>) (channel: Channel<'res>) =
        channel.Reply <| Error msg
        state

    let blockQueue<'res> (state: Pending<'res>) (channel: Channel<'res>) =
        channel.Reply <| Error "等待队列已超时。"
        Blocked state

    //#region 资源池操作

    let initQueue<'res> (items: 'res list) =
        Active { _front = items; _rear = [] }

    let enqueueEmpty<'res> (item: 'res) =
        Active { _front = [ item ]; _rear = [] }

    let dequeueEmpty<'res> (channel: Channel<'res>) =
        Pending { _front = [ channel, DateTime.Now ]; _rear = [] }

    let enqueueActive<'res> (state: Active<'res>) (item: 'res) =
        Active { state with _rear = item :: state._rear }

    let dequeueActive<'res> (state: Active<'res>) =
        match state with
        | { _front = head :: tail; _rear = [] } ->
            match tail with
            | [] -> Empty NoItems
            | _ -> Active { state with _front = tail }
            |> okState head
        | { _front = head :: tail; _rear = rear } ->
            match tail with
            | [] -> Active { _front = rear |> List.rev; _rear = [] }
            | _ -> Active { state with _front = tail }
            |> okState head
        | { _front = []; _rear = _ } -> errorState "Active状态下，Front集合不能为空。" <| Active state

    let enqueuePending<'res> (state: Pending<'res>) (item: 'res) timeout =
        match state with
        | { _front = head :: tail; _rear = [] } ->
            (fst head).Reply <| Ok item
            match tail with
            | [] -> Empty NoItems
            | tail when (DateTime.Now - (snd tail.Head)).TotalSeconds > timeout ->
                Blocked { state with _front = tail }
            | _ -> Pending { state with _front = tail }
        | { _front = head :: tail; _rear = rear } ->
            (fst head).Reply <| Ok item
            match tail, rear.Head with
            | [], r when (DateTime.Now - (snd r)).TotalSeconds > timeout ->
                Blocked { _front = rear |> List.rev; _rear = [] }
            | [], _ -> Pending { _front = rear |> List.rev; _rear = [] }
            | _ -> Pending { state with _front = tail }
        | { _front = []; _rear = _ } -> failwith "Pending状态下，Front集合不能为空。"

    let dequeuePending<'res> (state: Pending<'res>) timeout (channel: Channel<'res>) =
        match state._front.Head with
        | (_, t) when (DateTime.Now - t).TotalSeconds > timeout -> blockQueue state channel
        | _ -> Pending { state with _rear = (channel, DateTime.Now) :: state._rear }

    //#endregion

    //#region 根据状态控制

    type Empty<'res> with
        member __.InitQueue = initQueue<'res>
        member __.Enqueue = enqueueEmpty<'res>
        member __.Dequeue = dequeueEmpty<'res>

    type Active<'res> with
        member this.Enqueue = enqueueActive<'res> this
        member this.Dequeue = dequeueActive<'res> this
        member this.Decrease = dequeueActive<'res> this

    type Pending<'res> with
        member this.Enqueue = enqueuePending<'res> this
        member this.Dequeue = dequeuePending<'res> this
        member this.BlockQueue = blockQueue<'res> this

    let init<'res> (pool: Pool<'res>) (items: 'res list) =
        match pool with
        | Initial state -> state.InitQueue items
        | _ -> invalidOp "只有初始状态才能执行初始化操作。"

    let enqueue<'res> (pool: Pool<'res>) (item: 'res) timeout =
        match pool with
        | Active state -> state.Enqueue item
        | Pending state -> state.Enqueue item timeout
        | Empty state -> state.Enqueue item
        | Blocked state -> state.Enqueue item timeout
        | Initial _ -> invalidOp "初始状态只能执行初始化操作。"

    let dequeue<'res> (pool: Pool<'res>) timeout =
        match pool with
        | Active state -> state.Dequeue
        | Pending state -> state.Dequeue timeout
        | Empty state -> state.Dequeue
        | Blocked state -> state.BlockQueue
        | Initial state -> errorState "初始状态只能执行初始化操作。" <| Initial state

    let decrease<'res> (pool: Pool<'res>) =
        match pool with
        | Active state -> state.Decrease
        | state -> errorState "只有活动状态才能执行缩容操作。" state

    //#endregion

    let newPool : Pool<'res> = Initial NoItems

type Pool<'res> with
    member this.Init = ResourcePool.init this
    member this.Enqueue = ResourcePool.enqueue this
    member this.Dequeue = ResourcePool.dequeue this
    member this.Decrease = ResourcePool.decrease this