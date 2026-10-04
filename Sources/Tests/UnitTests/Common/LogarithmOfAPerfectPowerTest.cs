//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using AngouriMath.Functions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// https://github.com/asc-community/AngouriMath/issues/1212, question I.5's presentation:
    /// a logarithm of a rational literal that is a perfect power takes the exponent out, which
    /// is longer on its own and is what lets two logarithms of related literals collect.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class LogarithmOfAPerfectPowerTest
    {
        [Theory]
        [InlineData("16/9", "4/3", 2)]
        [InlineData("64", "2", 6)]
        [InlineData("1/8", "1/2", 3)]
        [InlineData("27/8", "3/2", 3)]
        [InlineData("10000", "10", 4)]
        public void APerfectPowerIsRead(string value, string root, int exponent)
        {
            Assert.True(TreeAnalyzer.TryPerfectPower((Rational)value.ToEntity(), out var readRoot, out var readExponent));
            Assert.Equal(root.ToEntity(), readRoot);
            Assert.Equal(exponent, readExponent);
        }

        [Theory]
        [InlineData("6")]
        [InlineData("12/5")]
        [InlineData("1")]
        [InlineData("-8")]
        [InlineData("0")]
        public void WhatIsNotAPerfectPowerIsNot(string value)
            => Assert.False(TreeAnalyzer.TryPerfectPower((Rational)value.ToEntity(), out _, out _));

        // The sheet's integral: ln(4/3) + ln(16/9) / 2 is 2 ln(4/3), which the metric then
        // prefers as the single literal ln(16/9).
        [Fact]
        public void TheOrientationWeekIntegralCollects()
            => Assert.Equal("ln(16/9)".ToEntity(), "integral((2x^2+x+1)/(x^3+x^2+x+1), x, 3/4, 4/3)".ToEntity().Simplify());

        [Theory]
        [InlineData("ln(4/3) + ln(16/9) / 2", "2 * ln(4/3)")]
        [InlineData("ln(8) - 3 * ln(2)", "0")]
        [InlineData("log(3, 81) / 4", "log(3, 3)")]
        public void RelatedLogarithmsCollect(string expr, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), expr.ToEntity().Simplify());

        // On its own the longer form loses to the literal: the rule is offered, not taken.
        [Theory]
        [InlineData("ln(16/9)")]
        [InlineData("ln(64)")]
        public void AloneTheLiteralStays(string expr)
            => Assert.Equal(expr.ToEntity(), expr.ToEntity().Simplify());
    }
}
