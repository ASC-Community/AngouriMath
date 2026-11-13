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
    /// A two-sided limit whose sides stopped at an indeterminate form with an infinity in it, 0 * floor(-oo) for
    /// x floor(2/x) at 0, answered NaN, which says there is no limit, where each side is 2. Such a side is undecided,
    /// so the limit is left unevaluated. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnUndecidedSideLeavesTheLimitUnevaluatedTest
    {
        [Theory]
        [InlineData("x * floor(2/x)")]
        [InlineData("floor(1/x) * sin(x)")]
        [InlineData("x^2 * floor(1/x^2)")]
        [InlineData("x * ceil(1/x)")]
        public void IsNotNaN(string input)
            => Assert.IsType<Entity.Limitf>(input.ToEntity().Limit("x", 0));

        // A side that has no limit, or two sides that differ, still mean there is none: 0 / 0 everywhere, infinities
        // in different directions, an oscillation, a jump.
        [Theory]
        [InlineData("(x - x) * sin(x) / ((x - x) * x)", "0")]
        [InlineData("ln(-2) / ln(x)", "1")]
        [InlineData("sin(1/x)", "0")]
        [InlineData("floor(2/x)", "0")]
        [InlineData("floor(x)", "0")]
        public void IsStillNaN(string input, string destination)
            => Assert.Equal(MathS.NaN, input.ToEntity().Limit("x", destination));
    }
}
