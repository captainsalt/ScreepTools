module Extract

open System.IO
open System.Text.RegularExpressions

let rec getFiles basePath = 
    let rec getFilesExec dirPaths = 
        if Seq.isEmpty dirPaths then Seq.empty else
            seq { yield! dirPaths |> Seq.collect Directory.EnumerateFiles
                  yield! dirPaths |> Seq.collect Directory.EnumerateDirectories |> getFilesExec }

    getFilesExec [basePath]
           
let getDotName filePath = Regex.Replace(filePath, @"[/\\]", ".")

/// Map files to dotname
let mapFiles files = files |> Seq.map (fun filePath -> (filePath , getDotName filePath))

let splitOnString (separator: char) (stopString: string) (input: string) = 
    input.Split(separator) 
    |> Array.rev
    |> Array.takeWhile (fun str -> str <> stopString)
    |> Array.rev
    |> String.concat (string separator)

let deleteMissing fileMap jsDir dist = 
    let splitDotName = DirectoryInfo(jsDir).Name |> splitOnString '.'  

    if Directory.Exists(dist) then
        Directory.EnumerateFiles(dist)
        |> Seq.iter 
            (fun distFile -> 
                let distFile = FileInfo(distFile)
                let sourceFileExists = fileMap |> Seq.exists (fun (_, dotName) -> dotName |> splitDotName = distFile.Name)

                if sourceFileExists = false then
                    distFile.Delete()
            )

let fixImports fileMappings jsDir text = 
    let createImportPattern = sprintf """require\("./(?:../){0,}%s(\.js)?"\);?"""
    let matches = Regex.Matches(text, createImportPattern "(.+?)")

    let fixImportsFold text (regexMatch: Match) = 
        let nodeImport = regexMatch.Groups.[1].Value
        let replacePattern = createImportPattern nodeImport
        let jsDirName = DirectoryInfo(jsDir).Name

        let (_, importDotName) = 
            fileMappings 
            |> Seq.find 
                (fun (path: string, _) -> Path.GetFileNameWithoutExtension(path) = FileInfo(nodeImport).Name)

        let replacement = splitOnString '.' jsDirName importDotName |> sprintf "require(\"%s\")"  

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


