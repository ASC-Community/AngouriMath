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
    /// <c>1/abs(x)</c> at 0 was NaN -- the claim that the limit does not exist -- where it is +oo from both sides. The sign
    /// a vanishing divisor keeps was read off its derivatives, and the derivative of <c>abs(x)</c> is <c>sgn(x)</c>, 0 at
    /// 0, with nothing after it; so each side was left unevaluated, and the two-sided limit was NaN for the descent's two
    /// readings not matching. And where both sides were answered, they were promoted to the two-sided limit only for an
    /// expression shown real on both sides, which knew nothing of an absolute value, a sine or an arcsine:
    /// <c>sin(arcsin(x)) * (tan(-1) + 2/x)</c> is 2 from each side and was NaN. The same on 2.6.0. Found by limitcheck at
    /// <c>DEPTH=3</c>, <c>SEED=29</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnAbsoluteValueVanishesFromAboveTest
    {
        [Theory]
        [InlineData("1/abs(x)", "0", "+oo")]
        [InlineData("1/abs(x)^3", "0", "+oo")]
        [InlineData("-2/abs(x)", "0", "-oo")]
        [InlineData("1/abs(x - 1)", "1", "+oo")]
        [InlineData("1/abs(sin(x))", "0", "+oo")]
        [InlineData("(x + 1)/abs(x)", "0", "+oo")]
        [InlineData("abs(x)^(-3/2)", "0", "+oo")]
        [InlineData("sqrt(x^2)^(-3/2)", "0", "+oo")]
        [InlineData("sin(arcsin(x)) * (tan(-1) + 2/x)", "0", "2")]
        public void IsTheLimitFromEachSideAndBoth(string input, string destination, string expected)
        {
            foreach (var side in new[] { ApproachFrom.Left, ApproachFrom.Right, ApproachFrom.BothSides })
                Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));
        }

        // Where the two sides differ, or one of them is not a limit of real values, there is still no two-sided limit.
        [Theory]
        [InlineData("x/abs(x)")]
        [InlineData("sqrt(x^2)/x")]
        [InlineData("abs(x)^(1/2)/x")]
        [InlineData("arcsin(x + 1)/x")]
        [InlineData("x^x")]
        public void HasNoTwoSidedLimit(string input)
            => Assert.Equal(MathS.NaN, MathS.FromString(input).Limit("x", 0));
    }
}
