namespace LogStore.Common.Utils

type internal Channel<'res> = AsyncReplyChannel<Result<'res, string>>

type internal Pool<'res>

[<RequireQualifiedAccess>]
module internal ResourcePool =
    val newPool : Pool<'res>

[<Sealed>]
type internal Pool<'res> with
    member Init : ('res list -> Pool<'res>)
    member Enqueue : ('res -> float -> Pool<'res>)
    member Dequeue : (float -> Channel<'res> -> Pool<'res>)
    member Decrease : (Channel<'res> -> Pool<'res>)