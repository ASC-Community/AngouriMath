//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A two-sided limit compares its sides only where the expression is known to be real on both, and an error function
    /// or a step function of a real argument was not known to be. (pi^2 + 1/(2x)) erf(x) at 0 was NaN, which says there
    /// is no limit, where each side is 1/sqrt(pi). Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ARealFunctionAgreesOnBothSidesTest
    {
        [Theory]
        [InlineData("(pi^2 + (1/2) / x) * erf(x)", "1 / sqrt(pi)")]
        [InlineData("(1 + 1/x) * erf(x)", "2 / sqrt(pi)")]
        [InlineData("(abs(x)^(x/pi))^sgn(x)", "1")]
        public void TheSidesAgree(string input, string expected)
        {
            var limit = input.ToEntity().Limit("x", 0);
            Assert.False(limit.ContainsNode("x"), $"the limit of {input} was not taken: {limit}");
            var difference = (limit - expected.ToEntity()).EvalNumerical();
            Assert.True(difference is Entity.Number.Real { IsFinite: true } real && real.Abs() < 1e-20, $"the limit of {input} is {limit}, not {expected}");
        }

        // Where the sides differ there is still no limit.
        [Theory]
        [InlineData("floor(x)")]
        [InlineData("sgn(x)")]
        [InlineData("erfc(x) / x")]
        public void TheSidesStillDiffer(string input)
            => Assert.Equal(MathS.NaN, input.ToEntity().Limit("x", 0));
    }
}
