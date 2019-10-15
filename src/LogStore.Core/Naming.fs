namespace LogStore.Core

open System.IO
open System.Text.RegularExpressions

type ValidFileName = ValidFileName of string

module Naming =

    let fileName (cfg: ChunkConfig) (index: int) =
        Path.Combine (cfg.Path, sprintf "%s\\%0*i.%s" cfg.Folder cfg.Length index cfg.Suffix)

    let validateName (cfg: ChunkConfig) (input: string list) =
        let pattern = sprintf "^\d{%i}\.%s$" cfg.Length cfg.Suffix |> Regex
        let select (elem: string) =
            match elem with
            | name when pattern.IsMatch (Path.GetFileName name) -> Some <| ValidFileName name
            | _ -> None
        input |> List.choose select