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
    /// At a finite point l'Hopital's rule reads the quotient it gets term by term by each factor's order, and now counts
    /// the powers of the logarithm of the distance to the point too, from the right: (x - sin(x)) * ln(x) / x^3 at 0 steps
    /// to ((1 - cos(x)) * ln(x) + (x - sin(x)) / x) / (3x^2), which is ln(x) / 6 to leading order. A logarithm of
    /// c * d^k is k * ln(d) to leading order. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ALogarithmIsReadByItsOrderTest
    {
        [Theory]
        [InlineData("(x - sin(x)) * ln(x) / x^3", "-oo")]
        [InlineData("(e^x - 1 - x) * ln(cos(x) * sqrt(x)) / x^2", "-oo")]
        [InlineData("(e^x - 1 - x) * ln(x^2 * cos(x)) / x^2", "-oo")]
        [InlineData("(x - sin(x)) * ln(x) / x^2", "0")]
        [InlineData("(1 - cos(x)) * ln(sin(x)) / x^2", "-oo")]
        [InlineData("x * ln(x)^2 / sin(x)", "+oo")]
        [InlineData("(x - sin(x)) / (x^3 * ln(x))", "0")]
        public void TheLimitIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);
    }
}
