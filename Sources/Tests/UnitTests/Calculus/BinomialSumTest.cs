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
    /// https://github.com/asc-community/AngouriMath/issues/1212, question II.3: a binomial sum
    /// with a power or a trigonometric weight is the binomial theorem read backwards.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class BinomialSumTest
    {
        private const string Binomial = "N! / (k! * (N - k)!)";

        private static void AgreesAt(string sum, string expected, params int[] ns)
        {
            var closed = sum.ToEntity().Simplify();
            Assert.IsNotType<Summationf>(closed);
            foreach (var n in ns)
            {
                var want = expected.ToEntity().Substitute("N", n).EvalNumerical();
                var got = closed.Substitute("N", n).EvalNumerical();
                Assert.True(((Complex)(want - got)).Abs() < Real.Create(PeterO.Numbers.EDecimal.Create(1, -20)),
                    $"at N = {n}: {closed} is {got}, expected {want}");
            }
        }

        [Fact]
        public void TheOrientationWeekSumIsAPowerOfThreeTimesACosine()
            => AgreesAt($"sum({Binomial} * cos(k * pi / 3), k, 0, N)", "3^(N/2) * cos(N * pi / 6)", 0, 1, 2, 3, 6, 7, 12);

        [Theory]
        [InlineData("sum(N! / (k! * (N - k)!), k, 0, N)", "2^N")]
        [InlineData("sum(N! / (k! * (N - k)!) * x^k, k, 0, N)", "(1 + x)^N")]
        [InlineData("sum(N! * x^k * y^(N - k) / (k! * (N - k)!), k, 0, N)", "(x + y)^N")]
        [InlineData("sum(N! / (k! * (N - k)!) * 3^k, k, 0, N)", "4^N")]
        [InlineData("sum(N! / (k! * (N - k)!) * sin(k * t), k, 0, N)", "(2 * cos(t / 2))^N * sin(N * t / 2)")]
        [InlineData("sum(N! / (k! * (N - k)!) * cos(k), k, 0, N)", "(2 * cos(1/2))^N * cos(N / 2)")]
        [InlineData("sum(x^k / (k! * (N - k)!), k, 0, N)", "(1 + x)^N / N!")]
        public void ABinomialSumIsClosed(string sum, string expected)
        {
            var closed = sum.ToEntity().Simplify();
            Assert.IsNotType<Summationf>(closed);
            foreach (var n in new[] { 0, 1, 2, 5 })
            {
                var want = expected.ToEntity().Substitute("N", n).Substitute("x", "3/7").Substitute("y", "2").Substitute("t", "0.7").EvalNumerical();
                var got = closed.Substitute("N", n).Substitute("x", "3/7").Substitute("y", "2").Substitute("t", "0.7").EvalNumerical();
                Assert.True(((Complex)(want - got)).Abs() < Real.Create(PeterO.Numbers.EDecimal.Create(1, -20)),
                    $"at N = {n}: {closed} is {got}, expected {want}");
            }
        }

        // The range is empty below zero and this library answers an empty range with 0, which
        // the formula does not; so a symbolic upper bound carries the condition.
        [Fact]
        public void ASymbolicUpperBoundCarriesTheRange()
        {
            var closed = $"sum({Binomial} * x^k, k, 0, N)".ToEntity().Simplify();
            Assert.Equal(0, closed.Substitute("N", -1).Substitute("x", 2).Simplify());
            Assert.Equal(9, closed.Substitute("N", 2).Substitute("x", 2).Simplify());
        }

        // A concrete bound past the expansion's hundred terms is answered by the closed form.
        [Fact]
        public void AConcreteLongRangeIsClosed()
            => Assert.Equal("2^300".ToEntity().Evaled, "sum(300! / (k! * (300 - k)!), k, 0, 300)".ToEntity().Simplify().Evaled);

        // Not of the shape: a trigonometric weight beside a power, a lower bound that is not
        // zero, a coefficient whose N is not the upper bound. A polynomial weight was here, and
        // is BinomialIdentities' now, see its test.
        [Theory]
        [InlineData("sum(N! / (k! * (N - k)!) * x^k * cos(k * t), k, 0, N)")]
        [InlineData("sum(N! / (k! * (N - k)!), k, 1, N)")]
        [InlineData("sum(M! / (k! * (M - k)!), k, 0, N)")]
        public void WhatIsNotOfTheShapeIsLeftAsWritten(string sum)
            => Assert.IsType<Summationf>(sum.ToEntity().Simplify());
    }
}
