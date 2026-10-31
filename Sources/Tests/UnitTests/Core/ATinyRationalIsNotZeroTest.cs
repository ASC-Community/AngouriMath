//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// An exact rational is zero, positive or negative as it is, and not as its decimal is: the decimal is written out in the
    /// precision context, whose exponent range writes every rational under 1e-199 in size as 0, so 0 / 10^(-200) was NaN.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2277">#2277</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class ATinyRationalIsNotZeroTest
    {
        [Theory]
        [InlineData("0 / 10^(-200)", "0")]
        [InlineData("0 / 10^(-250)", "0")]
        [InlineData("0 / (3/10^260)", "0")]
        [InlineData("0 / (-1/10^300)", "0")]
        [InlineData("0 ^ (1/10^250)", "0")]
        [InlineData("0 / 10^(-199)", "0")]
        [InlineData("10^(-250) * 10^250", "1")]
        [InlineData("(1/10^250) / (1/10^250)", "1")]
        public void ItIsWhatItIs(string expression, string value)
        {
            Assert.Equal(value.ToEntity(), expression.ToEntity().Evaled);
            Assert.Equal(value.ToEntity(), expression.ToEntity().Simplify());
        }

        [Fact]
        public void ItsSignIsItsOwn()
        {
            var tiny = Rational.Create(1, PeterO.Numbers.EInteger.FromInt32(10).Pow(250));
            Assert.False(tiny.IsZero);
            Assert.True(tiny.IsPositive);
            Assert.False(tiny.IsNegative);
            var negative = (Rational)(-tiny);
            Assert.True(negative.IsNegative);
            Assert.False(negative.IsPositive);
            Assert.True(Integer.Zero.IsZero);
            Assert.False(Integer.Zero.IsPositive || Integer.Zero.IsNegative);
        }
    }
}
