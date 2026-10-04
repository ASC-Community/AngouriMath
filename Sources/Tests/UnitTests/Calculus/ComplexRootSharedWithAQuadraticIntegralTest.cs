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
    /// A linear with a root off the real line that a quadratic beside it shares, with symbols in
    /// the rest: <c>1/((1 + i u)(c + d u)(1 + u^2))</c>, where <c>1 + u^2</c> is
    /// <c>(1 + i u)(1 - i u)</c>, the tangent substitution's form of Rubi's 4.3.2.1
    /// <c>1/((a + i a tan(x)) (c + d tan(x)))</c>, declined or past a minute. The integrands are
    /// complex for a real <c>x</c>, and compared as complex numbers.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ComplexRootSharedWithAQuadraticIntegralTest
    {
        [Theory]
        [InlineData("1/((1 + i*x)*(c + d*x)*(1 + x^2))")]
        [InlineData("1/((a + i*a*tan(x))*(c + d*tan(x)))")]
        [InlineData("1/((a + i*a*tan(x))^2*(c + d*tan(x)))")]
        [InlineData("1/((a + i*a*tan(x))*(c + d*tan(x))^2)")]
        public void OverTheSharedRoot(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("c", 1.1).Substitute("d", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.2, -0.7, 0.3, 0.8, 1.3, 2.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 5, $"only {compared} points could be compared for {integrand}");
        }
    }
}
