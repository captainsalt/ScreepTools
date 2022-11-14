module Extract

open System.Text.RegularExpressions
open System.IO.Abstractions
open Util
open Types

let deleteMissing (fs: IFileSystem) (fileRecords: FileRecord seq) targetPath = 
    if fs.Directory.Exists(targetPath) then
        fs.Directory.EnumerateFiles(targetPath)
        |> Seq.filter (fun path -> fs.FileInfo.FromFileName(path).Extension = ".js")
        |> Seq.iter 
            (fun filePath -> 
                fileRecords 
                |> Seq.tryFind (fun record -> record.dotFullPath = filePath)
                |> fun record -> 
                    if record.IsNone then 
                        fs.File.Delete(filePath)
            )

let replaceImports (fs: IFileSystem) (fileRecords: FileRecord seq) filePath = async {
    let! originalText = fs.File.ReadAllTextAsync(filePath) |> Async.AwaitTask
    let regexOptions = enum 36 // ignore whitespace and explicit capture
    let importPattern = """require\(['"]
                        (?<import>./(../){0,}.+?)
                        (\.js)?['"]\);?"""

    let matchedImports = Regex.Matches(originalText, importPattern, regexOptions)

    let updatedText = 
        matchedImports 
        |> Seq.fold 
            (fun text regexMatch -> 
                let nodeImport = regexMatch.Groups.["import"].Value
                let matchingRecord = getMatchingFileRecord fs fileRecords filePath nodeImport
                
                match matchingRecord with 
                | Some record ->
                    let replacePattern =  $"""require\("{nodeImport}(\.js)?"\);?""" 
                    let replacement =  $"""require("{record.dotName |> fs.Path.GetFileNameWithoutExtension}")"""
                    Regex.Replace(text, replacePattern, replacement)
                | None -> 
                    failwith $"Import {nodeImport} not found in {filePath}"
            ) originalText

    return (updatedText, originalText)
}

/// Extracts the sourceFile to the target path
let extractFile (fs: IFileSystem) (fileRecords: FileRecord seq) targetPath sourceFilePath = async {
    let newFilePath = 
        fileRecords 
        |> Seq.find (fun record -> record.sourceFullPath = sourceFilePath) 
        |> fun record -> record.dotFullPath

    if fs.Directory.Exists(targetPath) |> not then
        fs.Directory.CreateDirectory(targetPath) |> ignore

    let! (updatedText, originalText) = replaceImports fs fileRecords sourceFilePath

    if updatedText = originalText then 
        ()
    else 
        do! fs.File.WriteAllTextAsync(newFilePath, updatedText) |> Async.AwaitTask
}
