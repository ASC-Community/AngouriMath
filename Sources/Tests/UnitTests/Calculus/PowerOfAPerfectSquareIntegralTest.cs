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
    /// A power of a square written out, <c>(A + B w + C w^2)^p</c> with <c>B^2 = 4 A C</c> and
    /// <c>w</c> a power of x, integrated as the power of its root <c>L = w + B/(2 C)</c> times the
    /// factor <c>(A + B w + C w^2)^p / L^(2p)</c>, which is constant wherever <c>L</c> is not zero.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Differentiated back with <c>a</c> negative, so that <c>L</c> changes sign among the points
    /// and the factor is checked on both sides of its zero, where it is not the same constant.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfAPerfectSquareIntegralTest
    {
        private static readonly double[] Points = { 0.3, 0.9, 1.7, 2.6 };

        private static void DifferentiatesBack(string integrand, params (string, double)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());

            // The integration constant is C_1 where the integrand has a symbol named C.
            Entity original = integrand.ToEntity();
            var derivative = (original.Vars.Any(v => v.Name == "C") ? integral : integral.Substitute("C", 0)).Differentiate("x");
            foreach (var (name, value) in pins)
            {
                derivative = derivative.Substitute(name, value);
                original = original.Substitute(name, value);
            }
            var compared = 0;
            foreach (var at in Points)
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
        /// A symbolic power, and a power that is neither whole nor half an odd number: Rubi's
        /// 1.2.1.2, 1.2.2.2 and 1.2.3.2, which were declined.
        /// </summary>
        [Theory]
        [InlineData("(a^2 + 2*a*b*x + b^2*x^2)^p")]
        [InlineData("(a + b*x)*(d + e*x)^3*(a^2 + 2*a*b*x + b^2*x^2)^p")]
        [InlineData("x^2*(a^2 + 2*a*b*x^3 + b^2*x^6)^p")]
        [InlineData("(a^2 + 2*a*b*x^2 + b^2*x^4)^(3/4)")]
        public void APowerThatIsNotHalfAnOddNumber(string integrand)
            => DifferentiatesBack(integrand, ("a", -2.5), ("b", 1), ("d", 0.7), ("e", 1.3), ("p", 0.35));

        /// <summary>
        /// A square in a power of x that is not a whole number: a fraction, or a symbol, Rubi's
        /// <c>x sqrt(a^2 + 2 a b x^n + b^2 x^(2n))</c>.
        /// </summary>
        [Theory]
        [InlineData("x*(a^2 + 2*a*b*x^(1/3) + b^2*x^(2/3))^p")]
        [InlineData("x*sqrt(a^2 + 2*a*b*x^n + b^2*x^(2*n))")]
        public void ASquareInAPowerThatIsNotWhole(string integrand)
            => DifferentiatesBack(integrand, ("a", -2.5), ("b", 1), ("p", -1.7), ("n", 1.5));

        /// <summary>
        /// The square below the bar, where it is <c>1/F</c> that comes out of the integral.
        /// </summary>
        [Theory]
        [InlineData("1/(a^2 + 2*a*b*x^(1/3) + b^2*x^(2/3))^p")]
        [InlineData("1/(a^2 + b^2/x^(2/3) + 2*a*b/x^(1/3))^(3/4)")]
        [InlineData("x^(2*n - 1)/(a^2 + 2*a*b*x^n + b^2*x^(2*n))^(3/2)")]
        [InlineData("1/(x*sqrt(a^2 + 2*a*b*x^n + b^2*x^(2*n)))")]
        [InlineData("x/(a^2 + 2*a*b*x + b^2*x^2)^p")]
        public void TheSquareBelowTheBar(string integrand)
            => DifferentiatesBack(integrand, ("a", -2.5), ("b", 1), ("p", 0.35), ("n", 1.5));
    }
}
