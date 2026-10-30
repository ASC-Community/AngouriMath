//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The binomial coefficient, the falling and the rising factorial and the double factorial
    /// compile, in FastExpression and in the LINQ compiler, to what evaluation gives, on a grid of
    /// whole, half-whole and complex arguments. Each was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class CombinatorialCompilationTest
    {
        private static readonly string[] values =
            { "-3", "-2", "-1", "0", "1", "2", "5", "1/2", "-1/2", "3/2", "-5/2", "9/4", "1 + i", "i/2" };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        /// <summary>Whether two values agree: both NaN, or within a part in a billion.</summary>
        private static bool Agree(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
               ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
               : Complex.Abs(expected - actual) <= 1e-9 * System.Math.Max(1, Complex.Abs(expected));

        [Theory]
        [InlineData("binomial(x, y)")]
        [InlineData("fallingfactorial(x, y)")]
        [InlineData("risingfactorial(x, y)")]
        public void ATwoArgumentFunctionIsWhatEvaluationSays(string expression)
        {
            var fast = expression.ToEntity().Compile("x", "y");
            var linq = expression.ToEntity().Compile<Complex, Complex, Complex>("x", "y");
            var real = expression.ToEntity().Compile<double, double, double>("x", "y");
            var compared = 0;
            foreach (var a in values)
                foreach (var b in values)
                {
                    if (expression.ToEntity().Substitute("x", a).Substitute("y", b).Evaled is not Entity.Number.Complex evaluated)
                        continue;
                    var expected = evaluated.ToNumerics();
                    var (x, y) = (Numeric(a), Numeric(b));
                    Assert.True(Agree(expected, fast.Call(x, y)), $"{expression} at ({a}, {b}): {expected} evaluated, {fast.Call(x, y)} by FastExpression");
                    Assert.True(Agree(expected, linq(x, y)), $"{expression} at ({a}, {b}): {expected} evaluated, {linq(x, y)} by LINQ");
                    if (x.Imaginary == 0 && y.Imaginary == 0 && expected.Imaginary == 0)
                        Assert.True(Agree(expected, real(x.Real, y.Real)), $"{expression} at ({a}, {b}): {expected} evaluated, {real(x.Real, y.Real)} over double");
                    compared++;
                }
            Assert.True(compared > values.Length * values.Length * 3 / 4, $"only {compared} of {values.Length * values.Length} evaluated");
        }

        /// <summary>
        /// The Catalan numbers: exact on the whole numbers, <c>-1/2</c> at <c>-1</c>, <c>0</c> below it, no value at a
        /// negative half-integer, and the gamma ratio elsewhere.
        /// </summary>
        [Fact]
        public void TheCatalanNumbersAreWhatEvaluationSays()
        {
            var fast = "catalannumber(x)".ToEntity().Compile("x");
            var linq = "catalannumber(x)".ToEntity().Compile<Complex, Complex>("x");
            var real = "catalannumber(x)".ToEntity().Compile<double, double>("x");
            foreach (var a in values.Concat(new[] { "-4", "10", "30", "7/3", "-7/3" }))
            {
                var expected = Assert.IsAssignableFrom<Entity.Number.Complex>($"catalannumber({a})".ToEntity().Evaled).ToNumerics();
                var x = Numeric(a);
                Assert.True(Agree(expected, fast.Call(x)), $"catalannumber({a}): {expected} evaluated, {fast.Call(x)} by FastExpression");
                Assert.True(Agree(expected, linq(x)), $"catalannumber({a}): {expected} evaluated, {linq(x)} by LINQ");
                if (x.Imaginary == 0 && expected.Imaginary == 0)
                    Assert.True(Agree(expected, real(x.Real)), $"catalannumber({a}): {expected} evaluated, {real(x.Real)} over double");
            }
        }

        /// <summary>A Catalan number at a whole index is the integer, exactly, as far as a double holds it.</summary>
        [Theory]
        [InlineData(5, 42)]
        [InlineData(30, 3814986502092304)]
        public void ACatalanNumberAtAWholeIndexIsExact(int n, long expected)
            => Assert.Equal((double)expected, "catalannumber(x)".ToEntity().Compile("x").Call(n).Real);

        [Fact]
        public void TheDoubleFactorialIsWhatEvaluationSays()
        {
            var fast = "x!!".ToEntity().Compile("x");
            var linq = "x!!".ToEntity().Compile<Complex, Complex>("x");
            foreach (var a in values.Concat(new[] { "-5", "-7", "-4", "6", "7" }))
            {
                var expected = Assert.IsAssignableFrom<Entity.Number.Complex>($"({a})!!".ToEntity().Evaled).ToNumerics();
                Assert.True(Agree(expected, fast.Call(Numeric(a))), $"({a})!!: {expected} evaluated, {fast.Call(Numeric(a))} by FastExpression");
                Assert.True(Agree(expected, linq(Numeric(a))), $"({a})!!: {expected} evaluated, {linq(Numeric(a))} by LINQ");
            }
        }
    }
}
