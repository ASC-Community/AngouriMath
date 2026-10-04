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
    /// Powers of <c>a ± a sin(y)</c> with a symbolic exponent, beside a power of <c>g cos(y)</c> and
    /// a function of the sine, under <c>u = sin(y)</c>, where <c>(1 + u)(1 - u)</c> is the cosine's
    /// square; and the cosine's the same way. Rubi's <c>(a + b sin)^m (c + d sin)^n</c> files.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class PowersOfOnePlusAndMinusASineIntegralTest
    {
        [Theory]
        [InlineData("(a + a*sin(e + f*x))^m*sqrt(c - c*sin(e + f*x))")]
        [InlineData("(a + a*sin(e + f*x))^m*(c - c*sin(e + f*x))^(5/2)*(p + q*sin(e + f*x)^2)")]
        [InlineData("cos(e + f*x)^2*(a + a*sin(e + f*x))^m/sqrt(c - c*sin(e + f*x))")]
        [InlineData("(g*cos(e + f*x))^(1 - 2*m)*(a + a*sin(e + f*x))^m*(c - c*sin(e + f*x))^(m - 1)")]
        [InlineData("(a + a*sin(e + f*x))^m*(c - c*sin(e + f*x))^(-1 - m)")]
        [InlineData("(a - a*sin(e + f*x))^m*(c + c*sin(e + f*x))^n*(b*(m - n) + b*(1 + m + n)*sin(e + f*x))")]
        [InlineData("(a + a*cos(e + f*x))^m*sqrt(c - c*cos(e + f*x))")]
        public void ThroughTheSine(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.9).Substitute("c", 0.7)
                .Substitute("e", 0.4).Substitute("f", 1.3).Substitute("g", 0.8).Substitute("m", 0.37)
                .Substitute("n", 1.21).Substitute("p", 1.1).Substitute("q", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.1, -0.9, 0.3, 0.7, 1.6, 2.2 })
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
