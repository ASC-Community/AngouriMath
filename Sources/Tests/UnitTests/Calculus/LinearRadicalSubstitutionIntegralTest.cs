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
    /// Integrands holding a fractional power of something linear in the variable, which
    /// <c>u^q = a*x + b</c> turns into a rational function.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>x/sqrt(1 + x)</c> had no antiderivative, and the reason is worth keeping: the general
    /// substitution rewrites <em>sub-expressions</em>, so it found <c>1 + x</c>, replaced it, and
    /// was left holding a bare <c>x</c> it could not express in the new variable. This substitutes
    /// for <b>x itself</b>, so nothing is left behind.
    /// </para>
    /// <para>
    /// Checked by differentiating back and comparing at points, never against a printed form —
    /// these come out as polynomials in the radical and their shape says nothing about whether
    /// they differentiate back.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class LinearRadicalSubstitutionIntegralTest
    {
        /// <summary>
        /// Points where every radical below is real: each of the bases used here is positive on
        /// <c>(0, 0.6)</c>, which is what lets one set serve them all.
        /// </summary>
        private static readonly double[] Points = { 0.05, 0.17, 0.31, 0.44, 0.58 };

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
        /// For an even <c>q</c> the principal root <c>u = (a x + b)^(1/q)</c> is not negative
        /// wherever it is real, and a root holding a power of <c>u</c> gives that power up:
        /// <c>1/sqrt(x + x^(3/2))</c> under <c>u = sqrt(x)</c> is <c>2u/sqrt(u^2 + u^3)</c>, a
        /// root of a cubic that nothing reads, and is <c>2/sqrt(1 + u)</c>. Apostol's
        /// <c>x/sqrt(1 + x^2 + (1 + x^2)^(3/2))</c> is the same one step further in.
        /// </summary>
        [Theory]
        [InlineData("1/sqrt(x + x^(3/2))")]
        [InlineData("sqrt(x)/sqrt(x + x^2)")]
        [InlineData("x/sqrt(1 + x^2 + (1 + x^2)^(3/2))")]
        public void APowerOfTheRootLeavesTheRadical(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// That step reads <c>u</c> as a non-negative real, which it is only where the radical
        /// is real, and the answer says so: <c>sqrt(x)/sqrt(x + x^2)</c> comes back
        /// <c>provided x + 1 &gt;= 0</c>. Beyond the radicand's zero <c>u</c> is imaginary, and the
        /// integrand can still be real there -- Rubi's <c>x sqrt(c - a c x) / e^(3 atanh(a x))</c>
        /// above <c>a x = 1</c> is the product of two imaginary factors -- while the answer built
        /// for a real <c>u</c> is not its antiderivative: with <c>a = 3.1</c>, <c>c = 0.7</c> at
        /// <c>x = 0.59</c> its derivative was 14% off, and it shipped as an answer. Now it is
        /// <c>NaN</c> there, which is no claim, and right where the condition holds. Rubi's integrand
        /// is written over each linear once before that step is reached
        /// (<see cref="EachLinearWrittenOnceIntegralTest"/>), and is answered on both sides.
        /// https://github.com/asc-community/AngouriMath/issues/718
        /// </summary>
        [Fact]
        public void TheEvenRootStepSaysWhereItHolds()
        {
            var conditioned = "sqrt(x)/sqrt(x + x^2)".ToEntity().Integrate("x");
            Assert.Contains("provided x + 1 >= 0", conditioned.Stringize());

            var integrand = "x * sqrt(c - a*c*x) / e^(3 * atanh(a*x))".ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var pinned = integral.Substitute("C", 0).Substitute("a", 3.1).Substitute("c", 0.7);
            var derivative = pinned.Differentiate("x");
            var original = integrand.Substitute("a", 3.1).Substitute("c", 0.7);
            // On both sides of a x = 1: the integrand is real at 0.59 too, 3.1 * 0.59 being above 1.
            foreach (var at in new[] { 0.05, 0.17, 0.31, 0.59, 0.9 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)want.ImaginaryPart) < 1e-12, $"the integrand at x = {at} is {want}, not real");
                Assert.False(got.IsNaN, $"no answer at x = {at}");
                Assert.True(Math.Abs((double)(got - want).RealPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// <c>e^artanh(a x) sqrt(c - a c x)</c> is real past <c>a x = 1</c> as well, where both
        /// factors are imaginary, and there it is the negative of what it is inside. Through the
        /// quotient of the logarithm's linears the answer was the one for inside, given
        /// everywhere, so past <c>a x = 1</c> it was wrong by its sign: the check at sampled
        /// points skipped the points where the derivative has no value, and those were the
        /// points where it was wrong. Now the answer holds where it is given and says where that is.
        /// https://github.com/asc-community/AngouriMath/issues/1655
        /// </summary>
        [Fact]
        public void AnExponentialOfAnInverseTangentBesideTheRootSaysWhereItHolds()
        {
            var integrand = "e^atanh(a*x)*sqrt(c - a*c*x)".ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var pinned = integral.Substitute("C", 0).Substitute("a", 1.37).Substitute("c", 3.71);
            var derivative = pinned.Differentiate("x");
            var original = integrand.Substitute("a", 1.37).Substitute("c", 3.71);
            // Inside, a x < 1: the derivative is the integrand.
            foreach (var at in new[] { 0.29, -0.61 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative is {got} at x = {at}, where the integrand is {want}");
            }
            // Past it the integrand is real, and the answer is either right or claims nothing.
            foreach (var at in new[] { 1.43, 3.17 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)want.ImaginaryPart) < 1e-12, $"the integrand at x = {at} is {want}, not real");
                var got = derivative.Substitute("x", at).Evaled;
                Assert.True(got.IsNaN || got is Entity.Number.Complex number
                    && Math.Abs((double)(number - want).RealPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// The substitution search's even root reads the sign of a real <c>u</c> as well, and
        /// says where it does: <c>(1 + sec(x))^(5/2) sqrt(cos(x))</c> under <c>t = cos(x)</c> and
        /// <c>u = sqrt(t)</c> is <c>-2 u^2 (1 + 1/u^2)^(5/2)/sqrt(1 - u^4)</c>, whose power of a
        /// quotient is taken apart as <c>(1 + u^2)^(5/2)/|u|^5</c>. Where the cosine is negative the
        /// integrand is still real, a product of two imaginary factors, and the answer built for a
        /// real <c>u</c> was not its antiderivative there. Rubi's 4.5.3.1.
        /// </summary>
        [Fact]
        public void TheSearchsEvenRootSaysWhereItHolds()
        {
            var integrand = "(1 + sec(x))^(5/2)*sqrt(cos(x))".ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.Contains("provided cos(x) >= 0", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            // Inside the condition the derivative is the integrand.
            foreach (var at in new[] { 0.3, 0.9, -0.7, 5.9 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = integrand.Substitute("x", at).EvalNumerical();
                Assert.False(got.IsNaN, $"no answer at x = {at}, inside the condition");
                Assert.True(Math.Abs((double)(got - want).RealPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative is {got} at x = {at}, where the integrand is {want}");
            }
            // Beyond it the integrand is real and the answer claims nothing.
            var outside = integrand.Substitute("x", 2.8).EvalNumerical();
            Assert.True(Math.Abs((double)outside.ImaginaryPart) < 1e-12, $"the integrand at x = 2.8 is {outside}, not real");
            Assert.True(derivative.Substitute("x", 2.8).Evaled.IsNaN, "the answer claims a derivative outside its condition");
        }

        /// <summary>
        /// And where the rules below read no sign at all, the answer is checked where the radicand
        /// is negative. <c>sqrt(cos(x))/sqrt(1 + sec(x))</c> is real where the cosine is negative,
        /// two of its roots imaginary there; under <c>t = cos(x)</c> and <c>u = sqrt(t)</c> the
        /// product of roots below the bar was written with <c>sqrt(u^4) = u^2</c>, and the answer
        /// was negated wherever the cosine is negative. Rubi's 4.5.1.2. The same in <c>t</c>,
        /// <c>sqrt(t)/(sqrt(1/t + 1) sqrt(1 - t^2))</c>, is written over each linear once and
        /// answered on both sides (<see cref="EachLinearWrittenOnceIntegralTest"/>).
        /// https://github.com/asc-community/AngouriMath/issues/1581
        /// </summary>
        [Theory]
        [InlineData("sqrt(cos(x))/sqrt(1 + sec(x))", "provided cos(x) >= 0", new[] { 0.3, 0.9, -0.7 }, 2.4)]
        public void AnEvenRootTakenRealWithoutASignSaysWhereItHolds(string written, string condition, double[] inside, double outsideAt)
        {
            var integrand = written.ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.Contains(condition, integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            foreach (var at in inside)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = integrand.Substitute("x", at).EvalNumerical();
                Assert.False(got.IsNaN, $"no answer at x = {at}, inside the condition");
                Assert.True(Math.Abs((double)(got - want).RealPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative is {got} at x = {at}, where the integrand is {want}");
            }
            // Beyond it the integrand is real and the answer claims nothing.
            var outside = integrand.Substitute("x", outsideAt).EvalNumerical();
            Assert.True(Math.Abs((double)outside.ImaginaryPart) < 1e-12, $"the integrand at x = {outsideAt} is {outside}, not real");
            Assert.True(derivative.Substitute("x", outsideAt).Evaled.IsNaN, "the answer claims a derivative outside its condition");
        }

        /// <summary>
        /// A polynomial over a square root of something linear. None of these had an
        /// antiderivative, and each is a first-year exercise.
        /// </summary>
        [Theory]
        [InlineData("x/sqrt(1 + x)")]
        [InlineData("x/sqrt(2 - 3*x)")]
        [InlineData("x^2/sqrt(x + 1)")]
        [InlineData("(x + 2)/sqrt(x + 1)")]
        public void APolynomialOverASquareRootOfALinear(string integrand) => DifferentiatesBack(integrand);

        /// <summary>A root other than the square one, where <c>q</c> is the exponent's denominator.</summary>
        [Theory]
        [InlineData("x/(1 + x)^(1/3)")]
        [InlineData("1/(x + 1)^(1/3)")]
        public void ARootOtherThanTheSquare(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// Two radicals over one base, whose exponent denominators are taken together by their
        /// least common multiple, so a single substitution clears both.
        /// </summary>
        [Theory]
        [InlineData("1/(sqrt(x + 1) + (x + 1)^(1/3))")]
        public void TwoRootsOverOneBase(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A sum or difference of two square roots below the bar is multiplied above and
        /// below by its conjugate, the product being the difference of the radicands:
        /// Timofeev's <c>(1 + x)/(sqrt(x^2 + 2x + 4) - sqrt(x^2 + x + 1))</c> is
        /// <c>(1 + x)(sqrt(x^2 + 2x + 4) + sqrt(x^2 + x + 1))/(x + 3)</c>, two of Euler's; and
        /// <c>1/(sqrt(x + 1) + sqrt(x))</c> is <c>sqrt(x + 1) - sqrt(x)</c>, two powers.
        /// </summary>
        [Theory]
        [InlineData("(1 + x)/(sqrt(4 + 2*x + x^2) - sqrt(1 + x + x^2))")]
        [InlineData("1/(sqrt(x + 1) + sqrt(x))")]
        public void ASumOfTwoRootsIsRationalised(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The radicands need not be polynomials in x: Timofeev's
        /// <c>cos(3x)/(sqrt(3 cos(x)^2 - sin(x)^2) - sqrt(8 cos(x)^2 - 1))</c> has the
        /// difference of the radicands, <c>4 cos(x)^2</c> by Pythagoras, below once it is
        /// respelled, and each root beside it is a root of a quadratic in the sine under
        /// <c>u = sin(x)</c> -- once the unifier lets a radicand in both functions through,
        /// which it does where the cosine is in even powers only.
        /// </summary>
        [Fact]
        public void TheRadicandsMayBeTrigonometric()
        {
            var integrand = "cos(3*x)/(sqrt(3*cos(x)^2 - sin(x)^2) - sqrt(8*cos(x)^2 - 1))";
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in new[] { 0.1, 0.2, 0.3, 0.4 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart) < 1e-8 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// Two bases, when every radical is a square root: <c>sqrt(x)/(x sqrt(1 + x))</c> is what
        /// <c>sqrt(1 + tanh(4x))</c> becomes under <c>u = e^(8x)</c>, and with <c>s = sqrt(x)</c>
        /// the other root is <c>sqrt(1 + s^2)</c>, a root of a quadratic, which the rules for
        /// those answer. <c>sqrt(1 - x) + sqrt(1 + x)</c> was pinned here as declined for the
        /// second base; it is answered now, by linearity before this rule and by this rule
        /// when the sum is not at the top. What the rule hands on still has to be answered:
        /// <c>1/(sqrt(1 - x) + sqrt(1 + x))</c> becomes a root of <c>2 - u^2</c>, whose Euler
        /// form has <c>sqrt(2)</c> in its coefficients, and the rational integrator stops at
        /// those; <c>3 + x</c> beside <c>1 - x</c> becomes a root of <c>4 - u^2</c> and comes out.
        /// </summary>
        [Theory]
        [InlineData("sqrt(x)/(x*sqrt(1 + x))")]
        [InlineData("sqrt(x)/sqrt(1 + x)")]
        [InlineData("1/(sqrt(x) + sqrt(x + 1))")]
        [InlineData("1/(sqrt(1 - x) + sqrt(3 + x))")]
        public void ASecondBaseUnderASquareRoot(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// Declined, so the boundary is recorded rather than assumed: three bases, or a cube
        /// root beside a second base, would need more than the one substitution and the
        /// quadratic-radical rules behind it; and two bases that are both negative somewhere
        /// on the reals, where the integrand is real and the answer built through an imaginary
        /// <c>u</c> is not its antiderivative -- <c>sqrt(x - 1) sqrt(x - 2)</c> below <c>1</c>,
        /// measured by quadrature at <c>-3.66</c> against an answer's <c>-4.68</c>. Should any
        /// later be answered by something else, these move rather than being deleted.
        /// </summary>
        /// <summary>
        /// A root of a quotient of two linears is a base of its own kind: under
        /// <c>u = sqrt((1 + x)/(3 + 2x))</c>, <c>x = (3u^2 - 1)/(1 - 2u^2)</c> is rational, and
        /// the answer holds wherever the root is real -- below <c>-3/2</c> too, where both
        /// linears are negative and the root of each is not, which is why the split into a
        /// root over a root is declined for it and rightly. Differentiated back on both sides.
        /// </summary>
        [Theory]
        [InlineData("sqrt((1 + x)/(3 + 2*x))", new[] { -4.0, -2.5, -1.8, -0.5, 0.7, 2.1 })]
        [InlineData("sqrt((x - 1)/(x + 1))/x", new[] { -4.0, -2.5, -1.8, 1.4, 2.6, 5.0 })]
        [InlineData("((2*x + 1)/(x - 3))^(1/3)", new[] { -4.0, -2.5, -1.8, 4.0, 5.5, 9.0 })]
        public void ARootOfAQuotientOfLinears(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// A whole power below the bar one level down: under <c>u = sqrt(1 - x)</c> this is
        /// <c>2u/(u^7 (u^2 - 1)^5)</c>, and the normalisation writes the power below as
        /// <c>(u^2 - 1)^(5 * (-1))</c>, an exponent that is a product of numbers and not a
        /// number, which the rational readers read evaluated now and read as nothing before.
        /// </summary>
        [Theory]
        [InlineData("1/((1 - x)^(7/2)*x^5)")]
        public void AWholePowerBelowTheBarOneLevelDown(string integrand) => DifferentiatesBack(integrand);

        [Theory]
        [InlineData("1/(sqrt(x) + sqrt(x + 1) + sqrt(x + 2))")]
        [InlineData("x^(1/3)/sqrt(x + 1)")]
        [InlineData("x/(sqrt(x - 1)*sqrt(x - 2))")]
        public void ThreeBasesOrACubeRootBesideASecondAreDeclined(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("u_rad", integral.Stringize());
        }
    }
}
