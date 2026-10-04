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
    /// A whole power of a quotient with a symbol in it is the quotient of the powers:
    /// <c>(c/(a + c x^2))^2</c> is <c>c^2/(a + c x^2)^2</c>, which the table answers, and was
    /// declined. It is how the substitution <c>u = x^2</c> writes <c>x/(a + c x^4)^2</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 2.3</c>, <c>b = 0.7</c>, <c>c = 1.3</c>, on
    /// both sides of zero.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class WholePowerOfAQuotientIntegralTest
    {
        [Theory]
        [InlineData("(c/(a + c*x^2))^2")]
        [InlineData("(1/(a + c*x^2))^2")]
        [InlineData("(x/(a + c*x^2))^2")]
        [InlineData("(c/(a + c*x^2))^3")]
        [InlineData("((a + c*x^2)/x)^(-2)")]
        [InlineData("x/(a + c*x^4)^2")]
        [InlineData("x^2/(a + b*x^6)^2")]
        public void IsTheQuotientOfThePowers(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.7).Substitute("c", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.4, -0.6, 0.5, 1.2 })
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
