namespace LogStore.Common.Utils

type QueueMessage<'res> =
    | Init of 'res list
    | Enqueue of 'res
    | Dequeue of Channel<'res>
    | Decrease of Channel<'res>
    | Close

[<Sealed>]
type PoolAgent<'res>(res: 'res list, blockSeconds) =

    let mutable adjustedSize = 0

    let agent =
        MailboxProcessor<QueueMessage<'res>>.Start <| fun inbox ->
            let rec loop (oldDB: Pool<'res>) =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Enqueue item ->
                        let newDB = oldDB.Enqueue item blockSeconds
                        return! loop newDB
                    | Dequeue channel ->
                        let newDB = oldDB.Dequeue blockSeconds channel
                        return! loop newDB
                    | Init items ->
                        let newDB = oldDB.Init items
                        return! loop newDB
                    | Decrease channel ->
                        let newDB = oldDB.Decrease channel
                        return! loop newDB
                    | Close -> return ()
                }
            loop ResourcePool.newPool

    let decrease size =
        [ 1 .. size ]
        |> List.iter (fun _ ->
            match agent.PostAndReply <| Decrease with
            | Ok _ -> adjustedSize <- adjustedSize - 1
            | Error res -> invalidOp res
        )
        adjustedSize

    do agent.Post <| Init res

    member __.MinSize = res.Length

    member __.AdjustedSize = adjustedSize

    member __.Action f =
        match agent.PostAndReply <| Dequeue with
        | Ok res ->
            f res
            agent.Post <| Enqueue res
        | Error res -> invalidOp res

    member __.AsyncAction f =
        async {
            match! agent.PostAndAsyncReply <| Dequeue with
            | Ok res ->
                f res
                agent.Post <| Enqueue res
                return ()
            | Error res -> invalidOp res
        }

    member __.Func f =
        match agent.PostAndReply <| Dequeue with
        | Ok res ->
            let result = f res
            agent.Post <| Enqueue res
            result
        | Error res -> invalidOp res

    member __.AsyncFunc f =
        async {
            match! agent.PostAndAsyncReply <| Dequeue with
            | Ok res ->
                let result = f res
                agent.Post <| Enqueue res
                return result
            | Error res -> return invalidOp res
        }

    member __.IncreaseSize res =
        match res with
        | [] -> invalidArg "res" "参数长度必须大于零。"
        | _ ->
            res
            |> List.iter (agent.Post << Enqueue)
            adjustedSize <- adjustedSize + res.Length
            adjustedSize

    member __.DecreaseSize size =
        match size with
        | s when s <= 0 -> invalidArg "size" "参数必须大于零。"
        | _ ->
            match adjustedSize with
            | 0 -> invalidOp "可缩小的容量为零。"
            | a when a < size -> decrease a
            | _ -> decrease size

    member __.Close release =
        match release with
        | Some (f: 'res -> unit) ->
            [ 1 .. res.Length + adjustedSize ]
            |> List.iter (fun _ ->
                match agent.PostAndReply <| Dequeue with
                | Ok res -> f res
                | Error res -> invalidOp res
            )
        | None -> ()
        agent.Post Close