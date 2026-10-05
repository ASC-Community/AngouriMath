//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A symbolic quadratic below the bar is answered by arms on its leading coefficient and the
    /// sign of its discriminant, and an arm no real coefficients reach is left out: with no
    /// linear term and a number for the constant one the discriminant is zero only where the
    /// leading coefficient is, and with no constant term it is never positive.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class QuadraticDenominatorArmsTest
    {
        [Theory]
        [InlineData("1/(1 + q*x^2)")]
        [InlineData("1/(3 + q*x^2)^2")]
        [InlineData("1/(x*(a + q*x))")]
        [InlineData("1/(x*(a + q*x))^2")]
        public void HasThreeArms(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.Equal(3, integral.Nodes.OfType<Entity.Piecewise>().Sum(piecewise => piecewise.Cases.Count()));
            foreach (var q in new[] { 0.7, -0.7 })
            {
                Entity Pinned(Entity e) => e.Substitute("q", q).Substitute("a", 1.3);
                var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
                var original = Pinned(integrand.ToEntity());
                foreach (var at in new[] { 0.2, 0.5 })
                {
                    var want = original.Substitute("x", at).EvalNumerical();
                    var got = derivative.Substitute("x", at).EvalNumerical();
                    Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                                < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                        $"d/dx of the antiderivative of {integrand} at q = {q} is {got} at x = {at}, where the integrand is {want}");
                }
            }
        }
    }
}
