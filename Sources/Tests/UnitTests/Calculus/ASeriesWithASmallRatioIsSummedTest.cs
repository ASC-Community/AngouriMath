//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The series asked whether a ratio, or the limit of a term, was zero by
    /// <see cref="Number.IsZero(Number.Complex)"/>, which takes anything below 1e-16 for zero: a
    /// geometric series with ratio <c>sqrt(2) 10^(-20)</c> was left unsummed, and a series whose terms
    /// tend to it was not found to diverge. A ratio that is zero still leaves the sum as it is.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASeriesWithASmallRatioIsSummedTest
    {
        [Theory]
        [InlineData("sum((sqrt(2) * 10^(-20))^n, n, 0, +oo)", "1 / (1 - sqrt(2) * 10^(-20))")]
        [InlineData("sum(n * (sqrt(2) * 10^(-20))^n, n, 0, +oo)", "sqrt(2) * 10^(-20) / (1 - sqrt(2) * 10^(-20))^2")]
        [InlineData("sum((sqrt(2) * 10^(-20))^n / n!, n, 0, +oo)", "e^(sqrt(2) * 10^(-20))")]
        [InlineData("sum((2 * e^(-50))^n, n, 1, +oo)", "2 e^(-50) / (1 - 2 e^(-50))")]
        public void TheSumIsItsClosedForm(string sum, string expected)
        {
            var value = sum.ToEntity().Simplify();
            Assert.IsNotType<Summationf>(value);
            var difference = Assert.IsAssignableFrom<Number.Complex>((value - expected.ToEntity()).Evaled);
            Assert.True(difference.IsZero, $"{sum} is {value}");
        }

        [Theory]
        [InlineData("sum(sqrt(2) * 10^(-20) + 1/n^2, n, 1, +oo)")]
        [InlineData("sum(sqrt(2) * 10^(-20), n, 1, +oo)")]
        [InlineData("sum((sqrt(2) * 10^(-20))^n * n^n, n, 1, +oo)")]
        public void ASeriesWhoseTermsDoNotTendToZeroDiverges(string sum)
            => Assert.Equal(Number.Real.PositiveInfinity, sum.ToEntity().Simplify());

        [Fact]
        public void AZeroRatioIsLeftAsItIs()
            => Assert.IsType<Summationf>("sum((sqrt(2) - sqrt(2))^n, n, 0, +oo)".ToEntity().Simplify());
    }
}
