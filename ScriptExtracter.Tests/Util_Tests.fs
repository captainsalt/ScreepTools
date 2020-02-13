module Util_Tests
open Xunit

let testFolderPath = "TestFolder"

[<Fact>]
let ``getFiles returns all the files`` () =
    let discoverdFiles = Util.getFiles testFolderPath |> Seq.length
    Assert.Equal(3, discoverdFiles)

[<Fact>]
let ``getDotNames correcty names files`` () =
    Assert.Equal("one", Util.getDotName "one")
    Assert.Equal("one.two.three", Util.getDotName "one/two/three")

