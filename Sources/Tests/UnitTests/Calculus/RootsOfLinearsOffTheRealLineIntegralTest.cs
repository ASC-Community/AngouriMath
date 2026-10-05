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
    /// The roots of two linears over a linear whose coefficients are not real, where the closed
    /// forms chose between an arctangent and a logarithm by the sign of a quantity that has none,
    /// and the answer held at no point.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Compared as complex numbers, the integrands being complex, and differentiated before the
    /// symbols are pinned: a piecewise arm whose condition compares a number off the real line, pinned
    /// first and then differentiated, made the whole derivative NaN.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class RootsOfLinearsOffTheRealLineIntegralTest
    {
        [Theory]
        [InlineData("1/(sqrt(x)*sqrt(a + b*x)*(1 - i*x))")]
        [InlineData("sqrt(x)/(sqrt(a + b*x)*(1 + i*x))")]
        [InlineData("sqrt(x)*sqrt(a + b*x)/(1 + i*x)")]
        public void IsIntegratedOverTheLinear(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0).Differentiate("x"));
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.7, 1.2, 2.0 })
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
