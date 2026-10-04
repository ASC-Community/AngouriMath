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
    /// Integrands built from <c>sin(x)</c> and <c>cos(x)</c> by the field operations, which the
    /// half-angle substitution <c>t = tan(x/2)</c> turns into a rational function —
    /// <c>sin(x) = 2t/(1 + t^2)</c>, <c>cos(x) = (1 - t^2)/(1 + t^2)</c>, <c>dx = 2/(1 + t^2) dt</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are first-year table entries and none of them had an antiderivative. Measured against
    /// Rubi's suite, the band it rates as a <em>single table lookup</em> was the worst-served
    /// difficulty we had — 58%, barely above the 52% for problems needing two or three rules — and
    /// this family is a large part of why.
    /// </para>
    /// <para>
    /// Checked by differentiating back and comparing at points, never against a printed form: the
    /// answers come out in terms of <c>tan(x/2)</c> and their shape says nothing about whether
    /// they differentiate back. <c>int 1/cos(x)</c> in particular comes out as a logarithm of a
    /// quotient of two linear expressions in <c>tan(x/2)</c>, which is correct and is not what a
    /// textbook prints.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class HalfAngleSubstitutionIntegralTest
    {
        /// <summary>
        /// Points inside one branch of <c>tan(x/2)</c>, which has poles at odd multiples of pi.
        /// They stay on <c>(0, pi)</c> and away from both ends, since the substitution produces an
        /// antiderivative per branch rather than one across a pole.
        /// </summary>
        private static readonly double[] Points = { 0.4, 0.9, 1.3, 1.9, 2.4 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 4,
                $"only {compared} of {Points.Length} points were comparable for {integrand}, "
                + "so this asserts almost nothing");
        }

        /// <summary>
        /// The cosecant and the secant. Both are table entries in every calculus text and neither
        /// had an antiderivative; <c>1/sin(x)^2</c> did, through the rule for the cotangent, which
        /// is what made the gap easy to miss.
        /// </summary>
        [Theory]
        [InlineData("1/sin(x)")]
        [InlineData("1/cos(x)")]
        public void TheCosecantAndTheSecant(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A quotient whose denominator is linear in one of the two, where the substitution
        /// produces something the rational integrator answers directly. <c>1/(1 + cos(x))</c>
        /// becomes <c>int 1 dt</c> exactly, so the answer is <c>tan(x/2)</c>.
        /// </summary>
        [Theory]
        [InlineData("1/(1 + cos(x))")]
        public void ALinearDenominatorInCosine(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The rest of the family: any quotient whose denominator is linear in the sine, the
        /// cosine or both.
        /// </summary>
        /// <remarks>
        /// Every one of these rewrote correctly and was then declined, because the rewrite leaves
        /// a sum with a fraction in it — <c>1/(1 - sin(x))</c> becomes
        /// <c>2/((t^2 + 1)(1 + (-2)t/(t^2 + 1)))</c> — and nothing put that over a common
        /// denominator. <c>Simplify</c> does not; it never combines a sum, by design. Writing the
        /// rewritten integrand as a single quotient first is what turns that into
        /// <c>2/(t^2 - 2t + 1)</c>, which the rational integrator answers at once.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1239">#1239</a>
        /// </remarks>
        [Theory]
        [InlineData("1/(1 - sin(x))")]
        [InlineData("1/(2 + cos(x))")]
        [InlineData("1/(1 + cos(x)/2)")]
        [InlineData("1/(3 + 5*sin(x))")]
        [InlineData("1/(2*cos(x) + 3*sin(x))")]
        [InlineData("1/(sin(x) + cos(x))")]
        public void ADenominatorLinearInEither(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The other four functions of x are quotients of the two, wherever either is defined,
        /// and are read as such: Timofeev's <c>1/(3 + 2 sec(x))</c> is <c>cos(x)/(3 cos(x) + 2)</c>,
        /// and with the secant left standing it had no sine or cosine for the substitution to
        /// read.
        /// </summary>
        [Theory]
        [InlineData("1/(3 + 2*sec(x))")]
        [InlineData("1/(sec(x) + tan(x))")]
        [InlineData("1/(2 + csc(x))")]
        [InlineData("1/(1 + cot(x))")]
        public void TheOtherFourAreReadAsQuotientsOfTheTwo(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// One argument linear in x for every function, <c>a x + b</c> as well as <c>x</c>:
        /// <c>t = tan((a x + b)/2)</c> and <c>dx = 2 dt/(a (1 + t^2))</c>. Hearn's
        /// <c>sin(a x)/(b + c sin(a x))^2</c> was declined for the <c>a</c>.
        /// </summary>
        [Theory]
        [InlineData("1/(2 + cos(3*x))")]
        [InlineData("1/(3 + 5*sin(2*x + 1))")]
        [InlineData("sin(2*x)/(2 + cos(2*x))^2")]
        public void ALinearArgument(string integrand) => DifferentiatesBack(integrand);

        [Fact]
        public void ALinearArgumentWithSymbols()
        {
            var integrand = "sin(a*x)/(b + c*sin(a*x))^2".ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity pinned = integral.Substitute("C", 0);
            Entity original = integrand;
            foreach (var (name, value) in new[] { ("a", 1.5), ("b", 3.0), ("c", 2.0) })
            {
                pinned = pinned.Substitute(name, value);
                original = original.Substitute(name, value);
            }
            var derivative = pinned.Simplify().Differentiate("x");
            foreach (var at in new[] { 0.4, 0.9, 1.3, 1.9 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) < 1e-7, $"{integrand} at {at}: {got} for {want}");
            }
        }

        /// <summary>
        /// Every sine and cosine brings its own <c>1 + t^2</c> below the bar, and clearing them
        /// leaves the same power of it above and below -- where the simplifier does not see
        /// it once the denominator is a sum. Bondarenko's <c>1/(cos(x) + sin(x) + sqrt(2))</c>
        /// came out as a quartic below the bar that nothing split, and is a quadratic once the
        /// common <c>1 + t^2</c> is divided out.
        /// </summary>
        [Theory]
        [InlineData("1/(cos(x) + sin(x) + sqrt(2))")]
        [InlineData("1/(cos(x) + sin(x) + 2)")]
        [InlineData("(1 + sin(x))/(cos(x) + sin(x) + 2)")]
        public void TheCommonFactorIsDividedOut(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A numerator that mentions the sine too, so the rewrite produces an <b>improper</b>
        /// rational function and the long division in front of partial fractions has to run
        /// before anything else can. The answer carries an <c>x</c> term from the whole part.
        /// </summary>
        [Theory]
        [InlineData("sin(x)/(1 + sin(x))")]
        public void AnImproperQuotientAfterTheRewrite(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// Declined, and each for its own reason, so that the boundary is recorded rather than
        /// assumed. <c>sin(x) + x</c> is not a function of the sine alone. <c>sin(x)*sin(2*x)</c>
        /// leaves an <c>x</c> behind because <c>sin(2x)</c> is not <c>sin(x)</c> — the right
        /// outcome, since a product of sines wants the product-to-sum identity rather than a
        /// rational function in <c>t</c>.
        /// </summary>
        /// <remarks>
        /// These assert only that the rule does not fire wrongly. Should either later be answered
        /// by something else, this test moves to the group above rather than being deleted.
        /// </remarks>
        [Theory]
        [InlineData("sin(x) + x")]
        [InlineData("sin(x) * sin(2*x)")]
        public void NotAFunctionOfSineAndCosineAlone(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            // `sin(x) + x` is answered by linearity, so what is asserted is that the answer does
            // not come from this substitution — no half-angle appears in it.
            Assert.DoesNotContain("tan(x / 2)", integral.Stringize());
        }

        /// <summary>
        /// A sum this rule *could* answer and must not, because splitting it answers it better.
        /// </summary>
        /// <remarks>
        /// <c>cos(x) + 1</c> is a function of the cosine alone, so the rewrite succeeds and
        /// produces a correct antiderivative in <c>tan(x/2)</c> about forty characters long, where
        /// linearity gives <c>sin(x) + x</c>. Both are antiderivatives and only one is an answer
        /// anybody wants. This is why the rule runs after <c>SolveBySplittingSum</c>; it was
        /// caught by the F# wrapper's own test, which pins the readable form, and not by anything
        /// here — hence this.
        /// </remarks>
        [Theory]
        [InlineData("cos(x) + 1")]
        [InlineData("sin(x) + cos(x)")]
        [InlineData("2 * cos(x)")]
        public void ASumTheOrdinaryRulesAnswerBetter(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("tan(x / 2)", integral.Stringize());
        }

        /// <summary>
        /// Rational in the sine and cosine with symbols for coefficients. Every sine and cosine
        /// brings a <c>1 + t^2</c> below the bar, and the one the numerator shares with the
        /// denominator is taken off the factor where it stands: divided out of the whole, the
        /// denominator of <c>sin(x)^2/(a + b cos(x))</c> was a sextic with a symbol in every
        /// coefficient, which nothing splits. And the secant, tangent and their kin are written
        /// in the two wherever a sum holds the sine or cosine, so that
        /// <c>sec(x)^2/(a + b sin(x))</c> is rational in them rather than declined at once.
        /// </summary>
        [Theory]
        [InlineData("sin(x)^2/(a + b*cos(x))")]
        [InlineData("sec(x)^2/(a + b*sin(x))")]
        [InlineData("sec(x)^2/(a + b*cos(x))")]
        [InlineData("tan(x)^4/(a + a*cos(x))")]
        [InlineData("csc(x)^2/(a + a*cos(x))")]
        public void ARationalFunctionWithSymbolsForCoefficients(string integrand) => DifferentiatesBackWithSymbols(integrand);

        /// <summary>
        /// A power of <c>a + b cos(x)</c> past the square beside a power of the secant. The
        /// substitution's integrand in <c>t</c> is not simplified where a symbol is in it:
        /// simplified, it took 43 s at the cube and more than two minutes at the fourth power,
        /// where integrating it takes a tenth of a second.
        /// </summary>
        [Theory]
        [InlineData("sec(x)^2/(a + b*cos(x))^3")]
        [InlineData("sec(x)^2/(a + b*cos(x))^4")]
        [InlineData("sec(c + d*x)^2/(a + b*cos(c + d*x))^4")]
        [InlineData("sec(x)/(a + b*cos(x))^3")]
        [InlineData("1/(cos(x)*(a + b*cos(x))^2)")]
        public void APowerOfALinearInTheCosineBesideASecant(string integrand) => DifferentiatesBackWithSymbols(integrand);

        private static void DifferentiatesBackWithSymbols(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 1.7).Substitute("b", 0.6).Substitute("c", 0.3).Substitute("d", 1.1);
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
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }
    }
}
