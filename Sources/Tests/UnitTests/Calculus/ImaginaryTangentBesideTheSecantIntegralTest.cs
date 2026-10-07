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
    /// A power of <c>a + i a tan(x)</c> beside a power of the secant, one of them not whole and
    /// the two adding up to a whole number: <c>a + i a tan(x)</c> is <c>a sec(x) e^(i x)</c> on
    /// the real line, so the integrand is a constant on every interval where it is continuous times
    /// a power of the secant and an exponential. Rubi's 4.3.1.2. The integrands are complex along
    /// the real line and are compared as complex numbers, where the cosine is negative as well,
    /// which is where the fourth row's answer on master was off by a constant factor. A symbol
    /// for the powers, adding up to a whole number, as well.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ImaginaryTangentBesideTheSecantIntegralTest
    {
        [Theory]
        [InlineData("sqrt(a + i*a*tan(x))/sqrt(k*sec(x))")]
        [InlineData("sqrt(k*sec(x))*sqrt(a + i*a*tan(x))")]
        [InlineData("(a + i*a*tan(x))^(3/2)/(k*sec(x))^(7/2)")]
        [InlineData("(k*sec(x))^(5/2)/(a + i*a*tan(x))^(5/2)")]
        [InlineData("(k*sec(c + d*x))^(3/2)*(a - i*a*tan(c + d*x))^(3/2)")]
        // A symbol for the powers, adding up to -4, -1 and 0.
        [InlineData("(k*sec(c + d*x))^(-4 - n)*(a + i*a*tan(c + d*x))^n")]
        [InlineData("(k*sec(c + d*x))^(-1 - n)*(a + i*a*tan(c + d*x))^n")]
        [InlineData("(a + i*a*tan(c + d*x))^n/(k*sec(c + d*x))^n")]
        [InlineData("sqrt(a + i*a*tan(x))/(k*cos(x))^(3/2)")]
        [InlineData("(k*cos(c + d*x))^(5/2)/sqrt(a + i*a*tan(c + d*x))")]
        public void AsTheExponentialItIs(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("k", 0.8).Substitute("c", 0.4).Substitute("d", 1.1).Substitute("n", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -2.6, -1.1, -0.4, 0.3, 0.8, 1.2, 2.0 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 6, $"only {compared} points could be compared for {integrand}");
        }
    }
}
