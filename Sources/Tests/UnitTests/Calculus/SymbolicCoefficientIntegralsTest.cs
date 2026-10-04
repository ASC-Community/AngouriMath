//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// Quotients whose denominator is a quadratic with a symbolic leading coefficient and no
    /// x term, such as k/(a x^2 + c). These answered NaN: the a = 0 arm of the piecewise is
    /// written as (k/b) ln|bx + c|, which divides by zero when the denominator has no x term,
    /// and the resulting NaN propagates out of the whole piecewise as soon as a is symbolic
    /// and so the arm cannot be dropped for being unreachable. With a numeric leading
    /// coefficient a = 0 is decidably false, the arm is dropped, and the same integrals were
    /// answered correctly -- which is why this was invisible to every test using numbers.
    ///
    /// No issue is filed for this; it was found by work/intbench against Rubi's suite, where
    /// it accounted for six of the seven wrong answers.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class SymbolicCoefficientIntegralsTest
    {
        /// <summary>
        /// Integrates with the coefficients left symbolic, then substitutes values into the
        /// answer and differentiates it back. Specialising after integrating is the point:
        /// substituting first would take the numeric path, which never had the defect.
        /// </summary>
        private static void AssertIsAntiderivative(
            string integrand, string coefficients, params double[] points)
        {
            var f = integrand.ToEntity();
            var antiderivative = f.Integrate("x");
            Assert.DoesNotContain("integral(", antiderivative.Stringize());
            Assert.DoesNotContain("NaN", antiderivative.Stringize());
            // The root-of-a-quadratic arm only collapses once the *symbolic* answer is
            // simplified: the raw form still carries an undivided .../0 that has not been
            // turned into NaN yet, so checking the raw form alone would miss it.
            Assert.DoesNotContain("NaN", antiderivative.Simplify().Stringize());

            Entity specialisedIntegrand = f;
            Entity specialisedAnswer = antiderivative.Substitute("C", 0);
            foreach (var assignment in coefficients.Split(','))
            {
                var parts = assignment.Split('=');
                var name = parts[0].Trim();
                var value = double.Parse(parts[1].Trim(),
                    System.Globalization.CultureInfo.InvariantCulture);
                specialisedIntegrand = specialisedIntegrand.Substitute(name, value);
                specialisedAnswer = specialisedAnswer.Substitute(name, value);
            }

            // Simplify collapses the piecewise once the conditions are decidable; the
            // derivative of an undecided piecewise would say nothing.
            var derivative = specialisedAnswer.Simplify().Differentiate("x");
            foreach (var point in points)
            {
                var expected = specialisedIntegrand.Substitute("x", point)
                    .EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point)
                    .EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.Equal(expected, actual, 8);
            }
        }

        // The six wrong answers intbench found, in the form Rubi states them. Every one is a
        // denominator with a symbolic leading coefficient and no x term.
        [Theory]
        [InlineData("1 / (a ^ 2 + b ^ 2 * x ^ 2)", "a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (a ^ 2 - b ^ 2 * x ^ 2)", "a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (a + b * x ^ 2)", "a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (a * x ^ 2 + c)", "a = 3, c = 2", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (A ^ 4 - A ^ 2 * B ^ 2 + (-A ^ 2 + B ^ 2) * x ^ 2)",
            "A = 3, B = 2", new[] { 0.3, 1.7, -0.6 })]
        public void ASymbolicLeadingCoefficientWithNoXTerm(
            string integrand, string coefficients, double[] points) =>
            AssertIsAntiderivative(integrand, coefficients, points);

        /// <summary>
        /// The square of a symbolic linear is a quadratic whose discriminant is zero only once
        /// it is written out, and it was answered as a piecewise of three arms, two of them
        /// dividing by that zero, that nothing downstream could read: the exponential
        /// substitution's image of Welz's <c>x/(a e^(px) + b e^(-px))^2</c> is one. The
        /// perfect square is answered as such, in the generic case.
        /// </summary>
        [Theory]
        [InlineData("1 / (a * x + b) ^ 2", "a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (2 * (a * x + b) ^ 2)", "a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        public void ASquareOfASymbolicLinear(string integrand, string coefficients, double[] points)
        {
            Assert.DoesNotContain("piecewise", integrand.ToEntity().Integrate("x").Stringize());
            AssertIsAntiderivative(integrand, coefficients, points);
        }

        // A linear numerator over the same shape goes through the same arm, one rewrite later.
        [Theory]
        [InlineData("x / (a ^ 2 + b ^ 2 * x ^ 2)", "a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(2 * x + 1) / (a + b * x ^ 2)", "a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        public void ALinearNumeratorOverTheSameShape(
            string integrand, string coefficients, double[] points) =>
            AssertIsAntiderivative(integrand, coefficients, points);

        /// <summary>
        /// A polynomial over a power of a quadratic with a sum of symbols among its coefficients.
        /// The reduction divides the numerator by the quadratic a power at a time, and the
        /// remainder it took held the terms the division had taken away, written two ways:
        /// <c>x^4/(1/(-b + a) + x^2)^3</c> was declined where <c>x^4/(1/(a - b) + x^2)^3</c> was
        /// answered. The last row is what <c>t = sqrt(a + b x)</c> makes of it.
        /// </summary>
        /// <remarks>
        /// Checked with the symbols pinned and nothing simplified: the answers are piecewises
        /// over the sign of the quadratic's constant term, and simplifying them with the
        /// symbols left in is what the helper above does and what these do not need.
        /// </remarks>
        [Theory]
        [InlineData("x ^ 4 / (1 / (-b + a) + x ^ 2) ^ 3")]
        [InlineData("x ^ 4 / (c / (-b * c + a * d) + x ^ 2) ^ 3")]
        [InlineData("(1 - b * x ^ 2) ^ 3 / (c * (1 - b * x ^ 2) + a * d * x ^ 2) ^ 3")]
        [InlineData("x ^ 4 / (d * x ^ 2 + b * c - a * d) ^ 5")]
        [InlineData("x ^ 6 / (d * x ^ 2 + b * c - a * d) ^ 4")]
        [InlineData("(a + b * x) ^ (5/2) / (c + d * x) ^ 4")]
        public void APolynomialOverAPowerOfAQuadraticWithASumOfSymbolsInIt(string integrand)
        {
            var f = integrand.ToEntity();
            var antiderivative = f.Integrate("x");
            Assert.DoesNotContain("integral(", antiderivative.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 0.7).Substitute("b", 1.3).Substitute("c", 2.1).Substitute("d", 1.9);
            var derivative = Pin(antiderivative.Substitute("C", 0)).Differentiate("x");
            foreach (var point in new[] { 0.3, 0.6, 0.9 })
            {
                var expected = Pin(f).Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.Equal(expected, actual, 8);
            }
        }

        // The neighbouring shapes must keep working: a symbolic leading coefficient *with* an
        // x term never had the defect, because the a = 0 arm divides by a b that is not zero.
        [Theory]
        [InlineData("1 / (a * x ^ 2 + b * x + c)", "a = 1, b = 2, c = 5", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (a * x ^ 2 + 2 * x + 3)", "a = 1", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (1 + b * x + a * x ^ 2)", "a = 1, b = 2", new[] { 0.3, 1.7, -0.6 })]
        public void ASymbolicLeadingCoefficientWithAnXTerm(
            string integrand, string coefficients, double[] points) =>
            AssertIsAntiderivative(integrand, coefficients, points);

        // k/sqrt(a x^2 + c) has the same arm with the same division by zero in it, and it is
        // the last of intbench's seven wrong answers: 1/sqrt(-alpha^2 + 2 h r^2) came back as
        // sqrt(-alpha^2)/0 and Simplify turned the lot into NaN.
        [Theory]
        [InlineData("1 / sqrt(a * x ^ 2 + c)", "a = 2, c = 3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / sqrt(a * x ^ 2 + c)", "a = -9, c = 4", new[] { 0.3, 0.6, -0.5 })]
        [InlineData("1 / sqrt(-alpha ^ 2 + 2 * h * x ^ 2)", "alpha = 1, h = 2", new[] { 1.3, 2.4 })]
        [InlineData("1 / sqrt(a ^ 2 - b ^ 2 * x ^ 2)", "a = 2, b = 3", new[] { 0.3, 0.6, -0.5 })]
        public void ARootOfAQuadraticWithNoXTerm(
            string integrand, string coefficients, double[] points) =>
            AssertIsAntiderivative(integrand, coefficients, points);

        // And the shapes beside it, which have an x term and never had the defect.
        [Theory]
        [InlineData("1 / sqrt(a * x ^ 2 + b * x + c)", "a = 1, b = 2, c = 5", new[] { 0.3, 1.7 })]
        [InlineData("1 / sqrt(2 * x + 3)", "a = 1", new[] { 0.3, 1.7 })]
        public void ARootOfAQuadraticWithAnXTerm(
            string integrand, string coefficients, double[] points) =>
            AssertIsAntiderivative(integrand, coefficients, points);

        /// <summary>
        /// A linear beside the root of a quadratic, all coefficients symbols: through the
        /// Euler substitution this is a rational function of <c>t</c> whose partial-fraction
        /// coefficients are a linear system with the symbols in every entry. Solved on
        /// entities, whose arithmetic collects nothing, the answer was correct and 77 KB,
        /// with <c>(2b - 2b)</c> among the coefficients; solved over polynomials in the
        /// symbols, fraction-free, it is one arctangent or one logarithm per arm of a
        /// piecewise on the discriminant, and under 4 KB. Both arms are checked, and the
        /// size is the claim.
        /// </summary>
        [Theory]
        [InlineData("1 / ((x + a) * sqrt(x ^ 2 + b * x + c))", "a = 1, b = 1, c = 3", new[] { 0.3, 1.7 })]
        [InlineData("1 / ((x + a) * sqrt(x ^ 2 + b * x + c))", "a = 1, b = 1, c = -3", new[] { 1.7, 2.6 })]
        [InlineData("1 / ((x + 1) * sqrt(x ^ 2 + x + b))", "b = 3", new[] { 0.3, 1.7 })]
        public void ALinearBesideTheRootOfAQuadratic(string integrand, string coefficients, double[] points)
        {
            AssertIsAntiderivative(integrand, coefficients, points);
            var length = integrand.ToEntity().Integrate("x").Stringize().Length;
            Assert.True(length < 4000, $"{length} characters of answer for {integrand}");
        }

        /// <summary>
        /// An even polynomial over a symbolic biquadratic, by the two roots in <c>x^2</c>
        /// written with the root of the discriminant and <c>1/(x^2 - r)</c> as
        /// <c>atan(x/sqrt(-r))/sqrt(-r)</c> for any <c>r</c>. Checked with the discriminant
        /// negative (conjugate roots, the two terms summing to a real function), positive with
        /// both roots negative (real arctangents) and positive with both roots positive (the
        /// hyperbolic case through an imaginary argument), and with a higher even degree
        /// divided down.
        /// </summary>
        [Theory]
        [InlineData("x ^ 2 / (a + b * x ^ 2 + c * x ^ 4)", "a = 1.7, b = 2.3, c = 2.9", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (a + b * x ^ 2 + c * x ^ 4)", "a = 1, b = 5, c = 1", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(d + e * x ^ 2) / (a + b * x ^ 2 + c * x ^ 4)", "a = 1, b = -5, c = 1, d = 0.4, e = 1.1", new[] { 0.3, 1.7 })]
        [InlineData("x ^ 6 / (a + b * x ^ 2 + c * x ^ 4)", "a = 1.7, b = 2.3, c = 2.9", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (a + b + 2 * a * x ^ 2 + a * x ^ 4)", "a = 1.7, b = 2.3", new[] { 0.3, 1.7, -0.6 })]
        public void AnEvenPolynomialOverASymbolicBiquadratic(string integrand, string coefficients, double[] points)
        {
            AssertIsAntiderivative(integrand, coefficients, points);
            var length = integrand.ToEntity().Integrate("x").Stringize().Length;
            Assert.True(length < 1500, $"{length} characters of answer for {integrand}");
        }

        /// <summary>
        /// A power of a constant multiple of the variable with a symbolic exponent is
        /// integrated with the written power kept: <c>(c x)^m x^n</c> is
        /// <c>(c x)^m x^(n+1)/(m + n + 1)</c>, exact for every branch since
        /// <c>d/dx (c x)^m</c> is <c>m (c x)^m / x</c>, and the logarithm where the exponents
        /// sum to minus one. Rubi's <c>(e x)^m (a + b x^n)^p (c + d x^n)^q</c> with symbolic
        /// <c>m</c> and <c>n</c> is a sum of these once expanded and was declined term by term.
        /// Checked at a negative point too, where <c>(c x)^m</c> is not <c>c^m x^m</c>.
        /// </summary>
        [Theory]
        [InlineData("(c * x) ^ m * x ^ n", "c = 1.7, m = 0.7, n = 1.3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(c * x) ^ m", "c = -1.7, m = 0.7", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(c * x) ^ m * (a + b * x ^ n) * (A + B * x ^ n)", "c = 1.7, m = 0.7, n = 1.3, a = 2, b = 3, A = 0.5, B = 1.1", new[] { 0.3, 1.7 })]
        [InlineData("(c * x) ^ m * (a + b * x ^ n) ^ 3", "c = 1.7, m = 0.7, n = 2, a = 2, b = 3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(c * x) ^ m / x ^ (m + 1)", "c = 1.7, m = 0.7", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("x ^ 2 / (c * x) ^ 3", "c = 1.7", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(c * x) ^ m * (d * x) ^ k", "c = 1.7, d = 0.4, m = 0.7, k = -0.2", new[] { 0.3, 1.7 })]
        public void APowerOfAConstantMultipleOfTheVariable(string integrand, string coefficients, double[] points)
            => AssertIsAntiderivative(integrand, coefficients, points);

        /// <summary>
        /// The degenerate arm is not merely non-NaN, it is the right answer: where the
        /// leading coefficient really is zero and there is no x term, the integrand is the
        /// constant k/c and its antiderivative is kx/c. The old code claimed (k/b) ln|bx + c|
        /// there, which is wrong quite apart from dividing by zero.
        /// </summary>
        [Fact]
        public void TheDegenerateArmIsTheConstantIntegral()
        {
            var antiderivative = "1 / (a * x ^ 2 + 5)".ToEntity().Integrate("x")
                .Substitute("C", 0).Substitute("a", 0).Simplify();
            var derivative = antiderivative.Differentiate("x").Simplify();
            foreach (var point in new[] { 0.3, 1.7, -0.6 })
                Assert.Equal(0.2,
                    derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble(), 8);
        }
    }
}
