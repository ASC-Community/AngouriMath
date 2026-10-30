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
    /// A compiled power of real numbers is the real power where there is one, a positive base or a negative one to a
    /// whole power, and what evaluation gives at an infinity and at a zero base. <see cref="Complex.Pow(Complex, Complex)"/>
    /// made <c>x^2</c> at <c>+oo</c> <c>(NaN, NaN)</c>, <c>0^(-1)</c> <c>0</c> and <c>0^0</c> <c>1</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2225">#2225</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class RealPowerCompilationTest
    {
        private static readonly string[] bases = { "0", "1", "2", "1/2", "3", "7/3", "-2", "-1/2", "+oo", "-oo" };
        private static readonly string[] exponents = { "0", "1", "2", "3", "-1", "-2", "1/2", "5/2", "-1/3", "+oo", "-oo" };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        private static bool Agree(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
                ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
                : double.IsInfinity(expected.Real) ? expected == actual
                : Complex.Abs(expected - actual) <= 1e-14 * System.Math.Max(1, Complex.Abs(expected));

        [Fact]
        public void APowerOfRealsIsWhatEvaluationSays()
        {
            var fast = "x^y".ToEntity().Compile("x", "y");
            var linq = "x^y".ToEntity().Compile<Complex, Complex, Complex>("x", "y");
            var compared = 0;
            foreach (var a in bases)
                foreach (var b in exponents)
                {
                    var (x, y) = (Numeric(a), Numeric(b));
                    // A negative base to a power that is not whole: evaluation takes the real root for an odd
                    // denominator, which a double exponent cannot show, and the compilers the principal value.
                    if (x.Real < 0 && System.Math.Floor(y.Real) != y.Real)
                        continue;
                    if ($"({a})^({b})".ToEntity().Evaled is not Entity.Number.Complex evaluated)
                        continue;
                    var expected = evaluated.ToNumerics();
                    Assert.True(Agree(expected, fast.Call(x, y)), $"({a})^({b}): {expected} evaluated, {fast.Call(x, y)} by FastExpression");
                    Assert.True(Agree(expected, linq(x, y)), $"({a})^({b}): {expected} evaluated, {linq(x, y)} by LINQ");
                    compared++;
                }
            Assert.True(compared > 80, $"only {compared} powers compared");
        }

        [Theory]
        [InlineData("x^2", "+oo")]
        [InlineData("x^2", "-oo")]
        [InlineData("x^3", "-oo")]
        [InlineData("sqrt(x)", "+oo")]
        [InlineData("exp(-x)", "+oo")]
        [InlineData("exp(x)", "-oo")]
        public void AtAnInfinityTheValueIsEvaluations(string expression, string at)
        {
            var expected = ((Entity.Number.Complex)expression.ToEntity().Substitute("x", at).Evaled).ToNumerics();
            Assert.False(double.IsNaN(expected.Real), $"{expression} at {at} evaluates to {expected}");
            Assert.Equal(expected, expression.ToEntity().Compile("x").Call(Numeric(at)));
            Assert.Equal(expected, expression.ToEntity().Compile<Complex, Complex>("x")(Numeric(at)));
        }

        /// <summary>A complex power is the principal value it was.</summary>
        [Theory]
        [InlineData("i", "2", -1, 0)]
        [InlineData("-4", "1/2", 0, 2)]
        [InlineData("2", "i", 0.7692389013639721, 0.6389612763136348)]
        public void AComplexPowerIsThePrincipalValue(string a, string b, double real, double imaginary)
        {
            var value = "x^y".ToEntity().Compile("x", "y").Call(Numeric(a), Numeric(b));
            Assert.True(Complex.Abs(value - new Complex(real, imaginary)) < 1e-14, $"({a})^({b}): {value}");
        }
    }
}
