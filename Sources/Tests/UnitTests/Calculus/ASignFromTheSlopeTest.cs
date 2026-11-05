//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A step function whose argument arrives at a jump asks the sign the argument keeps on the way in, and that sign
    /// was read only off the first derivative that does not vanish. <c>sqrt(x)</c> at 0 has a derivative that diverges
    /// there, so <c>floor(sqrt(x))</c> at 0 from the right was not answered, where it is 0 all the way in. The slope
    /// <c>g(x) / (x - a)</c> settles it: <c>1 / sqrt(x)</c> tends to +oo, so <c>sqrt(x)</c> is positive on the right.
    /// Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=59</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASignFromTheSlopeTest
    {
        [Theory]
        [InlineData("floor(sqrt(x))", "0", ApproachFrom.Right, "0")]
        [InlineData("ceil(sqrt(x))", "0", ApproachFrom.Right, "1")]
        [InlineData("floor(-sqrt(x))", "0", ApproachFrom.Right, "-1")]
        [InlineData("floor(sqrt(x)) - (x^3)^(x/2)", "0", ApproachFrom.Right, "-1")]
        [InlineData("floor(sqrt(x) - x)", "0", ApproachFrom.Right, "0")]
        [InlineData("floor(x - sqrt(x))", "0", ApproachFrom.Right, "-1")]
        [InlineData("floor(1 - sqrt(1 - x))", "1", ApproachFrom.Left, "0")]
        [InlineData("ceil(1 - sqrt(1 - x))", "1", ApproachFrom.Left, "1")]
        [InlineData("floor(cbrt(x))", "0", ApproachFrom.Right, "0")]
        public void ReadsTheSignOffTheSlope(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected).Simplify(),
                MathS.FromString(input).Limit("x", destination, side).Simplify());

        // The slope of a complex value can tend to a real infinity: x ^ tanh(pi) on the left of 0 is |x| ^ tanh(pi)
        // times e ^ (i pi tanh(pi)), whose sign is not -1. A root's base that is not positive on the side approached
        // leaves the sign unread, and a declined limit keeps its node.
        [Theory]
        [InlineData("sgn(x ^ tanh(pi))", "0", ApproachFrom.Left)]
        [InlineData("floor(sqrt(x))", "0", ApproachFrom.Left)]
        public void LeavesAComplexValueUnread(string input, string destination, ApproachFrom side)
            => Assert.IsNotType<Entity.Number.Integer>(MathS.FromString(input).Limit("x", destination, side));
    }
}
