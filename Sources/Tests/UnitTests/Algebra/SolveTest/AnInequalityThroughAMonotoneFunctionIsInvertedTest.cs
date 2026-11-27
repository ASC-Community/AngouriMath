//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An inequality through a logarithm, an exponential with a constant base or an even root
    /// was refused with <c>NotSufficientlySupportedException</c>: <c>ln(x) &gt; 1</c> had no
    /// answer. Each of these is strictly monotone, so the inequality is now the one inside it,
    /// with the function's domain, and reversed for a base below 1.
    /// https://github.com/asc-community/AngouriMath/issues/2585
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnInequalityThroughAMonotoneFunctionIsInvertedTest
    {
        [Theory]
        [InlineData("ln(x) > 1", "(e; +oo)")]
        [InlineData("e^x < 2", "(-oo; ln(2))")]
        [InlineData("2^x >= 8", "[3; +oo)")]
        [InlineData("ln(x - 1) <= 0", "(1; 2]")]
        [InlineData("sqrt(x) < 2", "[0; 4)")]
        [InlineData("1 < ln(x)", "(e; +oo)")]
        [InlineData("(1/2)^x > 4", "(-oo; -2)")]
        [InlineData("log(1/2, x) >= 1", "(0; 1/2]")]
        [InlineData("e^x > -1", "RR")]
        [InlineData("e^x < 0", "{}")]
        [InlineData("sqrt(x) <= -1", "{}")]
        [InlineData("2^(x^2) <= 16", "[-2; 2]")]
        public void TheAnswerIsTheSetItHoldsOn(string inequality, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified,
                            inequality.ToEntity().Solve("x").Simplify().InnerSimplified);

        /// <summary>
        /// Checked at points on both sides of every endpoint and at the endpoints themselves,
        /// against the inequality evaluated there, rather than by how the answer prints.
        /// </summary>
        [Theory]
        [InlineData("ln(x) > 1")]
        [InlineData("ln(x^2) < 0")]
        [InlineData("ln(1/x) > 0")]
        [InlineData("log(1/2, x) >= 1")]
        [InlineData("e^(1/x) > 0")]
        [InlineData("2^(1/(x - 1)) >= -3")]
        [InlineData("3^(x - 1) < 1/3")]
        [InlineData("sqrt(x) > -1")]
        [InlineData("sqrt(x^2 - 1) < 1")]
        [InlineData("x^(3/2) < 8")]
        [InlineData("2 >= sqrt(x + 1)")]
        public void TheAnswerHoldsExactlyWhereTheInequalityDoes(string inequality)
        {
            Variable x = "x";
            var solved = (Set)inequality.ToEntity().Solve(x).Simplify();
            foreach (var point in new Entity[] { -5, -3, -2, "-sqrt(2)".ToEntity(), -1, "-1/2".ToEntity(), 0,
                                                 "1/2".ToEntity(), 1, "sqrt(2)".ToEntity(), 2, "e".ToEntity(),
                                                 3, 4, 5 })
            {
                var substituted = inequality.ToEntity().Substitute(x, point);
                // No real value there, as ln(x) at x = -1, 1/x at 0, or ln(0), which Simplify
                // reads as -oo and compares, but which the reals do not have.
                if (System.Linq.Enumerable.Any(substituted.DirectChildren, side => side.Evaled is not Number.Real { IsFinite: true }))
                    continue;
                if (substituted.Simplify() is not Boolean truth)
                    continue;
                Assert.True(solved.Contains(point) == (bool)truth,
                    $"{inequality} solved to {solved}, which disagrees with it at x = {point}");
            }
        }

        /// <summary>A bound with a free variable has no known sign, so the inequality is still declined.</summary>
        [Fact]
        public void ASymbolicBoundIsDeclined()
            => Assert.Throws<AngouriMath.Core.Exceptions.NotSufficientlySupportedException>(
                () => "ln(x) > a".ToEntity().Solve("x"));
    }
}
