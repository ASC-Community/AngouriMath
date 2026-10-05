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
    /// <c>(A + B cos(x) + C sin(x))/(a + b cos(x) + c sin(x))^n</c> through the denominator, its
    /// derivative and a constant, each power of the denominator brought down to its reciprocal.
    /// Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with the symbols pinned, inside <c>(-pi, pi)</c>, where the
    /// half-angle tangent the reciprocal is answered in is continuous.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class CosineAndSineOverAPowerOfAnotherIntegralTest
    {
        [Theory]
        [InlineData("sin(x)/(a + b*cos(x) + c*sin(x))")]
        [InlineData("(k + q*cos(x))/(a + b*cos(x) + c*sin(x))")]
        [InlineData("(k + p*cos(x) + q*sin(x))/(a + b*cos(x) + c*sin(x))^2")]
        [InlineData("(p*cos(x) + q*sin(x))/(a + b*cos(x) + c*sin(x))^3")]
        [InlineData("1/(a + b*cos(x) + c*sin(x))^2")]
        public void IsWrittenThroughItsDenominator(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.9).Substitute("c", 0.7)
                .Substitute("k", 1.1).Substitute("p", 0.6).Substitute("q", 0.8);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.6, -1.6, -0.4, 0.4, 1.4, 2.0 })
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
