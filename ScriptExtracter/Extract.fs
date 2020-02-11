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
    let newPath = 
        let path = Path.Combine(dist, filePath |> getDotName)
        let index = path |> Seq.findIndex (fun ch -> ch = '.')
        let pathSubStr = path.[index + 1..]
        Path.Combine(dist, pathSubStr)

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


