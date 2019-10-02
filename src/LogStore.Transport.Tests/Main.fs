module LogStore.Transport.Tests

open Expecto
open Hopac
open Logary
open Logary.Configuration
open Logary.Targets

[<EntryPoint>]
let main args =
    Config.create "LogStore.Transport" "localhost"
    |> Config.targets [ LiterateConsole.create LiterateConsole.empty "console" ]
    |> Config.loggerMinLevel "LogStore.Transport" Verbose
    |> Config.loggerMinLevel "LogStore.Data" Verbose
    |> Config.processing (Events.events |> Events.sink ["console";])
    |> Config.build
    |> run |> ignore

    runTestsInAssembly defaultConfig args