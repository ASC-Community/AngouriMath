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
    /// <c>tan(y) tan(2y)</c> is <c>sec(2y) - 1</c>, and written so a power of
    /// <c>c tan(y) tan(2y)</c> beside the secant or cosine of <c>2y</c> is in one argument, which
    /// the half-angle tangent answers. Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 0.4</c>, <c>b = 1.1</c>, <c>c = 1.3</c> where the
    /// integrand is real, <c>cos(2(a + b x))</c> positive, on both sides of the zero of
    /// <c>tan(a + b x)</c>: the rule the rewriting reaches is exact there.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class TangentTimesThatOfItsDoubleIntegralTest
    {
        [Theory]
        [InlineData("sec(2*(a + b*x))^2*sqrt(c*tan(a + b*x)*tan(2*(a + b*x)))")]
        [InlineData("cos(2*(a + b*x))*(c*tan(a + b*x)*tan(2*(a + b*x)))^(3/2)")]
        [InlineData("1/sqrt(c*tan(a + b*x)*tan(2*(a + b*x)))")]
        [InlineData("sec(2*(a + b*x))/(c*tan(a + b*x)*tan(2*(a + b*x)))^(3/2)")]
        [InlineData("tan(x)*tan(2*x)")]
        public void IsAPowerOfASecantLessOne(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 0.4).Substitute("b", 1.1).Substitute("c", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -0.9, -0.7, -0.5, 0.05, 0.2 })
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
