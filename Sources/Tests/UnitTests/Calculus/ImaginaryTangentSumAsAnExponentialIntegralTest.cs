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
    /// <c>a + i a tan(z)</c> below the bar, which is <c>a e^(i z)/cos(z)</c>: beside a root of the
    /// tangent the integrand is left to the substitution <c>t = tan(z)</c>, where the root is a root
    /// of <c>t</c>; and beside <c>c - i c tan(z)</c> above the bar, <c>c e^(-i z)/cos(z)</c>, the
    /// two are a polynomial in <c>e^(i z)</c> and its reciprocal, multiplied out. Rubi's 4.3.3.1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>Compared as complex numbers, the integrands being complex.</remarks>
    [Trait("Area", "Calculus")]
    public sealed class ImaginaryTangentSumAsAnExponentialIntegralTest
    {
        [Theory]
        [InlineData("sqrt(tan(c + d*x))/(a + i*a*tan(c + d*x))")]
        [InlineData("sqrt(tan(c + d*x))*(A + B*tan(c + d*x))/(a + i*a*tan(c + d*x))")]
        [InlineData("tan(c + d*x)^(3/2)*(A + B*tan(c + d*x))/(a + i*a*tan(c + d*x))^2")]
        [InlineData("(A + B*tan(c + d*x))/(sqrt(cot(c + d*x))*(a + i*a*tan(c + d*x)))")]
        [InlineData("(a + i*a*tan(c + d*x))^2*(A + B*tan(c + d*x))/(q - i*q*tan(c + d*x))^4")]
        [InlineData("(a + i*a*tan(c + d*x))^3*(A + B*tan(c + d*x))/(q - i*q*tan(c + d*x))^5")]
        public void IsIntegrated(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("A", 1.1).Substitute("B", 0.6)
                .Substitute("c", 0.4).Substitute("d", 1.1).Substitute("q", 0.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.1, -0.6, 0.3, 0.9 })
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
