//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A compiled binomial coefficient, factorial power or Catalan number whose factorials are past the doubles is its
    /// value, where it was <c>NaN</c>: <c>binomial(216, pi)</c> is about <c>2954596.68</c>, and <c>216!</c>, about
    /// <c>1e408</c>, made the gamma ratio <c>∞/∞</c>. The values are mpmath 1.3.0's at 50 digits.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2248">#2248</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class GammaRatioCompilationTest
    {
        private static bool Agree(Complex expected, Complex actual)
            => Complex.Abs(expected - actual) <= 1e-10 * Complex.Abs(expected);

        [Theory]
        [InlineData("binomial(216, pi)", 2954596.6790612643608, 0)]
        [InlineData("binomial(10, 601/2)", 7.7046763087758266086e-22, 0)]
        [InlineData("binomial(-601/2, 7/3)", 109419.23916039716406, 0)]
        [InlineData("fallingfactorial(300, 1/2)", 17.327726454436284054, 0)]
        [InlineData("risingfactorial(200, 1/2)", 14.133299559727925473, 0)]
        [InlineData("catalannumber(2017/5)", 5.1595508663052781318e+238, 0)]
        [InlineData("catalannumber(-2017/5)", -2.8923921724035808851e-247, 0)]
        [InlineData("binomial(200 + i, 1/2)", 15.967717653807835931, 0.039869146616281137615)]
        public void ItIsTheValue(string expression, double real, double imaginary)
        {
            var expected = new Complex(real, imaginary);
            var compiled = $"x + {expression}".ToEntity();
            var fast = compiled.Compile("x").Call(0);
            var linq = compiled.Compile<Complex, Complex>("x")(0);
            Assert.True(Agree(expected, fast), $"{expression}: {fast} by FastExpression, where {expected}");
            Assert.True(Agree(expected, linq), $"{expression}: {linq} by LINQ, where {expected}");
            if (imaginary == 0)
            {
                var overDoubles = compiled.Compile<double, double>("x")(0);
                Assert.True(Agree(expected, overDoubles), $"{expression}: {overDoubles} over double, where {expected}");
            }
        }

        /// <summary>Past the doubles, at about <c>9.8e486</c> and <c>8.2e597</c>, the value is <c>+oo</c>.</summary>
        [Theory]
        [InlineData("binomial(2000, 1001/2)")]
        [InlineData("catalannumber(1001)")]
        public void PastTheDoublesItIsInfinite(string expression)
        {
            var compiled = $"x + {expression}".ToEntity();
            Assert.Equal(new Complex(double.PositiveInfinity, 0), compiled.Compile("x").Call(0));
            Assert.Equal(new Complex(double.PositiveInfinity, 0), compiled.Compile<Complex, Complex>("x")(0));
            Assert.Equal(double.PositiveInfinity, compiled.Compile<double, double>("x")(0));
        }
    }
}
