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
    /// A rational function with symbols in it beside a root of a linear, split into partial
    /// fractions first: <c>1/(x (1 + x^2) sqrt(a + b x))</c> is
    /// <c>1/(x sqrt(a + b x)) - x/((1 + x^2) sqrt(a + b x))</c>, each answered alone, and was
    /// declined; so was what the tangent substitution makes of Rubi's 4.3.2.1,
    /// <c>cot(x)/sqrt(a + b tan(x))</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class SplitBesideARootIntegralTest
    {
        [Theory]
        [InlineData("1/(x*(1 + x^2)*sqrt(a + b*x))")]
        [InlineData("1/(x*(1 + x^2)*(a + b*x)^(3/2))")]
        [InlineData("1/((x + c)*(x + d)*sqrt(a + b*x))")]
        [InlineData("cot(x)/sqrt(a + b*tan(x))")]
        [InlineData("cot(x)^2/(a + b*tan(x))^(3/2)")]
        public void TermByTerm(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.7).Substitute("c", 1.3).Substitute("d", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.7, -0.9, 0.3, 0.8, 1.6, 2.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || Math.Abs((double)want.ImaginaryPart) > 1e-12)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 5, $"only {compared} points could be compared for {integrand}");
        }
    }
}
