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
    /// A quotient by the square of a base with a function in it, by parts against the base's
    /// reciprocal: <c>N/(M v^2)</c> is <c>g v'/v^2</c> with <c>g = N/(M v')</c>, whose integral is
    /// <c>-g/v</c> plus that of <c>g'/v</c>, which the base cancels from where its derivative is
    /// one term. <c>x^2/(a x cos(a x) - sin(a x))^2</c> was declined; Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 1.3</c> on both sides of zero.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class SquaredBaseByPartsIntegralTest
    {
        [Theory]
        [InlineData("x^2/(a*x*cos(a*x) - sin(a*x))^2")]
        [InlineData("sin(a*x)^2/(a*x*cos(a*x) - sin(a*x))^2")]
        [InlineData("sin(a*x)^3/(x*(a*x*cos(a*x) - sin(a*x))^2)")]
        [InlineData("sin(a*x)^4/(x^2*(a*x*cos(a*x) - sin(a*x))^2)")]
        [InlineData("x^2/(cos(a*x) + a*x*sin(a*x))^2")]
        [InlineData("cos(a*x)^2/(cos(a*x) + a*x*sin(a*x))^2")]
        [InlineData("cos(a*x)^3/(x*(cos(a*x) + a*x*sin(a*x))^2)")]
        public void IsIntegratedAgainstTheReciprocalOfTheBase(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.3, -1.1, -0.4, 0.4, 1.1, 2.3 })
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
