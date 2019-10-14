module LogStore.Messaging.Producer.Entry

open Hopac
open Logary
open Logary.Configuration
open Logary.Targets

[<EntryPoint>]
let main argv =
    Config.create "LogStore.Messaging.Producer" "localhost"
    |> Config.targets [ LiterateConsole.create LiterateConsole.empty "console" ]
    |> Config.loggerMinLevel "LogStore.Messaging.Producer" Verbose
    |> Config.processing (Events.events |> Events.sink ["console";])
    |> Config.build
    |> run |> ignore

    0