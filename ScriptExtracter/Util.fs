module Util
open System.Text.RegularExpressions
open System.IO

let getDotName filePath = Regex.Replace(filePath, @"[/\\]", ".")

/// Map files to dotname
let mapFiles files = files |> Seq.map (fun filePath -> (filePath , getDotName filePath))

let rec getFiles basePath = 
    let rec getFilesExec dirPaths = 
        if Seq.isEmpty dirPaths then Seq.empty else
            seq { yield! dirPaths |> Seq.collect Directory.EnumerateFiles
                  yield! dirPaths |> Seq.collect Directory.EnumerateDirectories |> getFilesExec }

    getFilesExec [basePath]

let splitOnString (separator: char) (stopString: string) (input: string) = 
    input.Split(separator) 
    |> Array.rev
    |> Array.takeWhile (fun str -> str <> stopString)
    |> Array.rev
    |> String.concat (string separator)
