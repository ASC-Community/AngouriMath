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
    /// A power of the secant or the cosine beside a power of <c>a + i a tan(x)</c>, the powers adding
    /// up to no whole number, integrated in that sum: under <c>u = a + i a tan(x)</c>,
    /// <c>sec(x)^2 = (u/a) ((2 a - u)/a)</c>. Rubi's 4.3.1.2. The integrands are complex for a real
    /// <c>x</c> and are compared as complex numbers.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class SecantBesideAnImaginaryTangentSumIntegralTest
    {
        [Theory]
        [InlineData("sec(x)^3*sqrt(a + i*a*tan(x))")]
        [InlineData("sec(x)^5/(a + i*a*tan(x))^(3/2)")]
        [InlineData("(k*cos(x))^(3/2)*sqrt(a + i*a*tan(x))")]
        [InlineData("cos(x)^9*(a + i*a*tan(x))^(7/2)")]
        [InlineData("(m*sec(x))^(2/3)*(a + i*a*tan(x))^(5/3)")]
        public void InTheSum(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length < 5000, $"{text.Length} characters of answer for {integrand}");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("k", 0.7).Substitute("m", 1.1);
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
