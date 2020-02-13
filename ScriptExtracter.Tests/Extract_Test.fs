module Extract_Tests

open System
open Xunit
open System.IO
open System.Collections

let testFolderPath = "TestFolder"

[<Theory>]
[<InlineData("""require("main.js")""", """require("main.js")""")>]
[<InlineData("""require("./main.js")""", """require("main.js")""")>]
let ``fixImports correctly changes imports`` expected actual =
    Assert.Equal(Extract.fixImports expected, actual)
