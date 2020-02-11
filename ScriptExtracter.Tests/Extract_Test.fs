module Extract_Tests

open System
open Xunit
open System.IO
open System.Collections

let testFolderPath = "TestFolder"

[<Fact>]
let ``getFiles returns all the files`` () =
    let discoverdFiles = Extract.getFiles testFolderPath |> Seq.length
    Assert.Equal(3, discoverdFiles)

[<Fact>]
let ``getDotNames correcty names files`` () =
    Assert.Equal("one", Extract.getDotName "one")
    Assert.Equal("one.two.three", Extract.getDotName "one/two/three")

[<Theory>]
[<InlineData("""require("main.js")""", """require("main.js")""")>]
[<InlineData("""require("./main.js")""", """require("main.js")""")>]
let ``fixImports correctly changes imports`` (expected: string) (actual: string) =
    Assert.Equal(Extract.fixImports expected, actual)