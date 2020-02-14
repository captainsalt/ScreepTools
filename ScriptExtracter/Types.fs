module Types

open System.IO.Abstractions
open System.IO

type FileRecord = {
    sourceName: string
    sourceFullPath: string
    dotName: string
    dotFullPath: string
}
