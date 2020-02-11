module Extract_Tests

open System
open Xunit
open System.IO
open System.Collections

let basePath = "testData"

[<Fact>]
let ``getFiles returns all the files`` () =
    let expected = [ 
        Path.Combine(basePath, "main.js") 
        Path.Combine(basePath, "subfolder", "subfile.js") ]
    let discoverdFiles = Extract.getFiles basePath |> Seq.toList

    Assert.Equal<string list>(expected, discoverdFiles)

[<Fact>]
let ``getDotNames correcty names files`` () =
    Assert.Equal("one", Extract.getDotName "one")
    Assert.Equal("one.two.three", Extract.getDotName "one/two/three")

[<Theory>]
[<InlineData("""require("main.js")""", """require("main.js")""")>]
[<InlineData("""require("./main.js")""", """require("main.js")""")>]
let ``fixImports correctly changes imports`` (expected: string) (actual: string) =
    Assert.Equal(Extract.fixImports expected, actual)