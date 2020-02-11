module Extract_Tests

open System
open Xunit
open System.IO

let basePath = "testFolder"

[<Fact>]
let ``getFiles returns all the files`` () =
    let expected = [ 
        Path.Combine("testFolder", "main.js") 
        Path.Combine("testFolder", "subfolder", "subfile.js") ]
    let discoverdFiles = Extract.getFiles basePath 
    Assert.Equal(expected, discoverdFiles)

[<Fact>]
let ``getDotNames correcty names files`` () =
    Assert.Equal("one", Extract.getDotName "one")
    Assert.Equal("one.two.three", Extract.getDotName "one/two/three")