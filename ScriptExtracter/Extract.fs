﻿module Extract

open System.IO
open System.Text.RegularExpressions
open Util

type FileRecord = {
    sourceFullPath: string
    sourceRelativePath: string
    distFullPath: string
    distRelativePath: string
}

let deleteMissing fileMap jsDir dist = 
    let splitDotName = DirectoryInfo(jsDir).Name |> splitOnString '.'  

    if Directory.Exists(dist) then
        Directory.EnumerateFiles(dist)
        |> Seq.filter(fun f -> FileInfo(f).Extension = ".js")
        |> Seq.iter 
            (fun distFile -> 
                let distFile = FileInfo(distFile)
                let sourceFileExists = fileMap |> Seq.exists (fun (_, dotName) -> dotName |> splitDotName = distFile.Name)

                if sourceFileExists = false then
                    distFile.Delete()
            )

let fixImports (fileMappings: (string * string) seq) jsDir text = 
    let createImportPattern = sprintf """require\("./(?:../){0,}%s(\.js)?"\);?"""
    let matches = Regex.Matches(text, createImportPattern "(.+?)")

    let fixImportsFold text (regexMatch: Match) = 
        let nodeImport = regexMatch.Groups.[1].Value
        let replacePattern = createImportPattern nodeImport
        let jsDirName = DirectoryInfo(jsDir).Name

        let getImportDotName = 
            let importMap = 
                fileMappings
                |> Seq.tryFind 
                    (fun (path, _) -> Path.GetFileNameWithoutExtension(path) = FileInfo(nodeImport).Name)

            match importMap with 
            | Some (_, importDotName) -> 
                splitOnString '.' jsDirName importDotName |> Path.GetFileNameWithoutExtension
            | None -> 
                failwithf "Import not found for: %s" nodeImport

        let replacement = splitOnString '.' jsDirName getImportDotName |> sprintf "require(\"%s\")"  

        Regex.Replace(text, replacePattern, replacement)

    matches |> Seq.fold fixImportsFold text

let transformFile fileMappings jsDir dist filePath = async {
    let (filePath, dotName) = fileMappings |> Seq.find (fun map -> fst map = filePath) 
    let! text = File.ReadAllTextAsync(filePath) |> Async.AwaitTask
    let jsDirName = DirectoryInfo(jsDir).Name
    let newPath = Path.Combine(dist, dotName |> splitOnString '.' jsDirName)

    if Directory.Exists(dist) |> not then
        Directory.CreateDirectory(dist) |> ignore

    let writeToFile (text: string) = async {
        use fileStream = new FileStream(newPath, FileMode.Create)
        use streamWriter = new StreamWriter(fileStream)
        streamWriter.AutoFlush <- true

        do! streamWriter.WriteAsync(text) |> Async.AwaitTask
    }

    let fixedText = text |> fixImports fileMappings jsDir

    match File.Exists(newPath) with
    | true ->
        let! fileText = File.ReadAllTextAsync(newPath) |> Async.AwaitTask 

        match fileText = fixedText with
        | true -> ()
        | false ->
            writeToFile fixedText |> Async.RunSynchronously
    | false ->
            writeToFile fixedText |> Async.RunSynchronously
}


