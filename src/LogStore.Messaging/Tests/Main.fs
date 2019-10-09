module LogStore.Transport.Tests

open Expecto
open Hopac
open Logary
open Logary.Configuration
open Logary.Targets

[<EntryPoint>]
let main argv =
    Config.create "LogStore.Messaging" "localhost"
    |> Config.targets [ LiterateConsole.create LiterateConsole.empty "console" ]
    |> Config.loggerMinLevel "LogStore.Messaging" Verbose
    |> Config.processing (Events.events |> Events.sink ["console";])
    |> Config.build
    |> run
    |> ignore

    runTestsInAssembly defaultConfig argv