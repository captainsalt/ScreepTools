module Program
open Extract
open System.IO
open System.IO.Abstractions
open Util
open System.Diagnostics

[<EntryPoint>]
let main args = 
    let watch = Stopwatch()
    watch.Start()

    match args with
    | [| sourceDir; targetDir |] ->
        if Directory.Exists(sourceDir) |> not then
            failwithf "Root directory: %s does not exist" sourceDir

        printfn "Extracting files..."

        let fileSystem = FileSystem()

        let deleteMissingFiles = 
            let jsFiles = getSourceFiles fileSystem sourceDir
            let fileRecords = generateFileRecords fileSystem sourceDir targetDir jsFiles
            deleteMissing fileSystem  fileRecords targetDir
        deleteMissingFiles
        
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