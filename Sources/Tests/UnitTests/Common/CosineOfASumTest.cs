//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The expansion rules opened a sine of a sum and not a cosine, so no shift of a cosine was
    /// reduced: <c>sin(pi - x) - sin(x)</c> was 0 and <c>cos(pi - x) + cos(x)</c> stayed as it
    /// was, and so did <c>cos(s + t) - cos(s) cos(t) + sin(s) sin(t)</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2135">#2135</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class CosineOfASumTest
    {
        [Theory]
        [InlineData("cos(pi - x) + cos(x)")]
        [InlineData("cos(x - pi) + cos(x)")]
        [InlineData("cos(pi/2 + x) + sin(x)")]
        [InlineData("cos(s + t) - cos(s) * cos(t) + sin(s) * sin(t)")]
        [InlineData("cos(s - t) - cos(s) * cos(t) - sin(s) * sin(t)")]
        [InlineData("cos(pi/3 - x) - cos(x) / 2 - sqrt(3) * sin(x) / 2")]
        public void TheIdentityIsZero(string identity)
            => Assert.Equal(Integer.Zero, identity.Simplify());

        /// <summary>
        /// Where the shifted form is longer than what it reduces to, the reduced one is the
        /// answer. <c>cos(pi - x)</c> on its own stays, as <c>sin(pi + x)</c> always has:
        /// <c>-cos(x)</c> is rated no shorter.
        /// </summary>
        [Theory]
        [InlineData("cos(pi/2 - x)", "sin(x)")]
        [InlineData("cos(2 * pi - x)", "cos(x)")]
        public void AShiftIsReducedWhereThatIsShorter(string shifted, string reduced)
            => Assert.Equal(reduced.ToEntity(), shifted.Simplify());

        [Theory]
        [InlineData("cos(a + b)", "cos(a) * cos(b) - sin(a) * sin(b)")]
        [InlineData("cos(a - b)", "cos(a) * cos(b) + sin(a) * sin(b)")]
        public void ExpandOpensACosineOfASum(string cosine, string expanded)
            => Assert.Equal(Integer.Zero, (cosine.ToEntity().Expand() - expanded).Simplify());
    }
}
