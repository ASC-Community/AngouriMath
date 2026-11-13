//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The arccotangent here is arctan(1/u), with range (-pi/2, pi/2]: arccot(-0.001) = -1.5698, arccot(0) = pi/2,
    /// arccot(0.001) = 1.5698. It jumps at 0, and limits put 0 in and answered pi/2 from either side, so arccot(x) at 0
    /// had a two-sided limit and arccot(x) / x from the left was -oo where it is +oo. Found by limitcheck; no issue was
    /// filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnArccotangentAtItsJumpTest
    {
        [Theory]
        [InlineData("arccot(x)", ApproachFrom.Left, "-pi / 2")]
        [InlineData("arccot(x)", ApproachFrom.Right, "pi / 2")]
        [InlineData("arccot(sin(x))", ApproachFrom.Left, "-pi / 2")]
        [InlineData("arccot(-x^2)", ApproachFrom.BothSides, "-pi / 2")]
        [InlineData("arccot(x^2)", ApproachFrom.BothSides, "pi / 2")]
        [InlineData("arccot(x) / x", ApproachFrom.Left, "+oo")]
        [InlineData("arccot(x) / x", ApproachFrom.BothSides, "+oo")]
        [InlineData("x * arccot(x) / sin(x)^2", ApproachFrom.BothSides, "+oo")]
        [InlineData("(arccot(x) - pi / 2) / x", ApproachFrom.Right, "-1")]
        [InlineData("(arccot(x) - pi / 2) / x", ApproachFrom.Left, "+oo")]
        [InlineData("(arccot(x) + pi / 2) / x", ApproachFrom.Left, "-1")]
        // x arccot(x) is positive on both sides of 0: its derivative there holds arccot(x) itself,
        // whose value at 0 is not its limit from the left, so it does not give the sign.
        [InlineData("arccot(x * arccot(x))", ApproachFrom.Left, "pi / 2")]
        [InlineData("arccot(x * arccot(x))", ApproachFrom.BothSides, "pi / 2")]
        [InlineData("sgn(x * arccot(x))", ApproachFrom.Left, "1")]
        [InlineData("1 / (x * arccot(x))", ApproachFrom.Left, "+oo")]
        // An argument that is 0 everywhere: the arccotangent of it is the constant pi/2.
        [InlineData("arccot((x^2 - 1) - (x - 1) * (x + 1))", ApproachFrom.BothSides, "pi / 2")]
        public void IsTakenFromTheSide(string input, ApproachFrom side, string expected)
            => Assert.Equal(expected.Simplify(), input.ToEntity().Limit("x", 0, side).Simplify());

        // Where the two sides arrive on different sides of the jump, there is no two-sided limit.
        [Theory]
        [InlineData("arccot(x)")]
        [InlineData("arccot(sin(x))")]
        [InlineData("arccot(x) + arctan(x)")]
        [InlineData("(arccot(x) - pi / 2) / x")]
        public void HasNoTwoSidedLimitAcrossTheJump(string input)
            => Assert.Equal(MathS.NaN, input.ToEntity().Limit("x", 0));
    }
}
