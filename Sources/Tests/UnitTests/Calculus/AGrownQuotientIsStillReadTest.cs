//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A derivatives' quotient that grew too much to differentiate again is still read at the point once expanded. For
    /// ln(sqrt(x) cos(x)) / (1 / x) the product rule gives -x^(3/2) (x^(-1/2) cos(x) / 2 - sqrt(x) sin(x)) sec(x),
    /// which has no value at 0 and tends to 0 there, so the limit was left unevaluated, and (sqrt(x) cos(x))^x with
    /// it, while x^(1/3) cos(x) in its place was answered. Found by probing; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AGrownQuotientIsStillReadTest
    {
        [Theory]
        [InlineData("ln(sqrt(x) * cos(x)) / (1 / x)", "0")]
        [InlineData("x * ln(sqrt(x) * cos(x))", "0")]
        [InlineData("(sqrt(x) * cos(x))^x", "1")]
        public void AtZeroFromTheRight(string input, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", 0, ApproachFrom.Right));

        // Left of 0 the base is imaginary; that side is not decided here, and the two-sided limit stays NaN as before.
        [Fact]
        public void TwoSidedIsUnchanged()
            => Assert.Equal(MathS.NaN, MathS.FromString("(sqrt(x) * cos(x))^x").Limit("x", 0));
    }
}
