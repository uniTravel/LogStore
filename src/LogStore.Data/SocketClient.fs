namespace LogStore.Data

open System.IO

module SocketClient =

    let write (writeTo: BinaryWriter -> unit) (bw: BinaryWriter) : unit =
        bw.BaseStream.SetLength 4L
        bw.BaseStream.Position <- 4L
        writeTo bw
        let length = int bw.BaseStream.Length - 4
        bw.Write length
        bw.BaseStream.Position <- 0L
        bw.Write length