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
    /// A whole power below <c>-1</c> of a linear beside a power that is not whole of another, times
    /// a polynomial, by the recurrence that raises the whole power to <c>-1</c>. Rubi's
    /// <c>P(x) (a + b x)^m (c + d x)^n</c> files.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LinearPowerRecurrenceIntegralTest
    {
        [Theory]
        [InlineData("1/((a + b*x)^4*sqrt(c + d*x))")]
        [InlineData("(p + q*x + r*x^2 + s*x^3)/((a + b*x)^5*sqrt(c + d*x))")]
        [InlineData("(c + d*x)^(3/2)/(a + b*x)^3")]
        [InlineData("1/(x^3*(c + d*x)^(5/2))")]
        [InlineData("x/((a + b*x)^4*sqrt(c + d*x))")]
        public void ByTheRecurrence(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.9).Substitute("c", 0.7)
                .Substitute("d", 1.1).Substitute("p", 0.4).Substitute("q", 1.3).Substitute("r", 0.8)
                .Substitute("s", 0.6);
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
