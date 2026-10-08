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
    /// <c>a + i a tan(g + f x)</c> below the bar, beside other functions of the same shifted linear,
    /// integrated in <c>u = g + f x</c>. Rubi's 4.3.3.1. The integrands are complex for a real <c>x</c>
    /// and are compared as complex numbers.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnImaginaryTangentSumOfAShiftedLinearIntegralTest
    {
        [Theory]
        [InlineData("sqrt(a + i*a*tan(g + f*x))*(A + B*tan(g + f*x))/sqrt(c - i*c*tan(g + f*x))")]
        [InlineData("(A + B*tan(g + f*x))/(sqrt(a + i*a*tan(g + f*x))*(c - i*c*tan(g + f*x))^(3/2))")]
        public void InTheLinear(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length < 5000, $"{text.Length} characters of answer for {integrand}");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("c", 0.7).Substitute("g", 0.4).Substitute("f", 1.1).Substitute("A", 0.9).Substitute("B", 1.7);
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
