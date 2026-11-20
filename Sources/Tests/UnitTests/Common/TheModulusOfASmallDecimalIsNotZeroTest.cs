//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The modulus of a real number was made as a residual of a cancellation, which takes a value
    /// below half the working digits for 0, so <c>abs(2.08E-52)</c> was 0. A non-real number is
    /// divided through its divisor's modulus, so <c>i / 2.08E-52</c> was <c>NaN</c>, a root of an
    /// equation in <c>Re(x)</c> simplified to <c>NaN</c> and was dropped as a pole, and
    /// <c>2 + 10^26 x + (2 + 2i) 10^26 x Re(10^26 x) = 0</c> was answered <c>{ }</c>. Found while
    /// solving generated equations with <c>x</c> written as <c>10^26 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheModulusOfASmallDecimalIsNotZeroTest
    {
        [Theory]
        [InlineData("abs(2.08E-52)", "2.08E-52")]
        [InlineData("abs(-3E-60)", "3E-60")]
        [InlineData("abs(-2.5)", "5/2")]
        public void TheModulusIsTheNumberWithoutItsSign(string expression, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, expression.ToEntity().Evaled);

        [Theory]
        [InlineData("i / 2.08E-52", "i / 2.08 * 10^52")]
        [InlineData("i / (3 * 10^(-78))^(2/3)", "i * (3 * 10^(-78))^(-2/3)")]
        public void ANonRealNumberDividesByASmallOne(string expression, string expected)
        {
            var quotient = Assert.IsAssignableFrom<Number.Complex>(expression.ToEntity().Evaled);
            Assert.False(quotient.IsNaN);
            var difference = Assert.IsAssignableFrom<Number.Complex>((quotient - expected.ToEntity().Evaled).Evaled);
            Assert.True(difference.Abs().EDecimal.Abs().CompareTo(quotient.Abs().EDecimal.Multiply(PeterO.Numbers.EDecimal.FromString("1E-60"))) < 0);
        }

        [Fact]
        public void AnEquationInTheRealPartOfALargeMultipleKeepsItsRoot()
        {
            var answer = Assert.IsType<Set.FiniteSet>("2 + 10^26 x + (2 + 2i) * 10^26 x * Re(10^26 x) = 0".ToEntity().Solve("x"));
            var root = Assert.IsAssignableFrom<Number.Complex>((Assert.Single(answer) * MathS.Pow(10, 26)).Evaled);
            // The root of 2 + y + (2 + 2i) y Re(y) = 0 is about -0.42219 - 2.29079i.
            Assert.Equal(-0.42219, root.RealPart.EDecimal.ToDouble(), 5);
            Assert.Equal(-2.29079, root.ImaginaryPart.EDecimal.ToDouble(), 5);
        }
    }
}
