//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// In a limit, (g^a)^b with a and b real constants is g^(a b) where g is positive near the point. Read as written,
    /// sqrt(x^3) / x at 0 from the right was 0 only after four seconds of l'Hopital's rule, and floor(sqrt(x^3)) took six,
    /// asking it for the sign of sqrt(x^3). Where g is negative the two differ, so from the left and from both sides
    /// nothing changes. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerOfAPowerIsOnePowerTest
    {
        [Theory]
        [InlineData("sqrt(x^3) / x", "0")]
        [InlineData("floor(sqrt(x^3))", "0")]
        [InlineData("sgn(sqrt(x^3))", "1")]
        [InlineData("sgn(sqrt(x^5))", "1")]
        [InlineData("sqrt(x^3) / x^(3/2)", "1")]
        public void AtZeroFromTheRight(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", 0, ApproachFrom.Right));

        [Fact]
        public void BothSidesOfAQuotientAgree()
            => Assert.Equal("0".ToEntity(), "sqrt(x^3) / x".ToEntity().Limit("x", 0, ApproachFrom.BothSides));

        // Left of 0, x^3 is negative and sqrt(x^3) is imaginary: floor of it has no limit to give.
        [Theory]
        [InlineData(ApproachFrom.Left)]
        [InlineData(ApproachFrom.BothSides)]
        public void ANegativeBaseIsLeftAlone(ApproachFrom side)
            => Assert.IsType<Entity.Limitf>("floor(sqrt(x^3))".ToEntity().Limit("x", 0, side));
    }
}
