namespace LogStore.Core.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("UniCloud.LogStore.Core")>]
[<assembly: AssemblyProductAttribute("UniCloud.LogStore.Core")>]
[<assembly: AssemblyCopyrightAttribute("Copyright 2019")>]
[<assembly: AssemblyDescriptionAttribute("Core library for LogStore")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
[<assembly: InternalsVisibleToAttribute("LogStore.Core.Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "UniCloud.LogStore.Core"
    let [<Literal>] AssemblyProduct = "UniCloud.LogStore.Core"
    let [<Literal>] AssemblyCopyright = "Copyright 2019"
    let [<Literal>] AssemblyDescription = "Core library for LogStore"
    let [<Literal>] AssemblyVersion = "0.0.1"
    let [<Literal>] AssemblyFileVersion = "0.0.1"
    let [<Literal>] InternalsVisibleTo = "LogStore.Core.Tests"