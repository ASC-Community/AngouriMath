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
    /// A summation of a polynomial in the index times a power with the index in the exponent
    /// is summed in closed form, to a bound or to <c>+oo</c>: the discrete antiderivative
    /// <c>q(k) r^k</c>. §5.3.4 Try 6 of Sullivan and Mackey's <i>An Introduction to Proofs</i>,
    /// <c>sum((-1)^(k - 1) k^2, k, 1, n) = (-1)^(n - 1) n (n + 1)/2</c>, is the row that asked
    /// for it. <see href="https://github.com/asc-community/AngouriMath/issues/1409"/>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class PolynomialGeometricSeriesTest
    {
        /// <summary>The closed form against the sum written out, at a bound past the expansion's hundred terms and at small ones.</summary>
        [Theory]
        [InlineData("sum(k * 2^k, k, 1, n)", "(n - 1) * 2^(n + 1) + 2")]
        [InlineData("sum((-1)^(k - 1) * k^2, k, 1, n)", "(-1)^(n - 1) * n * (n + 1) / 2")]
        [InlineData("sum(k * 3^k, k, 0, n)", "((2 n - 1) * 3^(n + 1) + 3) / 4")]
        [InlineData("sum((k^2 + 1) * 3^(2 k + 1), k, 2, n)", "3 * ((77/256 - 9/32 * (n + 1) + (n + 1)^2 / 8) * 9^(n + 1) - 4941/256)")]
        [InlineData("sum(k^3 * (-2)^(k - 1), k, 1, n)", "sum(k^3 * (-2)^(k - 1), k, 1, n)")]
        public void AFiniteRangeIsSummedInClosedForm(string sum, string expected)
        {
            var closed = sum.ToEntity().Simplify();
            Assert.IsNotType<Summationf>(closed);
            foreach (var n in new[] { 1, 2, 3, 7, 12, 150 })
            {
                var want = expected.ToEntity().Substitute("n", n).Evaled;
                Assert.Equal(want, closed.Substitute("n", n).Evaled);
                Assert.Equal(want, sum.ToEntity().Substitute("n", n).Evaled);
            }
        }

        [Fact]
        public void AnEmptyRangeIsZero()
            => Assert.Equal(Number.Integer.Zero, "sum(k * 2^k, k, 1, n)".ToEntity().Simplify().Substitute("n", 0).Evaled);

        [Theory]
        [InlineData("sum(k / 2^k, k, 1, +oo)", "2")]
        [InlineData("sum(k * 2^(-k), k, 0, +oo)", "2")]
        [InlineData("sum(k^2 / 3^k, k, 0, +oo)", "3/2")]
        [InlineData("sum((k + 1) * (-1/2)^k, k, 0, +oo)", "4/9")]
        public void ASeriesWithANumericRatioBelowOneIsSummed(string sum, string expected)
            => Assert.Equal(expected.ToEntity(), sum.ToEntity().Simplify());

        [Fact]
        public void ASeriesWithASymbolicRatioCarriesItsConditionAndItsRatioOneCase()
        {
            var value = "sum(k * x^k, k, 0, +oo)".ToEntity().Simplify();
            Assert.IsNotType<Summationf>(value);
            Assert.Equal("2".ToEntity(), value.Substitute("x", "1/2").Simplify());
            Assert.Equal("-2/9".ToEntity(), value.Substitute("x", "-1/2").Simplify());
            Assert.NotEqual("2".ToEntity(), value.Substitute("x", "2").Simplify());

            var finite = "sum(k^2 * q^k, k, 0, n)".ToEntity().Simplify();
            Assert.IsNotType<Summationf>(finite);
            // At q = 1 the summand is k^2 and the sum is n(n + 1)(2n + 1)/6, not a division by zero.
            Assert.Equal("30".ToEntity(), finite.Substitute("q", 1).Substitute("n", 4).Evaled);
            Assert.Equal("sum(k^2 * 2^k, k, 0, 4)".ToEntity().Evaled, finite.Substitute("q", 2).Substitute("n", 4).Evaled);
        }

        /// <summary>The statement the book proves by induction is decided from the closed form.</summary>
        [Theory]
        [InlineData("forall n in ZZ+ : sum((-1)^(k-1)*k^2, k, 1, n) = (-1)^(n-1)*n*(n+1)/2")]
        [InlineData("forall n in ZZ+ : sum(k*2^k, k, 1, n) = (n-1)*2^(n+1) + 2")]
        public void TheIdentityIsDecided(string statement) => Assert.Equal(Boolean.True, statement.ToEntity().Evaled);
    }
}
