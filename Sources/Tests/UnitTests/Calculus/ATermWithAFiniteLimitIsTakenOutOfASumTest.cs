//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The limit of a sum depended on its bracketing. A sum whose halves tend to opposite infinities was left
    /// unevaluated, so <c>ln(x + 1) + (-1/2 ln(x^2 + 1) + arctan(x))</c> at <c>+oo</c> had no answer, while the same
    /// sum bracketed as <c>(ln(x + 1) - 1/2 ln(x^2 + 1)) + arctan(x)</c> was <c>pi/2</c>. That is the antiderivative of
    /// <c>1/((x + 1)(x^2 + 1))</c>, so the integral over <c>[0, +oo)</c> was left unevaluated where it is <c>pi/4</c>,
    /// and so was every rational function with a linear and an irreducible quadratic factor below the bar.
    /// https://github.com/asc-community/AngouriMath/issues/2599
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ATermWithAFiniteLimitIsTakenOutOfASumTest
    {
        [Theory]
        [InlineData("ln(x + 1) + (-1/2 * ln(x^2 + 1) + arctan(x))", "+oo", "pi / 2")]
        [InlineData("(ln(x + 1) - 1/2 * ln(x^2 + 1)) + arctan(x)", "+oo", "pi / 2")]
        [InlineData("arctan(x) + ln(x + 1) - 1/2 * ln(x^2 + 1)", "+oo", "pi / 2")]
        [InlineData("ln(x + 1) + (arctan(x) - 1/2 * ln(x^2 + 1))", "+oo", "pi / 2")]
        [InlineData("sqrt(x^2 + x) + (arctan(x) - x)", "+oo", "1/2 + pi / 2")]
        // A term that has no limit is not taken out: x + (sin(x) - x) is sin(x), which has none.
        [InlineData("x + (sin(x) - x)", "+oo", "NaN")]
        [InlineData("x + (arctan(x) + sin(x))", "+oo", "+oo")]
        public void TheLimitIsTheSameHoweverTheSumIsBracketed(string input, string to, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", to).Evaled);

        [Fact]
        public void AtAFinitePointToo()
            => Assert.Equal(Number.Integer.One,
                "ln(x) + (exp(x) - ln(sin(x)))".ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);

        /// <summary>The values, checked numerically against closed forms computed by hand and with mpmath.</summary>
        [Theory]
        [InlineData("1/((x + 1)(x^2 + 1))", "0", "pi / 4")]
        [InlineData("x/((x + 1)(x^2 + 1))", "0", "pi / 4")]
        [InlineData("1/((x + 2)(x^2 + 1))", "0", "(pi - ln(2)) / 5")]
        [InlineData("1/((x + 1)(x^2 + x + 1))", "0", "pi / (3 sqrt(3))")]
        [InlineData("1/(x^3 + 1)", "0", "2 pi / (3 sqrt(3))")]
        [InlineData("1/(x^3 + 1)", "1", "pi / (3 sqrt(3)) - ln(2) / 3")]
        [InlineData("1/(x^3 + 8)", "0", "pi / (6 sqrt(3))")]
        [InlineData("1/(x^4 + 1)", "0", "pi / (2 sqrt(2))")]
        public void TheIntegralUpToInfinityIsAnswered(string integrand, string from, string expected)
        {
            var integral = integrand.ToEntity().Integrate("x", from.ToEntity(), "+oo".ToEntity()).Simplify();
            Assert.DoesNotContain(integral.Nodes, node => node is Integralf);
            var difference = (integral - expected.ToEntity()).EvalNumerical();
            Assert.True(difference.Abs() < 1e-12, $"{integrand} from {from} to +oo was {integral}, not {expected}");
        }
    }
}
