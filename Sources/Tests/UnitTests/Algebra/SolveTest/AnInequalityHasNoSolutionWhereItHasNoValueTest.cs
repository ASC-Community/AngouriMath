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
    /// An inequality holds only where its expression has a value. The solver reads a quotient after
    /// a factor its numerator and denominator share has cancelled, so its answer had the points where
    /// that factor is zero, at which the quotient is 0/0: <c>(4x - x^3)/x &gt; 0</c> was
    /// <c>(-2; 2)</c>, with 0 in it, and <c>x/x &gt; 0</c> was <c>RR</c>. Found by a cross-check of
    /// rational inequalities against their exact values; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnInequalityHasNoSolutionWhereItHasNoValueTest
    {
        [Theory]
        [InlineData("(4x - x^3)/x > 0", "1", true)]
        [InlineData("(4x - x^3)/x > 0", "0", false)]
        [InlineData("x/x > 0", "5", true)]
        [InlineData("x/x > 0", "0", false)]
        [InlineData("(-x + 2x^2 - x^3)/x <= 0", "-3", true)]
        [InlineData("(-x + 2x^2 - x^3)/x <= 0", "0", false)]
        [InlineData("(x^2 - 1)/(x - 1) >= 0", "-1", true)]
        [InlineData("(x^2 - 1)/(x - 1) >= 0", "1", false)]
        [InlineData("(x^2 - 1)/(x^2 - 1) < 2", "1", false)]
        [InlineData("(x^2 - 1)/(x^2 - 1) < 2", "-1", false)]
        [InlineData("(x^2 - 1)/(x^2 - 1) < 2", "0", true)]
        public void APointWithoutAValueIsNotASolution(string inequality, string at, bool solves)
        {
            var answer = (Set)inequality.ToEntity().Solve("x");
            Assert.True(answer.TryContains(at.ToEntity(), out var contains), $"{at} undecided in {answer}");
            Assert.Equal(solves, contains);
        }
    }
}
