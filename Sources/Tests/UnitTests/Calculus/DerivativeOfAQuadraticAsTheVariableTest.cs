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
    /// A power of a multiple of a quadratic's derivative beside a power of the quadratic, Rubi's
    /// 1.2.1.2 <c>(b d + 2 c d x)^m (a + b x + c x^2)^p</c>: under <c>t = b d + 2 c d x</c> the
    /// quadratic is <c>(t^2/d^2 - (b^2 - 4 a c))/(4 c)</c>, and the integrand a binomial in <c>t</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Each was declined, or ran past the corpus's budget; the binomial with a compound constant
    /// term ran out of time too, and is asked with the constant named by a symbol of its own.
    /// Checked by differentiating back with the symbols pinned, at points on both sides of the
    /// derivative's root where the integrand is real.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class DerivativeOfAQuadraticAsTheVariableTest
    {
        [Theory]
        [InlineData("(a + b*x + c*x^2)^(3/2)/(b*d + 2*c*d*x)^3")]
        [InlineData("(a + b*x + c*x^2)^(1/2)/(b*d + 2*c*d*x)^7")]
        [InlineData("(a + b*x + c*x^2)^(5/2)/(b*d + 2*c*d*x)^5")]
        [InlineData("(b*d + 2*c*d*x)^(5/2)/(a + b*x + c*x^2)^3")]
        public void IsABinomialInTheDerivative(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 2.3).Substitute("b", 1.1).Substitute("c", 0.7).Substitute("d", 1.9);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -3.1, -1.4, 0.3, 0.9, 1.7 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || Math.Abs((double)want.ImaginaryPart) > 1e-12 * Math.Max(1, Math.Abs((double)want.RealPart)))
                    continue;
                var got = derivative.Substitute("x", at).EvalNumerical();
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                Assert.True(difference < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 3, $"only {compared} points where {integrand} is real");
        }
    }
}
