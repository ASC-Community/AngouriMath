//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A cube or fourth root of a quadratic binomial beside another, at the ratios where the
    /// integral, elliptic otherwise, is elementary: Rubi's 1.1.2.3.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back at points where the integrand is real, with the symbols
    /// pinned in each sign case the closed forms are written for.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class EllipticLookingQuotientOfBinomialsTest
    {
        private static void DifferentiatesBack(string integrand, double[] points, params (string, double)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            Entity original = integrand.ToEntity();
            foreach (var (name, value) in pins)
            {
                derivative = derivative.Substitute(name, value);
                original = original.Substitute(name, value);
            }
            var compared = 0;
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 3, $"only {compared} points could be compared for {integrand}");
        }

        /// <summary>
        /// <c>B C + 3 A D = 0</c> and <c>B C - 9 A D = 0</c> under a cube root, for each sign of
        /// <c>b/a</c>, and <c>B C - 2 A D = 0</c> under a fourth root, for each sign of <c>b</c> with
        /// <c>a</c> positive and for <c>a</c> negative.
        /// </summary>
        [Theory]
        [InlineData("1/((a + b*x^2)^(1/3)*(3*a - b*x^2))", 1.3, 0.7, new[] { 0.3, 0.9, 1.7, -0.6 })]
        [InlineData("1/((a + b*x^2)^(1/3)*(3*a - b*x^2))", 1.3, -0.7, new[] { 0.3, 0.9, 1.1, -0.6 })]
        [InlineData("1/((a + b*x^2)^(1/3)*(9*a + b*x^2))", 1.3, 0.7, new[] { 0.3, 0.9, 1.7, -0.6 })]
        [InlineData("1/((a + b*x^2)^(1/3)*(9*a + b*x^2))", 1.3, -0.7, new[] { 0.3, 0.9, 1.1, -0.6 })]
        [InlineData("1/((a + b*x^2)^(1/4)*(2*a + b*x^2))", 1.3, 0.7, new[] { 0.3, 0.9, 1.7, -0.6 })]
        [InlineData("1/((a + b*x^2)^(1/4)*(2*a + b*x^2))", 1.3, -0.7, new[] { 0.3, 0.9, 1.1, -0.6 })]
        [InlineData("1/((a + b*x^2)^(1/4)*(2*a + b*x^2))", -1.3, 0.7, new[] { 1.5, 2.0, 2.6, -1.8 })]
        public void AtTheRatioWhereItIsElementary(string integrand, double a, double b, double[] points)
            => DifferentiatesBack(integrand, points, ("a", a), ("b", b));

        /// <summary>With numbers in the constants.</summary>
        [Theory]
        [InlineData("1/((1 - x^2)^(1/3)*(3 + x^2))", new[] { 0.3, 0.6, 0.9, -0.5 })]
        [InlineData("1/((1 + x^2)^(1/3)*(9 + x^2))", new[] { 0.3, 0.9, 1.7, -0.6 })]
        [InlineData("1/((3 - x^2)*(1 + x^2)^(1/3))", new[] { 0.3, 0.9, 1.4, -0.6 })]
        [InlineData("1/((2 + 3*x^2)^(1/4)*(4 + 3*x^2))", new[] { 0.3, 0.9, 1.7, -0.6 })]
        [InlineData("1/((-2 + 3*x^2)*(-1 + 3*x^2)^(1/4))", new[] { 0.7, 1.1, 1.9, -1.3 })]
        public void WithNumbers(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// <c>x^2</c> over the three-quarter power at the fourth root's ratio, the same two
        /// functions' difference: for each sign of <c>b</c> with <c>a</c> positive, for <c>a</c>
        /// negative, and with numbers. Rubi's 1.1.2.4.
        /// </summary>
        [Theory]
        [InlineData("x^2/((a + b*x^2)^(3/4)*(2*a + b*x^2))", 1.3, 0.7, new[] { 0.3, 0.9, 1.7, -0.6 })]
        [InlineData("x^2/((a + b*x^2)^(3/4)*(2*a + b*x^2))", 1.3, -0.7, new[] { 0.3, 0.9, 1.1, -0.6 })]
        [InlineData("x^2/((a + b*x^2)^(3/4)*(2*a + b*x^2))", -1.3, 0.7, new[] { 1.5, 2.0, 2.6, -1.8 })]
        [InlineData("x^2/((a - b*x^2)^(3/4)*(2*a - b*x^2))", 1.3, 0.7, new[] { 0.3, 0.9, 1.1, -0.6 })]
        [InlineData("x^2/((-2 + 3*x^2)*(-1 + 3*x^2)^(3/4))", 0, 0, new[] { 0.7, 1.1, 1.9, -1.3 })]
        [InlineData("x^2/((2 - 3*x^2)^(3/4)*(4 - 3*x^2))", 0, 0, new[] { 0.2, 0.5, 0.7, -0.4 })]
        public void XSquaredOverTheThreeQuarterPower(string integrand, double a, double b, double[] points)
            => DifferentiatesBack(integrand, points, ("a", a), ("b", b));

        /// <summary>
        /// Across 0, where the cube root's answers are written over <c>x</c>: the value is the one
        /// quadrature gives.
        /// </summary>
        [Theory]
        [InlineData("integral(1/((1 + x^2)^(1/3)*(9 + x^2)), x, -1, 1)", 0.197369774498477432459)]
        [InlineData("integral(1/((1 - x^2)^(1/3)*(3 + x^2)), x, -1/2, 1/2)", 0.334352054745427304208)]
        public void ADefiniteIntegralAcrossZero(string integral, double expected)
        {
            var value = integral.ToEntity().Simplify();
            Assert.IsNotType<Entity.Integralf>(value);
            var number = value.EvalNumerical();
            Assert.True(Math.Abs((double)number.RealPart - expected) < 1e-9 && Math.Abs((double)number.ImaginaryPart) < 1e-9,
                $"{integral} is {value}, {number}, where quadrature gives {expected}");
        }
    }
}
