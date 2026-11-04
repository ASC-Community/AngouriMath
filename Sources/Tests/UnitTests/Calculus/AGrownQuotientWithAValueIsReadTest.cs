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
    /// l'Hopital's rule bounds how much its derivatives' quotient may grow, which is a bound on differentiating again, and
    /// the product rule doubles <c>cos(x) * sinh(x)</c>. The quotient it turned away, <c>cos(x) * (e^x + e^(-x)) - sin(x)
    /// * (e^x - e^(-x))</c> over 2, is 1 at 0 and needed no more of it: <c>cos(x) * sinh(x) / sin(x)</c> from the right at
    /// 0 was left unevaluated, with <c>ln(sin(x)) / ln(sinh(x))</c>, and <c>(sinh(x) * (x + 2))^arcsin(2x)</c> took 5.7 s
    /// to answer 1. Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=31</c> and <c>SEED=33</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AGrownQuotientWithAValueIsReadTest
    {
        [Theory]
        [InlineData("cos(x) * sinh(x) / sin(x)", "1")]
        [InlineData("cos(x) * (e^x - e^(-x)) / sin(x)", "2")]
        [InlineData("ln(sin(x)) / ln(sinh(x))", "1")]
        [InlineData("log(sinh(x), sin(x)) * cos(x - 2)", "cos(2)")]
        [InlineData("(sinh(x) * (x + 2))^arcsin(2x)", "1")]
        public void IsTheLimitFromTheRight(string input, string expected)
        {
            var limit = MathS.FromString(input).Limit("x", 0, ApproachFrom.Right);
            Assert.False(limit is Entity.Limitf, $"lim x->0+ {input} was left unevaluated");
            var difference = (limit - MathS.FromString(expected)).EvalNumerical();
            Assert.True(difference is Entity.Number.Complex value && Entity.Number.Complex.Abs(value) is Entity.Number.Real abs && abs < 1e-10,
                $"lim x->0+ {input} = {limit}, which is not {expected}");
        }
    }
}
