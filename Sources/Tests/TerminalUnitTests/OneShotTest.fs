/// The commands amcli answers once and exits on, without opening the terminal.
/// https://github.com/asc-community/AngouriMath/issues/1649
module AngouriMath.Terminal.OneShotTest

open System.IO
open Xunit
open AngouriMath.Terminal.Lib

/// The exit code, what was printed, and what was printed to stderr.
let private run (args: string list) (stdin: string) =
    use input = new StringReader(stdin)
    use output = new StringWriter()
    use error = new StringWriter()
    let code = OneShot.run args input output error
    code, output.ToString().Replace("\r\n", "\n").TrimEnd('\n'), error.ToString()

let private answers (args: string list) (expected: string) =
    let code, printed, error = run args ""
    Assert.Equal("", error)
    Assert.Equal(0, code)
    Assert.Equal(expected, printed)

[<Fact>]
let ``simp simplifies`` () = answers [ "simp"; "sin(x)^2 + cos(x)^2" ] "1"

[<Fact>]
let ``fsimp normalises without the search`` () = answers [ "fsimp"; "x + 0" ] "x"

/// A rational is printed as a decimal, which is what the command has always promised.
[<Fact>]
let ``eval writes a rational as a decimal`` () = answers [ "eval"; "1 / 2" ] "0.5"

[<Fact>]
let ``eval decides a comparison`` () = answers [ "eval"; "e ^ pi > pi ^ e" ] "True"

[<Fact>]
let ``diff takes the variable first`` () = answers [ "diff"; "x"; "sin(x)" ] "cos(x)"

[<Fact>]
let ``latex writes LaTeX`` () = answers [ "latex"; "x ^ 2" ] "{x}^{2}"

[<Fact>]
let ``sub substitutes the value for the variable`` () = answers [ "sub"; "x"; "y"; "x + 1" ] "y + 1"

/// One root per line, so that a script can read them with a loop.
[<Fact>]
let ``solve writes finitely many roots one per line`` () =
    let code, printed, _ = run [ "solve"; "x"; "x ^ 2 - 1 = 0" ] ""
    Assert.Equal(0, code)
    Assert.Equal<string Set>(set [ "1"; "-1" ], printed.Split('\n') |> Set.ofArray)

[<Fact>]
let ``solve writes infinitely many roots on one line`` () =
    let code, printed, _ = run [ "solve"; "x"; "x ^ 2 > 1" ] ""
    Assert.Equal(0, code)
    Assert.DoesNotContain("\n", printed)

[<Fact>]
let ``an argument left out is read from stdin`` () =
    let code, printed, _ = run [ "diff"; "x" ] "1 + x ^ 2"
    Assert.Equal(0, code)
    Assert.Equal("2 * x", printed)

[<Fact>]
let ``an argument written as an underscore is read from stdin`` () =
    let code, printed, _ = run [ "sub"; "x"; "_"; "x + 1" ] "y"
    Assert.Equal(0, code)
    Assert.Equal("y + 1", printed)

[<Fact>]
let ``a missing argument with nothing on stdin is a usage error`` () =
    let code, printed, error = run [ "diff"; "x" ] ""
    Assert.Equal(2, code)
    Assert.Equal("", printed)
    Assert.Contains("amcli diff needs more arguments", error)

[<Fact>]
let ``a command that does not exist is a usage error, and names itself`` () =
    let code, _, error = run [ "frobnicate"; "x" ] ""
    Assert.Equal(2, code)
    Assert.Contains("amcli has no command frobnicate", error)

[<Fact>]
let ``input that does not parse fails, and says why`` () =
    let code, printed, error = run [ "simp"; "x + * 2" ] ""
    Assert.Equal(1, code)
    Assert.Equal("", printed)
    Assert.NotEqual<string>("", error.Trim())

[<Fact>]
let ``help lists every command`` () =
    let code, printed, _ = run [ "help" ] ""
    Assert.Equal(0, code)
    for command in [ "eval"; "simp"; "fsimp"; "diff"; "solve"; "sub"; "latex"; "info"; "mcp" ] do
        Assert.Contains($"amcli {command} ", printed)

/// Not one answer but a session: the server reads requests from stdin until it ends, and writes
/// nothing to stdout but its replies.
[<Fact>]
let ``mcp serves the protocol on stdin and stdout`` () =
    let code, printed, error = run [ "mcp" ] "{\"jsonrpc\":\"2.0\",\"id\":0,\"method\":\"initialize\",\"params\":{}}"
    Assert.Equal("", error)
    Assert.Equal(0, code)
    Assert.StartsWith("{\"jsonrpc\":\"2.0\",\"id\":0,\"result\":{\"protocolVersion\":\"2024-11-05\"", printed)
    Assert.DoesNotContain("\n", printed)

/// What info says about an expression's stationary points, by the second derivative test.
[<Theory>]
[<InlineData("x^2 - 2*x", "stationary point x = 1: a minimum, value -1")>]
[<InlineData("2*x - x^2", "stationary point x = 1: a maximum, value 1")>]
[<InlineData("x^3", "stationary point x = 0: a degenerate point, which the second derivative test does not decide, value 0")>]
[<InlineData("x^2 + y^2", "stationary point (x, y) = (0, 0): a minimum, value 0")>]
[<InlineData("-x^2 - y^2", "stationary point (x, y) = (0, 0): a maximum, value 0")>]
[<InlineData("x^2 - y^2", "stationary point (x, y) = (0, 0): a saddle point, value 0")>]
[<InlineData("x^3 + x", "stationary point x = sqrt(-1/3): not real")>]
let ``info classifies each stationary point`` (expression: string) (expected: string) =
    let code, printed, error = run [ "info"; expression ] ""
    Assert.Equal("", error)
    Assert.Equal(0, code)
    Assert.Contains(expected, printed.Split('\n'))

[<Fact>]
let ``info names the variables and gives the derivative and the roots for each`` () =
    let code, printed, _ = run [ "info"; "x^2 - 4" ] ""
    Assert.Equal(0, code)
    let lines = printed.Split('\n')
    Assert.Contains("variables: x", lines)
    Assert.Contains("derivative with respect to x: 2 * x", lines)
    Assert.Contains(lines, fun line -> line.StartsWith("roots for x: ") && line.Contains("-2") && line.Contains("2"))
