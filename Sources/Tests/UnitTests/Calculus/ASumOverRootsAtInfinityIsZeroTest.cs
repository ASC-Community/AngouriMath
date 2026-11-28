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
    /// The antiderivative of <c>1/(x^3 + x + 1)</c> is a sum over the roots of <c>r^3 + r + 1</c>,
    /// <c>sum(ln(x - r) / (3 r^2 + 1), r)</c>. At <c>+oo</c> it was the same sum with <c>+oo</c> in it, which
    /// evaluates to <c>NaN</c>, so the integral over <c>[0, +oo)</c> simplified to <c>NaN</c>, a claim that the
    /// integral does not exist, where it is <c>0.92176...</c>. Across a real root the root sum was taken as if it were
    /// continuous, and the integral of the same function over <c>[-1, 0]</c>, which diverges at the root
    /// <c>-0.6823...</c>, was a complex number.
    /// https://github.com/asc-community/AngouriMath/issues/2624
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASumOverRootsAtInfinityIsZeroTest
    {
        /// <summary>Values from mpmath's quadrature.</summary>
        [Theory]
        [InlineData("1/(x^3 + x + 1)", "0", "0.921763372185058")]
        [InlineData("1/(x^3 - x^2 + 1)", "0", "1.79044536812823")]
        [InlineData("1/(x^5 + x + 1)", "0", "0.795904895024779")]
        [InlineData("1/(x^5 + x + 1)", "1", "0.142012887209207")]
        [InlineData("x/(x^3 + x + 1)", "0", "0.96934396330681")]
        [InlineData("(x + 2)/(x^3 + x + 1)", "0", "2.81287070767692")]
        [InlineData("1/(x^4 + x + 1)", "0", "0.835877591843161")]
        [InlineData("1/(x^5 + 3x + 5)", "0", "0.229150744145838")]
        public void TheIntegralUpToInfinityIsItsValue(string integrand, string from, string expected)
        {
            var integral = integrand.ToEntity().Integrate("x", from.ToEntity(), "+oo".ToEntity()).Simplify();
            Assert.DoesNotContain(integral.Nodes, node => node is Integralf);
            var value = integral.EvalNumerical();
            Assert.True((value - expected.ToEntity().EvalNumerical()).Abs() < 1e-12,
                $"{integrand} over [{from}, +oo) was {integral}, {value}, not {expected}");
        }

        /// <summary>Never NaN, where the integral diverges or is not settled.</summary>
        [Theory]
        [InlineData("x^2/(x^3 + x + 1)", "0", "+oo")]
        [InlineData("1/(x^3 + x + 1)", "-1", "0")]
        [InlineData("1/(x^3 + x + 1)", "-oo", "-1")]
        [InlineData("1/(x^3 + x + 1)", "-oo", "+oo")]
        public void NoValueIsNotNaN(string integrand, string from, string to)
        {
            var integral = integrand.ToEntity().Integrate("x", from.ToEntity(), to.ToEntity()).Simplify();
            Assert.False(integral.Evaled.IsNaN, $"{integrand} over [{from}, {to}] was NaN");
            // Across the real root -0.6823... the integral diverges, so it has no finite value.
            if (from == "-1")
                Assert.Contains(integral.Nodes, node => node is Integralf);
        }
    }
}
