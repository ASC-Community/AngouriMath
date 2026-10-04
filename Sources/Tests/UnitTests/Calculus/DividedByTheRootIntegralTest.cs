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
    /// A rational function of <c>x^n</c> times <c>(c + d x^n)^(k - 1/n)</c>, which
    /// <c>u = x/(c + d x^n)^(1/n)</c> makes a rational function of <c>u</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back at points where the integrand is real, with the symbols
    /// pinned after the integral is taken.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class DividedByTheRootIntegralTest
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

        /// <summary>Timofeev's, which is <c>1/(1 + u^4)</c> under the substitution.</summary>
        [Fact]
        public void TimofeevsQuarticBesideAFourthRoot()
            => DifferentiatesBack("1/((1 + x^4)*(2 + x^4)^(1/4))", new[] { -1.3, -0.4, 0.3, 0.9, 1.7 });

        /// <summary>
        /// A higher power of the root with symbols in it, beside another quadratic: the rational
        /// function of <c>u</c> grows with the power, and the larger ones ran past 90 s.
        /// </summary>
        [Theory]
        [InlineData("1/((a + b*x^2)^(5/2)*(1 + x^2))")]
        [InlineData("1/((a + b*x^2)^(7/2)*(1 + x^2))")]
        [InlineData("1/((a + b*x^2)^(9/2)*(1 + x^2))")]
        public void AHalfOddPowerOfASymbolicQuadratic(string integrand)
            => DifferentiatesBack(integrand, new[] { -1.3, -0.4, 0.3, 0.9, 1.7 }, ("a", 1.3), ("b", 0.7));

        /// <summary>
        /// Beside a power of x that is not one of x^n: <c>x^j</c> times a rational function of
        /// <c>x^n</c> times <c>(a + b x^n)^(k - (j + 1)/n)</c> is <c>u^j</c> times the same in
        /// <c>u</c>. Rubi's 1.1.3.4; each was declined. On both sides of zero, the cube root of a
        /// negative being real.
        /// </summary>
        [Theory]
        [InlineData("x/((a + b*x^3)^(2/3)*(c + d*x^3))")]
        [InlineData("x^4/((a + b*x^3)^(2/3)*(c + d*x^3))")]
        [InlineData("x^7/((a + b*x^3)^(2/3)*(c + d*x^3))")]
        [InlineData("x^4*(a + b*x^3)^(1/3)/(c + d*x^3)")]
        [InlineData("x/((1 + x^3)^(2/3)*(2 + x^3))")]
        public void BesideAPowerOfX(string integrand)
            => DifferentiatesBack(integrand, new[] { -2.7, -1.3, -0.6, 0.4, 1.3, 2.6 }, ("a", 2.3), ("b", 0.7), ("c", 1.3), ("d", 1.7));
    }
}
