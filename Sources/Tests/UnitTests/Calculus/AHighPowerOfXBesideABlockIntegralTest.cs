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
    /// A power of x past the twelfth beside a block with symbols in it is split off as the first
    /// terms of the block's series at 0: Rubi's 1.1.3.8 asks for up to <c>x^17</c> beside
    /// <c>(a + b x^3)^3</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AHighPowerOfXBesideABlockIntegralTest
    {
        [Theory]
        [InlineData("(c + d*x^3 + g*x^6 + f*x^9)/(x^14*(a + b*x^3))")]
        [InlineData("(c + d*x^3 + g*x^6 + f*x^9)/(x^17*(a + b*x^3))")]
        [InlineData("(c + d*x^3 + g*x^6 + f*x^9)/(x^14*(a + b*x^3)^2)")]
        public void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2).Substitute("b", 3).Substitute("c", 5)
                .Substitute("d", -1).Substitute("g", 7).Substitute("f", 1);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.7, -0.6, 0.45, 1.3 })
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
