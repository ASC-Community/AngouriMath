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
    /// A power of a monomial in a symbolic power of x, distributed with its exponent of x
    /// simplified: <c>(c x^n)^(2/n)</c> is <c>c^(2/n) x^2</c>. Rubi's 1.1.3.2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 1.3</c>, <c>b = 0.7</c>, <c>c = 0.4</c> and
    /// <c>n = 1.7</c>, at positive x, where a symbolic power of it is real.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class DistributedPowerOfAMonomialIntegralTest
    {
        [Theory]
        [InlineData("1/(a + b*(c*x^n)^(2/n))")]
        [InlineData("1/(x^2*(a + b*(c*x^n)^(1/n)))")]
        [InlineData("1/(a + b*(c*x^n)^(3/n))")]
        [InlineData("1/(1 + 4*(x^(2*n))^(1/n))")]
        public void IsIntegrated(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 0.4).Substitute("n", 1.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.8, 1.4, 2.1 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
