module Extract_Tests

open System
open Xunit
open System.IO
open System.Collections
open System.IO.Abstractions.TestingHelpers

[<Fact>]
let ``fixImports correctly changes imports`` () =
    let sourcePath = @"C:\js\"
    let targetPath = @"C:\target"
    let subFileText = "require(\"./../main.js\")"

    let mockFs = 
        let fileSystem = MockFileSystem()
        fileSystem.AddFile(@"C:\js\main.js", new MockFileData(""))
        fileSystem.AddFile(@"C:\js\subfolder\subfile.js", new MockFileData(""))
        fileSystem

    let fileRecords = 
        let testFiles = Util.getSourceFiles mockFs sourcePath

        Util.generateFileRecords
        <| mockFs 
        <| sourcePath 
        <| targetPath 
        <| testFiles

    let fixedText = Extract.fixImports mockFs fileRecords subFileText  
    let expectedText = "require(\"main.js\")"
    Assert.Equal(expectedText, fixedText)
