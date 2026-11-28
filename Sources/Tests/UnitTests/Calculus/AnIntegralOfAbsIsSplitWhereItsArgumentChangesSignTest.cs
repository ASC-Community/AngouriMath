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
    /// A definite integral of a function of <c>abs</c> was left unevaluated unless an antiderivative of the whole
    /// integrand was found, and for <c>sqrt(abs(x))</c>, <c>exp(-abs(x))</c> or <c>abs(x^2 - 1)</c> none was. Split
    /// where the argument changes sign, each piece has <c>abs(u)</c> as <c>u</c> or <c>-u</c> and integrates.
    /// https://github.com/asc-community/AngouriMath/issues/2605
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnIntegralOfAbsIsSplitWhereItsArgumentChangesSignTest
    {
        /// <summary>The values, checked numerically against closed forms computed by hand and with mpmath.</summary>
        [Theory]
        [InlineData("1/sqrt(abs(x))", "-1", "1", "4")]
        [InlineData("sqrt(abs(x))", "-1", "1", "4/3")]
        [InlineData("abs(x)^(-1/3)", "-1", "8", "15/2")]
        [InlineData("1/sqrt(abs(x - 1))", "0", "2", "4")]
        [InlineData("1/sqrt(abs(x^2 - 1))", "0", "2", "pi/2 + ln(2 + sqrt(3))")]
        [InlineData("abs(x^2 - 1)", "-2", "2", "4")]
        [InlineData("abs(sin(x))", "0", "2 pi", "4")]
        [InlineData("exp(-abs(x))", "-oo", "+oo", "2")]
        [InlineData("x * exp(-abs(x))", "-oo", "+oo", "0")]
        [InlineData("1/(1 + abs(x))", "-1", "1", "2 ln(2)")]
        [InlineData("abs(x)/(1 + x^2)", "-1", "1", "ln(2)")]
        [InlineData("1/sqrt(abs(x))", "1", "-1", "-4")]
        // Split at the pole of the argument as well as at its zero.
        [InlineData("abs((x - 1)/(x + 2))", "0", "3", "1 + 3 ln(9/10)")]
        public void TheIntegralIsAnswered(string integrand, string from, string to, string expected)
        {
            var integral = integrand.ToEntity().Integrate("x", from.ToEntity(), to.ToEntity()).Simplify();
            Assert.DoesNotContain(integral.Nodes, node => node is Integralf);
            var difference = (integral - expected.ToEntity()).EvalNumerical();
            Assert.True(difference.Abs() < 1e-12, $"{integrand} from {from} to {to} was {integral}, not {expected}");
        }

        /// <summary>A divergent integral is given no finite value.</summary>
        [Theory]
        [InlineData("1/abs(x)")]
        [InlineData("1/abs(x)^2")]
        // Not +oo + i pi, which the antiderivative -ln(x) of the piece on [-1, 0] gave.
        [InlineData("abs(1/x)")]
        [InlineData("1/x^2")]
        public void ADivergentIntegralIsPlusInfinity(string integrand)
            => Assert.Equal(Number.Real.PositiveInfinity,
                integrand.ToEntity().Integrate("x", -1, 1).Simplify().Evaled);

        [Theory]
        [InlineData("1/x")]
        [InlineData("1/(x * abs(x))")]
        public void APrincipalValueIsNotAnIntegral(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x", -1, 1).Simplify();
            Assert.True(integral.Evaled.IsNaN || integral.Nodes.Any(node => node is Integralf), $"{integrand} over [-1, 1] was {integral}");
        }
    }
}
