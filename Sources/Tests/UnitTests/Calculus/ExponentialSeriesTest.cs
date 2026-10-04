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
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// https://github.com/asc-community/AngouriMath/issues/1212, question I.1: a summation to
    /// +oo of a polynomial times a power over a factorial of the index is a closed form in e.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialSeriesTest
    {
        private static void AgreesTo(string expected, Entity actual, int digits = 25)
        {
            Assert.IsNotType<Summationf>(actual);
            var want = (Real)expected.ToEntity().EvalNumerical();
            var got = (Real)actual.EvalNumerical();
            var gap = (want - got).Abs();
            Assert.True(gap < Real.Create(PeterO.Numbers.EDecimal.Create(1, -digits)),
                $"{actual} is {got}, expected {want}");
        }

        [Theory]
        [InlineData("sum(3^(k+2) * (k^2 + k + 1) / (k + 3)!, k, 0, +oo)", "(8 * e^3 - 41) / 6")]
        [InlineData("sum(1 / k!, k, 0, +oo)", "e")]
        [InlineData("sum(1 / (2 * n!), n, 1, +oo)", "(e - 1) / 2")]
        [InlineData("sum(k / k!, k, 0, +oo)", "e")]
        [InlineData("sum(k^2 / k!, k, 0, +oo)", "2 * e")]
        [InlineData("sum(k^3 / k!, k, 0, +oo)", "5 * e")]
        [InlineData("sum(2^k / k!, k, 3, +oo)", "e^2 - 5")]
        [InlineData("sum((k^2 + 1) * 3^k / (2 * (k + 1)!), k, 0, +oo)", "(4 * e^3 - 1) / 3")]
        [InlineData("sum((-1)^k / k!, k, 0, +oo)", "1 / e")]
        [InlineData("sum(1 / (k + 2)!, k, 0, +oo)", "e - 2")]
        public void TheSeriesIsAClosedFormInE(string sum, string expected)
            => AgreesTo(expected, sum.ToEntity().Simplify());

        [Theory]
        [InlineData("sum(x^k / k!, k, 0, +oo)", "e^x")]
        [InlineData("sum(x^k / k!, k, 1, +oo)", "e^x - 1")]
        [InlineData("sum(k * x^k / k!, k, 0, +oo)", "x * e^x")]
        public void ASymbolicBaseStaysSymbolic(string sum, string expected)
        {
            var actual = sum.ToEntity().Simplify();
            Assert.IsNotType<Summationf>(actual);
            Assert.Equal(expected.ToEntity().Simplify(), actual);
        }

        // Not of the shape: no factorial, a factorial in the numerator, a symbolic lower bound,
        // a power whose exponent is twice the index, a finite upper bound handled elsewhere.
        [Theory]
        [InlineData("sum(1 / k, k, 1, +oo)")]
        [InlineData("sum(k!, k, 0, +oo)")]
        [InlineData("sum(x^k / k!, k, n, +oo)")]
        [InlineData("sum(3^(2 * k) / k!, k, 0, +oo)")]
        [InlineData("sum(1 / (k - 2)!, k, 0, +oo)")]
        public void WhatIsNotOfTheShapeIsLeftAsWritten(string sum)
            => Assert.IsType<Summationf>(sum.ToEntity().Simplify());

        [Fact]
        public void AFiniteRangeIsStillExpanded()
            => Assert.Equal("5/2".ToEntity(), "sum(1 / k!, k, 0, 2)".ToEntity().Simplify());
    }
}
