module Program
open Extract
open System.IO

[<EntryPoint>]
let main args = 
    match args with
    | [| jsDir; dist |] ->
        if Directory.Exists(jsDir) |> not then
            failwithf "Root directory: %s does not exist" jsDir

        let jsFiles = getFiles jsDir
        let fileMap = jsFiles |> mapFiles

        jsFiles
        |> mapFiles
        |> Seq.map (fun fMap -> transformFile fileMap jsDir dist (fst fMap))
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
    | _ -> 
        printfn "Please add arguments jsDir and dist"
        ()

    0