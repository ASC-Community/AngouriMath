//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// An exact integer times an irrational past the working digits simplified to a rounded number. Past about
    /// <c>10^100</c> the decimal of the product, its last digits rounded to 0, was taken for an integer, so
    /// <c>2^340 sqrt(2)</c> simplified to a 103-digit integer. Past the decimals' exponent range the product was
    /// <c>+oo</c> and below it <c>0</c>, because a rational that large or that small reads as not finite through the
    /// decimals, and the guards against such roundings asked exactly that.
    /// https://github.com/asc-community/AngouriMath/issues/2615
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AnExactProductPastTheWorkingDigitsStaysExactTest
    {
        [Theory]
        [InlineData("2^340 * sqrt(2)", "sqrt(2)")]
        [InlineData("2^400 * pi", "pi")]
        [InlineData("10^200 * sqrt(2)", "sqrt(2)")]
        [InlineData("2^4000 * sqrt(2)", "sqrt(2)")]
        [InlineData("2^(-20000) * sqrt(2)", "sqrt(2)")]
        [InlineData("sqrt(2) / 2^3400", "sqrt(2)")]
        [InlineData("2^(-3400) * pi", "pi")]
        public void TheIrrationalFactorIsKept(string expression, string irrational)
        {
            var simplified = expression.ToEntity().Simplify();
            Assert.False(simplified is Number, $"{expression} simplified to the number {simplified}");
            Assert.Contains(simplified.Nodes, node => node == irrational.ToEntity());
            // And the same multiple of it, exactly: with the irrational a variable, the difference is 0.
            Variable t = "t";
            var difference = (simplified.Substitute(irrational.ToEntity(), t) - expression.ToEntity().Substitute(irrational.ToEntity(), t)).Simplify();
            Assert.Equal(Number.Integer.Zero, difference);
        }

        /// <summary>A rounded decimal that large is no integer when evaluated either.</summary>
        [Fact]
        public void TheEvaluatedValueIsADecimal()
            => Assert.IsNotType<Number.Integer>("2^340 * sqrt(2)".ToEntity().Evaled);

        /// <summary>Exact arithmetic on large integers and their values is unchanged.</summary>
        [Theory]
        [InlineData("2^340 * 2", "2^341")]
        [InlineData("sqrt(2)^400", "2^200")]
        [InlineData("2^4000 * 3 - 2^4000 * 2", "2^4000")]
        public void AnExactIntegerStaysAnInteger(string expression, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, expression.ToEntity().Simplify());
    }
}
