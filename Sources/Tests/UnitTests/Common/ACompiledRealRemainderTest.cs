//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Compiled over <c>double</c> and <c>float</c>, the floored remainder was <c>((a % b) + b) % b</c>, which rounds a
    /// second time where the first remainder already has the divisor's sign: <c>2 mod e</c> was 1.9999999999999996 and
    /// <c>floor(2 mod e)</c> 1, where both evaluate to 2. No issue was filed; found by work/compilesweep at seed 11.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ACompiledRealRemainderTest
    {
        [Theory]
        [InlineData("2 mod e", 2.0)]
        [InlineData("floor(2 mod e)", 2.0)]
        [InlineData("(-2) mod (-e)", -2.0)]
        [InlineData("(-6) mod 3", 0.0)]
        [InlineData("7.5 mod 2", 1.5)]
        public void IsTheFlooredRemainderRoundedOnce(string expression, double expected)
        {
            Assert.Equal(expected, expression.ToEntity().Compile<double, double>("x")(0));
            Assert.Equal((float)expected, expression.ToEntity().Compile<float, float>("x")(0));
        }

        // A remainder whose sign differs from the divisor's moves by one divisor, to the divisor's side.
        [Theory]
        [InlineData("(-2) mod e")]
        [InlineData("2 mod (-e)")]
        public void TakesTheSignOfTheDivisor(string expression)
        {
            var value = ((Entity.Number.Complex)expression.ToEntity().EvalNumerical()).RealPart.EDecimal.ToDouble();
            Assert.Equal(value, expression.ToEntity().Compile<double, double>("x")(0), 15);
        }
    }
}
