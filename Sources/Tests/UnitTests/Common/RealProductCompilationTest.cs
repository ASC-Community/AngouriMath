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
    /// A compiled product or quotient of real numbers is real, so an infinity keeps an imaginary part of 0, where
    /// <see cref="Complex"/>'s own product made it <c>∞ · 0</c>, <c>NaN</c>: <c>-x</c> at <c>+oo</c> was
    /// <c>(-∞, NaN)</c>. <a href="https://github.com/asc-community/AngouriMath/issues/2222">#2222</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class RealProductCompilationTest
    {
        [Theory]
        [InlineData("-x", "+oo")]
        [InlineData("-x", "-oo")]
        [InlineData("2 * x", "+oo")]
        [InlineData("x / 2", "+oo")]
        [InlineData("x / (-3)", "-oo")]
        [InlineData("x * x", "-oo")]
        [InlineData("-x + 1", "+oo")]
        [InlineData("3 * x - 1", "-oo")]
        public void AtAnInfinityTheValueIsEvaluations(string expression, string at)
        {
            var expected = ((Entity.Number.Complex)expression.ToEntity().Substitute("x", at).Evaled).ToNumerics();
            var x = ((Entity.Number.Complex)at.ToEntity().Evaled).ToNumerics();
            Assert.True(double.IsInfinity(expected.Real) && expected.Imaginary == 0, $"{expression} at {at} evaluates to {expected}");
            Assert.Equal(expected, expression.ToEntity().Compile("x").Call(x));
            Assert.Equal(expected, expression.ToEntity().Compile<Complex, Complex>("x")(x));
        }

        /// <summary>A division by 0 still has no value, as evaluation has none, and a complex product is a complex product.</summary>
        [Theory]
        [InlineData("1 / x", "0", double.NaN, double.NaN)]
        [InlineData("x / x", "0", double.NaN, double.NaN)]
        [InlineData("(1 + i) * x", "1 - i", 2, 0)]
        [InlineData("x * x", "i", -1, 0)]
        [InlineData("x / (1 + i)", "2", 1, -1)]
        [InlineData("x * 3", "-2", -6, 0)]
        public void ElsewhereTheValueIsWhatItWas(string expression, string at, double real, double imaginary)
        {
            var x = ((Entity.Number.Complex)at.ToEntity().Evaled).ToNumerics();
            foreach (var value in new[] { expression.ToEntity().Compile("x").Call(x), expression.ToEntity().Compile<Complex, Complex>("x")(x) })
                if (double.IsNaN(real))
                    Assert.True(double.IsNaN(value.Real) || double.IsNaN(value.Imaginary), $"{expression} at {at}: {value}");
                else
                    Assert.True(Complex.Abs(value - new Complex(real, imaginary)) < 1e-15, $"{expression} at {at}: {value}");
        }
    }
}
