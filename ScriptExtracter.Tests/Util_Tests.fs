﻿module Util_Tests
open Xunit
open System.IO.Abstractions.TestingHelpers

[<Fact>]
let ``Assert getSourceFiles returns all the files`` () =
    let sourcePath = @"C:\js\"
    let mockFs = MockFileSystem()
    mockFs.AddFile(@"C:\js\main.js", new MockFileData(""))
    mockFs.AddFile(@"C:\js\subfile.js", new MockFileData(""))
    mockFs.AddFile(@"C:\ignoreme.js", new MockFileData(""))

    let getFilesLength = Util.getSourceFiles mockFs sourcePath |> Seq.length
    let expectedLength = 2

    Assert.Equal(expectedLength, getFilesLength)

[<Fact>]
let ``Assert getDotNames correcty names files`` () =
    Assert.Equal("one", Util.getDotName "one")
    Assert.Equal("one.two.three", Util.getDotName "one/two/three")

[<Fact>]
let ``Assert gerateFileRecords generates records with correct information`` () =
    let sourcePath = @"C:\js\"
    let targetPath = @"C:\target"
    let mockFs = 
        let fileSystem = MockFileSystem()
        fileSystem.AddFile(@"C:\js\main.js", new MockFileData(""))
        fileSystem.AddFile(@"C:\js\sub1\subfile.js", new MockFileData(""))

        fileSystem

    let subFileRecord = 
        let fileRecords = 
            let testFiles = Util.getSourceFiles mockFs sourcePath
            
            Util.generateFileRecords
            <| mockFs 
            <| sourcePath 
            <| targetPath 
            <| testFiles

        fileRecords 
        |> Seq.find (fun record -> record.sourceName = "subfile.js")

    let subFileInfo = 
        mockFs.FileInfo.FromFileName(subFileRecord.sourceFullPath)

    let expextedDotFullPath = 
        let fullTargetPath = mockFs.Path.GetFullPath(targetPath)
        mockFs.Path.Combine(fullTargetPath, subFileRecord.dotName)

    let expectedDotName = "sub1.subfile.js"

    Assert.Equal(subFileInfo.Name, subFileRecord.sourceName)
    Assert.Equal(subFileInfo.FullName, subFileRecord.sourceFullPath)
    Assert.Equal(expectedDotName, subFileRecord.dotName)
    Assert.Equal(expextedDotFullPath, subFileRecord.dotFullPath)
