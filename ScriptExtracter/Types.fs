module Types

open System.IO.Abstractions
open System.IO

type FileRecord = {
    sourceName: string
    sourceFullPath: string
    dotName: string
    dotFullPath: string
}

type IFileOps = 
    abstract WriteToFile: path: string -> text: string -> Async<unit>
    abstract DeleteFile: path: string -> unit

type FileOps() = 
    interface IFileOps with
        member this.WriteToFile path text = async {
            do! File.WriteAllTextAsync(path, text) |> Async.AwaitTask
        }

        member this.DeleteFile path = 
            File.Delete(path)
