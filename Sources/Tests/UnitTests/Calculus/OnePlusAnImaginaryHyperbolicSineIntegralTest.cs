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
    /// Half-odd powers of <c>a + i a sinh(y)</c>, which is <c>a (cosh(y/2) + i sinh(y/2))^2</c>:
    /// beside a power of <c>x</c> each is a sum of exponentials of the half angle times that power,
    /// up to a constant on every interval where both are continuous. Rubi's 6.1.1 and 6.1.5. The
    /// integrands are complex, and compared as complex numbers on both sides of zero.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class OnePlusAnImaginaryHyperbolicSineIntegralTest
    {
        [Theory]
        [InlineData("x^3*sqrt(a + i*a*sinh(g + f*x))")]
        [InlineData("x*sqrt(a + i*a*sinh(g + f*x))")]
        [InlineData("sqrt(a + i*a*sinh(g + f*x))/x")]
        [InlineData("x^3*(a + i*a*sinh(g + f*x))^(3/2)")]
        [InlineData("1/sqrt(a + i*a*sinh(g + f*x))")]
        public void ByTheHalfAngle(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("g", 0.4).Substitute("f", 1.1);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.2, -0.4, 0.3, 0.8, 1.5 })
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
