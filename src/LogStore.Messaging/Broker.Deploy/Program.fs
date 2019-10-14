module LogStore.Messaging.Broker.Entry

open Hopac
open Logary
open Logary.Configuration
open Logary.Targets

[<EntryPoint>]
let main argv =
    Config.create "LogStore.Messaging.Broker" "localhost"
    |> Config.targets [ LiterateConsole.create LiterateConsole.empty "console" ]
    |> Config.loggerMinLevel "LogStore.Messaging.Broker" Verbose
    |> Config.processing (Events.events |> Events.sink ["console";])
    |> Config.build
    |> run |> ignore

    0