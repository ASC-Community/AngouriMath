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
    /// Powers of the conjugate sums <c>a + i a tan(x)</c> and <c>c - i c tan(x)</c>, both half-odd,
    /// beside a function of the tangent: <c>a + i a tan(x)</c> is <c>a sec(x) e^(i x)</c> and
    /// <c>c - i c tan(x)</c> is <c>c sec(x) e^(-i x)</c>, so the pair is a constant times
    /// <c>sec(x)^k e^(i (p - q) x)</c>, rational in <c>e^(i x)</c>. Rubi's 4.3.2.1 and 4.3.3.1. The
    /// integrands are complex for a real <c>x</c>, and compared as complex numbers.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ConjugatePairOfImaginaryTangentSumsIntegralTest
    {
        [Theory]
        [InlineData("(a + i*a*tan(x))^(7/2)*(A + B*tan(x))/(c - i*c*tan(x))^(9/2)")]
        [InlineData("(a + i*a*tan(x))^(3/2)*(A + B*tan(x))/(c - i*c*tan(x))^(3/2)")]
        [InlineData("(a + i*a*tan(x))^(3/2)/(c - i*c*tan(x))^(3/2)")]
        [InlineData("1/((a + i*a*tan(x))^(7/2)*(c - i*c*tan(x))^(3/2))")]
        [InlineData("(A + B*tan(x))/((a + i*a*tan(x))^(3/2)*(c - i*c*tan(x))^(3/2))")]
        public void AsTheExponentialTheyMake(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("c", 0.7).Substitute("A", 0.4).Substitute("B", 1.1);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.2, -0.7, 0.3, 0.8, 1.3, 2.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                if (want.IsNaN)
                    continue;
                compared++;
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 5, $"only {compared} points could be compared for {integrand}");
        }
    }
}
