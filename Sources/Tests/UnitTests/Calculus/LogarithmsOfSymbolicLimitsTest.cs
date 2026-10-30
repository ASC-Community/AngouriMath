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
    /// A quotient of logarithms whose arguments tend to symbolic values tends to the quotient of those values' logarithms,
    /// the logarithm being continuous. It answered the quotient of the values themselves: ln(y + x) / ln(z + x) at 0 was
    /// y / z, while the same limit with numbers, ln(x + 2) / ln(x + 3), was ln(2) / ln(3).
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2271">#2271</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LogarithmsOfSymbolicLimitsTest
    {
        [Theory]
        [InlineData("limit(ln(y + x) / ln(z + x), x, 0)", "ln(y) / ln(z)")]
        [InlineData("limitright(ln(a + x) / ln(b + x), x, 0)", "ln(a) / ln(b)")]
        [InlineData("limit(ln(a + 1/x) / ln(b + 1/x), x, +oo)", "ln(a) / ln(b)")]
        [InlineData("limit(ln(a + e^(-x)) / ln(b + e^(-x)), x, +oo)", "ln(a) / ln(b)")]
        [InlineData("limit(log(b + 1/x, a + 1/x), x, +oo)", "ln(a) / ln(b)")]
        [InlineData("limit(ln(1 + x) / ln(y + x), x, 0)", "0 provided not ln(y) = 0")]
        [InlineData("limit(ln(x + 2) / ln(x + 3), x, 0)", "ln(2) / ln(3)")]
        public void ItIsTheQuotientOfTheLogarithms(string limit, string value)
            => Assert.Equal(value.ToEntity(), limit.ToEntity().Simplify());
    }
}
