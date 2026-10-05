//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A quadratic denominator raised to a power. <c>1/(x^2 + 1)^2</c> had no antiderivative,
    /// while <c>1/(x^2 - 1)^2</c> and <c>1/(x^2 + 2x + 1)^2</c> both did — a denominator with
    /// real roots comes apart into linear factors and never reached the gap.
    /// https://github.com/asc-community/AngouriMath/issues/180
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class RepeatedQuadraticIntegralTest
    {
        /// <summary>
        /// Differentiates the answer back and compares it with the integrand at points. The
        /// guard on <c>integral(</c> is the whole point of the helper: differentiating an
        /// unevaluated <c>integral(f, x)</c> gives back <c>f</c>, so without it every case
        /// the rule declines would pass.
        /// </summary>
        private static Entity AssertIsAntiderivative(string integrand, params double[] points)
        {
            var f = integrand.ToEntity();
            var antiderivative = f.Integrate("x");
            Assert.DoesNotContain("integral(", antiderivative.Stringize());
            var derivative = antiderivative.Substitute("C", 0).Differentiate("x");
            foreach (var point in points)
            {
                var expected = f.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.Equal(expected, actual, 8);
            }
            return antiderivative;
        }

        // k/(ax^2 + bx + c)^n with no real roots -- the shape that had no antiderivative at all.
        [Theory]
        [InlineData("1 / (x ^ 2 + 1) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("1 / (x ^ 2 + 4) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("1 / (3 * x ^ 2 + 5) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("1 / (2 * x ^ 2 + 3 * x + 7) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("5 / (x ^ 2 + 2 * x + 5) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        public void AConstantOverARepeatedIrreducibleQuadratic(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // The reduction takes one power off at a time, so a third and a fourth power are the
        // same line applied again rather than new cases.
        [Theory]
        [InlineData("1 / (x ^ 2 + 1) ^ 3", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("1 / (x ^ 2 + 1) ^ 4", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("1 / (x ^ 2 + 2) ^ 5", new[] { 0.37, 1.4, -0.83 })]
        public void AHigherPowerIsTheSameReductionAppliedAgain(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // (px + q)/(ax^2 + bx + c)^n, by writing the numerator as a multiple of the
        // denominator's derivative plus a constant.
        [Theory]
        [InlineData("(x + 3) / (x ^ 2 + 2 * x + 5) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("x / (x ^ 2 + 1) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("x / (x ^ 2 + 1) ^ 3", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("(2 * x + 1) / (x ^ 2 + x + 1) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        public void ALinearNumeratorOverARepeatedQuadratic(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // A numerator of higher degree is divided by the quadratic: N = qQ + r takes one power
        // off the denominator and two degrees off the numerator, and ends.
        [Theory]
        [InlineData("x ^ 2 / (x ^ 2 + 2) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("(x ^ 2 + 1) / (x ^ 2 + 2) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("x ^ 3 / (x ^ 2 + 1) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("(x ^ 3 + x + 1) / (x ^ 2 + 1) ^ 3", new[] { 0.37, 1.4, -0.83 })]
        [InlineData("(x ^ 2 + x) / (x ^ 2 + 3) ^ 2", new[] { 0.37, 1.4, -0.83 })]
        public void APolynomialNumeratorIsDividedByTheQuadratic(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// The one case checked exactly rather than at points: the derivative of the answer is
        /// the integrand, with a residual of zero, which is the identity itself rather than a
        /// consequence of it holding at the places sampled.
        /// </summary>
        /// <remarks>
        /// The zero comes back <b>conditional</b>, and that is right rather than a shortfall.
        /// Both the integrand and its antiderivative are undefined where <c>1 + x^2 = 0</c> —
        /// off the real line, at <c>+-i</c> — and "these two agree everywhere" would be a
        /// claim about points where neither has a value. Asserted on the node rather than on
        /// the printed form, so that a change of spelling does not read as a change of answer.
        /// </remarks>
        [Fact]
        public void TheAnswerDifferentiatesBackToAConditionalZero()
        {
            var antiderivative = "1 / (x ^ 2 + 1) ^ 2".ToEntity().Integrate("x").Substitute("C", 0);
            var residual = (antiderivative.Differentiate("x") - "1 / (x ^ 2 + 1) ^ 2".ToEntity()).Simplify();

            var zero = residual is Entity.Providedf(var expression, _) ? expression : residual;
            Assert.Equal(Entity.Number.Integer.Create(0), zero);
        }

        /// <summary>
        /// A numerator that is already a multiple of the denominator's derivative leaves no
        /// second integral, and the answer carries no condition. Multiplying the reduction by
        /// zero instead of dropping it would attach one, since <c>0 * (2x/(x^2 + 1))</c> is
        /// <c>0 provided not 1 + x^2 = 0</c> and not <c>0</c>.
        /// </summary>
        [Fact]
        public void ANumeratorAlongTheDerivativeIsOwedNoCondition()
        {
            var answer = "x / (x ^ 2 + 1) ^ 2".ToEntity().Integrate("x").Stringize();
            Assert.DoesNotContain("provided", answer);
        }

        // A symbolic quadratic cannot have its discriminant's sign decided, so every arm
        // survives and the answer is the piecewise. Without the two arms the reduction cannot
        // speak for -- a zero leading coefficient and a zero discriminant -- those inputs would
        // carry a division by zero into the answer.
        [Theory]
        [InlineData("1 / (a * x ^ 2 + c) ^ 2")]
        [InlineData("1 / (a * x ^ 2 + b * x + c) ^ 2")]
        public void ASymbolicQuadraticIsAnsweredAsAPiecewise(string integrand)
        {
            var answer = integrand.ToEntity().Integrate("x").Stringize();
            Assert.DoesNotContain("integral(", answer);
            Assert.Contains("piecewise", answer);
            Assert.Contains("arctan", answer);
        }

        /// <summary>
        /// The reduction's arms are on the discriminant as the first power's rule writes it,
        /// expanded: a split hands a quadratic's powers to the integrator side by side, and the
        /// piecewise that adds their answers read <c>4 (-c - d) = 0</c> and
        /// <c>(-4) c + (-4) d = 0</c> as two conditions, so their arms multiplied. The second
        /// row came back with 27 arms and 18,355 characters, where nine hold every case.
        /// </summary>
        [Theory]
        [InlineData("1 / (x ^ 2 - c - d) ^ 3 + 1 / (x ^ 2 - c - d)", 3)]
        [InlineData("2 * d ^ 3 * x ^ 2 / ((x ^ 2 - c - d) ^ 3 * (x ^ 2 - c + d))", 9)]
        [InlineData("x ^ 2 / (a * x ^ 2 + b * x + c) ^ 2", 10)]
        public void ThePowersOfAQuadraticShareTheirArms(string integrand, int arms)
        {
            var answer = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", answer.Stringize());
            var most = answer.Nodes.OfType<Entity.Piecewise>().Max(piecewise => piecewise.Cases.Count());
            Assert.True(most <= arms, $"the antiderivative of {integrand} has {most} arms: {answer}");
            // One set of values on each side of the discriminant's sign.
            foreach (var (a, b, c, d) in new[] { (1.3, 0.4, 0.7, 0.4), (0.9, -0.3, -2.0, 0.5) })
            {
                Entity Pinned(Entity e) => e.Substitute("a", a).Substitute("b", b).Substitute("c", c).Substitute("d", d);
                var derivative = Pinned(answer.Substitute("C", 0)).Differentiate("x");
                var original = Pinned(integrand.ToEntity());
                foreach (var point in new[] { 0.2, 1.4, -2.3 })
                {
                    var expected = original.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                    var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                    Assert.Equal(expected, actual, 8);
                }
            }
        }

        // Already answered before this rule, by the power rule and by partial fractions, and
        // still answered: a denominator with real roots comes apart into linear factors, and a
        // perfect square is a linear factor to twice the power.
        [Theory]
        [InlineData("1 / (x ^ 2 - 1) ^ 2", new[] { 1.4, 2.6, -3.1 })]
        [InlineData("1 / (x + 1) ^ 2", new[] { 0.37, 1.4, 2.6 })]
        [InlineData("1 / (x ^ 2 + 2 * x + 1) ^ 2", new[] { 0.37, 1.4, 2.6 })]
        [InlineData("1 / (x ^ 2 - 2) ^ 2", new[] { 0.37, 2.6, -3.1 })]
        public void ADenominatorThatFactorsIsStillAnswered(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// An improper fraction is declined by this rule rather than divided out in it — the
        /// recursion bottoms out at a single power with a numerator still of degree two, which
        /// is the case <c>SolveByPartialFractions</c> already opens with a long division. It is
        /// answered; the point is that it is answered there.
        /// </summary>
        [Fact]
        public void AnImproperFractionIsAnsweredByTheDivisionThatAlreadyExisted() =>
            AssertIsAntiderivative("x ^ 4 / (x ^ 2 + 1) ^ 2", 0.37, 1.4, -0.83);

        /// <summary>
        /// The reduction answers each constant over a power of the quadratic on its own, down
        /// to the first power, so a numerator of degree four over a fourth power is three
        /// reductions each carrying the logarithm the first power is, with coefficients that
        /// sum to zero where the antiderivative is rational: <c>8(x^4 + x^2)/(x^2 - 1)^4</c> is
        /// <c>-8x^3/(3(x^2 - 1)^3)</c>, and it was written with <c>ln((x - 1)/(x + 1))</c> three
        /// times over. The coefficient is added up before anything is written. Where it is
        /// not zero the logarithm or the arctangent stays, as for <c>1/(x^2 + 1)^3</c> above.
        /// </summary>
        [Theory]
        [InlineData("8 * (x ^ 4 + x ^ 2) / (x ^ 2 - 1) ^ 4", new[] { 0.37, 1.4, -0.83, 2.6 })]
        [InlineData("(x ^ 2 - 1) / (x ^ 2 + 1) ^ 2", new[] { 0.37, 1.4, -0.83, 2.6 })]
        public void ALogarithmWhoseCoefficientsSumToZeroIsNotWritten(string integrand, double[] points)
        {
            var antiderivative = AssertIsAntiderivative(integrand, points);
            Assert.DoesNotContain("ln(", antiderivative.Stringize());
            Assert.DoesNotContain("arctan(", antiderivative.Stringize());
        }
    }
}
