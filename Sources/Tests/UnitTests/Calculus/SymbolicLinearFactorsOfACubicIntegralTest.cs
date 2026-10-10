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
    /// A symbolic cubic whose leading and constant coefficients are single terms, written as the
    /// linear factors that divide it before the partial fractions. Rubi's 1.3.1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class SymbolicLinearFactorsOfACubicIntegralTest
    {
        [Theory]
        [InlineData("1/(a*c*h + (b*c*h + a*d*h + a*c*f)*x + (b*d*h + b*c*f + a*d*f)*x^2 + b*d*f*x^3)")]
        [InlineData("1/(a*c*h + (b*c*h + a*d*h + a*c*f)*x + (b*d*h + b*c*f + a*d*f)*x^2 + b*d*f*x^3)^2")]
        public void OverTheFoundLinears(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length < 40000, $"{text.Length} characters of answer for {integrand}");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.4).Substitute("c", 1.9).Substitute("d", 1.6).Substitute("f", 0.7).Substitute("h", 0.45);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -2.1, -0.4, 0.3, 0.6, 1.6, 2.5 })
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
