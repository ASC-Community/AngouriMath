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
    /// A power of <c>a + b cos(y) + c sin(y)</c> with <c>a^2 = b^2 + c^2</c>, which is
    /// <c>a (1 ± cos(y - phi))</c>, a square of the half angle: half-odd powers either way and
    /// negative whole ones, in closed form through <c>T = b sin(y) - c cos(y)</c>. Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Compared as complex numbers on both sides of zero and away from the zeros of the base:
    /// with <c>a</c> negative the base is not positive anywhere, and the integrand is imaginary
    /// on the whole line.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class CosineAndSinePlusTheirAmplitudeIntegralTest
    {
        [Theory]
        [InlineData("sqrt(5 + 4*cos(x) + 3*sin(x))")]
        [InlineData("(5 + 4*cos(x) + 3*sin(x))^(5/2)")]
        [InlineData("1/sqrt(5 + 4*cos(x) + 3*sin(x))")]
        [InlineData("1/(5 + 4*cos(x) + 3*sin(x))^(3/2)")]
        [InlineData("(-5 + 4*cos(x) + 3*sin(x))^(3/2)")]
        [InlineData("1/(b*cos(g + f*x) + c*sin(g + f*x) + sqrt(b^2 + c^2))^(5/2)")]
        [InlineData("(b*cos(g + f*x) + c*sin(g + f*x) - sqrt(b^2 + c^2))^(3/2)")]
        [InlineData("1/(b*cos(g + f*x) + c*sin(g + f*x) - sqrt(b^2 + c^2))^3")]
        public void IsWrittenThroughTheDerivativeOfItsBase(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("b", 0.9).Substitute("c", 0.7).Substitute("g", 0.4).Substitute("f", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.9, -1.1, -0.4, 0.4, 1.1, 1.9 })
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
