//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>gcd</c> and <c>lcm</c> compile, in FastExpression and in the LINQ compiler over <c>Complex</c> and over
    /// <c>double</c>: exactly what evaluation gives at whole arguments, rounded once, and <c>NaN</c> at any other.
    /// Evaluation takes them over the rationals too, but a compiled argument is a double, and the double nearest
    /// <c>0.1</c> is not <c>1/10</c>. Both were refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class GcdLcmCompilationTest
    {
        // 2^60 and 3 * 2^58 are past 2^53, where a double is still the whole number it reads as; 999999999999989
        // times 999999999999947 is past it too, so the lcm of the two is rounded.
        private static readonly string[] wholes =
            { "-12", "-4", "-1", "0", "1", "4", "6", "7", "12", "18", "2^60", "3 * 2^58", "999999999999989", "999999999999947" };

        private static readonly Complex[] notWhole =
            { 0.5, 2.5, -1.5, new Complex(0, 1), new Complex(1, 1), new Complex(4, 1e-300), double.NaN,
              double.PositiveInfinity, new Complex(double.NaN, 0) * new Complex(0, 1) };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        [Theory]
        [InlineData("gcd(x, y)")]
        [InlineData("lcm(x, y)")]
        public void AtWholeArgumentsItIsExactlyWhatEvaluationSays(string expression)
        {
            var fast = expression.ToEntity().Compile("x", "y");
            var linq = expression.ToEntity().Compile<Complex, Complex, Complex>("x", "y");
            var real = expression.ToEntity().Compile<double, double, double>("x", "y");
            foreach (var a in wholes)
                foreach (var b in wholes)
                {
                    var evaluated = Assert.IsType<Entity.Number.Integer>(expression.ToEntity().Substitute("x", a).Substitute("y", b).Evaled);
                    var expected = EFloat.FromEInteger(evaluated.EInteger).ToDouble();
                    var (x, y) = (Numeric(a), Numeric(b));
                    Assert.True(fast.Call(x, y) == expected, $"{expression} at ({a}, {b}): {expected} evaluated, {fast.Call(x, y)} by FastExpression");
                    Assert.True(linq(x, y) == expected, $"{expression} at ({a}, {b}): {expected} evaluated, {linq(x, y)} by LINQ");
                    Assert.True(real(x.Real, y.Real) == expected, $"{expression} at ({a}, {b}): {expected} evaluated, {real(x.Real, y.Real)} over double");
                }
        }

        // Evaluation, measured: gcd(-4, 6) = 2, gcd(0, 5) = gcd(0, -5) = 5, gcd(0, 0) = 0, lcm(-4, 6) = 12, lcm(0, 3) = 0.
        [Theory]
        [InlineData("gcd(x, y)", -4, 6, 2)]
        [InlineData("gcd(x, y)", 0, 5, 5)]
        [InlineData("gcd(x, y)", 0, -5, 5)]
        [InlineData("gcd(x, y)", 0, 0, 0)]
        [InlineData("lcm(x, y)", -4, 6, 12)]
        [InlineData("lcm(x, y)", -4, -6, 12)]
        [InlineData("lcm(x, y)", 0, 3, 0)]
        [InlineData("lcm(x, y)", 0, 0, 0)]
        public void BothAreNonNegativeAsEvaluationHasThem(string expression, int a, int b, int expected)
        {
            Assert.Equal(expected, ((Entity.Number.Integer)expression.ToEntity().Substitute("x", a).Substitute("y", b).Evaled).EInteger.ToInt32Checked());
            Assert.Equal(new Complex(expected, 0), expression.ToEntity().Compile("x", "y").Call(a, b));
            Assert.Equal(new Complex(expected, 0), expression.ToEntity().Compile<Complex, Complex, Complex>("x", "y")(a, b));
            Assert.Equal(expected, expression.ToEntity().Compile<double, double, double>("x", "y")(a, b));
        }

        [Theory]
        [InlineData("gcd(x, y)")]
        [InlineData("lcm(x, y)")]
        public void AtAnyOtherArgumentItIsNaN(string expression)
        {
            var fast = expression.ToEntity().Compile("x", "y");
            var linq = expression.ToEntity().Compile<Complex, Complex, Complex>("x", "y");
            var real = expression.ToEntity().Compile<double, double, double>("x", "y");
            foreach (var other in notWhole)
                foreach (var whole in new Complex[] { 0, 1, 6 })
                    foreach (var (x, y) in new[] { (other, whole), (whole, other) })
                    {
                        Assert.True(IsNaN(fast.Call(x, y)), $"{expression} at ({x}, {y}): {fast.Call(x, y)} by FastExpression");
                        Assert.True(IsNaN(linq(x, y)), $"{expression} at ({x}, {y}): {linq(x, y)} by LINQ");
                        if (x.Imaginary == 0 && y.Imaginary == 0)
                            Assert.True(double.IsNaN(real(x.Real, y.Real)), $"{expression} at ({x}, {y}): {real(x.Real, y.Real)} over double");
                    }
        }

        // gcd(12, 18, 8) is read as gcd(gcd(12, 18), 8), and so compiled.
        [Fact]
        public void SeveralArgumentsAreNested()
        {
            Assert.Equal(new Complex(2, 0), "gcd(x, 18, y)".ToEntity().Compile("x", "y").Call(12, 8));
            Assert.Equal(12, "lcm(x, 3, y)".ToEntity().Compile<double, double, double>("x", "y")(2, 4));
        }

        private static bool IsNaN(Complex z) => double.IsNaN(z.Real) || double.IsNaN(z.Imaginary);
    }
}
