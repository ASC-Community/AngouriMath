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
    /// Powers of two linears whose exponents sum to a whole number below <c>-2</c>, beside a
    /// polynomial of degree at most two less than its negative: under <c>t = L1/L2</c> a power of
    /// <c>t</c> beside a polynomial, so the antiderivative is the two powers times a polynomial.
    /// Rubi's <c>(a + b x)^m (c + d x)^n</c> files.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class TwoLinearPowersIntegralTest
    {
        [Theory]
        [InlineData("(a + b*x)^m*(c + d*x)^(-3 - m)")]
        [InlineData("x^(n - 4)/(a + b*x)^n")]
        [InlineData("(a + b*x)^m*(c + d*x)^(-5 - m)*(p + q*x)^3")]
        [InlineData("(a + b*x)*(c + d*x)^(n - 4)/(p + q*x)^n")]
        [InlineData("(a + b*x)^m*(c + d*x)^(-4 - m)*(p + q*x)*(g + h*x)")]
        [InlineData("(a + b*x)^(5/2)/(c + d*x)^(11/2)")]
        public void UnderTheQuotientOfTheLinears(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.9).Substitute("c", 0.7)
                .Substitute("d", 1.1).Substitute("g", 0.8).Substitute("h", 0.6).Substitute("m", 0.37)
                .Substitute("n", 1.21).Substitute("p", 0.4).Substitute("q", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.4, -1.5, 0.3, 0.8, 1.5, 2.4 })
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
