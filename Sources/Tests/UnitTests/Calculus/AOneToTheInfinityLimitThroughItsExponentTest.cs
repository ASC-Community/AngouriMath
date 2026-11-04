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
    /// The second remarkable limit writes a 1^oo power <c>f^g</c> as <c>e^(g (f - 1))</c>, and the product in its exponent
    /// the descent reads as 0 * oo, NaN, where only a base of 1 plus a polynomial was answered by another route:
    /// <c>(cos(x))^(1/x^2)</c> at 0, which is <c>e^(-1/2)</c>, was NaN. The exponent is the quotient
    /// <c>(f - 1) / (1/g)</c> now, which a function of an indeterminate quotient asks whole. No issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AOneToTheInfinityLimitThroughItsExponentTest
    {
        [Theory]
        [InlineData("(cos(x))^(1/x^2)", ApproachFrom.BothSides, "e^(-1/2)")]
        [InlineData("(cos(x))^(1/x^2)", ApproachFrom.Right, "e^(-1/2)")]
        [InlineData("(sin(x)/x)^(1/x^2)", ApproachFrom.BothSides, "e^(-1/6)")]
        [InlineData("(cos(2x))^(1/x^2)", ApproachFrom.BothSides, "e^(-2)")]
        [InlineData("(1 + sin(x))^(1/x)", ApproachFrom.BothSides, "e")]
        [InlineData("(cos(x))^(1/x)", ApproachFrom.Right, "1")]
        // As before.
        [InlineData("(1 + x)^(1/x)", ApproachFrom.BothSides, "e")]
        [InlineData("(1 - x^2)^(1/x^2)", ApproachFrom.BothSides, "1/e")]
        public void HasItsValue(string input, ApproachFrom side, string expected)
        {
            var limit = MathS.FromString(input).Limit("x", 0, side);
            Assert.IsAssignableFrom<Number>(limit.Evaled);
            var difference = (limit - MathS.FromString(expected)).EvalNumerical();
            Assert.True(difference.Abs() < (Number.Real)1e-30, $"the limit of {input} at 0 is {limit}, where it is {expected}");
        }

        [Theory]
        [InlineData("x * ln(x)")]
        [InlineData("sin(x) * ln(x)")]
        [InlineData("x^x")]
        public void TheTwoSidedConventionIsKept(string input)
            => Assert.Equal(MathS.NaN, MathS.FromString(input).Limit("x", 0).Evaled);
    }
}
