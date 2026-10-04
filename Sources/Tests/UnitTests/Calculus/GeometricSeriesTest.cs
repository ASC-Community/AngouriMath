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
    /// A summation of a power with the index in the exponent is summed in closed form: the
    /// geometric series, to a bound or to <c>+oo</c>. Part of question I.2 of
    /// https://github.com/asc-community/AngouriMath/issues/1212.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class GeometricSeriesTest
    {
        [Theory]
        [InlineData("sum(2^(-k), k, 0, +oo)", "2")]
        [InlineData("sum(1 / 2^k, k, 1, +oo)", "1")]
        [InlineData("sum(3 * (1/2)^k, k, 0, +oo)", "6")]
        [InlineData("sum((-1/2)^k, k, 0, +oo)", "2/3")]
        [InlineData("sum(2^(-2k), k, 0, +oo)", "4/3")]
        [InlineData("sum(2^(1 - k), k, 0, +oo)", "4")]
        [InlineData("sum((1/3)^(k + 2), k, 0, +oo)", "1/6")]
        public void ASeriesWithANumericRatioBelowOneIsSummed(string sum, string expected)
            => Assert.Equal(expected.ToEntity(), sum.ToEntity().Simplify());

        [Fact]
        public void ASeriesWithASymbolicRatioCarriesItsCondition()
        {
            var value = "sum(x^k, k, 0, +oo)".ToEntity().Simplify();
            Assert.IsNotType<Summationf>(value);
            Assert.Equal("2".ToEntity(), value.Substitute("x", "1/2").Simplify());
            Assert.Equal("2/3".ToEntity(), value.Substitute("x", "-1/2").Simplify());
            // At x = 0 the sum is its first term, and the answer must not carry `not x = 0`
            // from an x^0 it never needed to write.
            Assert.Equal("1".ToEntity(), value.Substitute("x", "0").Simplify());
            Assert.Equal("3/2".ToEntity(), "sum(x^k, k, 1, +oo)".ToEntity().Simplify().Substitute("x", "3/5").Simplify());
        }

        // Outside the disc the series has no sum, and a substitution says so rather than
        // answering with the formula.
        [Fact]
        public void OutsideTheDiscTheFormulaIsNotAnswered()
        {
            var value = "sum(x^k, k, 0, +oo)".ToEntity().Simplify();
            Assert.NotEqual("-1".ToEntity(), value.Substitute("x", "2").Simplify());
        }

        // A finite range past the hundred terms that are expanded term by term.
        [Theory]
        [InlineData("sum(2^k, k, 0, 200)", "2^201 - 1")]
        [InlineData("sum(3 * 2^(2k + 1), k, 0, 150)", "2 * (4^151 - 1)")]
        [InlineData("sum((1/2)^k, k, 5, 400)", "(1/2)^4 - (1/2)^400")]
        public void AFiniteRangeIsSummedInClosedForm(string sum, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), sum.ToEntity().Simplify());

        // A symbolic bound: the formula where the range is not empty and the ratio is not 1,
        // the number of terms where it is 1, and 0 for an empty range.
        [Theory]
        [InlineData("2", "3", "15")]
        [InlineData("1", "3", "4")]
        [InlineData("1/2", "2", "7/4")]
        [InlineData("2", "-2", "0")]
        [InlineData("1", "-1", "0")]
        [InlineData("0", "3", "1")]
        public void ASymbolicBoundIsAnsweredAsAPiecewise(string x, string n, string expected)
        {
            var value = "sum(x^k, k, 0, n)".ToEntity().Simplify();
            Assert.IsNotType<Summationf>(value);
            Assert.Equal(expected.ToEntity(), value.Substitute("x", x).Substitute("n", n).Simplify());
        }

        [Theory]
        [InlineData("2", "4", "28")]
        [InlineData("3", "1", "0")]
        public void ASymbolicLowerBoundIsAnsweredToo(string a, string n, string expected)
        {
            var value = "sum(2^k, k, a, n)".ToEntity().Simplify();
            Assert.IsNotType<Summationf>(value);
            Assert.Equal(expected.ToEntity(), value.Substitute("a", a).Substitute("n", n).Simplify());
        }

        // Not this family: a ratio at or beyond 1 to +oo, a polynomial beside the power, an
        // exponent that is not linear in the index, a base with the index in it.
        // `sum(2^k, k, 0, +oo)` was here: this reader still declines it, a ratio at or beyond 1
        // having no sum, but the nth-term test now answers it `+oo`. See DivergentSeriesTest.
        // And `sum(k * (1/2)^k, k, 0, +oo)`: a polynomial beside the power is still not this
        // reader's, and is summed by PolynomialGeometricSeries to 2, see its test.
        [Theory]
        [InlineData("sum((-1)^k, k, 0, +oo)")]
        [InlineData("sum(2^(k^2), k, 0, 200)")]
        [InlineData("sum(k^k, k, 1, 200)")]
        [InlineData("sum(2^k, k, 0, 5/2)")]
        public void WhatIsNotAGeometricSeriesIsLeftAsWritten(string sum)
            => Assert.IsType<Summationf>(sum.ToEntity().Simplify());
    }
}
