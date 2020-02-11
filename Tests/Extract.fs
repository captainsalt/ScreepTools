module Extract_Tests

open NUnit.Framework
open System.IO

let basePath = "testFolder"

[<SetUp>]
let Setup () =
    ()

[<Test>]
let ``getFiles returns all the files`` () =
    let expected = [ 
        Path.Combine("testFolder", "main.js") 
        Path.Combine("testFolder", "subfolder", "subfile.js") ]
    let discoverdFiles = Extract.getFiles basePath 
    Assert.AreEqual(expected, discoverdFiles)

[<Test>]
let ``getDotNames correcty names files`` () =
    Assert.AreEqual("one", Extract.getDotName "one")
    Assert.AreEqual("one.two.three", Extract.getDotName "one/two/three")

