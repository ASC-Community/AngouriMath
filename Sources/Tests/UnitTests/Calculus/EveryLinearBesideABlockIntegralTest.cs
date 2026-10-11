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
    /// Every linear beside a block with symbols in it is split off, at the same level, and a
    /// quadratic over the rationals with rational roots is taken as its two linears: under the
    /// sine, <c>sec(x)/(a + b sin(x)^3)</c> is <c>1/((1 - u^2)(a + b u^3))</c>. Rubi's 4.1.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class EveryLinearBesideABlockIntegralTest
    {
        [Theory]
        [InlineData("1/((1 - x)*(1 + x)*(a + b*x^3))")]
        [InlineData("1/(x*(1 - x^2)*(a + b*x^3))")]
        [InlineData("sec(x)/(a + b*sin(x)^3)")]
        public void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2).Substitute("b", 3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.15, 0.35, 0.6, 0.85 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
