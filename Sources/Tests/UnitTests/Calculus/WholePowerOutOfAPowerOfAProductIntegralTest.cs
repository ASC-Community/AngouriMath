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
    /// A power that is not whole of a constant times a whole power of something of x,
    /// <c>(c S^k)^p</c>, as <c>K S^(k p)</c> with <c>K</c> constant where <c>S</c> is not zero.
    /// Rubi's 1.3.2:338-352, which the substitution search, asked first, declines.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = -1.3</c>, <c>b = 0.7</c> and <c>c = 0.4</c>, on
    /// both sides of 0 and of the roots of <c>a + b x^2</c>, where <c>K</c> changes sign, wherever
    /// the integrand is real.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class WholePowerOutOfAPowerOfAProductIntegralTest
    {
        private static readonly double[] Points = { -2.3, -1.9, -0.8, -0.3, 0.4, 0.9, 1.9, 2.3 };

        [Theory]
        [InlineData("x^2*(c*(a + b*x^2)^2)^(3/2)")]
        [InlineData("x^4*(c*(a + b*x^2)^2)^(3/2)")]
        [InlineData("(c*(a + b*x^2)^2)^(3/2)")]
        [InlineData("(c*(a + b*x^2)^2)^(3/2)/x^2")]
        [InlineData("(c*(a + b*x^2)^3)^(3/2)")]
        [InlineData("(c*(a + b*x^2)^3)^(3/2)/x^2")]
        public void IsIntegrated(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", -1.3).Substitute("b", 0.7).Substitute("c", 0.4);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in Points)
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || Math.Abs((double)want.ImaginaryPart) > 1e-12 * Math.Max(1, Math.Abs((double)want.RealPart)))
                    continue;
                var got = derivative.Substitute("x", at).EvalNumerical();
                compared++;
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} points were real for {integrand}");
        }
    }
}
