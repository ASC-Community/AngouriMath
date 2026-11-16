module AngouriMath.FSharp.Tests.Order

open AngouriMath.FSharp.Core
open AngouriMath.FSharp.Functions
open Xunit

let x = symbol "x"
let y = symbol "y"
let z = symbol "z"

[<Fact>]
let ``Derivative test`` () = Assert.Equal(parsed "derivative(x, y)", derivativeNode y x)
[<Fact>]
let ``Partial derivative test`` () = Assert.Equal(parsed "partialderivative(x, y)", partialDerivativeNode y x)
[<Fact>]
let ``Integral test`` () = Assert.Equal(parsed "integral(x, y)", integralNode y x)
[<Fact>]
let ``Limit test`` () = Assert.Equal(parsed "limit(x, y, z)", limitNode y z x)
[<Fact>]
let ``Differentiate test`` () = Assert.Equal(parsed "2x + 2", derivative x "x2 + 2x")
[<Fact>]
let ``Integrate test`` () = Assert.Equal(parsed "sin(x) + x + C", integral x "1 + cos(x)")
[<Fact>]
let ``Limited test`` () = Assert.Equal(parsed "a", limit x 0 "a x / sin(x)")
