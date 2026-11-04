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
    /// A function of a quotient that tends to 0/0 took the quotient's limit from the descent alone, which reads 0/0 off
    /// the two parts as NaN -- the claim that there is no limit -- where only the descent cannot tell: <c>sin(x)/x</c> at 0
    /// is 1, and <c>(sin(x)/x)^2</c> and <c>exp(sin(x)/x)</c> were NaN two-sided and unevaluated from either side. The
    /// quotient's whole limit is asked now, once, and only its value is taken. Found while checking a limit
    /// limitcheck reported; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AFunctionOfAnIndeterminateQuotientTest
    {
        [Theory]
        [InlineData("(sin(x)/x)^2", ApproachFrom.BothSides, "1")]
        [InlineData("exp(sin(x)/x)", ApproachFrom.BothSides, "e")]
        [InlineData("exp(sin(x)/x)", ApproachFrom.Right, "e")]
        [InlineData("sin(sin(x)/x)", ApproachFrom.BothSides, "sin(1)")]
        [InlineData("exp((cos(x) - 1)/x^2)", ApproachFrom.BothSides, "e^(-1/2)")]
        [InlineData("sqrt(sin(x)/x)", ApproachFrom.Right, "1")]
        // As before.
        [InlineData("sin(x)/x", ApproachFrom.BothSides, "1")]
        [InlineData("exp(x/x)", ApproachFrom.BothSides, "e")]
        public void HasTheValueOfTheFunctionAtTheQuotientsLimit(string input, ApproachFrom side, string expected)
        {
            var limit = MathS.FromString(input).Limit("x", 0, side);
            Assert.IsAssignableFrom<Number>(limit.Evaled);
            var difference = (limit - MathS.FromString(expected)).EvalNumerical();
            Assert.True(difference.Abs() < (Number.Real)1e-30, $"the limit of {input} at 0 is {limit}, where it is {expected}");
        }
    }
}
