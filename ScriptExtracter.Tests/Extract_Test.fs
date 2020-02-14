module Extract_Tests

open Xunit
open System.IO.Abstractions.TestingHelpers

[<Fact>]
let ``Assert fixImports correctly changes imports`` () =
    let sourcePath = @"C:\js\"
    let targetPath = @"C:\target"
    let subFilePath = @"C:\js\subfolder\subfile.js"

    let mockFs = 
        let fileSystem = MockFileSystem()
        fileSystem.AddFile(@"C:\js\main.js", new MockFileData(""))
        fileSystem.AddFile(@"C:\js\subfolder\subfile.js", new MockFileData("require(\"./../main.js\")"))
        fileSystem

    let fileRecords = 
        let testFiles = Util.getSourceFiles mockFs sourcePath

        Util.generateFileRecords
        <| mockFs 
        <| sourcePath 
        <| targetPath 
        <| testFiles

    let (fixedText, _) = Extract.fixImports mockFs fileRecords subFilePath |> Async.RunSynchronously  
    let expectedText = "require(\"main.js\")"
    Assert.Equal(expectedText, fixedText)

[<Fact>]
let ``Assert deleteMissing removes missing files from target directory`` () = 
    let sourcePath = @"C:\js\"
    let targetPath = @"C:\target"
    let subFilePath = @"C:\js\subfolder\subfile.js"

    let mockFs = 
        let fileSystem = MockFileSystem()
        fileSystem.AddFile(@"C:\js\noDelete.js", new MockFileData(""))

        fileSystem.AddFile(@"C:\target\delete.js", new MockFileData(""))
        fileSystem.AddFile(@"C:\target\noDelete.js", new MockFileData(""))
        fileSystem

    let fileRecords = 
        let testFiles = Util.getSourceFiles mockFs sourcePath

        Util.generateFileRecords
        <| mockFs 
        <| sourcePath 
        <| targetPath 
        <| testFiles

    Extract.deleteMissing mockFs fileRecords targetPath
    Assert.DoesNotContain(@"C:\target\delete.js", mockFs.AllFiles)
    Assert.Contains(@"C:\target\noDelete.js", mockFs.AllFiles)
