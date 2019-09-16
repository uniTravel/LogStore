namespace LogStore.Common.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("UniCloud.LogStore.Common")>]
[<assembly: AssemblyProductAttribute("UniCloud.LogStore.Common")>]
[<assembly: AssemblyCopyrightAttribute("Copyright 2019")>]
[<assembly: AssemblyDescriptionAttribute("Common library for LogStore")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
[<assembly: InternalsVisibleToAttribute("LogStore.Common.Tests")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "UniCloud.LogStore.Common"
    let [<Literal>] AssemblyProduct = "UniCloud.LogStore.Common"
    let [<Literal>] AssemblyCopyright = "Copyright 2019"
    let [<Literal>] AssemblyDescription = "Common library for LogStore"
    let [<Literal>] AssemblyVersion = "0.0.1"
    let [<Literal>] AssemblyFileVersion = "0.0.1"