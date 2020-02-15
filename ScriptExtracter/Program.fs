module Program
open Extract
open System.IO
open System.IO.Abstractions
open Util

[<EntryPoint>]
let main args = 
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

        fileRecords 
        |> Seq.map(fun record -> extractFile fileSystem fileRecords targetDir record.sourceFullPath)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore

        printfn "Done"
    | _ -> 
        printfn "Please add arguments jsDir and dist"
        ()

    0