﻿module Program
open Extract
open System.IO
open Util

[<EntryPoint>]
let main args = 
    match args with
    | [| jsDir; dist |] ->
        if Directory.Exists(jsDir) |> not then
            failwithf "Root directory: %s does not exist" jsDir

        let jsFiles = getFiles jsDir

        let removeMissingFiles = 
            let fileMap = jsFiles |> mapFiles
            deleteMissing fileMap jsDir dist 
        removeMissingFiles

        let fileMap = jsFiles |> mapFiles

        jsFiles
        |> mapFiles
        |> Seq.map (fun (fName, _) -> transformFile fileMap jsDir dist fName)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
    | _ -> 
        printfn "Please add arguments jsDir and dist"
        ()

    0