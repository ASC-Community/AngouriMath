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
    /// A half-odd power of <c>b^2 + 2 a b f + a^2 f^2</c>, a perfect square in one trigonometric
    /// function <c>f</c>, is that power of <c>sqrt(a^2) |f + b/a|</c>, the sign in front of the
    /// integral. Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 1.3</c>, <c>b = 0.7</c>, on both sides of the
    /// zeros of <c>f + b/a</c> and of zero.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class RootOfASquareInATrigonometricFunctionIntegralTest
    {
        [Theory]
        [InlineData("(a + b*sin(x))*sqrt(b^2 + 2*a*b*sin(x) + a^2*sin(x)^2)")]
        [InlineData("(a + b*tan(x))/sqrt(b^2 + 2*a*b*tan(x) + a^2*tan(x)^2)")]
        [InlineData("(a + b*sec(x))*(b^2 + 2*a*b*sec(x) + a^2*sec(x)^2)^(3/2)")]
        [InlineData("(a + b*sin(g + f*x))/(b^2 + 2*a*b*sin(g + f*x) + a^2*sin(g + f*x)^2)^(3/2)")]
        public void IsAPowerOfTheModulusOfTheLinear(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("g", 0.4).Substitute("f", 1.1);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.3, -1.1, -0.3, 0.4, 1.1, 2.3 })
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
