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
    /// A sine, cosine or exponential of a quotient of two linears, <c>(a + b x)/(c + d x)</c>,
    /// written as the constant <c>b/d</c> plus <c>(a d - b c)/d</c> over <c>c + d x</c>, and
    /// answered in the sine, cosine and exponential integrals. Rubi's 4.7.7, 6.1.5 and 6.2.5.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 0.3</c>, <c>b = 0.9</c>, <c>c = 1.1</c>,
    /// <c>d = 0.7</c>, on both sides of the pole at <c>c + d x = 0</c>.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class QuotientOfLinearsAsAnArgumentIntegralTest
    {
        [Theory]
        [InlineData("sin((a + b*x)/(c + d*x))")]
        [InlineData("sin((a + b*x)/(c + d*x))^2")]
        [InlineData("sin((a + b*x)/(c + d*x))^3")]
        [InlineData("cos((a + b*x)/(c + d*x))")]
        [InlineData("cos((a + b*x)/(c + d*x))^2")]
        [InlineData("sinh((a + b*x)/(c + d*x))")]
        [InlineData("sinh((a + b*x)/(c + d*x))^3")]
        [InlineData("cosh((a + b*x)/(c + d*x))^2")]
        [InlineData("e^((a + b*x)/(c + d*x))")]
        [InlineData("sin(2*(a + b*x)/(c + d*x))")]
        public void IsWrittenOverTheDenominator(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 0.3).Substitute("b", 0.9).Substitute("c", 1.1).Substitute("d", 0.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.5, -2.0, 0.3, 0.8, 1.4 })
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
