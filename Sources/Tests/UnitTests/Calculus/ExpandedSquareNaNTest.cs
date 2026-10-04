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
    /// The power rule reads its exponent normalised, so a monomial does not depend on how its
    /// exponent happens to be written.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1258">#1258</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>(a^2 + 2abx^2 + b^2x^4)^3/x^7</c> came back <c>NaN + C</c> while
    /// <c>(a + bx^2)^6/x^7</c> — the same integrand with its base not written out — was answered.
    /// <c>NaN</c> is <b>this does not exist</b>, not <b>I could not settle it</b>, so that was a
    /// wrong answer rather than a gap.
    /// </para>
    /// <para>
    /// The exponent <c>-1</c> was recognised only when it was spelled as the integer. Repeated
    /// integration by parts feeds an antiderivative back in as an integrand, and the power rule
    /// returned <c>x^(p + 1)/(p + 1)</c> with the sum left standing — so a round accumulated
    /// <c>x^(-3 + 1 + 1)</c>, which is <c>x^(-1)</c> and wanted a logarithm, was not recognised
    /// as one, and became <c>x^(-3 + 1 + 1 + 1)/(-3 + 1 + 1 + 1)</c>: <c>x^0/0</c>.
    /// </para>
    /// <para>
    /// The trigger is internal, so these are pinned through the integrals that expose it rather
    /// than by handing the rule an exponent no parser produces — <c>x^(-3 + 1 + 1)</c> is folded
    /// to <c>x^(-1)</c> before it ever reaches the integrator, and asserting on it would pin
    /// nothing.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class ExpandedSquareNaNTest
    {
        private static readonly double[] Points = { 0.41, 0.77, 1.3, 2.2 };

        /// <summary>
        /// Every parameter is pinned before the comparison, since these integrands carry two or
        /// three of them and an antiderivative holding a parameter cannot be evaluated.
        /// </summary>
        private static Entity Pin(Entity expr) => expr
            .Substitute("a", 2).Substitute("b", 3)
            .Substitute("c", 2).Substitute("d", 3).Substitute("e", 5)
            .Substitute("m", 1.7).Substitute("n", 2.9);

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            Assert.DoesNotContain("integral(", integral.Stringize());

            var derivative = Pin(integral.Substitute("C", 0).Differentiate("x"));
            var original = Pin(integrand.ToEntity());
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 3,
                $"only {compared} of {Points.Length} points were comparable for {integrand}, "
                + "so this asserts almost nothing");
        }

        /// <summary>
        /// The reported pair, and the two neighbours that decide it is the perfect square rather
        /// than the shape: a quartic with independent coefficients was always answered, and so
        /// was the unexpanded base.
        /// </summary>
        [Theory]
        [InlineData("(a^2 + 2*a*b*x^2 + b^2*x^4)^3/x^7")]
        [InlineData("(a + b*x^2)^6/x^7")]
        [InlineData("(c + d*x^2 + e*x^4)^3/x^7")]
        [InlineData("(1 + 2*x^2 + x^4)^3/x^7")]
        public void TheReportedPair(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The denominator decides how many rounds the accumulation runs for, so the boundary is
        /// pinned either side: <c>/x^5</c> was answered and <c>/x^7</c> was not.
        /// </summary>
        [Theory]
        [InlineData("(a^2 + 2*a*b*x^2 + b^2*x^4)^3/x^3")]
        [InlineData("(a^2 + 2*a*b*x^2 + b^2*x^4)^3/x^5")]
        [InlineData("(a^2 + 2*a*b*x^2 + b^2*x^4)^3/x^7")]
        [InlineData("(a^2 + 2*a*b*x^2 + b^2*x^4)^3/x^9")]
        [InlineData("(a^2 + 2*a*b*x^2 + b^2*x^4)^4/x^7")]
        public void EveryDepthOfTheAccumulation(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The same shape in one variable, which is the linear rather than the biquadratic
        /// perfect square and reaches the power rule by the same route.
        /// </summary>
        [Theory]
        [InlineData("(1 + 2*x + x^2)^3/x^7")]
        [InlineData("(1 + x)^6/x^7")]
        [InlineData("(a^2 + 2*a*b*x + b^2*x^2)^3/x^7")]
        public void TheSameShapeInOneVariable(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The power rule itself, which the fix changes and must still answer: the logarithmic
        /// case, the ordinary one, and a negative exponent.
        /// </summary>
        [Theory]
        [InlineData("1/x")]
        [InlineData("x^(-1)")]
        [InlineData("x^2")]
        [InlineData("x^(-2)")]
        [InlineData("x^(-7)")]
        [InlineData("x^(1/2)")]
        [InlineData("x^n")]
        [InlineData("x^(n - 1)")]
        public void ThePowerRuleIsUnchanged(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A symbolic exponent that is <c>-1</c> as a value, which the power rule read as written:
        /// <c>x^(-1 - 3n + 3n)</c> is what expanding <c>x^(-1 - 3n) (a + b x^n)^3</c> leaves for its
        /// last term, and came out as <c>x^0/0</c>, so the whole answer had no value anywhere.
        /// Rubi's 1.1.3.2.
        /// </summary>
        [Theory]
        [InlineData("x^(-1 - 3*n)*(a + b*x^n)^3")]
        [InlineData("x^(-1 - 7*n)*(a + b*x^n)^8")]
        [InlineData("1/(a*(b*x^m)^n)^(1/(m*n))")]
        [InlineData("x^(-1 - 3*n + 3*n)")]
        [InlineData("x^(-n/n)")]
        public void AnExponentThatIsMinusOneAsAValue(string integrand) => DifferentiatesBack(integrand);
    }
}
