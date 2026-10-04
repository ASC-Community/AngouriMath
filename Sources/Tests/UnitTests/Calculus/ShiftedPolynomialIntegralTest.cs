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
    /// A polynomial below the bar with a symbol among its coefficients that is a binomial, or a
    /// quartic even in its variable, once written in <c>y = x + s</c> with
    /// <c>s = a_(n-1)/(n a_n)</c>: Rubi's 1.3.1. Nothing factors a polynomial with a symbol in it,
    /// and in y the rules for a binomial and for an even quartic read it at once.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with the symbols pinned after the integral is taken.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class ShiftedPolynomialIntegralTest
    {
        private static readonly double[] Points = { 0.3, 0.6, 0.9 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 0.7).Substitute("b", 1.3).Substitute("c", 2.1);
            var derivative = Pin(integral.Substitute("C", 0).Differentiate("x"));
            var original = Pin(integrand.ToEntity());
            foreach (var point in Points)
            {
                var expected = original.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.True(Math.Abs(expected - actual) < 1e-8 * Math.Max(1, Math.Abs(expected)),
                    $"d/dx of the antiderivative of {integrand} is {actual} at x = {point}, where the integrand is {expected}");
            }
        }

        /// <summary>
        /// <c>c^2 x^3 + 3 b c x^2 + 3 b^2 x + 3 a b</c> is <c>((c x + b)^3 + 3 a b c - b^3)/c</c>.
        /// </summary>
        [Theory]
        [InlineData("1/(3*a*b + 3*b^2*x + 3*b*c*x^2 + c^2*x^3)")]
        [InlineData("1/(3*a*b + 3*b^2*x + 3*b*c*x^2 + c^2*x^3)^2")]
        public void ACubicThatIsABinomialInALinear(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// <c>a + 8 x - 8 x^2 + 4 x^3 - x^4</c> is <c>a + 3 - 2 y^2 - y^4</c> in <c>y = x - 1</c>.
        /// </summary>
        [Theory]
        [InlineData("x/(a + 8*x - 8*x^2 + 4*x^3 - x^4)")]
        [InlineData("1/(a + 8*x - 8*x^2 + 4*x^3 - x^4)")]
        public void AQuarticThatIsEvenInALinear(string integrand) => DifferentiatesBack(integrand);
    }
}
