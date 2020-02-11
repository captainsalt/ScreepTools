module Program
open Extract
open System.IO

[<EntryPoint>]
let main args = 
    match args with
    | [| rootDir; dist |] ->
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