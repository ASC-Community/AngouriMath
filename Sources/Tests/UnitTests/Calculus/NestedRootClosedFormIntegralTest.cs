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
    /// Four nested roots with a closed form, Rubi's 1.3.3: a root of a constant plus a root of a
    /// quadratic, a root over the root of the quartic inside it, a reciprocal of that quartic
    /// beside the root, and a root over <c>x</c> and the root of the quadratic inside it.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 0.7</c>, <c>b = 1.3</c>, <c>c = 0.6</c> and
    /// <c>d = 1.1</c>, on both sides of 0 where the integrand is real there.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class NestedRootClosedFormIntegralTest
    {
        [Theory]
        [InlineData("sqrt(1 + sqrt(1 - x^2))")]
        [InlineData("sqrt(a + b*sqrt(a^2/b^2 + c*x^2))")]
        [InlineData("sqrt(x^2 + sqrt(1 + x^4))/sqrt(1 + x^4)")]
        [InlineData("sqrt(-b*x^2 + sqrt(a + b^2*x^4))/sqrt(a + b^2*x^4)")]
        [InlineData("1/((1 + x^4)*sqrt(-x^2 + sqrt(1 + x^4)))")]
        [InlineData("1/((a + b*x^4)*sqrt(c*x^2 + d*sqrt(a + b*x^4)))")]
        [InlineData("sqrt(a*x^2 + b*x*sqrt(-a/b^2 + a^2*x^2/b^2))/(x*sqrt(-a/b^2 + a^2*x^2/b^2))")]
        public void IsIntegrated(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 0.7).Substitute("b", 1.3).Substitute("c", 0.6).Substitute("d", 1.1);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -0.9, -0.4, 0.3, 0.8 })
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
            Assert.True(compared >= 2, $"only {compared} points were comparable for {integrand}");
        }
    }
}
