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
    /// A sum of two squares below the bar beside a repeated factor, written over its two
    /// conjugates. Under <c>u = tan(x)</c> and <c>t = sqrt(c + d u)</c>, the <c>1 + u^2</c> of
    /// Rubi's 4.3.2.1 <c>(c + d tan(x))^(3/2)/(a + b tan(x))^2</c> is <c>(t^2 - c)^2 + d^2</c>,
    /// a quartic beside the square of <c>a d + b (t^2 - c)</c>; the Hermite reduction solved for
    /// its numerators with the symbols in every entry and ran past two minutes. Written as
    /// <c>(t^2 - c - i d)(t^2 - c + i d)</c>, every factor is one the split by residues reads.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Under a root of the quotient of two such linears the sum is <c>(b - d t^2)^2 + (c t^2 - a)^2</c>,
    /// both parts in <c>t</c>, and its conjugates are quadratics as well. The integrands are real
    /// where <c>a + b tan(x)</c> and <c>c + d tan(x)</c> are positive, which the points are.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class SumOfTwoSquaresBelowTheBarIntegralTest
    {
        [Theory]
        [InlineData("(c + d*tan(x))^(3/2)/(a + b*tan(x))^2")]
        [InlineData("(c + d*tan(x))^(5/2)/(a + b*tan(x))^2")]
        [InlineData("(c + d*tan(x))^(3/2)/(a + b*tan(x))^3")]
        [InlineData("x^4/((a*d + (x^2 - c)*b)^2*((x^2 - c)^2 + d^2))")]
        [InlineData("1/((a + b*tan(x))^(3/2)*(c + d*tan(x))^(3/2))")]
        [InlineData("1/((a + b*tan(x))^(5/2)*(c + d*tan(x))^(3/2))")]
        public void OverItsConjugates(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 1.1).Substitute("d", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.2, 0.5, 0.9, 1.2 })
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
