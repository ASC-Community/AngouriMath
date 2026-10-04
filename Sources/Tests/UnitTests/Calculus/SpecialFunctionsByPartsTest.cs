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
    /// The special functions integrated by parts: each has an elementary derivative, so against a
    /// power of <c>x</c> it is the factor differentiated, and alone it is integrated against 1.
    /// The rows are Rubi's, from its files 8.1, 8.3, 8.4 and 8.5.
    /// https://github.com/asc-community/AngouriMath/issues/1501
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class SpecialFunctionsByPartsTest
    {
        /// <summary>Off 0, where the negative powers are undefined.</summary>
        private static readonly double[] Points = { -1.7, -0.6, 0.35, 0.9, 1.45 };

        /// <summary>
        /// Integrates, pins the parameters, and compares the derivative of the answer with the
        /// integrand at <see cref="Points"/>. The parameters are pinned after integrating, so the
        /// rule is asked the symbolic question.
        /// </summary>
        private static void DifferentiatesBack(string integrand, params (string Name, string Value)[] pins)
            => DifferentiatesBackAt(Points, integrand, pins);

        /// <summary><see cref="DifferentiatesBack"/> at <paramref name="points"/>.</summary>
        private static void DifferentiatesBackAt(double[] points, string integrand, params (string Name, string Value)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => pins.Aggregate(e, (current, pin) => current.Substitute(pin.Name, pin.Value.ToEntity()));
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        private static readonly (string, string)[] Parameters = { ("a", "2/5"), ("b", "13/10"), ("c", "7/10"), ("d", "19/10") };

        /// <summary>
        /// Alone, against 1: <c>int Ei(u) = u Ei(u) - e^u</c> and its kin, each over the rate.
        /// </summary>
        [Theory]
        [InlineData("Ei(b*x)")]
        [InlineData("Ei(a + b*x)")]
        [InlineData("Si(b*x)")]
        [InlineData("Si(a + b*x)")]
        [InlineData("Ci(b*x)")]
        [InlineData("Shi(a + b*x)")]
        [InlineData("Chi(b*x)")]
        public void ASpecialFunctionAloneIsByPartsAgainstOne(string integrand)
            => DifferentiatesBack(integrand, Parameters);

        /// <summary>
        /// The logarithmic integral, where its argument stays above 0 at every point: <c>a = 3</c>
        /// keeps <c>a + b x</c> between 0.79 and 4.9.
        /// </summary>
        [Theory]
        [InlineData("li(a + b*x)")]
        [InlineData("x*li(a + b*x)")]
        public void TheLogarithmicIntegralIsByParts(string integrand)
            => DifferentiatesBack(integrand, ("a", "3"), ("b", "13/10"));

        /// <summary>
        /// Over <c>x</c>, the antiderivative of <c>1/x</c> is taken as <c>ln(b x)</c>, the logarithm the
        /// derivative of <c>li(b x)</c> divides by, and the remainder is <c>b</c>:
        /// <c>li(b x) ln(b x) - b x</c>. At positive <c>x</c>, where <c>li(b x)</c> is real, and off
        /// <c>x = 1/b</c>, where <c>ln(b x)</c> is 0.
        /// </summary>
        [Fact]
        public void TheLogarithmicIntegralOverXIsOneRoundOfParts()
            => DifferentiatesBackAt(new[] { 0.35, 1.45, 2.3 }, "li(b*x)/x", ("b", "13/10"));

        /// <summary>
        /// Beside a power of a monomial, the remainder is <c>b (d x)^m x/((m + 1) ln(b x))</c>, a power
        /// of <c>x</c> over a logarithm of a monomial, which is <c>Ei((m + 2) ln(b x))</c> times a
        /// locally constant factor. Rubi's 8.3 row 269.
        /// </summary>
        [Fact]
        public void TheLogarithmicIntegralBesideAPowerOfAMonomial()
            => DifferentiatesBackAt(new[] { 0.35, 1.45, 2.3 }, "(d*x)^m*li(b*x)", ("b", "13/10"), ("d", "19/10"), ("m", "1/2"));

        /// <summary>
        /// Against a power of <c>x</c>, the special function is the factor differentiated: what is
        /// left is the power times <c>e^(-u^2)</c>, <c>e^u/u</c>, <c>sin(u)/u</c> and the like.
        /// </summary>
        [Theory]
        [InlineData("x^3*erf(b*x)")]
        [InlineData("x^2*erf(b*x)")]
        [InlineData("x*erf(b*x)")]
        [InlineData("erf(b*x)/x^2")]
        [InlineData("erf(b*x)/x^3")]
        [InlineData("(c + d*x)^2*erfc(a + b*x)")]
        [InlineData("x*erfi(b*x)")]
        [InlineData("x^2*Ei(b*x)")]
        [InlineData("x*Ei(a + b*x)")]
        [InlineData("Ei(b*x)/x^2")]
        [InlineData("x*Si(b*x)")]
        [InlineData("x^2*Ci(b*x)")]
        [InlineData("Si(b*x)/x^2")]
        [InlineData("x*Shi(b*x)")]
        [InlineData("x^3*Chi(b*x)")]
        public void APowerTimesASpecialFunctionIsByParts(string integrand)
            => DifferentiatesBack(integrand, Parameters);

        /// <summary>
        /// Beside a power of <c>x</c> times the elementary function its derivative is made of, the
        /// special function is still the factor differentiated: <c>x sin(b x)</c> is integrated by
        /// its polynomial's parts, and what is left, <c>sin(b x)/x</c> times that, is products of
        /// sines and cosines over powers of <c>x</c>.
        /// </summary>
        [Theory]
        [InlineData("x*Si(b*x)*sin(b*x)")]
        [InlineData("x^3*Si(b*x)*sin(b*x)")]
        [InlineData("x^2*Ci(b*x)*cos(b*x)")]
        [InlineData("x*Ci(b*x)*sin(b*x)")]
        [InlineData("x*Si(a + b*x)*sin(a + b*x)")]
        [InlineData("x*Si(c + d*x)*sin(a + b*x)")]
        public void BesideAPowerAndTheElementaryFactorOfItsDerivative(string integrand)
            => DifferentiatesBack(integrand, Parameters);

        /// <summary>
        /// A square of a special function of <c>b x</c> is two rounds of parts: the first against
        /// the power of <c>x</c> leaves the special function once, beside its derivative, and that
        /// is the case above or a substitution. The remainder of a round is asked term by term,
        /// since it comes back as one product over a sum -- <c>(b x Ei(b x) - e^(b x)) e^(b x)/(b x)</c>
        /// for <c>Ei(b x)^2</c> -- that no rule reads whole.
        /// </summary>
        [Theory]
        [InlineData("Ei(b*x)^2")]
        [InlineData("x*Ei(b*x)^2")]
        [InlineData("x^2*Ei(b*x)^2")]
        [InlineData("x*Si(b*x)^2")]
        [InlineData("Ci(b*x)^2")]
        [InlineData("x*Ci(b*x)^2")]
        [InlineData("x^2*erf(b*x)^2")]
        [InlineData("x*erfc(b*x)^2")]
        [InlineData("erfi(b*x)^2/x^3")]
        [InlineData("x*Shi(b*x)^2")]
        [InlineData("Chi(b*x)^2")]
        public void ASquareIsTwoRoundsOfParts(string integrand)
            => DifferentiatesBack(integrand, Parameters);

        /// <summary>
        /// Of <c>a + b x</c>, the first round against <c>x</c> takes <c>x^2/2</c> less its value at
        /// <c>-a/b</c>, written as <c>a + b x</c> times the quotient, so the linear the derivative
        /// divides by cancels and what is left is the case above.
        /// </summary>
        [Theory]
        [InlineData("x*Shi(a + b*x)^2")]
        [InlineData("x*Ei(a + b*x)^2")]
        [InlineData("Shi(a + b*x)^2")]
        [InlineData("x*Si(a + b*x)^2")]
        [InlineData("x^2*Ei(a + b*x)^2")]
        [InlineData("erf(a + b*x)^2")]
        public void ASquareOfAShiftedArgumentIsTwoRoundsOfParts(string integrand)
            => DifferentiatesBack(integrand, Parameters);

        /// <summary>
        /// Beside a linear, the square of a complementary error function of <c>a + b x</c> is not
        /// answered yet, and what is asserted is the value of any answer given in its place. Under
        /// <c>u = erfc(a + b x)</c> what is left in x is a polynomial, which is no power of u, however
        /// small <c>erfc</c> is at the points that screen it.
        /// </summary>
        [Fact]
        public void ASquareOfAShiftedComplementaryErrorFunctionBesideALinearHasNoWrongAnswer()
        {
            if (!"(c + d*x)*erfc(a + b*x)^2".ToEntity().Integrate("x").Stringize().Contains("integral("))
                DifferentiatesBack("(c + d*x)*erfc(a + b*x)^2", Parameters);
        }
    }
}
