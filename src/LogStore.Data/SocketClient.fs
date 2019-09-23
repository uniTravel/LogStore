namespace LogStore.Data

open System.IO

module SocketClient =

    let write (data: byte[]) (bw: BinaryWriter) : unit =
        bw.Write data.Length
        bw.Write data
        bw.Write data.Length