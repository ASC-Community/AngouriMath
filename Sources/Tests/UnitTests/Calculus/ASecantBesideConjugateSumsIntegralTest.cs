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
    /// <c>sec(y) (a ± a sec(y))^m (c ∓ c sec(y))^n</c>, with <c>n</c> half-odd and <c>m</c> a
    /// symbol, in <c>u = a ± a sec(y)</c>, and the cosecant's. Rubi's 4.5.2.3.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASecantBesideConjugateSumsIntegralTest
    {
        [Theory]
        [InlineData("sec(g + f*x)*(a + a*sec(g + f*x))^m*(c - c*sec(g + f*x))^(5/2)")]
        [InlineData("sec(g + f*x)*(a + a*sec(g + f*x))^m*sqrt(c - c*sec(g + f*x))")]
        [InlineData("sec(x)*(a - a*sec(x))^m*sqrt(c + c*sec(x))")]
        [InlineData("csc(x)*(a + a*csc(x))^m*sqrt(c - c*csc(x))")]
        public void InTheSum(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length < 2000, $"{text.Length} characters of answer for {integrand}");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("c", 0.9).Substitute("m", 2.3).Substitute("g", 0.2).Substitute("f", 0.7);
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
