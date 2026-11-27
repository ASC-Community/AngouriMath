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
    /// An inequality with <c>abs</c> of <c>x</c> in it was refused with
    /// <c>NotSufficientlySupportedException</c>: <c>abs(2x - 3) &lt;= 5</c> had no answer.
    /// It is now two inequalities without the <c>abs</c>, one on each half of the line where
    /// its argument keeps a sign.
    /// https://github.com/asc-community/AngouriMath/issues/2579
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnInequalityInAbsIsSplitBySignTest
    {
        [Theory]
        [InlineData("abs(2x - 3) <= 5", "[-1; 4]")]
        [InlineData("abs(x) < 0", "{}")]
        [InlineData("abs(x) > x", "(-oo; 0)")]
        [InlineData("abs(x - 1) + abs(x + 1) < 4", "(-2; 2)")]
        [InlineData("abs(x - 1) >= abs(x + 1)", "(-oo; 0]")]
        public void TheAnswerIsTheSetItHoldsOn(string inequality, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified,
                            inequality.ToEntity().Solve("x").Simplify().InnerSimplified);

        /// <summary>
        /// Checked at points on both sides of every endpoint and at the endpoints themselves,
        /// against the inequality evaluated there, rather than by how the answer prints.
        /// </summary>
        [Theory]
        [InlineData("abs(x) > 1")]
        [InlineData("abs(x) >= 1")]
        [InlineData("abs(x^2 - 4) < 3")]
        [InlineData("abs(1/x) > 1")]
        [InlineData("abs(x) >= -1")]
        [InlineData("abs(x - pi) <= 1")]
        [InlineData("abs(abs(x) - 2) < 1")]
        [InlineData("abs(x^2 - 1) > abs(x)")]
        [InlineData("2 < abs(3 - x)")]
        public void TheAnswerHoldsExactlyWhereTheInequalityDoes(string inequality)
        {
            Variable x = "x";
            var solved = (Set)inequality.ToEntity().Solve(x).Simplify();
            foreach (var point in new Entity[] { -5, -3, -2, "-sqrt(7)".ToEntity(), "-3/2".ToEntity(), -1,
                                                 "-1/2".ToEntity(), 0, "1/2".ToEntity(), 1, "3/2".ToEntity(),
                                                 2, "sqrt(7)".ToEntity(), 3, "pi + 1".ToEntity(), 5 })
            {
                var holds = inequality.ToEntity().Substitute(x, point).Simplify();
                if (holds is not Boolean truth)
                    continue; // x = 0 in abs(1/x): no value, so neither in nor out says anything
                Assert.True(solved.Contains(point) == (bool)truth,
                    $"{inequality} solved to {solved}, which disagrees with it at x = {point}");
            }
        }

        /// <summary>
        /// <c>sqrt(x)</c> is imaginary for a negative <c>x</c>, so <c>abs(sqrt(x))</c> is not
        /// <c>sqrt(x)</c> or <c>-sqrt(x)</c> there and the split does not apply: still declined.
        /// </summary>
        [Fact]
        public void AnArgumentThatCanBeImaginaryIsNotSplit()
            => Assert.Throws<AngouriMath.Core.Exceptions.NotSufficientlySupportedException>(
                () => "abs(sqrt(x)) > 1".ToEntity().Solve("x"));
    }
}
