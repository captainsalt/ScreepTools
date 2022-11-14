module Program
open Extract
open System.IO
open System.IO.Abstractions
open Util
open System.Diagnostics

[<EntryPoint>]
let main args = 
    match args with
    | [| sourceDir; targetDir |] ->
        if Directory.Exists(sourceDir) |> not then
            failwithf "Root directory: %s does not exist" sourceDir

        printfn "Extracting files..."
        let watch = Stopwatch()
        watch.Start()

        let fileSystem = FileSystem()

        // Delete missing files
        let jsFiles = getSourceFiles fileSystem sourceDir
        let fileRecords = generateFileRecords fileSystem sourceDir targetDir jsFiles
        deleteMissing fileSystem  fileRecords targetDir
        
        let jsFiles = getSourceFiles fileSystem sourceDir
        let fileRecords = generateFileRecords fileSystem sourceDir targetDir jsFiles

        try
            fileRecords 
            |> Seq.map(fun record -> extractFile fileSystem fileRecords targetDir record.sourceFullPath)
            |> Async.Parallel
            |> Async.RunSynchronously
            |> ignore
        with
        | ex -> printfn "%s" ex.Message

        watch.Stop()
        printfn "Done in %s seconds" <| watch.Elapsed.ToString("s\.ffff")
    | _ -> 
        printfn "Please add arguments jsDir and dist"
        ()

    0