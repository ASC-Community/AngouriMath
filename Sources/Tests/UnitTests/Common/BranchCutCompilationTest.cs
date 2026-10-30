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
    /// A compiled function with a branch cut takes the side evaluation takes, where its argument is a number on the cut
    /// that <see cref="Complex"/> computes with <c>-0</c> as a part: <c>cos(2)</c> is <c>(-0.416, -0)</c> and
    /// <c>conj(-2)</c> is <c>(-2, -0)</c>, and <c>ln(cos(x))</c> at 2 was the conjugate of the logarithm.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2236">#2236</a>. And FastExpression's logarithm
    /// keeps a complex base's imaginary part. <a href="https://github.com/asc-community/AngouriMath/issues/2239">#2239</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class BranchCutCompilationTest
    {
        private static readonly double[] points = { -4, -2.5, -2, -1, -0.5, 0.5, 1, 2, 2.5, 3, 4 };

        private static bool Agree(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
                ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
                : double.IsInfinity(expected.Real) || double.IsInfinity(expected.Imaginary)
                    ? expected == actual
                    : Complex.Abs(expected - actual) <= 1e-9 * System.Math.Max(1, Complex.Abs(expected));

        [Theory]
        [InlineData("ln({0})")]
        [InlineData("sqrt({0})")]
        // An exponent whose denominator is even: evaluation takes the real root for an odd one, which a double
        // exponent cannot show, and the principal value here, as the compilers do.
        [InlineData("({0})^(1/4)")]
        [InlineData("({0})^(1/2 + i)")]
        [InlineData("log(2, {0})")]
        [InlineData("log({0}, 2)")]
        [InlineData("arcsin({0})")]
        [InlineData("arccos({0})")]
        [InlineData("arctan({0})")]
        [InlineData("arccotan({0})")]
        [InlineData("arcsec({0})")]
        [InlineData("arccosec({0})")]
        [InlineData("arg({0})")]
        [InlineData("Ei({0})")]
        [InlineData("li({0})")]
        [InlineData("Ci({0})")]
        [InlineData("Chi({0})")]
        [InlineData("lambertw(0, {0})")]
        [InlineData("lambertw(-1, {0})")]
        public void OnACutTheSideIsEvaluations(string function)
        {
            var compared = 0;
            foreach (var inner in new[] { "conj(x)", "cos(x)", "i * x" })
            {
                var expression = string.Format(function, inner).ToEntity();
                var fast = expression.Compile("x");
                var linq = expression.Compile<Complex, Complex>("x");
                foreach (var x in points)
                {
                    if (expression.Substitute("x", x).Evaled is not Entity.Number.Complex evaluated)
                        continue;
                    var expected = evaluated.ToNumerics();
                    // Not at a pole, arctan at -i among them, which is not what this is about.
                    if (double.IsInfinity(expected.Real) || double.IsInfinity(expected.Imaginary))
                        continue;
                    Assert.True(Agree(expected, fast.Call(x)), $"{expression} at {x}: {expected} evaluated, {fast.Call(x)} by FastExpression");
                    Assert.True(Agree(expected, linq(x)), $"{expression} at {x}: {expected} evaluated, {linq(x)} by LINQ");
                    compared++;
                }
            }
            Assert.True(compared > points.Length, $"only {compared} of {function} evaluated");
        }

        [Theory]
        [InlineData("log(x, 2)", 1, 1)]
        [InlineData("log(x, x + 1)", 0.5, 1.2)]
        [InlineData("log(x, 3 - i)", -2, 0.5)]
        public void ALogarithmToAComplexBaseIsEvaluations(string expression, double re, double im)
        {
            var x = new Complex(re, im);
            var expected = ((Entity.Number.Complex)expression.ToEntity().Substitute("x", (Entity.Number.Complex)x).Evaled).ToNumerics();
            Assert.True(Agree(expected, expression.ToEntity().Compile("x").Call(x)), $"{expression} at {x}: {expected} evaluated, {expression.ToEntity().Compile("x").Call(x)} by FastExpression");
            Assert.True(Agree(expected, expression.ToEntity().Compile<Complex, Complex>("x")(x)), $"{expression} at {x} by LINQ");
        }
    }
}
