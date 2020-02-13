﻿module Util
open System.Text.RegularExpressions
open System.IO
open Types
open System.IO.Abstractions

let getDotName filePath = Regex.Replace(filePath, @"[/\\]", ".")

let splitOnString (separator: char) (stopString: string) (input: string) = 
    input.Split(separator) 
    |> Array.rev
    |> Array.takeWhile (fun str -> str <> stopString)
    |> Array.rev
    |> String.concat (string separator)

/// Map files to FileRecord
let mapFiles sourcePath targetPath filePaths = 
    filePaths 
    |> Seq.map 
        (fun filePath -> 
            let sourceInfo = DirectoryInfo(sourcePath)
            let targetInfo = DirectoryInfo(targetPath)

            let fInfo = FileInfo(filePath)
            let getDotName = 
                splitOnString 
                <| '.' 
                <| sourceInfo.Name
                <| getDotName fInfo.FullName 

            { 
                sourceName = fInfo.Name
                sourceFullPath = fInfo.FullName
                dotName = getDotName 
                dotFullPath = Path.Combine(targetInfo.FullName, getDotName)
            }
        )

let rec getFiles (fs: IFileSystem) basePath = 
    let rec getFilesExec dirPaths = 
        if Seq.isEmpty dirPaths then Seq.empty else
            seq { yield! dirPaths |> Seq.collect fs.Directory.EnumerateFiles
                  yield! dirPaths |> Seq.collect fs.Directory.EnumerateDirectories |> getFilesExec }

    getFilesExec [basePath]


