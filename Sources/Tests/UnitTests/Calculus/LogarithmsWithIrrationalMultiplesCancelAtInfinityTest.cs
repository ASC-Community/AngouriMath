//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The antiderivative of <c>1/(x^5 + 1)</c> has three logarithms whose multiples, <c>-(sqrt(5) + 1)/20</c>,
    /// <c>(sqrt(5) - 1)/20</c> and <c>1/5</c>, are not rational multiples of each other. Each diverges at <c>+oo</c> and
    /// their sum does not, but only logarithms with rational ratios were gathered, so the integral over
    /// <c>[0, +oo)</c> was left unevaluated after eight seconds.
    /// https://github.com/asc-community/AngouriMath/issues/2623
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LogarithmsWithIrrationalMultiplesCancelAtInfinityTest
    {
        /// <summary>The values, checked numerically against closed forms and against mpmath's quadrature.</summary>
        [Theory]
        [InlineData("1/(x^5 + 1)", "pi / (5 sin(pi/5))")]
        [InlineData("x^3/(x^5 + 1)", "pi / (5 sin(4 pi/5))")]
        [InlineData("x/(x^5 + 1)", "pi / (5 sin(2 pi/5))")]
        [InlineData("1/(x^3 + 1)", "2 pi / (3 sqrt(3))")]
        [InlineData("1/(x^6 + 1)", "pi / 3")]
        public void TheIntegralUpToInfinityIsAnswered(string integrand, string expected)
        {
            var integral = integrand.ToEntity().Integrate("x", 0, "+oo".ToEntity()).Simplify();
            Assert.DoesNotContain(integral.Nodes, node => node is Integralf);
            var difference = (integral - expected.ToEntity()).EvalNumerical();
            Assert.True(difference.Abs() < 1e-12, $"{integrand} over [0, +oo) was {integral}, not {expected}");
        }

        /// <summary>Logarithms whose divergent parts do not cancel still diverge.</summary>
        [Fact]
        public void ADivergentIntegralStaysDivergent()
            => Assert.Equal(Number.Real.PositiveInfinity,
                "x^4/(x^5 + 1)".ToEntity().Integrate("x", 0, "+oo".ToEntity()).Simplify().Evaled);
    }
}
