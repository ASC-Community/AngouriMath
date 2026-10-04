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
    /// Quotients of polynomials of low degree. Only a constant numerator was recognised, so
    /// x/(x^2 + 2x + 5) and x/(x + 1) had no antiderivative at all. Each answer is checked by
    /// differentiating it back and comparing at points, since what matters is that it is an
    /// antiderivative and not what form it is written in.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class RationalIntegralsTest
    {
        private static void AssertIsAntiderivative(string integrand, params double[] points)
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
        }

        // (px + q) / (ax^2 + bx + c). The numerator is written as a multiple of the
        // denominator's derivative plus a constant, which splits it into a logarithm and the
        // constant-numerator case that was already there.
        [Theory]
        [InlineData("x / (x ^ 2 + 2 * x + 5)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(x + 3) / (x ^ 2 + 3 * x + 2)", new[] { 0.3, 1.7, 4.2 })]
        [InlineData("(2 * x + 1) / (x ^ 2 + x + 1)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("x / (x ^ 2 + 1)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(3 * x - 2) / (2 * x ^ 2 + 5)", new[] { 0.3, 1.7, -0.6 })]
        public void ALinearNumeratorOverAQuadratic(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // The same with the coefficients symbols: a piecewise on the sign of the discriminant
        // and an arm for a vanishing leading coefficient, which is (px + q)/(bx + c) and not
        // the rewrite that divided by it. Timofeev's (b1 + c1 x)/(a + 2b x + c x^2) and
        // (B + A x)/(c + 2b x + a x^2)^2 had no antiderivative for the symbol in front; the
        // arms are checked with the symbols pinned to each case.
        [Theory]
        [InlineData("(q + p * x) / (c + b * x + a * x ^ 2)", 1.0, new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(q + p * x) / (c + b * x + a * x ^ 2)", -1.0, new[] { 0.3, 0.5, -0.6 })]
        [InlineData("(q + p * x) / (c + b * x + a * x ^ 2)", 0.0, new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(q + p * x) / (c + b * x + a * x ^ 2) ^ 2", 1.0, new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(q + p * x) / (c + b * x + a * x ^ 2) ^ 2", 0.0, new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(q + p * x) / (c + b * x + a * x ^ 2) ^ 3", 1.0, new[] { 0.3, 1.7, -0.6 })]
        public void ALinearNumeratorOverASymbolicQuadratic(string integrand, double leading, double[] points)
        {
            var f = integrand.ToEntity();
            var antiderivative = f.Integrate("x");
            Assert.DoesNotContain("integral(", antiderivative.Stringize());
            Entity pinned = antiderivative.Substitute("C", 0);
            foreach (var (name, value) in new[] { ("a", leading), ("b", 0.7), ("c", 1.9), ("p", 1.3), ("q", -0.4) })
            {
                pinned = pinned.Substitute(name, value);
                f = f.Substitute(name, value);
            }
            var derivative = pinned.Simplify().Differentiate("x");
            foreach (var point in points)
            {
                var expected = f.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.Equal(expected, actual, 6);
            }
        }

        // (px + q) / (bx + c), the same rewrite one degree down.
        [Theory]
        [InlineData("x / (x + 1)", new[] { 0.3, 1.7 })]
        [InlineData("(2 * x + 1) / (x - 3)", new[] { 0.3, 1.7 })]
        [InlineData("(3 * x) / (2 * x + 5)", new[] { 0.3, 1.7 })]
        public void ALinearNumeratorOverALinearDenominator(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // 1/cos(u)^2 and 1/sin(u)^2 are written that way at least as often as sec(u)^2 and
        // csc(u)^2, and neither of the four shapes was recognised.
        [Theory]
        [InlineData("1 / cos(x) ^ 2", new[] { 0.3, 1.1, -0.6 })]
        [InlineData("1 / sin(x) ^ 2", new[] { 0.3, 1.1, 2.2 })]
        [InlineData("2 / cos(3 * x) ^ 2", new[] { 0.3, 0.9 })]
        [InlineData("sec(x) ^ 2", new[] { 0.3, 1.1, -0.6 })]
        [InlineData("cosec(x) ^ 2", new[] { 0.3, 1.1, 2.2 })]
        public void ReciprocalSquaresOfTheWaves(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // The shapes these sit next to in the table have to keep working. A constant over a
        // quadratic in particular is matched by the earlier arm and must stay there.
        [Theory]
        [InlineData("1 / (x ^ 2 + 1)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (x ^ 2 + 2 * x + 5)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("1 / (2 * x + 3)", new[] { 0.3, 1.7 })]
        [InlineData("1 / x", new[] { 0.3, 1.7 })]
        [InlineData("x ^ 2", new[] { 0.3, 1.7 })]
        [InlineData("sin(x)", new[] { 0.3, 1.7 })]
        [InlineData("tan(x)", new[] { 0.3, 1.1 })]
        [InlineData("x * e ^ x", new[] { 0.3, 1.7 })]
        public void NeighbouringFormsAreUnaffected(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // A denominator whose rational root repeats. Splitting off one factor and giving up
        // if the quotient still vanished there left 1/(x^4 + x^2) with no antiderivative:
        // its only rational root is zero, twice. A root of multiplicity m contributes a term
        // over the m-th power, and taking all m out at once is what the single-root arm was
        // already doing for m = 1.
        [Theory]
        [InlineData("1 / (x ^ 4 + x ^ 2)", new[] { 0.7, 1.3, -1.7 })]
        [InlineData("1 / (x ^ 3 + x ^ 2)", new[] { 0.7, 1.3, -1.7 })]
        [InlineData("1 / (x ^ 2 * (x + 1))", new[] { 0.7, 1.3, -1.7 })]
        [InlineData("1 / ((x - 1) ^ 2 * (x + 2))", new[] { 0.7, 2.3, -1.7 })]
        [InlineData("x / ((x - 1) ^ 2 * (x ^ 2 + 1))", new[] { 0.7, 2.3, -1.7 })]
        [InlineData("1 / ((x + 1) ^ 3 * (x - 2))", new[] { 0.7, 2.3, -1.7 })]
        [InlineData("(x + 1) / (x ^ 3 - x ^ 2)", new[] { 0.7, 1.3, -1.7 })]
        public void ARepeatedRationalRootDecomposesToo(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // A numerator that is a constant multiple of the denominator's derivative is the
        // logarithm of the denominator whatever the denominator is. Welz's quartic was answered
        // by the substitution rule with u the denominator, until sums stopped being that rule's
        // candidates for a rational function -- each cost a simplification of the quotient, and
        // (1 + t^2)/((sqrt(2) - 1)t^2 + 2t + 1 + sqrt(2)) spent eight seconds on them -- so
        // the case is read here, where it is one division.
        [Theory]
        [InlineData("(3 - 3*x + 30*x^2 + 160*x^3)/(9 + 24*x - 12*x^2 + 80*x^3 + 320*x^4)", new[] { 0.3, 1.7, 3.2 })]
        [InlineData("(x^3 + 1)/(x^4 + 4*x + 2)", new[] { 0.3, 1.7, 3.2 })]
        [InlineData("(2*x + 1)/(x^2 + x + 1)", new[] { 0.3, 1.7, -0.6 })]
        public void TheLogarithmicDerivative(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // The candidates the substitution rule no longer offers for a rational function --
        // a sum, the base of a written power below the bar, and a power of x that the
        // exponents say cannot be exact -- answered all the same, and the cases the
        // restriction is for. `x/(x^6 + 1)` and `x^2/(x^6 + 1)` are the powers that are exact,
        // u = x^2 and u = x^3, and stay candidates.
        [Theory]
        [InlineData("(1 + x^2)/((sqrt(2) - 1)*x^2 + 2*x + 1 + sqrt(2))", new[] { 0.3, 1.7, 3.2 })]
        [InlineData("x*(x^2 + 1)^3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("(2*x + 1)/(x^2 + x + 1)^3", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("x/(x^6 + 1)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("x^2/(x^6 + 1)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("x^5/(x^4 + 1)", new[] { 0.3, 1.7, -0.6 })]
        [InlineData("x^3/(x^4 + 1)", new[] { 0.3, 1.7, -0.6 })]
        public void ARationalFunctionWithoutSumCandidates(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // A denominator that factors over Q with no rational root anywhere in it, which the
        // split at a root cannot get a foothold on. x^4 + 3x^2 + 2 is (x^2 + 1)(x^2 + 2) and
        // x^4 + 4 is (x^2 - 2x + 2)(x^2 + 2x + 2); the split at a coprime pair of factors
        // reaches both. https://github.com/asc-community/AngouriMath/issues/919
        [Theory]
        [InlineData("1 / (x ^ 4 + 3 * x ^ 2 + 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 4 + 4)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void ADenominatorThatFactorsWithNoRationalRoot(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // A repeated factor the spelling hides. Every split reads the denominator as written --
        // the Hermite reduction wants its repeated factor written as a power -- and
        // 1/(x^4 + 2x^2 + 1) was declined for that while 1/(x^2 + 1)^2 was answered. A
        // denominator whose written factors are not squarefree between them is written in its
        // irreducible factors over Q first, equal ones gathered into one power. The second
        // spelling of each pair is what Euler's substitution hands the rational integrator.
        [Theory]
        [InlineData("1 / (x ^ 4 + 2 * x ^ 2 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("(x ^ 2 + 1) / (x ^ 4 - 2 * x ^ 2 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / ((x ^ 2 + 1) * (x ^ 4 + 2 * x ^ 2 + 1))", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("(2 - 4 * x ^ 2 + 2 * x ^ 4) / ((-1 - x ^ 2) * (1 - 2 * x - 2 * x ^ 3 - x ^ 4))", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void ARepeatedFactorTheSpellingHides(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// A written base that is a polynomial spelled some other way -- what the
        /// linear-radical substitution makes of <c>1 + x + 2x^2</c> under <c>u = sqrt(3 - 2x)</c>
        /// -- is written as its polynomial before the splits read it; and the Hermite
        /// reduction of a high power is built in coefficient arithmetic and solved by p-adic
        /// lifting, where the expander's term estimate declined the products and the
        /// Gauss-Jordan over the rationals took minutes: Welz's
        /// <c>1/((3 - 2x)^(11/2) (1 + x + 2x^2)^5)</c> and <c>1/((3 - 2x)^(21/2) (1 + x + 2x^2)^10)</c>,
        /// the latter a system of sixty-three unknowns.
        /// </summary>
        [Theory]
        [InlineData("-8 / (u ^ 6 * (2 + (u ^ 2 - 3) ^ 2 - (u ^ 2 - 3)) ^ 3)", new[] { 0.3, 0.7, 1.4, 2.2, -1.1 })]
        [InlineData("1 / (u ^ 10 * (u ^ 4 - 7 * u ^ 2 + 14) ^ 5)", new[] { 0.3, 0.7, 1.4, 2.2, -1.1 })]
        [InlineData("1 / (u ^ 20 * (u ^ 4 - 7 * u ^ 2 + 14) ^ 10)", new[] { 0.7, 1.4, 2.2, -1.1 })]
        public void AHighPowerOfAWrittenBase(string integrand, double[] points)
        {
            var f = integrand.ToEntity();
            var antiderivative = f.Integrate("u");
            Assert.DoesNotContain("integral(", antiderivative.Stringize());
            var derivative = antiderivative.Substitute("C", 0).Differentiate("u");
            foreach (var point in points)
            {
                var expected = f.Substitute("u", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("u", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.True(System.Math.Abs(expected - actual) <= 1e-7 * System.Math.Max(1, System.Math.Abs(expected)),
                    $"at {point}: {actual} for {expected}");
            }
        }

        // A denominator that does not factor over Q and is not a biquadratic either -- an odd
        // power puts it past the step that factors over the reals -- has its logarithms in a sum
        // over its roots (https://github.com/asc-community/AngouriMath/issues/1285). What is out
        // of reach is the same denominator with a symbol among its coefficients, which the sum
        // is not written for. Recorded so the boundary is visible rather than inferred from an
        // absence.
        //
        // x^2/(x^4 + 1) was the first entry here, on the grounds that x^4 + 1 is irreducible
        // over Q and only factors once real coefficients are allowed. Allowing them is what the
        // real-quadratic step now does, so it moved to PartialFractionsTest as an answer; and
        // 1/(x^4 + 2x^2 + 1), pinned here as a power of a single irreducible with no coprime
        // pair to split into, is (x^2 + 1)^2 and is answered above once written so.
        [Theory]
        [InlineData("1 / (x ^ 4 + x + 1)")]
        [InlineData("1 / (x ^ 4 + x ^ 3 + 1)")]
        public void ADenominatorThatDoesNotFactorIsASumOverItsRoots(string integrand)
        {
            Assert.Contains(integrand.ToEntity().Integrate("x").Nodes, node => node is Entity.SumOverSetf);
            AssertIsAntiderivative(integrand, 0.3, 1.7, 3.2, -2.4);
        }

        [Theory]
        [InlineData("1 / (x ^ 4 + a * x + 1)")]
        [InlineData("1 / (x ^ 3 + x + a)")]
        public void ADenominatorWithASymbolThatDoesNotFactorIsStillDeclined(string integrand) =>
            Assert.Contains("integral(", integrand.ToEntity().Integrate("x").Stringize());
    }
}
