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
    /// <c>exp(log(1/x, arcsin(x)))</c> at 0 from the right was left unevaluated, where the logarithm alone is -1 and
    /// the limit is <c>1/e</c>. The logarithm reaches the descent as <c>ln(arcsin(x)) / ln(1/x)</c>, a quotient of
    /// two infinities, and the limit its derivatives' quotient was taken to came back as the form <c>0 * +oo / 0</c>,
    /// which has no value; the descent handed that on as the exponent's limit rather than asking the quotient's whole
    /// limit, which it asked only for a 0/0 read off two finite parts. A sine of it was left unevaluated too, and a
    /// hyperbolic sine after ten seconds. The same on 2.6.0. Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=40</c>;
    /// no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AFunctionOfAQuotientOfInfinitiesTest
    {
        [Theory]
        [InlineData("e^log(1/x, arcsin(x))", "1/e")]
        [InlineData("sin(log(1/x, arcsin(x)))", "sin(-1)")]
        [InlineData("sinh(log(1/x, arcsin(x)))", "(1/e - e)/2")]
        [InlineData("log(1/x, arcsin(x))", "-1")]
        public void IsTheLimitFromTheRight(string input, string expected)
            => Assert.Equal(MathS.FromString(expected).Simplify(),
                MathS.FromString(input).Limit("x", 0, ApproachFrom.Right).Simplify());

        // ln(arcsin(x)) / ln(x) tends to 1 from the left as well, and was left unevaluated there after six seconds.
        [Fact]
        public void IsTheLimitFromTheLeft()
            => Assert.Equal(MathS.FromString("tanh(1)").Simplify(),
                MathS.FromString("tanh(log(x, arcsin(x)))").Limit("x", 0, ApproachFrom.Left).Simplify());

        // The two-sided convention is kept: ln(x) has no real values to the left of 0.
        [Theory]
        [InlineData("x * ln(x)")]
        [InlineData("sin(x) * ln(x)")]
        public void HasNoTwoSidedLimit(string input)
            => Assert.Equal(MathS.NaN, MathS.FromString(input).Limit("x", 0));
    }
}
