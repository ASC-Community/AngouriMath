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

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// The binomial coefficient's identities: Pascal's rule, the chairperson identity and the
    /// symmetry as rewrite rules, and the binomial theorem read backwards for a sum written
    /// with <c>binomial(n, k)</c>. Item 7 of the #1409 docket (Sullivan and Mackey,
    /// §8.4: Props 8.4.1–8.4.4, Thm 8.4.8, Ex 8.4.9, §8.4.5 Try 3).
    /// <see href="https://github.com/asc-community/AngouriMath/issues/1409"/>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class BinomialIdentityTest
    {
        /// <summary>Props 8.4.1–8.4.3 as equalities of symbols, decided by rewriting one side into the other.</summary>
        [Theory]
        [InlineData("binomial(n, k) = binomial(n, n - k)")]
        [InlineData("binomial(n, k) = binomial(n - 1, k) + binomial(n - 1, k - 1)")]
        [InlineData("binomial(n + 1, k + 1) = binomial(n, k + 1) + binomial(n, k)")]
        [InlineData("k * binomial(n, k) = n * binomial(n - 1, k - 1)")]
        public void AnIdentityIsTrue(string identity)
            => Assert.Equal(Boolean.True, identity.ToEntity().Simplify());

        /// <summary>The rewrites themselves, each in the direction that collects.</summary>
        [Theory]
        [InlineData("binomial(n - 1, k) + binomial(n - 1, k - 1)", "binomial(n, k)")]
        [InlineData("binomial(n, n - 2)", "binomial(n, 2)")]
        [InlineData("binomial(n, n - k)", "binomial(n, k)")]
        [InlineData("n * binomial(n - 1, k - 1)", "k * binomial(n, k)")]
        [InlineData("binomial(7, 3) + binomial(7, 2)", "56")]
        public void ARewriteCollects(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        /// <summary>The symmetry fires only where the complement is the smaller expression: <c>binomial(n, 2)</c> stays.</summary>
        [Fact]
        public void TheSymmetryPrefersTheSmallerLowerIndex()
        {
            Assert.Equal("binomial(n, 2)".ToEntity(), "binomial(n, 2)".ToEntity().Simplify());
            Assert.Equal("binomial(n, k)".ToEntity(), "binomial(n, k)".ToEntity().Simplify());
        }

        /// <summary>Thm 8.4.8, Prop 8.4.4, §8.4.5 Try 3 and Ex 8.4.9: the sum written with the node, in closed form.</summary>
        [Theory]
        [InlineData("sum(binomial(n, k), k, 0, n)", "piecewise((2 ^ n) provided (n >= 0), 0 provided True)")]
        [InlineData("sum(binomial(n, k) * 2^(n - k), k, 0, n)", "piecewise((3 ^ n) provided (n >= 0), 0 provided True)")]
        [InlineData("sum(binomial(n, k) * x^k * y^(n - k), k, 0, n)", "piecewise(((x + y) ^ n) provided (n >= 0), 0 provided True)")]
        [InlineData("sum(binomial(n, n - k) * x^k, k, 0, n)", "piecewise(((x + 1) ^ n) provided (n >= 0), 0 provided True)")]
        [InlineData("sum(binomial(4, k) * 2^(4 - k), k, 0, 4)", "81")]
        [InlineData("sum(binomial(10, k), k, 0, 10)", "1024")]
        public void ABinomialSumIsTheTheoremReadBackwards(string sum, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, sum.ToEntity().Evaled);

        /// <summary>
        /// Ex 8.4.9: the alternating sum is <c>0^n</c>, which is <c>1</c> at <c>n = 0</c> and
        /// <c>0</c> above it. Written as the power it carried a condition the piecewise read as
        /// no value, and the sum was <c>0</c> at <c>n = 0</c> where it is <c>1</c> -- in the
        /// factorial spelling too, which is fixed by the same arm.
        /// </summary>
        [Theory]
        [InlineData("sum(binomial(n, k) * (-1)^k, k, 0, n)", "piecewise(1 provided (n = 0), 0 provided True)")]
        [InlineData("sum(n! / (k! * (n - k)!) * (-1)^k, k, 0, n)", "piecewise(1 provided (n = 0), 0 provided True)")]
        [InlineData("sum(binomial(0, k) * (-1)^k, k, 0, 0)", "1")]
        [InlineData("sum(binomial(4, k) * (-1)^k, k, 0, 4)", "0")]
        public void TheAlternatingSumIsOneAtZero(string sum, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, sum.ToEntity().Evaled);
    }
}
