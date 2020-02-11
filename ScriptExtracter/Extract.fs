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

let removeRootPath (path: string) = 
    path
    |> Seq.findIndex (fun ch -> 
        ch = '.' || ch = Path.DirectorySeparatorChar)
    |> fun i -> path.[i + 1 ..]

let deleteMissing files dist = 
    match Directory.Exists(dist) with
    | true -> 
        files
        |> Seq.map(fun fPath -> getDotName fPath |> removeRootPath)
        |> Seq.except <| Seq.map(fun f -> removeRootPath f) (Directory.EnumerateFiles(dist))
        |> Seq.iter (fun fPath -> File.Delete(Path.Combine(dist, fPath)))
    | false -> 
        ()

let fixImports text = 
    let createImportPattern = sprintf """require\("./(?:../){0,}%s"\);?"""
    let matches = Regex.Matches(text, createImportPattern "(.+)")

    matches 
    |> Seq.fold (fun text m -> 
        let importPath = m.Groups.[1].Value
        let dotName = importPath |> getDotName
        let replacePattern = createImportPattern importPath

        Regex.Replace(text, replacePattern, sprintf "require(\"%s\")" dotName)) text

let transformFile dist filePath = async {
    let! text = File.ReadAllTextAsync(filePath) |> Async.AwaitTask
    let newPath = Path.Combine(dist, filePath |> getDotName |> removeRootPath) 

    if Directory.Exists(dist) |> not then
        Directory.CreateDirectory(dist) |> ignore

    let writeToFile (text: string) = async {
        use fileStream = new FileStream(newPath, FileMode.Create)
        use streamWriter = new StreamWriter(fileStream)
        streamWriter.AutoFlush <- true

        do! streamWriter.WriteAsync(text) |> Async.AwaitTask
    }

    let fixedText = text |> fixImports

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


