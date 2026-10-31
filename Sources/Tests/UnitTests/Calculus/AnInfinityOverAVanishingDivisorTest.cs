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
    /// A quotient whose dividend tends to an infinity and whose divisor vanishes from one side tends to the infinity of
    /// the two signs' product. The quotient of the parts' limits, (-oo) / 0, says nothing about the divisor's side and is
    /// NaN, and the rule that reads the side asked for a finite dividend: ln(x^2) / (cos(x) - 1) at 0, +oo from both
    /// sides, was NaN, the claim that it has no limit.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2274">#2274</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnInfinityOverAVanishingDivisorTest
    {
        [Theory]
        [InlineData("limit(ln(x^2) / ln(1 - x^2/2), x, 0)", "+oo")]
        [InlineData("limit(ln(x^2) / (cos(x) - 1), x, 0)", "+oo")]
        [InlineData("limit(ln(abs(x)) / ln(cos(x)), x, 0)", "+oo")]
        [InlineData("limit(ln(x^2) / ln(cos(x)), x, 0)", "+oo")]
        [InlineData("limitright(ln(x) / sin(x), x, 0)", "-oo")]
        [InlineData("limitleft(ln(-x) / sin(x), x, 0)", "+oo")]
        [InlineData("limitright((1/x) / sin(x), x, 0)", "+oo")]
        [InlineData("limit(1/x^2, x, 0)", "+oo")]
        public void ItIsTheInfinityOfTheSigns(string limit, string value)
            => Assert.Equal(value.ToEntity(), limit.ToEntity().Simplify());

        /// <summary>Where the sides disagree, or one has no real value, there is still no limit.</summary>
        [Theory]
        [InlineData("limit(1/x, x, 0)")]
        [InlineData("limit(x^x, x, 0)")]
        [InlineData("limit(ln(x) / x, x, 0)")]
        public void WhereTheSidesDoNotAgreeThereIsNone(string limit)
            => Assert.Equal(MathS.NaN, limit.ToEntity().Simplify());
    }
}
