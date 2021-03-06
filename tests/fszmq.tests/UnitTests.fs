namespace fszmq.tests

open System
open fszmq
open NUnit.Framework

[<TestFixture>]
module UnitTest =

  [<Test;Category("Miscellany")>]
  let ``libzmq version should be at least 4``() =
    let vsn = ZMQ.version
    printfn "%A" vsn
    match vsn with
    | Version(major,minor,build) -> Assert.That (major,Is.EqualTo 4)
                                    Assert.That (minor,Is.GreaterThanOrEqualTo 0)
                                    Assert.That (build,Is.GreaterThanOrEqualTo 0)
    //TODO: find a better way to handle failure
    | otherwise -> Assert.That (otherwise,Is.EqualTo Unknown)

  [<Test;Category("Miscellany")>]
  let ``recv throws TimeoutException if RCVTIMEO expires`` () =
    let testFn () =
      use ctx = new Context ()
      let sck = Context.pair ctx
      Socket.setOption sck (ZMQ.RCVTIMEO,10)
      Socket.bind sck "inproc://dummy"
      sck
      |> Socket.recv
      |> printfn "msg: %A"

    let error = Assert.Throws<TimeoutException> (TestDelegate(testFn))
    Assert.That (error.Message.Replace ("-"," "),Is.StringContaining "has timed out")

(* ZCURVE & Z85 Tests *)
  let BINARY = [| 0x86uy; 0x4Fuy; 0xD2uy; 0x6Fuy; 0xB5uy; 0x59uy; 0xF7uy; 0x5Buy |]
  let STRING = "HelloWorld"

  let inline isIn items v = Seq.exists ((=) v) items

  [<Test;Category("ZCURVE")>]
  let ``keypair generation requires sodium`` () =
    let error = Assert.Throws<ZMQError> (fun () -> Curve.curveKeyPair() |> ignore)
    Assert.That(error.Message.ToLower(), Is.StringContaining "not supported")

  //TODO: write passing tests, once you figure out libsodium installation

  [<Test;Category("Z85")>]
  let ``can encode (binary-to-string)`` () =
    let encoded = Z85.encode(BINARY)
    Assert.That (encoded, Is.EqualTo STRING)

  [<Test;Category("Z85")>]
  let ``unencoded binary must be divisible by 4`` () =
    let binary = BINARY.[1 ..] // 7 bytes shouldn't be divisible by 4
    let error  = Assert.Throws<ZMQError> (fun () -> Z85.encode(binary) |> ignore)
    Assert.That (error.Message,Is.EqualTo "Invalid argument")

  [<Test;Category("Z85")>]
  let ``unencoded binary must not be zero-length`` () =
    let error = Assert.Throws<ZMQError> (fun () -> Z85.encode([||]) |> ignore)
    Assert.That (error.Message,Is.EqualTo "Invalid argument")

  [<Test;Category("Z85")>]
  let ``can decode (string-to-binary)`` () =
    let decoded = Z85.decode(STRING)
    Assert.That (decoded,Is.EqualTo BINARY)

  [<Test;Category("Z85")>]
  let ``encoded string must be divisible by 5`` () =
    let string = STRING + "!" // 11 bytes shouldn't be divisible by 5
    let error  = Assert.Throws<ZMQError> (fun () -> Z85.decode(string) |> ignore)
    Assert.That (error.Message,Is.EqualTo "Invalid argument")

  [<Test;Category("Z85")>]
  let ``encoded string must not be zero-length`` () =
    let error = Assert.Throws<ZMQError> (fun () -> Z85.decode("") |> ignore)
    Assert.That (error.Message,Is.EqualTo "Invalid argument")
