module Util_Tests
open Xunit
open System.IO

let testSourcePath = "TestFolder"
let testTargetPath = "dist"

[<Fact>]
let ``getFiles returns all the files`` () =
    let getFilesLength = Util.getFiles testSourcePath |> Seq.length
    let expectedLength = 3

    Assert.Equal(expectedLength, getFilesLength)

[<Fact>]
let ``getDotNames correcty names files`` () =
    Assert.Equal("one", Util.getDotName "one")
    Assert.Equal("one.two.three", Util.getDotName "one/two/three")

[<Fact>]
let ``Assert mapfiles generates correct records`` () =
    let testFiles = Util.getFiles testSourcePath
    let fileRecords = Util.mapFiles testSourcePath testTargetPath testFiles
    let subFileInfo = FileInfo(@"TestFolder\sub1\subfile.js")

    let subFileRecord = fileRecords |> Seq.find (fun record -> record.sourceName = "subfile.js")

    let expextedDotFullPath = Path.Combine(Path.GetFullPath(testTargetPath), subFileRecord.dotName)

    Assert.Equal(subFileInfo.Name, subFileRecord.sourceName)
    Assert.Equal(subFileInfo.FullName, subFileRecord.sourceFullPath)
    Assert.Equal("sub1.subfile", subFileRecord.dotName)
    Assert.Equal(expextedDotFullPath, subFileRecord.dotFullPath)
