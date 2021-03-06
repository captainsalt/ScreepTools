﻿module Extract

open System.Text.RegularExpressions
open System.IO.Abstractions
open Types

let deleteMissing (fs: IFileSystem) (fileRecords: FileRecord seq) targetPath = 
    if fs.Directory.Exists(targetPath) then
        fs.Directory.EnumerateFiles(targetPath)
        |> Seq.filter(fun path -> fs.FileInfo.FromFileName(path).Extension = ".js")
        |> Seq.iter 
            (fun filePath -> 
                fileRecords 
                |> Seq.tryFind (fun record -> record.dotFullPath = filePath)
                |> fun record -> 
                    if record.IsNone then 
                        fs.File.Delete(filePath)
            )

let fixImports (fs: IFileSystem) (fileRecords: FileRecord seq) filePath = async {
    let! fileText = fs.File.ReadAllTextAsync(filePath) |> Async.AwaitTask
    let importPattern = """require\(['"]
                        (?<import>./(../){0,}.+?)
                        (\.js)?['"]\);?"""

    let regexOptions = enum 36 // ignore whitespace and explicit capture
    let matches = Regex.Matches(fileText, importPattern, regexOptions)

    let fixedText = 
        matches 
        |> Seq.fold 
            (fun text regexMatch -> 
                let nodeImport = regexMatch.Groups.["import"].Value

                let importReplacement = 
                    let getImportRecord = 
                        fileRecords
                        |> Seq.tryFind 
                            (fun fRecord -> 
                                let importFullPath = 
                                    let fileDirectory = fs.Path.GetDirectoryName(filePath)
                                    let importRelativePath = fs.Path.Combine(fileDirectory, nodeImport + ".js")
                                    fs.Path.GetFullPath(importRelativePath)

                                fRecord.sourceFullPath = importFullPath
                            )

                    match getImportRecord with 
                    | Some record -> 
                        record.dotName
                        |> fs.Path.GetFileNameWithoutExtension
                    | None -> 
                        failwithf "Import %s not found in %s" nodeImport filePath
                
                let replacePattern = sprintf """require\("%s(\.js)?"\);?""" nodeImport
                let replacement = importReplacement |> sprintf "require(\"%s\")"  

                Regex.Replace(text, replacePattern, replacement)
            ) fileText

    return (fixedText, fileText)
}

/// Extracts the sourceFile to the target path
let extractFile (fs: IFileSystem) (fileRecords: FileRecord seq) targetPath sourceFilePath = async {
    let newFilePath = 
        fileRecords 
        |> Seq.find (fun record -> record.sourceFullPath = sourceFilePath) 
        |> fun record -> record.dotFullPath

    if fs.Directory.Exists(targetPath) |> not then
        fs.Directory.CreateDirectory(targetPath) |> ignore

    let! (fixedText, oldText) = fixImports fs fileRecords sourceFilePath

    if fs.File.Exists(newFilePath) then
        if oldText = fixedText then 
            ()
        else
            do! fs.File.WriteAllTextAsync(newFilePath, fixedText) |> Async.AwaitTask
    else
        do! fs.File.WriteAllTextAsync(newFilePath, fixedText) |> Async.AwaitTask
}

