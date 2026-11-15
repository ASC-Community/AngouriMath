//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A definite integral whose antiderivative breaks between the bounds is taken piece by
    /// piece, each piece with its limit at the break from its own side. The antiderivative
    /// <c>-1/x</c> taken at the bounds alone made the integral of the positive <c>1/x^2</c> over
    /// <c>[-1, 1]</c> equal to <c>-2</c>.
    /// https://github.com/asc-community/AngouriMath/issues/1508
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class IntegralAcrossABreakTest
    {
        private static Entity Definite(string integrand, string from, string to)
            => MathS.Integral(integrand.ToEntity(), "x", from.ToEntity(), to.ToEntity()).Simplify();

        /// <summary>A pole the integral diverges across is <c>+oo</c> or <c>-oo</c>, with the range's direction.</summary>
        [Theory]
        [InlineData("1/x^2", "-1", "1", "+oo")]
        [InlineData("1/(x - 2)^2", "0", "3", "+oo")]
        [InlineData("tan(x)^2", "0", "3", "+oo")]
        [InlineData("sec(x)^2", "0", "pi", "+oo")]
        [InlineData("1/x^2", "1", "-1", "-oo")]
        public void APoleInsideTheRangeDiverges(string integrand, string from, string to, string value)
            => Assert.Equal(value.ToEntity(), Definite(integrand, from, to));

        /// <summary>
        /// Where the pieces diverge in opposite directions the integral has no value, and not
        /// the finite one the antiderivative at the bounds gave.
        /// </summary>
        [Theory]
        [InlineData("1/x", "-1", "1")]
        [InlineData("1/(x - 2)", "0", "3")]
        [InlineData("tan(x)", "0", "3")]
        [InlineData("x/(x^2 - 1)", "0", "2")]
        [InlineData("1/(x^2 - 4)", "-3", "3")]
        public void OppositeDivergencesHaveNoValue(string integrand, string from, string to)
            => Assert.Equal(MathS.NaN, Definite(integrand, from, to));

        /// <summary>
        /// A singularity the integral converges across is joined from both sides: <c>x ln|x| - x</c>
        /// tends to 0 at 0 from either one.
        /// </summary>
        [Fact]
        public void AnIntegrableSingularityKeepsItsValue() => Assert.Equal(-2, Definite("ln(abs(x))", "-1", "1"));

        /// <summary>
        /// A jump in the antiderivative where the integrand is continuous is not counted.
        /// <c>1/(2 + cos(x))</c> over <c>[0, 2 pi]</c>, through the half-angle antiderivative's
        /// <c>tan(x/2)</c>, which jumps from <c>+oo</c> to <c>-oo</c> at <c>pi</c>, was <c>0</c>.
        /// </summary>
        [Fact]
        public void AJumpInTheAntiderivativeIsNotCounted()
            => Assert.Equal("2 * pi / sqrt(3)".ToEntity().Simplify(), Definite("1/(2 + cos(x))", "0", "2 * pi"));

        /// <summary>A break outside the range, or at a bound, changes nothing.</summary>
        [Theory]
        [InlineData("1/x^2", "1", "2", "1/2")]
        [InlineData("1/(x^2 + 1)", "-1", "1", "pi / 2")]
        [InlineData("tan(x)", "0", "1", "-ln(cos(1))")]
        public void ABreakOutsideTheRangeChangesNothing(string integrand, string from, string to, string value)
            => Assert.Equal(value.ToEntity().Simplify(), Definite(integrand, from, to));

        private static void AgreesWith(Entity got, double real, double imaginary, string what)
        {
            Assert.DoesNotContain(got.Nodes, node => node is Entity.Integralf);
            var value = got.EvalNumerical();
            var error = System.Math.Abs((double)value.RealPart - real) + System.Math.Abs((double)value.ImaginaryPart - imaginary);
            Assert.True(error < 1e-12 * System.Math.Max(1, System.Math.Abs(real) + System.Math.Abs(imaginary)),
                $"{what} is {got} = {value}, where it is {real} + {imaginary}i");
        }

        /// <summary>
        /// A complex constant takes the antiderivative's argument off the real line, and there it can cross a branch
        /// cut where the integrand is continuous: <c>arctan(x - 3i)</c> meets <c>arctan</c>'s cut at <c>-3i</c> and
        /// jumps by <c>pi</c>, which <c>F(b) - F(a)</c> counted. The values are mpmath's quadrature to thirty digits.
        /// https://github.com/asc-community/AngouriMath/issues/2460
        /// </summary>
        [Theory]
        [InlineData("1/(1 + (x - 3i)^2)", "-2", "2", -0.321750554396642193, 0)]
        [InlineData("1/(1 + (x + 3i)^2)", "-2", "2", -0.321750554396642193, 0)]
        [InlineData("1/(1 + (x - 3i)^2)", "2", "-2", 0.321750554396642193, 0)]
        [InlineData("1/(1 + (x - 3i)^2)", "-oo", "+oo", 0, 0)]
        [InlineData("1/(1 + (x - 2i)^2)", "-oo", "+oo", 0, 0)]
        // Within the strip |Im| < 1 the path does not meet the cut.
        [InlineData("1/(1 + (x - i/2)^2)", "-oo", "+oo", System.Math.PI, 0)]
        [InlineData("1/(1 + (x - i/2)^2)", "-2", "2", 2.25311288166964470, 0)]
        // A bound on the cut takes the value from inside the range.
        [InlineData("1/(1 + (x - 3i)^2)", "0", "2", -0.160875277198321097, 0.117500907311433888)]
        [InlineData("1/(1 + (x - 3i)^2)", "-2", "0", -0.160875277198321097, -0.117500907311433888)]
        [InlineData("1/(1 + (x - 3i)^2)", "0", "+oo", 0, 0.346573590279972655)]
        [InlineData("1/(1 + ((2 - i) x - 3i)^2)", "-2", "2", -0.119090420546762822, -0.0923407433318177927)]
        // The logarithm's cut, the negative real axis, by a linear argument, by a quadratic one, and under a cosine
        // and a sine.
        [InlineData("1/((1 + i) x - 5 - 3i)", "0", "5", -1.59062445440680808, -0.520591372658672695)]
        [InlineData("x/(1 + (x - 3i)^2)", "-2", "2", 0, 0.283794109208327846)]
        // A power whose base crosses the negative real axis only outside the range.
        [InlineData("sqrt((1 + i) x - 5 - 3i)", "0", "2", 0.957866108597855882, -4.10504061682829902)]
        [InlineData("tan(x + i)", "0", "1", 0.176464319095173717, 0.870327424991119277)]
        [InlineData("tan(x + i)", "0", "4", 0.137574017201678, 3.86425535734661)]
        [InlineData("tan(x - i/2)", "-4", "4", 0, -7.2657696334242)]
        [InlineData("cot(x + i)", "-2", "1", -0.0276416386049222, -3.02215428925017)]
        // A complex slope: the imaginary part of the cosine's argument is zero at one point, and its real part at
        // multiples of pi.
        [InlineData("tan((1 + i) x)", "0", "1", 0.306505456801632903, 0.563821968189486374)]
        [InlineData("tan((1 + i) x)", "0", "4", 0.346432013793773035, 3.65323607730064999)]
        [InlineData("tan((2 - i) x + 1/2)", "0", "2", 0.330209656421608542, -2.0810570895816348)]
        [InlineData("cot((1 + i) x + 1)", "0", "3", 0.0240825030271072548, -2.45573766107787857)]
        [InlineData("tan(i x + 1)", "-2", "2", 1.96643859505754655, 0)]
        public void Issue2460_ACrossingOfABranchCutIsCounted(string integrand, string from, string to, double real, double imaginary)
            => AgreesWith(Definite(integrand, from, to), real, imaginary, $"the integral of {integrand} from {from} to {to}");

        /// <summary>
        /// Where a crossing cannot be placed or its jump settled, the integral is left as written: a power's jump is
        /// not a constant, a bound that is not a number cannot say whether the crossing is in the range, and a symbol
        /// beside the complex constant cannot say where it is. Each of these answered <c>F(b) - F(a)</c>, and
        /// <c>1/(1 + (x - i)^2)</c>, which has a pole at 0, answered a finite value for the range across it.
        /// https://github.com/asc-community/AngouriMath/issues/2460
        /// </summary>
        [Theory]
        [InlineData("sqrt((1 + i) x - 5 - 3i)", "0", "5")]
        [InlineData("1/(1 + (x - 3i)^2)", "0", "s")]
        [InlineData("1/(1 + (x - y - 3i)^2)", "-2", "2")]
        [InlineData("1/(1 + (x - i)^2)", "-1", "1")]
        public void Issue2460_ACrossingThatCannotBeSettledIsNotAnswered(string integrand, string from, string to)
            => Assert.Contains(Definite(integrand, from, to).Nodes, node => node is Entity.Integralf);
    }
}
