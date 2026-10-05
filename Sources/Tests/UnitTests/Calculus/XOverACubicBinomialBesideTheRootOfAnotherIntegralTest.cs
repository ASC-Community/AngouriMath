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
    /// <c>x/((a + b x^3) sqrt(c + d x^3))</c> at the two ratios where its integral is elementary,
    /// Rubi's 1.1.3.4: <c>4 b c = a d</c>, in closed form by the sign of <c>c</c>, and
    /// <c>8 b c + a d = 0</c>, as three integrals.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>c</c> pinned to either sign and <c>d = 0.7</c>, on
    /// both sides of 0 wherever the integrand is real.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class XOverACubicBinomialBesideTheRootOfAnotherIntegralTest
    {
        private static readonly double[] Points = { -2.3, -1.4, -0.8, -0.3, 0.4, 0.9, 1.6, 2.4 };

        [Theory]
        // 4 b c = a d.
        [InlineData("x/((4 - x^3)*sqrt(1 - x^3))", 1.3)]
        [InlineData("x/((4 - d*x^3)*sqrt(-1 + d*x^3))", 1.3)]
        [InlineData("x/((4*c + d*x^3)*sqrt(c + d*x^3))", 1.3)]
        [InlineData("x/((4*c + d*x^3)*sqrt(c + d*x^3))", -1.3)]
        // 8 b c + a d = 0.
        [InlineData("x/((8 - d*x^3)*sqrt(1 + d*x^3))", 1.3)]
        [InlineData("x/((8*c - d*x^3)*sqrt(c + d*x^3))", 1.3)]
        [InlineData("x/((8*c - d*x^3)*sqrt(c + d*x^3))", -1.3)]
        [InlineData("3*x/((8 + x^3)*sqrt(-1 + x^3))", 1.3)]
        public void IsIntegrated(string integrand, double c)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("c", c).Substitute("d", 0.7);
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
            Assert.True(compared >= 2, $"only {compared} points were real for {integrand}");
        }
    }
}
