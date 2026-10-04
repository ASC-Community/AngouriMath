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
    /// <c>e^h R</c>, with <c>h</c> and <c>R</c> rational in the variable, integrated by the
    /// ansatz <c>F = e^h N/D</c>; and with no exponential, the Hermite reduction
    /// <c>R = (N/D)' + M/D_1</c> for a denominator with a repeated factor.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Liouville's theorem says the elementary antiderivative of <c>e^h R</c>, where there is
    /// one, is <c>e^h</c> times a rational function; so an ansatz of that shape, solved as a
    /// linear system in the coefficients of <c>N</c>, finds the antiderivative or shows there
    /// is none of that shape. <c>e^x x/(1 + x)^2</c>, <c>e^(x^2)(1 + 2x^2)</c> and
    /// <c>(4x^5 - 1)/(1 + x + x^5)^2</c> had no antiderivative; each is the derivative of
    /// something of its own shape.
    /// </para>
    /// <para>
    /// Every answer is differentiated back and compared to the integrand numerically; the
    /// non-elementary neighbours — <c>e^x/x</c>, <c>e^(x^3)</c> — are pinned as declined, since
    /// an elementary answer to them would be a wrong answer and not a missing one.
    /// (<c>e^(x^2)</c> is answered, with <c>erfi</c>, since
    /// https://github.com/asc-community/AngouriMath/issues/1501.)
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialAnsatzTest
    {
        private static readonly double[] Points = { 0.3, 0.9, 1.7, 2.6 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

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
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 3,
                $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// An exponential of the variable, of its square, of its reciprocal and of a rational
        /// function, each times a rational function: Moses's, Hearn's and Hebisch's from the
        /// Rubi suite. The sum is Moses's too, and is read whole.
        /// </summary>
        [Theory]
        [InlineData("e^x*x/(1 + x)^2")]
        [InlineData("e^x*(x^2 + 1)/(x + 1)^2")]
        [InlineData("e^(x^2)*(1 + 2*x^2)")]
        [InlineData("e^(x^2) + 2*e^(x^2)*x^2")]
        [InlineData("e^(1/x)*(1 + x)/x^4")]
        [InlineData("(1 - 3*x - x^2 + x^3)*e^(1/(x^2 - 1))/(1 - x - x^2 + x^3)")]
        [InlineData("e^(-x)/(1 - x)^2 - e^(-x)/(1 - x)")]
        public void AnExponentialTimesARationalFunction(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The same ansatz in the half-angle tangent, for <c>e^(a x)</c> times a rational
        /// function of the sine and cosine: Timofeev's <c>e^x (1 - sin(x))/(1 - cos(x))</c> is
        /// <c>-e^x cot(x/2)</c>, which nothing here read -- the exponential-times-trigonometric
        /// rule wants a polynomial in the two, the half-angle substitution wants no exponential,
        /// and by parts goes round in a circle. Under <c>t = tan(x/2)</c> it is
        /// <c>e^x (1 - t)^2/(2 t^2)</c> and the ansatz <c>e^x P(t)/Q(t)</c> has <c>Q = t</c>,
        /// <c>P = -1</c>. The non-elementary neighbours are pinned as declined: <c>e^x/(1 + cos(x))</c>
        /// needs the sine beside it, and <c>e^x tan(x/2)</c> has no such <c>P/Q</c> at all.
        /// </summary>
        [Theory]
        [InlineData("e^x*(1 - sin(x))/(1 - cos(x))")]
        [InlineData("e^x*(1 + sin(x))/(1 + cos(x))")]
        [InlineData("e^(2*x)*(1 + 2*sin(x))/(1 + cos(x))")]
        [InlineData("e^x*cos(x)")]
        public void AnExponentialTimesARationalFunctionOfTheSineAndCosine(string integrand) => DifferentiatesBack(integrand);

        [Theory]
        [InlineData("e^x/(1 + cos(x))")]
        [InlineData("e^x*tan(x/2)")]
        public void ItsNonElementaryNeighboursAreDeclined(string integrand)
            => Assert.Contains("integral(", integrand.ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// One level up the tower: an exponential -- of something that may hold <c>ln(x)</c> --
        /// times a rational function of <c>x</c> and <c>ln(x)</c>. Hearn's
        /// <c>(-1 + (1 - x) ln(x))/(e^x ln(x)^2)</c> is <c>(x e^(-x)/ln(x))'</c> and Hebisch's
        /// <c>e^(x + 1/ln(x)) (-1 + (1 + x) ln(x)^2)/ln(x)^2</c> is <c>(x e^(x + 1/ln(x)))'</c>;
        /// neither is reached by parts or by a substitution, since the logarithm is not a
        /// whole subtree to replace. With <c>L</c> for <c>ln(x)</c> and <c>L' = 1/x</c> the
        /// derivative of <c>e^h P(x, L)/(D L^k)</c> over <c>e^h</c> is rational in the two, and
        /// the identity is linear in <c>P</c>'s coefficients, one equation per <c>x^i L^j</c>.
        /// </summary>
        [Theory]
        [InlineData("(-1 + (1 - x)*ln(x))/(e^x*ln(x)^2)")]
        [InlineData("e^(x + 1/ln(x))*(-1 + (1 + x)*ln(x)^2)/ln(x)^2")]
        [InlineData("e^x*(1/ln(x) - 1/(x*ln(x)^2))")]
        [InlineData("e^x*(x*ln(x) + 1)/x")]
        public void OneLevelUpTheTower(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A non-elementary neighbour of the tower, pinned as declined: <c>e^x/ln(x)</c> has no such
        /// <c>P/(D L^k)</c>, and no antiderivative in the library's functions either. <c>e^x ln(x)</c>,
        /// the other neighbour, is <c>e^x ln(x) - Ei(x)</c> by parts, since <c>Ei</c> is a node.
        /// </summary>
        [Theory]
        [InlineData("e^x/ln(x)")]
        public void TheTowersNonElementaryNeighboursAreDeclined(string integrand)
            => Assert.Contains("integral(", integrand.ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// The exact forms, so that the ansatz is seen to find the short answer and not a
        /// longer equivalent.
        /// </summary>
        [Theory]
        [InlineData("e^x*x/(1 + x)^2", "e ^ x / (1 + x) + C")]
        [InlineData("(-1 + 4*x^5)/(1 + x + x^5)^2", "-x / (1 + x + x ^ 5) + C")]
        public void TheAnswerIsTheShortOne(string integrand, string expected)
            => Assert.Equal(expected, integrand.ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// No exponential: the Hermite reduction for a repeated factor, whose logarithmic part
        /// is then the splits' to answer. Apostol's, Stewart's and two of Timofeev's; the last
        /// took seventeen seconds of peeling and re-factoring to reach the same answer before.
        /// </summary>
        [Theory]
        [InlineData("(-1 + 4*x^5)/(1 + x + x^5)^2")]
        [InlineData("(1 + x^2 + x^4)/((1 + x^2)*(4 + x^2)^2)")]
        [InlineData("x^5/(1 + x^4)^3")]
        [InlineData("(1 + x^2)/(x*(1 + x^3)^2)")]
        [InlineData("1/((x + 1)^3*(x + 2)^4)")]
        public void TheHermiteReduction(string integrand) => DifferentiatesBack(integrand);

        [Fact]
        public void TheHermiteReductionIsQuick()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            DifferentiatesBack("(1 + x^2)/(x*(1 + x^3)^2)");
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
        }

        /// <summary>
        /// An exponential of a linear times a polynomial times an odd half power of a
        /// quadratic: Timofeev's <c>e^x (1 - x - x^2)/sqrt(1 - x^2)</c> is <c>(e^x sqrt(1 - x^2))'</c>,
        /// found by the ansatz <c>e^(a x) R Q^(k + 1/2)</c> with no remainder; and
        /// <c>e^x x/sqrt(1 + x^2)</c>, which has no elementary antiderivative, is declined.
        /// </summary>
        [Theory]
        [InlineData("e^x*(1 - x - x^2)/sqrt(1 - x^2)")]
        [InlineData("e^(2*x)*(2*x^2 + x + 2)/sqrt(x^2 + 1)")]
        public void AnExponentialTimesARootOfAQuadratic(string integrand) => DifferentiatesBack(integrand);

        [Fact]
        public void AnExponentialTimesARootOfAQuadraticIsDeclinedWhereThereIsNone()
            => Assert.Contains("integral(", "e^x*x/sqrt(1 + x^2)".ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// What has no antiderivative in the library's functions is declined, not answered: the
        /// ansatz finds no <c>N</c>, which by Liouville is the proof that none is elementary.
        /// <c>e^x/x</c> and <c>e^x/(1 + x)</c> are not elementary either, but they are <c>Ei</c>.
        /// </summary>
        [Theory]
        [InlineData("e^(x^3)")]
        public void ANonElementaryOneIsDeclined(string integrand)
            => Assert.Contains("integral(", integrand.ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// <c>e^(1/x)</c> has no elementary antiderivative either, and it has one in <c>Ei</c>:
        /// <c>x e^(1/x) - Ei(1/x)</c>, found under <c>u = 1/x</c>, where what is left in x is
        /// <c>-1/u^2</c>.
        /// </summary>
        [Fact]
        public void AnExponentialOfAReciprocalIsWrittenWithEi()
        {
            Assert.Contains("Ei(", "e^(1/x)".ToEntity().Integrate("x").Stringize());
            DifferentiatesBack("e^(1/x)");
        }

        /// <summary>
        /// What by parts answered before keeps its form: a polynomial times an exponential is
        /// still integrated by parts, since the ansatz runs after it.
        /// </summary>
        [Fact]
        public void APolynomialTimesAnExponentialKeepsItsForm()
            => Assert.Equal("x * e ^ x + -e ^ x + C", "x*e^x".ToEntity().Integrate("x").Stringize());

        /// <summary>
        /// The Risch-Norman ansatz, one level up: a rational function of <c>x</c> and of a
        /// tower of exponentials and logarithms, integrated as a rational function of the
        /// same plus logarithms of the denominator's factors, with the coefficients unknown and
        /// solved for. Hearn's <c>e^(1 - x e^(x^2) + 2x^2)(x + 2x^3)/(1 - x e^(x^2))^2</c> is
        /// <c>(e^(1 - x e^(x^2))/(1 - x e^(x^2)))'</c>, an exponential of something with an
        /// exponential in it; his
        /// <c>e^(x^2)/x + 2x e^(x^2) ln(x) + (ln(x) - 2)/(x + ln(x)^2)^2 + (1 + 1/x + 2 ln(x)/x)/(x + ln(x)^2)</c>
        /// is <c>(e^(x^2) ln(x) - ln(x)/(x + ln(x)^2) + ln(x + ln(x)^2))'</c>, a sum whose terms are
        /// not elementary apart, so that every split loses it. Read whole, before the split.
        /// </summary>
        [Theory]
        [InlineData("e^(1 - e^(x^2)*x + 2*x^2)*(x + 2*x^3)/(1 - e^(x^2)*x)^2")]
        [InlineData("e^(x^2)/x + 2*e^(x^2)*x*ln(x) + (-2 + ln(x))/(x + ln(x)^2)^2 + (1 + 1/x + 2*ln(x)/x)/(x + ln(x)^2)")]
        [InlineData("e^(x^2)/x + 2*e^(x^2)*x*ln(x)")]
        public void ATowerOfExponentialsAndLogarithms(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// <see cref="DifferentiatesBack"/> with every symbol but <c>x</c> pinned to a distinct
        /// value off the integers, so that a symbolic base or slope is a number on both sides.
        /// </summary>
        private static void DifferentiatesBackWithParametersPinned(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var pinned = 0;
            foreach (var parameter in original.Vars)
            {
                if (parameter.Name == "x")
                    continue;
                var value = new[] { 1.3, 2.1, 0.7, 1.9, 3.1, 0.4 }[pinned++ % 6] + pinned / 6;
                derivative = derivative.Substitute(parameter, value);
                original = original.Substitute(parameter, value);
            }
            var compared = 0;
            foreach (var at in points)
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
            Assert.True(compared >= 3, $"only {compared} of {points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// A base other than <c>e</c>: <c>F^u</c> is read as <c>e^(u ln F)</c>, and the identity
        /// has <c>ln F</c> -- or <c>ln 2</c>, a number that is not exact -- in every coefficient,
        /// which the elimination treats as a symbol of its own; both were declined where
        /// <c>e^(k u)</c> is answered. The answer is written back in the base: Rubi's
        /// <c>F^(a + b x + c x^3)(b + 3 c x^2)</c> is <c>F^(a + b x + c x^3)/ln F</c>.
        /// </summary>
        [Theory]
        [InlineData("F^(a + b*x + c*x^3)*(b + 3*c*x^2)", "F ^ (a + b * x + c * x ^ 3)")]
        [InlineData("2^(x + c*x^3)*(1 + 3*c*x^2)", "2 ^ (x + c * x ^ 3)")]
        [InlineData("f^(a + b/x)/x^4", "f ^ (a + b / x)")]
        [InlineData("2^(1/x)/x^4", "2 ^ (1 / x)")]
        public void ABaseOtherThanE(string integrand, string writtenInTheBase)
        {
            DifferentiatesBackWithParametersPinned(integrand, Points);
            Assert.Contains(writtenInTheBase, integrand.ToEntity().Integrate("x").Stringize());
        }
    }
}
