namespace LogStore.Core

open System.IO

type DB = Active of WorkArea

module ChunkDB =

    let chunksError (chunks: Chunk list) (reason: string) =
        chunks |> List.iter Chunk.close
        Error reason

    let rec initReaders (ra: (int * Reader) array) (rl: Reader list) (num: int) =
        match rl with
        | [] -> ra
        | head :: tail ->
            ra.[num % ra.Length] <- num, head
            initReaders ra tail (num - 1)

    //#region 验证Chunk文件库

    let validateDirectory (path: string) =
        match Directory.Exists path with
        | false -> Error <| sprintf "文件夹%s不存在。" path
        | _ -> Ok path

    let validatePath (path: string) =
        let files = Directory.EnumerateFiles (path) |> List.ofSeq
        match files with
        | [] -> Error "文件夹为空。"
        | _ -> Ok files

    let validateName (cfg: ChunkConfig) (files: string list) =
        let vfs = List.sortDescending files |> Chunk.validateName cfg
        match files, vfs with
        | _ when files.Length = vfs.Length -> Ok <| List.truncate cfg.CacheSize vfs
        | _ -> Error "Chunk文件的命名模式不一致。"

    let validateIndex (cfg: ChunkConfig) (files: ValidFileName list) =
        let chunks = files |> List.map Chunk.fromFile
        let idxs = chunks |> List.map (fun elem -> elem.Header.ChunkNumber)
        let compare vfn idx = Chunk.compare cfg vfn idx
        let continuous (l: int list) = List.head l = List.last l + l.Length - 1
        match files, idxs with
        | f, i when List.forall2 compare f i && continuous i -> Ok chunks
        | _ -> chunksError chunks "工作Chunk集合中的ChunkNumber有误。"

    let validateState (chunks: Chunk list) =
        match chunks with
        | h :: t when not h.Footer.IsCompleted && t |> List.forall (fun e -> e.Footer.IsCompleted) -> Ok chunks
        | _ -> chunksError chunks "Chunk文件的完成状态有误。"

    let validateChecksum (chunks: Chunk list) =
        match chunks.Tail with
        | cs when cs |> List.forall Chunk.validateChecksum -> Ok chunks
        | _ -> chunksError chunks "已完成的文件Checksum校验不正确。"

    //#endregion

    let init (cfg: ChunkConfig) : unit =
        Chunk.create cfg

    let buildWorkArea (cfg: ChunkConfig) (chunks: Chunk list) =
        let rs = chunks.Tail |> List.map (Chunk.buildReader cfg)
        let (num, pos, w, r) = Chunk.buildWriter cfg chunks.Head
        let ra = Array.zeroCreate chunks.Length
        Ok (pos, Active { ChunkNum = num; Writer = w; Readers = initReaders ra (r :: rs) num })

    let openDB (cfg: ChunkConfig) : int64 * DB =
        let result =
            Ok cfg.Path
            |> Result.bind validateDirectory
            |> Result.bind validatePath
            |> Result.bind (validateName cfg)
            |> Result.bind (validateIndex cfg)
            |> Result.bind validateState
            |> Result.bind validateChecksum
            |> Result.bind (buildWorkArea cfg)
        match result with
        | Ok r -> r
        | Error e -> failwith e

    let closeDB (Active db) () =
        db.Writer |> Chunk.closeWriter
        db.Readers |> Array.unzip |> snd |> Array.iter Chunk.closeReader

    let complete (Active db) (cfg: ChunkConfig) : Reader * DB =
        let (reader, workarea) = Chunk.complete cfg db
        reader, Active workarea

    let getChunkNumber (Active db) = db.ChunkNum

    let getWriter (Active db) = db.Writer

    let getReaders (Active db) = db.ChunkNum, db.Readers

type DB with
    member this.Close = ChunkDB.closeDB this
    member this.Complete = ChunkDB.complete this
    member this.ChunkNumber = ChunkDB.getChunkNumber this
    member this.Writer = ChunkDB.getWriter this
    member this.Readers = ChunkDB.getReaders this