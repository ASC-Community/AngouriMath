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
    /// A whole power of a sum of two square roots below the bar, multiplied above and below by the
    /// same power of the conjugate: <c>x/(sqrt(a + b x) + sqrt(c + b x))^3</c> is
    /// <c>x (sqrt(a + b x) - sqrt(c + b x))^3/(a - c)^3</c>. The first power was rationalised and
    /// the cubes were declined. Rubi's 1.3.2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerOfASumOfRootsIntegralTest
    {
        [Theory]
        [InlineData("x/(sqrt(a + b*x) + sqrt(c + b*x))^3")]
        [InlineData("1/(sqrt(a + b*x) + sqrt(a + c*x))^3")]
        [InlineData("x^2/(sqrt(a + b*x) + sqrt(a + c*x))^3")]
        [InlineData("x^3/(sqrt(a + b*x) + sqrt(a + c*x))^2")]
        public void ThroughTheConjugate(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 0.4);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.8, 1.5, 2.4 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
