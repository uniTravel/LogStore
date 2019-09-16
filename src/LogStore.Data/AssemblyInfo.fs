namespace LogStore.Core.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("UniCloud.LogStore.Data")>]
[<assembly: AssemblyProductAttribute("UniCloud.LogStore.Data")>]
[<assembly: AssemblyCopyrightAttribute("Copyright 2019")>]
[<assembly: AssemblyDescriptionAttribute("Data handle library for LogStore")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
[<assembly: InternalsVisibleToAttribute("LogStore.Core.Tests")>]
[<assembly: InternalsVisibleToAttribute("LogStore.Transport.Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "UniCloud.LogStore.Data"
    let [<Literal>] AssemblyProduct = "UniCloud.LogStore.Data"
    let [<Literal>] AssemblyCopyright = "Copyright 2019"
    let [<Literal>] AssemblyDescription = "Data handle library for LogStore"
    let [<Literal>] AssemblyVersion = "0.0.1"
    let [<Literal>] AssemblyFileVersion = "0.0.1"