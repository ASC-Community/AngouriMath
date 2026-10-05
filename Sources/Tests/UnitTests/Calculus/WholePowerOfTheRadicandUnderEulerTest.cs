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
    /// Euler's substitution writes a whole power of the radicand, <c>Q^n</c>, as the root to the
    /// <c>2n</c>th power. It wrote the <c>n</c>th, which is <c>Q^(n/2)</c>, and integrated a
    /// different integrand from the one asked.
    /// https://github.com/asc-community/AngouriMath/issues/1770
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class WholePowerOfTheRadicandUnderEulerTest
    {
        private static readonly double[] Points = { -1.7, -0.6, 0.4, 1.3, 2.2 };

        private static void DifferentiatesBack(Entity integrand, Entity integral)
        {
            var derivative = integral.Differentiate("x");
            foreach (var at in Points)
            {
                var got = (double)((Entity.Number.Complex)derivative.Substitute("x", at).EvalNumerical()).RealPart;
                var want = (double)((Entity.Number.Complex)integrand.Substitute("x", at).EvalNumerical()).RealPart;
                Assert.True(Math.Abs(got - want) < 1e-9 * Math.Max(1, Math.Abs(want)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        [Theory]
        [InlineData("1/((x^2 + 3)^2*(x + sqrt(x^2 + 3)))")]
        [InlineData("1/((x^2 + 2*x + 3)^2*(x + sqrt(x^2 + 2*x + 3)))")]
        public void ItIsAnswered(string written)
        {
            var integrand = written.ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain(integral.Nodes, node => node is Entity.Integralf);
            DifferentiatesBack(integrand, integral);
        }

        // Past the substitution's bound on the degree with the power read right: declined, as
        // it may be, or answered rightly.
        [Theory]
        [InlineData("(x^2 + 3)^2/(x + sqrt(x^2 + 3))")]
        [InlineData("(x^2 + 3)^3/(1 + sqrt(x^2 + 3))")]
        public void ItIsNotAnsweredWrongly(string written)
        {
            var integrand = written.ToEntity();
            var integral = integrand.Integrate("x");
            if (!integral.Nodes.Any(node => node is Entity.Integralf))
                DifferentiatesBack(integrand, integral);
        }
    }
}
