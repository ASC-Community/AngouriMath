//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A complex number's parts are decimals in the precision context, whose exponents end at a thousand. A
    /// product or a power of finite numbers a part of which passed that end had <c>oo - oo</c> in it and was
    /// NaN, which says the value does not exist: <c>(3 + 4 * i)^2000</c>, <c>(1 + i)^(10^6)</c> and
    /// <c>(1e600 + 1e600 * i)^2</c> among them. Each part past the range is the infinity of its sign now, as a
    /// real past it is <c>+oo</c>, and an exact simplification leaves the expression as written. No issue was
    /// filed.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AComplexOverflowIsNotNaNTest
    {
        [Theory]
        [InlineData("(1e600 + 1e600 * i)^2")]
        [InlineData("(1e600 + 1e600 * i) * (1e600 + 1e600 * i)")]
        public void AProductPastTheRangeHasItsParts(string input)
        {
            var value = Within(() => input.ToEntity().Evaled, input);
            var complex = Assert.IsAssignableFrom<Complex>(value);
            Assert.True(complex.RealPart.EDecimal.IsZero, $"{input} has the real part {complex.RealPart}");
            Assert.True(complex.ImaginaryPart.EDecimal.IsPositiveInfinity(), $"{input} has the imaginary part {complex.ImaginaryPart}");
        }

        [Fact]
        public void APowerOfOnePlusIPastTheRangeIsPositiveInfinity()
            => Assert.Equal(Real.PositiveInfinity, Within(() => "(1 + i)^(10^6)".ToEntity().Evaled, "(1 + i)^(10^6)"));

        [Theory]
        [InlineData(3, 4, 2000)]
        [InlineData(3, 4, 2001)]
        [InlineData(1, 2, 3000)]
        [InlineData(-5, 7, 2500)]
        public void AGaussianPowerPastTheRangeHasTheSignsOfItsParts(int real, int imaginary, int power)
        {
            var input = $"({real} + {imaginary} * i)^{power}";
            var value = Assert.IsAssignableFrom<Complex>(Within(() => input.ToEntity().Evaled, input));
            var (exactReal, exactImaginary) = (EInteger.One, EInteger.Zero);
            for (var k = 0; k < power; k++)
                (exactReal, exactImaginary) = (exactReal * real - exactImaginary * imaginary, exactReal * imaginary + exactImaginary * real);
            AssertRoundedInto(exactReal, value.RealPart.EDecimal, input);
            AssertRoundedInto(exactImaginary, value.ImaginaryPart.EDecimal, input);
        }

        [Theory]
        [InlineData("(3 + 4 * i)^1400")]
        [InlineData("(3 + 4 * i)^(-2000)")]
        public void APowerWithinTheRangeIsFinite(string input)
            => Assert.True(Within(() => input.ToEntity().Evaled, input).IsFinite, $"{input} is not finite");

        [Theory]
        [InlineData("(3 + 4 * i)^2000")]
        [InlineData("(1 + i)^(10^6)")]
        [InlineData("(3 + 4 * i)^2000 + x")]
        public void AnExactSimplificationLeavesItAsWritten(string input)
        {
            Assert.True(Within(() => input.ToEntity().InnerSimplified, input) is not Number { IsFinite: false }, $"{input} was simplified to a number that is not finite");
            Assert.True(Within(() => input.ToEntity().Simplify(), input) is not Number { IsFinite: false }, $"{input} was simplified to a number that is not finite");
        }

        /// <summary>The exact part rounded into the context: the same infinity past it, and the same sign within.</summary>
        private static void AssertRoundedInto(EInteger exact, EDecimal evaluated, string input)
        {
            var rounded = EDecimal.FromEInteger(exact).RoundToPrecision(MathS.Settings.DecimalPrecisionContext);
            Assert.True(rounded.IsInfinity() == evaluated.IsInfinity() && rounded.Sign == evaluated.Sign,
                $"{input} has the part {evaluated}, where the exact part is {rounded}");
        }

        private static Entity Within(Func<Entity> evaluation, string input)
        {
            var task = Task.Factory.StartNew(evaluation, TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), $"{input} did not finish in thirty seconds");
            return task.Result;
        }
    }
}
