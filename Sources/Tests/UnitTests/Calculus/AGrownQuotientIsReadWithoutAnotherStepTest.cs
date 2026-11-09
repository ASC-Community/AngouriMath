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
    /// l'Hopital's rule bounds how much its derivatives' quotient may grow, and one past the bound was taken only where
    /// it had a value at the point. (sqrt(x) - erf(x)) / x at 0 from the right steps to
    /// 1/2 * x^(-1/2) - 2 / sqrt(pi) * e^(-x^2), which is +oo there, so it was left unevaluated where 2.6.0 answered it.
    /// Such a quotient is now also read term by term, each a value at the point or a pole whose sign the side decides.
    /// Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AGrownQuotientIsReadWithoutAnotherStepTest
    {
        [Theory]
        [InlineData("(sqrt(x) - erf(x)) / x", "+oo")]
        [InlineData("(sqrt(x) - erf(x)) / (2 * x)", "+oo")]
        [InlineData("(sqrt(x) + erf(x)) / x", "+oo")]
        [InlineData("(erf(x) - sqrt(x)) / x", "-oo")]
        [InlineData("(x^(1/3) - erf(x)) / x", "+oo")]
        [InlineData("floor(sqrt(x) - erf(x))", "0")]
        [InlineData("(1 - cos(x)) / x^3", "+oo")]
        public void AtZeroFromTheRight(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", 0, ApproachFrom.Right));

        // Left of 0, sqrt(x) is imaginary and the quotient has no real limit to give.
        [Fact]
        public void FromTheLeftNothingChanges()
            => Assert.IsType<Entity.Limitf>("(sqrt(x) - erf(x)) / x".ToEntity().Limit("x", 0, ApproachFrom.Left));
    }
}
