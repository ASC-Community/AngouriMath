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
    /// A summation to <c>+oo</c> whose terms do not tend to zero has no finite value, and where
    /// the limit has a sign the answer is <c>+oo</c> or <c>-oo</c> rather than nothing. The
    /// nth-term test, asked for on the review of
    /// https://github.com/asc-community/AngouriMath/pull/1218.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class DivergentSeriesTest
    {
        [Theory]
        [InlineData("sum(2^k, k, 0, +oo)")]
        [InlineData("sum(n^n, n, 1, +oo)")]
        [InlineData("sum(1, k, 0, +oo)")]
        [InlineData("sum(k, k, 1, +oo)")]
        [InlineData("sum(k^2 + 1, k, 0, +oo)")]
        [InlineData("sum(3 * 2^k, k, 0, +oo)")]
        [InlineData("sum(k^3 - k, k, 2, +oo)")]
        public void TermsThatDoNotVanishSumToPositiveInfinity(string sum)
            => Assert.Equal(Number.Real.PositiveInfinity, sum.ToEntity().Simplify());

        [Theory]
        [InlineData("sum(-1, k, 0, +oo)")]
        [InlineData("sum(-k, k, 1, +oo)")]
        [InlineData("sum(-2^k, k, 0, +oo)")]
        public void ANegativeLimitSumsToNegativeInfinity(string sum)
            => Assert.Equal(Number.Real.NegativeInfinity, sum.ToEntity().Simplify());

        // The case the nth-term test says nothing about, and the reason it must not guess:
        // both of these have terms tending to 0, one diverges and one converges.
        [Theory]
        [InlineData("sum(1/k, k, 1, +oo)")]
        [InlineData("sum(1/k^2, k, 1, +oo)")]
        [InlineData("sum(1/k^3, k, 1, +oo)")]
        public void AVanishingLimitIsLeftAsWritten(string sum)
            => Assert.IsType<Summationf>(sum.ToEntity().Simplify());

        // A limit that does not exist means no value rather than an infinite one, and telling
        // "does not exist" from "was not computed" is not something to infer from a failure.
        [Theory]
        [InlineData("sum((-1)^k, k, 0, +oo)")]
        [InlineData("sum(sin(k), k, 1, +oo)")]
        public void ALimitThatDoesNotExistIsLeftAsWritten(string sum)
            => Assert.IsType<Summationf>(sum.ToEntity().Simplify());

        // A single undefined term makes the sum undefined, not infinite. The terms of the first
        // tend to 1, so a reader that only looked at the limit would answer +oo and be wrong
        // about the term at k = 5.
        [Theory]
        [InlineData("sum(k / (k - 5), k, 0, +oo)")]
        [InlineData("sum(k / (k - 5), k, 0, n)")]
        [InlineData("sum(1 / (k - 5) + k, k, 0, +oo)")]
        public void APoleInTheIndexIsLeftAsWritten(string sum)
            => Assert.IsType<Summationf>(sum.ToEntity().Simplify());

        // What converges is summed by a closed form, not tested for divergence.
        [Theory]
        [InlineData("sum(2^(-k), k, 0, +oo)", "2")]
        [InlineData("sum(1 / 2^k, k, 1, +oo)", "1")]
        [InlineData("sum(x^k / k!, k, 0, +oo)", "e^x")]
        public void WhatConvergesIsStillSummed(string sum, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), sum.ToEntity().Simplify());

        // A finite range is a finite sum whatever the terms do.
        [Theory]
        [InlineData("sum(2^k, k, 0, 5)", "63")]
        [InlineData("sum(k, k, 1, 100)", "5050")]
        public void AFiniteRangeIsUnaffected(string sum, string expected)
            => Assert.Equal(expected.ToEntity(), sum.ToEntity().Simplify());

        // A symbolic lower bound could be -oo, where there is no first term to be finite.
        [Fact]
        public void ASymbolicLowerBoundIsLeftAsWritten()
            => Assert.IsType<Summationf>("sum(2^k, k, a, +oo)".ToEntity().Simplify());

        // The condition a power attaches is dropped where it holds over the range: the split
        // hands over `n ^ n provided not n = 0 or n > 0`, which from 1 upwards is just n ^ n.
        [Fact]
        public void AConditionThatHoldsOverTheRangeIsDropped()
            => Assert.Equal(Number.Real.PositiveInfinity,
                "sum(n^n provided not n = 0 or n > 0, n, 1, +oo)".ToEntity().Simplify());

        // A condition that does *not* hold over the range is a term that may fail to exist.
        [Fact]
        public void AConditionThatDoesNotHoldIsLeftAsWritten()
            => Assert.IsType<Summationf>("sum(k^2 provided k > 100, k, 1, +oo)".ToEntity().Simplify());

        // The integral that asked for this: #1218's review. The split leaves sum(n^n, n, 1, +oo).
        [Fact]
        public void TheStepIntegralThatAskedForThisIsInfinite()
            => Assert.Equal(Number.Real.PositiveInfinity,
                "integral(floor(x)^floor(x), x, 1, +oo)".ToEntity().Simplify());
    }
}
