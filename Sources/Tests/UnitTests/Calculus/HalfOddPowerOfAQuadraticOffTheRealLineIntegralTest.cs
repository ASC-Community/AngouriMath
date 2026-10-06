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
    /// A polynomial times an odd half power of a quadratic with <c>i</c> among its coefficients:
    /// the reduction to <c>R Q^(k + 1/2) + K/sqrt(Q)</c> is one linear solve, exact whatever the
    /// coefficients are, and <c>K/sqrt(Q)</c> the table answers off the real line as on it, so
    /// <c>sqrt(3 i x + 4 x^2)</c> was answered and <c>(3 i x + 4 x^2)^(5/2)</c> declined. The
    /// integrands are complex for a real <c>x</c>, and compared as complex numbers.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class HalfOddPowerOfAQuadraticOffTheRealLineIntegralTest
    {
        [Theory]
        [InlineData("(3*i*x + 4*x^2)^(5/2)")]
        [InlineData("1/(3*i*x + 4*x^2)^(7/2)")]
        [InlineData("(1 + i*x + x^2)^(3/2)")]
        [InlineData("x*(1 + i*x + x^2)^(3/2)")]
        [InlineData("(a + i*b*x + x^2)^(3/2)")]
        public void IsReducedAsOnTheRealLine(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.7, -0.4, 0.3, 0.9, 2.2 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || got.IsNaN)
                    continue;
                compared++;
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} points could be compared for {integrand}");
        }
    }
}
