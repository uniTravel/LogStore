namespace LogStore.Messaging.Broker

type Store = Active of string

module Store =

    let q = 1

    let newStore : Store =
        failwith ""

    let init (Active ac) () : Store =
        failwith ""

type Store with
    member this.Init = Store.init this