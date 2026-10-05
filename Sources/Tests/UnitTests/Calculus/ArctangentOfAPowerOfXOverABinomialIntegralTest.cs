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
    /// The derivative of <c>w = x^j/(x^p + l)</c> over a multiple of <c>(x^p + l)^2</c> plus a
    /// power of x: an arctangent of <c>w</c>, or a hyperbolic one. Rubi's 1.3.2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>d = 0.7</c>, <c>f = 0.4</c>, <c>k = 1.3</c>,
    /// <c>m = 0.6</c> and <c>n = 1.7</c>: on both sides of 0 where the powers of x are whole, and
    /// at positive x where they are symbols.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class ArctangentOfAPowerOfXOverABinomialIntegralTest
    {
        [Theory]
        [InlineData("(k - 4*f*x^3)/(k^2 + 4*d*f*x^2 + 4*k*f*x^3 + 4*f^2*x^6)")]
        [InlineData("(k - 4*f*x^3)/(k^2 - 4*d*f*x^2 + 4*k*f*x^3 + 4*f^2*x^6)")]
        [InlineData("x^2*(3*k + 2*f*x^2)/(k^2 + 4*k*f*x^2 + 4*f^2*x^4 + 4*d*f*x^6)")]
        [InlineData("x*(2*k - 2*f*x^3)/(k^2 + 4*k*f*x^3 - 4*d*f*x^4 + 4*f^2*x^6)")]
        public void IsIntegratedOnBothSidesOfZero(string integrand)
            => DifferentiatesBack(integrand, new[] { -1.6, -0.7, 0.3, 0.9, 1.7 });

        [Theory]
        [InlineData("(k - 2*f*(n - 1)*x^n)/(k^2 + 4*d*f*x^2 + 4*k*f*x^n + 4*f^2*x^(2*n))")]
        [InlineData("x^m*(k*(1 + m) + 2*f*(1 + m - n)*x^n)/(k^2 + 4*d*f*x^(2 + 2*m) + 4*k*f*x^n + 4*f^2*x^(2*n))")]
        [InlineData("x^m*(k*(1 + m) + 2*f*(1 + m - n)*x^n)/(k^2 - 4*d*f*x^(2 + 2*m) + 4*k*f*x^n + 4*f^2*x^(2*n))")]
        public void IsIntegratedInSymbolicPowersOfX(string integrand)
            => DifferentiatesBack(integrand, new[] { 0.3, 0.9, 1.7, 2.6 });

        private static void DifferentiatesBack(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.Contains("arctan(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("d", 0.7).Substitute("f", 0.4).Substitute("k", 1.3).Substitute("m", 0.6).Substitute("n", 1.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in points)
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
