//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A rational to a rational power gives up its whole part however large the exponent's
    /// numerator is. The radicand was raised to that numerator before being split, and past
    /// 2^40 nothing was extracted: <c>2^(19/2)</c> was <c>512 sqrt(2)</c> and <c>2^(21/2)</c> was
    /// left as written, so <c>32768 * 2^(-29/2)</c> did not cancel to <c>sqrt(2)</c>.
    /// https://github.com/asc-community/AngouriMath/issues/2609
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheWholePartOfALargeRationalPowerTest
    {
        [Theory]
        [InlineData("2^(21/2)", "1024 * sqrt(2)")]
        [InlineData("2^(-21/2)", "sqrt(2) / 2048")]
        [InlineData("2^(29/2)", "16384 * sqrt(2)")]
        [InlineData("2^(201/2)", "1267650600228229401496703205376 * sqrt(2)")]
        [InlineData("5^(17/2)", "390625 * sqrt(5)")]
        [InlineData("2^(65/3)", "2097152 * 2^(2/3)")]
        [InlineData("(2/3)^(21/2)", "1024/177147 * sqrt(6)")]
        [InlineData("32768 * 2^(-29/2)", "sqrt(2)")]
        [InlineData("2 / (2^(-29/2) * 32768)", "sqrt(2)")]
        [InlineData("2^(1/2) * 2^(3/2)", "4")]
        [InlineData("8^(1/3) * 2^(-1)", "1")]
        [InlineData("3^(5/2) / 9", "sqrt(3)")]
        public void IsExtracted(string power, string expected)
        {
            var simplified = power.ToEntity().Simplify();
            Assert.Equal(expected.ToEntity().InnerSimplified, simplified.InnerSimplified);
            var value = power.ToEntity().Evaled;
            Assert.True(((Entity.Number.Complex)(simplified.Evaled - value).Evaled).IsZero
                        || ((Entity.Number.Real)((simplified.Evaled - value) / value).Evaled).EDecimal.Abs().CompareTo(PeterO.Numbers.EDecimal.FromString("1E-50")) < 0,
                $"{simplified} = {simplified.Evaled} against {value}");
        }

        /// <summary>
        /// A power too large to have its whole part taken out is answered as it was before.
        /// Past about 10^100 a product of an integer and a radical is folded into a rounded
        /// decimal and read back as an integer, so extracting one would replace a correct power
        /// with that. 2^(681/2) and 2^(801/2) already come back as integers from
        /// InnerSimplified, on master and on 2.10.0, by that same fold. This pins that the
        /// extraction adds nothing to it; the fold is
        /// https://github.com/asc-community/AngouriMath/issues/2615
        /// </summary>
        [Theory]
        [InlineData("2^(601/2)")]
        [InlineData("2^(681/2)")]
        [InlineData("2^(801/2)")]
        [InlineData("2^(10000001/2)")]
        [InlineData("2^(-10000001/2)")]
        public void TooLargeAWholePartIsLeftInThePower(string power)
            => Assert.Equal(power.ToEntity().InnerSimplified, power.ToEntity().Simplify());
    }
}
