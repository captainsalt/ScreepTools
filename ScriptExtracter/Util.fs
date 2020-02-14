module Util
open System.Text.RegularExpressions
open System.IO
open Types
open System.IO.Abstractions

let getDotName filePath = Regex.Replace(filePath, @"[/\\]", ".")

let generateFileRecords (fs: IFileSystem) sourcePath targetPath filePaths = 
    filePaths 
    |> Seq.map 
        (fun sourceFile -> 
            let targetInfo = fs.DirectoryInfo.FromDirectoryName(targetPath)

            let sourceInfo = fs.FileInfo.FromFileName(sourceFile)
            let getDotName = 
                fs.Path.GetRelativePath(sourcePath, sourceFile)
                |> getDotName

            { 
                sourceName = sourceInfo.Name
                sourceFullPath = sourceInfo.FullName
                dotName = getDotName 
                dotFullPath = Path.Combine(targetInfo.FullName, getDotName)
            }
        )

let rec getSourceFiles (fs: IFileSystem) basePath = 
    let rec getFilesExec dirPaths = 
        if Seq.isEmpty dirPaths then Seq.empty else
            seq { yield! dirPaths |> Seq.collect fs.Directory.EnumerateFiles
                  yield! dirPaths |> Seq.collect fs.Directory.EnumerateDirectories |> getFilesExec }

    getFilesExec [basePath]


