module LogStore.Messaging.Consumer.Entry

open Hopac
open Logary
open Logary.Configuration
open Logary.Targets

[<EntryPoint>]
let main argv =
    Config.create "LogStore.Messaging.Consumer" "localhost"
    |> Config.targets [ LiterateConsole.create LiterateConsole.empty "console" ]
    |> Config.loggerMinLevel "LogStore.Messaging.Consumer" Verbose
    |> Config.processing (Events.events |> Events.sink ["console";])
    |> Config.build
    |> run |> ignore

    0