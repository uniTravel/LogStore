namespace LogStore.Core.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("UniCloud.LogStore.Messaging.Broker")>]
[<assembly: AssemblyProductAttribute("UniCloud.LogStore.Messaging.Broker")>]
[<assembly: AssemblyCopyrightAttribute("Copyright 2019")>]
[<assembly: AssemblyDescriptionAttribute("Messaging broker for LogStore")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
[<assembly: InternalsVisibleToAttribute("LogStore.Messaging.Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "UniCloud.LogStore.Messaging.Broker"
    let [<Literal>] AssemblyProduct = "UniCloud.LogStore.Messaging.Broker"
    let [<Literal>] AssemblyCopyright = "Copyright 2019"
    let [<Literal>] AssemblyDescription = "Messaging broker for LogStore"
    let [<Literal>] AssemblyVersion = "0.0.1"
    let [<Literal>] AssemblyFileVersion = "0.0.1"
