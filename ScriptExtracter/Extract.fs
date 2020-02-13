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


