//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A rational function of <c>x</c> and one cube root of a polynomial, integrated by the
    /// ansatz over logarithms of <c>L - y</c> for linear <c>L</c> and their conjugates.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// No substitution rationalises <c>y^3 = P(x)</c> for a cubic <c>P</c>, and Welz's suite
    /// of these had no antiderivative at all. Each answer here is a sum of
    /// <c>ln(L - y)</c>, <c>ln(L^2 + L y + y^2)</c> and <c>arctan((2L + y)/(sqrt(3) y))</c>
    /// over the lines <c>L</c> whose cube meets <c>P</c> at the poles, with logarithms of
    /// the pole factors and a rational part in <c>y</c> and <c>y^2</c>.
    /// </para>
    /// <para>
    /// Every answer is differentiated back at points on both sides of zero, with any symbol
    /// in the integrand pinned; where the radicand is negative the cube root is the
    /// principal complex one on both sides, and the identity holds there as well.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class CubeRootLogarithmAnsatzTest
    {
        private static readonly double[] Points = { -0.7, -0.3, 0.2, 0.5, 0.8, 1.6, 2.4 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var symbol in original.Vars.Where(v => v.Name != "x"))
            {
                derivative = derivative.Substitute(symbol, 2.3);
                original = original.Substitute(symbol, 2.3);
            }
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-8,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 5,
                $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// Answers over the rationals: the line is the tangent at the pole, or through a root
        /// of <c>P</c> and infinity, and for <c>3 + x^2</c> the cube roots of <c>-8</c> times
        /// the cube roots of unity of <c>Q(sqrt(-3))</c>. Timofeev's has a rational part,
        /// <c>-y/x</c>.
        /// </summary>
        [Theory]
        [InlineData("1/(-5 + 7*x - 3*x^2 + x^3)^(1/3)")]
        [InlineData("(-1 + x)/((1 + x)*(2 + x^3)^(1/3))")]
        [InlineData("1/((3 + x^2)*(1 + 3*x^2)^(1/3))")]
        [InlineData("((-1 + x)^2*(1 + x))^(1/3)/x^2")]
        public void OverTheRationals(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// Answers over <c>Q(2^(1/3))</c>, the cube root of <c>P</c> at the pole <c>-1</c> or
        /// of <c>P</c> modulo <c>1 - x + x^2</c>; the coefficients are found exactly in that
        /// field, and <c>4^(1/3)</c> is written as <c>2^(2/3)</c>.
        /// </summary>
        [Theory]
        [InlineData("x/((1 + x)*(1 - x^3)^(1/3))")]
        [InlineData("1/(x*(4 - 6*x + 3*x^2)^(1/3))")]
        [InlineData("(1 - x^3)^(1/3)/(1 - x + x^2)")]
        [InlineData("1/((1 - x^3)^(1/3)*(1 + x^3))")]
        [InlineData("(1 + x)^2/((1 - x^3)^(1/3)*(1 + x^3))")]
        [InlineData("(1 - x)/((1 + x + x^2)*(1 + x^3)^(1/3))")]
        [InlineData("(1 - x^3)^(2/3)/(1 + x^3)")]
        public void OverTheCubeRootOfTwo(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A symbol in the radicand, with the coefficients of the answer constants: the system
        /// is solved with the symbol pinned twice, and the two agreeing settles it.
        /// </summary>
        [Theory]
        [InlineData("1/(x*(-q + x^2))^(1/3)")]
        [InlineData("1/((-1 + x)*(q - 2*x + x^2))^(1/3)")]
        public void WithASymbolInTheRadicand(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// Declined, quickly, rather than answered wrongly: a constant <c>q^(1/3)</c> the field
        /// does not hold, and a pole pair over <c>Q(sqrt(3))</c> whose answer needs lines with
        /// coefficients there.
        /// </summary>
        [Theory]
        [InlineData("1/(x*((-1 + x)*(q - 2*q*x + x^2))^(1/3))")]
        [InlineData("1/((3 - x^2)*(2 + x^2)^(1/3))")]
        public void DeclinedWhereTheFieldIsNotHeld(string integrand)
            => Assert.Contains("integral(", integrand.ToEntity().Integrate("x").Stringize());
    }
}
