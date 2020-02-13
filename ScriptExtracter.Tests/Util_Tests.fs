module Util_Tests
open Xunit
open System.IO
open System.IO.Abstractions.TestingHelpers


[<Fact>]
let ``Assert getFiles returns all the files`` () =
    let sourcePath = @"C:\js\"
    let mockFs = MockFileSystem()
    mockFs.AddFile(@"C:\js\main.js", new MockFileData("require(\"root.js\")"))
    mockFs.AddFile(@"C:\js\subfile.js", new MockFileData("require(\"../main.js\")"))
    mockFs.AddFile(@"C:\ignoreme.js", new MockFileData("Please ignore me"))

    let getFilesLength = Util.getFiles mockFs sourcePath |> Seq.length
    let expectedLength = 2

    Assert.Equal(expectedLength, getFilesLength)

[<Fact>]
let ``getDotNames correcty names files`` () =
    Assert.Equal("one", Util.getDotName "one")
    Assert.Equal("one.two.three", Util.getDotName "one/two/three")

//[<Fact>]
//let ``Assert mapfiles generates correct records`` () =
//    let testFiles = Util.getFiles testSourcePath
//    let fileRecords = Util.mapFiles testSourcePath testTargetPath testFiles
//    let subFileInfo = FileInfo(@"TestFolder\sub1\subfile.js")

//    let subFileRecord = fileRecords |> Seq.find (fun record -> record.sourceName = "subfile.js")

//    let expextedDotFullPath = Path.Combine(Path.GetFullPath(testTargetPath), subFileRecord.dotName)

//    Assert.Equal(subFileInfo.Name, subFileRecord.sourceName)
//    Assert.Equal(subFileInfo.FullName, subFileRecord.sourceFullPath)
//    Assert.Equal("sub1.subfile.js", subFileRecord.dotName)
//    Assert.Equal(expextedDotFullPath, subFileRecord.dotFullPath)
