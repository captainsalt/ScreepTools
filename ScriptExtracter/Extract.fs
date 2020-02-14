module Extract

open System.Text.RegularExpressions
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

let fixImports (fs: IFileSystem) (fileRecords: FileRecord seq) filePath = async {
    let! fileText = fs.File.ReadAllTextAsync(filePath) |> Async.AwaitTask
    let createImportPattern = sprintf """require\("./(?<traversal>(?:../){0,})%s(\.js)?"\);?"""
    let matches = Regex.Matches(fileText, createImportPattern "(?<import>.+?)")

    let fixedText = 
        matches 
        |> Seq.fold 
            (fun text regexMatch -> 
                let nodeImport = regexMatch.Groups.["import"].Value
                let replacePattern = createImportPattern nodeImport

                let importRecord = 
                    let getImportRecord = 
                        fileRecords
                        |> Seq.tryFind 
                            (fun fRecord -> 
                                let recordFileName = fs.Path.GetFileNameWithoutExtension(fRecord.sourceName) 
                                let nodeImportName = fs.FileInfo.FromFileName(nodeImport).Name
                                recordFileName = nodeImportName
                            )

                    match getImportRecord with 
                    | Some record -> 
                        record
                    | None -> 
                        failwithf "Import not found for: %s" nodeImport

                let replacement = importRecord.dotName |> sprintf "require(\"%s\")"  

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

    let (fixedText, oldText) = fixImports fs fileRecords sourceFilePath |> Async.RunSynchronously

    if fs.File.Exists(newFilePath) then
        if oldText = fixedText then 
            ()
        else
            do! fs.File.WriteAllTextAsync(newFilePath, fixedText) |> Async.AwaitTask
    else
        do! fs.File.WriteAllTextAsync(newFilePath, fixedText) |> Async.AwaitTask
}

