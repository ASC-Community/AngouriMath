//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A quotient is zero exactly where its numerator is and its denominator is not. Left to the
    /// spellings, <c>(18x^2 - 3x^3 - 4x^4 + x^5)/(9x^2 - 6x^4 + x^6) = 0</c> was taken by a
    /// substitution <c>t = x^2</c> into half-integer powers of <c>t</c> and given to Newton's method,
    /// which answered three near-copies of 3 beside it and left out -2; and the inequality with it
    /// for its <c>= 0</c> part lost -2 as well. Found by a cross-check of rational inequalities
    /// against their exact values; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AQuotientIsZeroWhereItsNumeratorIsTest
    {
        [Theory]
        [InlineData("(18x^2 - 3x^3 - 4x^4 + x^5)/(9x^2 - 6x^4 + x^6) = 0", "{ -2, 3 }")]
        [InlineData("(108x^2 - 18x^3 - 24x^4 + 6x^5)/(27x^2 - 18x^4 + 3x^6) = 0", "{ -2, 3 }")]
        // Zeros of the numerator that the denominator shares are not roots: 0/0 has no value.
        [InlineData("(x^2 - 1)/(x - 1) = 0", "{ -1 }")]
        [InlineData("(x^4 - 9x^2)/(x^6 - 3x^4) = 0", "{ 3, -3 }")]
        public void TheRootsAreTheNumeratorsTheDenominatorDoesNotShare(string equation, string expected)
            => Assert.Equal(expected.ToEntity(), equation.ToEntity().Solve("x"));

        [Fact]
        public void TheNonStrictInequalityKeepsTheRootsOfItsEquation()
        {
            var answer = (Set)"(108x^2 - 18x^3 - 24x^4 + 6x^5)/(27x^2 - 18x^4 + 3x^6) >= 0".ToEntity().Solve("x");
            foreach (var (at, inside) in new[] { ("-2", true), ("3", true), ("1", true), ("-3", false), ("0", false), ("sqrt(3)", false) })
            {
                Assert.True(answer.TryContains(at.ToEntity(), out var contains), $"{at} undecided in {answer}");
                Assert.Equal(inside, contains);
            }
        }
    }
}
