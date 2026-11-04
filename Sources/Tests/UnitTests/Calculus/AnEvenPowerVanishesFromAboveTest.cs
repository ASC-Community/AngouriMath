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
    /// <c>1/((x^2)^tanh(1))</c> at 0 was NaN -- the claim that the limit does not exist -- where it is +oo from both sides.
    /// A base was read as never negative only for an absolute value, or a power, product or quotient of never-negative
    /// parts, so <c>x^2</c> was not, and a vanishing power of it to a constant that is not a number, or to an exponent
    /// tending to a negative number, as <c>(sinh(x)^2)^(sin(x) - 1)</c>, was left unevaluated from each side. An even
    /// power of a base real for real <c>x</c> is never negative now. The same on 2.6.0. Found by limitcheck at
    /// <c>DEPTH=3</c>, <c>SEED=36</c>, and <c>sqrt(x^2)^tan(pi - 1)</c> at
    /// <c>SEED=29</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnEvenPowerVanishesFromAboveTest
    {
        [Theory]
        [InlineData("1/((x^2)^tanh(1))", "0", "+oo")]
        [InlineData("-1/((x^2)^tanh(1))", "0", "-oo")]
        [InlineData("(sinh(x)^2)^(sin(x) - 1)", "0", "+oo")]
        [InlineData("((x - 1)^2)^(-pi/4)", "1", "+oo")]
        [InlineData("(sin(x)^4)^(-tanh(1))", "0", "+oo")]
        [InlineData("sqrt(x^2)^tan(pi - 1)", "0", "+oo")]
        public void IsTheLimitFromEachSideAndBoth(string input, string destination, string expected)
        {
            foreach (var side in new[] { ApproachFrom.Left, ApproachFrom.Right, ApproachFrom.BothSides })
                Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));
        }

        // (i x)^2 is -x^2, negative on both sides, so a power of it to an exponent that is not whole is not real there,
        // and is not claimed to diverge to +oo.
        [Fact]
        public void AnEvenPowerOfSomethingNotRealIsNotClaimed()
            => Assert.NotEqual(MathS.FromString("+oo"),
                MathS.FromString("1/(((i*x)^2)^tanh(1))").Limit("x", 0, ApproachFrom.Right));
    }
}
