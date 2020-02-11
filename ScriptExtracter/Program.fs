module Program
open Extract
open System.IO

[<EntryPoint>]
let main args = 
    match args with
    | [| rootDir; dist |] ->
        if Directory.Exists(rootDir) |> not then
            failwithf "Root directory: %s does not exist" rootDir

        let files = getFiles rootDir

        deleteMissing files dist

        files
        |> Seq.map (fun fPath -> transformFile dist fPath)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
    | _ -> 
        printfn "Please add arguments rootDir and dist"
        ()

    0