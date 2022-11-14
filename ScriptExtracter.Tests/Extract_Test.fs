module Extract_Tests

open Xunit
open System.IO.Abstractions.TestingHelpers

[<Fact>]
let ``Assert replaceImports correctly changes imports`` () =
    let sourcePath = @"C:\js\"
    let targetPath = @"C:\target"

    // Changed import should not be pointing to this
    let undesiredSourceFile = @"C:\js\main.js"
    // Import should point to this 
    let desiredSourceFile = "C:\js\sub1\main.js"
    // File asking for the import
    let subFile = @"C:\js\sub1\sub2\subfile.js"
   
    let mockFs = 
        let fileSystem = MockFileSystem()
        fileSystem.AddFile(undesiredSourceFile, new MockFileData(""))
        fileSystem.AddFile(desiredSourceFile, new MockFileData(""))
        fileSystem.AddFile(subFile, new MockFileData("require(\"./../main.js\")"))
        fileSystem

    let fileRecords = 
        let testFiles = Util.getSourceFiles mockFs sourcePath

        Util.generateFileRecords
        <| mockFs 
        <| sourcePath 
        <| targetPath 
        <| testFiles

    let (fixedText, _) = Extract.replaceImports mockFs fileRecords subFile |> Async.RunSynchronously  
    let expectedText = "require(\"sub1.main\")"
    Assert.Equal(expectedText, fixedText)

[<Fact>]
let ``Assert deleteMissing removes missing files from target directory`` () = 
    let sourcePath = @"C:\js\"
    let targetPath = @"C:\target"

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
