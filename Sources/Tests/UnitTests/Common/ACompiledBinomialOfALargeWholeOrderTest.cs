//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using Xunit;
using Complex = System.Numerics.Complex;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A compiled <c>binomial(n, k)</c> for a whole <c>k</c> far past <c>n</c>. Past the longs it went through
    /// the gamma function, whose two logarithms near <c>k ln k</c> cancelled to their rounding error, and was 1:
    /// <c>binomial(-5/2, 10^20)</c> is <c>7.52e29</c> and <c>binomial(277.7, 36!)</c> is <c>2e-11027</c>. Within
    /// them it was a product of k factors, which for <c>binomial(-1/2, 10^12)</c> never reach 0. Found by
    /// compilesweep; no issue was filed. The values are mpmath's at 30 digits, to 20.
    /// </summary>
    [Trait("Area", "Compilation")]
    public sealed class ACompiledBinomialOfALargeWholeOrderTest
    {
        [Theory]
        [InlineData(-2.5, 1e20, 7.5225277806367504928e+29)]
        [InlineData(277.7, 1e20, 0)]
        [InlineData(277.7, 3.7199332678990125e41, 0)]
        [InlineData(-0.5, 1e12, 5.6418958354768576325e-7)]
        [InlineData(-0.5, 1e12 + 1, -5.6418958354740366846e-7)]
        [InlineData(3.25, 2e6, 3.0992465282944267377e-27)]
        [InlineData(-7.5, 1e7 + 1, -1.6899292703910971418e+42)]
        // A whole n at least 0 below k has nothing to choose, and nor has a negative k.
        [InlineData(5, 1e20, 0)]
        [InlineData(-2.5, -1e20, 0)]
        public void IsItsValue(double n, double k, double expected)
        {
            var real = "binomial(x, y)".ToEntity().Compile<double, double, double>("x", "y");
            var fast = "binomial(x, y)".ToEntity().Compile("x", "y");
            var linq = "binomial(x, y)".ToEntity().Compile<Complex, Complex, Complex>("x", "y");
            var task = Task.Run(() => (real(n, k), fast.Call(n, k), linq(n, k)));
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), $"binomial({n}, {k}) did not finish");
            var (overDouble, byFast, byLinq) = task.Result;
            Assert.True(Close(expected, overDouble), $"binomial({n}, {k}) over double is {overDouble}");
            Assert.True(Close(expected, byFast.Real) && byFast.Imaginary == 0, $"binomial({n}, {k}) by FastExpression is {byFast}");
            Assert.True(Close(expected, byLinq.Real) && byLinq.Imaginary == 0, $"binomial({n}, {k}) by LINQ is {byLinq}");
        }

        [Fact]
        public void AComplexUpperArgumentIsItsValue()
        {
            var value = "binomial(x, y)".ToEntity().Compile("x", "y").Call(new Complex(0.5, 1), 1e7);
            var expected = new Complex(6.5666206043323881952e-11, -1.7327461680588210534e-11);
            Assert.True((value - expected).Magnitude < 1e-12 * expected.Magnitude, $"binomial(1/2 + i, 10^7) is {value}");
        }

        private static bool Close(double expected, double actual)
            => expected == 0 ? actual == 0 : Math.Abs(actual - expected) < 1e-12 * Math.Abs(expected);
    }
}
