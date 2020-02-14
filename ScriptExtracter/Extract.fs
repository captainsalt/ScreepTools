module Extract

open System.IO
open System.Text.RegularExpressions
open Util
open System.IO.Abstractions
open Types

let deleteMissing (fs: IFileSystem) (fileRecords: FileRecord seq) targetPath = 
    if fs.Directory.Exists(targetPath) then
        fs.Directory.EnumerateFiles(targetPath)
        |> Seq.filter(fun path -> fs.FileInfo.FromFileName(path).Extension = ".js")
        |> Seq.iter 
            (fun distFile -> 
                let distFile = fs.FileInfo.FromFileName(distFile)
                let sourceFileExists = fileRecords |> Seq.exists (fun record -> record.dotName = distFile.Name)

                if sourceFileExists = false then
                    fs.File.Delete(distFile.FullName)
            )

let fixImports (fs: IFileSystem) (fileRecords: FileRecord seq) text = 
    let createImportPattern = sprintf """require\("./(?<traversal>(?:../){0,})%s(\.js)?"\);?"""
    let matches = Regex.Matches(text, createImportPattern "(?<import>.+?)")

    let fixImportsFold text (regexMatch: Match) = 
        let nodeImport = regexMatch.Groups.["import"].Value
        let replacePattern = createImportPattern nodeImport

        let importRecord = 
            let getImportRecord = 
                fileRecords
                |> Seq.tryFind 
                    (fun fRecord -> 
                        fs.Path.GetFileNameWithoutExtension(fRecord.dotName) = fs.FileInfo.FromFileName(nodeImport).Name)

            match getImportRecord with 
            | Some record -> 
                record
            | None -> 
                failwithf "Import not found for: %s" nodeImport

        let replacement = importRecord.dotName |> sprintf "require(\"%s\")"  

        Regex.Replace(text, replacePattern, replacement)

    matches |> Seq.fold fixImportsFold text

/// Extracts the sourceFile to the target path
let extractFile (fs: IFileSystem) (fileRecords: FileRecord seq) targetPath sourceFile = async {
    let! fileText = File.ReadAllTextAsync(sourceFile) |> Async.AwaitTask
    let newFilePath = 
        fileRecords 
        |> Seq.find (fun record -> record.sourceFullPath = sourceFile) 
        |> fun record -> record.dotFullPath

    if fs.Directory.Exists(targetPath) |> not then
        Directory.CreateDirectory(targetPath) |> ignore

    let fixedText = fixImports fs fileRecords fileText

    match File.Exists(newFilePath) with
    | true ->
        match fileText = fixedText with
        | true -> ()
        | false ->
            do! File.WriteAllTextAsync(newFilePath, fixedText) |> Async.AwaitTask
    | false ->
            do! File.WriteAllTextAsync(newFilePath, fixedText) |> Async.AwaitTask
}

