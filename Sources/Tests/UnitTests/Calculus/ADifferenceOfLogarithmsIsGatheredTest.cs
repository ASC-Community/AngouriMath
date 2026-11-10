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
    /// Two logarithms to one base tending to opposite infinities are one logarithm, where both antilogarithms are
    /// positive on the way: ln(sin(x)) - ln(x) at 0 from the right is ln(sin(x) / x), and tends to 0. The ratio of the
    /// two tends to 1, which settles nothing, so it was left unevaluated. An antilogarithm that is negative is not
    /// gathered: ln(x) - ln(-x) is -pi i there, and gathered it would be ln(-1), pi i. Found by limitcheck; no issue was
    /// filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ADifferenceOfLogarithmsIsGatheredTest
    {
        [Theory]
        [InlineData("ln(sin(x)) - ln(x)", "0")]
        [InlineData("(ln(sin(x)) - ln(x)) / x^2", "-1/6")]
        [InlineData("(ln(sin(x)) - ln(x)) / x", "0")]
        [InlineData("log(2, sin(x)) - log(2, x)", "0")]
        [InlineData("2 * ln(sin(x)) - ln(x^2)", "0")]
        [InlineData("ln(x) - ln(-x)", "-pi * i")]
        public void TheLimitFromTheRightIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);
    }
}
