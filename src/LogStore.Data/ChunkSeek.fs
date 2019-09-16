namespace LogStore.Data

open System.IO

module ChunkSeek =

    let rec freeSeek (br: BinaryReader) toWrite =
        let length = br.ReadInt32 ()
        match length with
        | l when l <= 0 -> toWrite
        | l ->
            br.ReadBytes length |> ignore
            let suffixLength = br.ReadInt32 ()
            match suffixLength with
            | s when s <> l -> toWrite
            | _ -> freeSeek br <| toWrite + length + 2 * sizeof<int>

    let rec fixedSeek (fixedLength: int) (br: BinaryReader) toWrite =
        br.ReadBytes fixedLength |> ignore
        let length = br.ReadInt32 ()
        match length with
        | l when l = fixedLength -> fixedSeek fixedLength br <| toWrite + fixedLength + sizeof<int>
        | _ -> toWrite