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
    /// A linear over a linear, or a quadratic over a quadratic, beside the reciprocal of the
    /// square root of a cubic binomial, at the coefficients where the integral is elementary:
    /// Rubi's 1.3.2, by its 1.3.3's rules.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 1.3</c>, <c>b = 0.7</c>, <c>c = 1.1</c> and
    /// <c>d = 0.6</c>, at points where the radicand is positive and away from the pole.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PseudoEllipticQuotientIntegralTest
    {
        [Theory]
        // b c^3 = 4 a d^3 and 2 d e + c f = 0.
        [InlineData("(c - 2*d*x)/((c + d*x)*sqrt(c^3 + 4*d^3*x^3))")]
        [InlineData("(2^(2/3) - 2*x)/((2^(2/3) + x)*sqrt(1 + x^3))")]
        // b c^3 = -8 a d^3 and 2 d e + c f = 0.
        [InlineData("(a^(1/3) + b^(1/3)*x)/((2*a^(1/3) - b^(1/3)*x)*sqrt(a + b*x^3))")]
        // b^2 c^6 - 20 a b c^3 d^3 - 8 a^2 d^6 = 0, at its second condition.
        [InlineData("(1 + x + sqrt(3))/((1 + x - sqrt(3))*sqrt(1 + x^3))")]
        [InlineData("(1 + (b/a)^(1/3)*x - sqrt(3))/((1 + (b/a)^(1/3)*x + sqrt(3))*sqrt(a + b*x^3))")]
        // A quadratic over a quadratic.
        [InlineData("(2 - 2*x - x^2)/((2 + d + d*x + x^2)*sqrt(1 + x^3))")]
        public void IsIntegrated(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 1.1).Substitute("d", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -0.8, -0.4, 0.3, 1.1, 1.6 })
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
