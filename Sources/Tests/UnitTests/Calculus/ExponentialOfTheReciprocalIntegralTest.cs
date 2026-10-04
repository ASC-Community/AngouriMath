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
    /// An exponential of a power of the reciprocal of a linear, beside a power of the linear, that
    /// is not a Gaussian in <c>u = 1/L</c>: <c>F^(a + b/L) L^m</c> is
    /// <c>-(1/d) u^(-m - 2) F^(a + b u)</c>, an exponential beside a power. Rubi's 2.3.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialOfTheReciprocalIntegralTest
    {
        [Theory]
        [InlineData("F^(a + b/(c + d*x))*(c + d*x)^2")]
        [InlineData("F^(a + b/(c + d*x))")]
        [InlineData("F^(a + b/(c + d*x))/(c + d*x)")]
        [InlineData("F^(a + b/(c + d*x)^3)/(c + d*x)")]
        [InlineData("F^(a + b/(c + d*x)^3)*(c + d*x)^2")]
        public void UnderTheReciprocal(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("F", 2.3).Substitute("a", 0.4).Substitute("b", 1.1)
                .Substitute("c", 0.6).Substitute("d", 1.3);
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
