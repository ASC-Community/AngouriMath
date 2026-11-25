//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Algebra.Polynomials
{
    /// <summary>
    /// <see cref="MathS.Polynomials"/> — that it answers, that it refuses where it must, and
    /// that the worked examples in its documentation are the output it actually produces.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class PolynomialSurfaceTest
    {
        /// <summary>
        /// The printed form is the assertion here, deliberately and against the usual rule.
        /// These are the examples in the XML documentation, and what is being tested is that a
        /// reader who runs one gets what the page says — so a change of form is exactly the
        /// failure this has to catch. Everything else in this file asserts the mathematics.
        /// </summary>
        [Theory]
        [InlineData("x ^ 4 - 5 * x ^ 2 + 4", "(x + 1) * (x + 2) * (x - 2) * (x - 1)")]
        [InlineData("x ^ 3 - 3 * x ^ 2 + 3 * x - 1", "(x - 1) ^ 3")]
        [InlineData("x ^ 2 + 1", "x ^ 2 + 1")]
        public void TheFactorExamplesPrintWhatTheDocumentationSays(string input, string expected)
            => Assert.Equal(expected, MathS.Polynomials.Factor(input, "x")?.Stringize());

        [Theory]
        [InlineData("x ^ 2 - 1", "x ^ 2 + 2 * x + 1", "x + 1")]
        [InlineData("x ^ 2 - y ^ 2", "x ^ 2 - 2 * x * y + y ^ 2", "x - y")]
        [InlineData("x ^ 2 + 1", "x ^ 2 + 2", "1")]
        public void TheGcdExamplesPrintWhatTheDocumentationSays(string left, string right, string expected)
            => Assert.Equal(expected, MathS.Polynomials.Gcd(left, right)?.Stringize());

        [Theory]
        [InlineData("x ^ 2 + y ^ 2 - 1", "x + y - 1", "y", "2 * x ^ 2 - 2 * x")]
        [InlineData("x ^ 2 - 1", "x - a", "x", "a ^ 2 - 1")]
        public void TheResultantExamplesPrintWhatTheDocumentationSays(
            string left, string right, string eliminate, string expected)
            => Assert.Equal(expected, MathS.Polynomials.Resultant(left, right, eliminate)?.Stringize());

        [Theory]
        [InlineData("a * x ^ 2 + b * x + c", "-4 * a * c + b ^ 2")]
        [InlineData("x ^ 3 - 3 * x + 1", "81")]
        [InlineData("x ^ 2 - 2 * x + 1", "0")]
        public void TheDiscriminantExamplesPrintWhatTheDocumentationSays(string input, string expected)
            => Assert.Equal(expected, MathS.Polynomials.Discriminant(input, "x")?.Stringize());

        [Theory]
        [InlineData("(x - 1) ^ 3 * (x + 2) ^ 2", "x ^ 2 + x - 2")]
        [InlineData("x ^ 2 + 1", "x ^ 2 + 1")]
        public void TheSquareFreePartExamplesPrintWhatTheDocumentationSays(string input, string expected)
            => Assert.Equal(expected, MathS.Polynomials.SquareFreePart(input, "x")?.Stringize());

        /// <summary>The factors multiply back to what they came from.</summary>
        [Theory]
        [InlineData("x ^ 4 - 5 * x ^ 2 + 4")]
        [InlineData("x ^ 6 - 1")]
        [InlineData("2 * x ^ 2 - 2")]
        [InlineData("x ^ 2 / 4 - 1")]
        [InlineData("x ^ 8 - 1")]
        [InlineData("x ^ 4 + 3 * x ^ 2 + 2")]
        [InlineData("x ^ 3 - 3 * x ^ 2 + 3 * x - 1")]
        [InlineData("x + 1")]
        [InlineData("x ^ 2 + 1")]
        public void AFactorisationIsTheSamePolynomial(string input)
        {
            var factored = MathS.Polynomials.Factor(input, "x");
            Assert.NotNull(factored);
            Assert.Equal(Integer.Create(0),
                (factored! - input.ToEntity()).Expand().Simplify());
        }

        /// <summary>The greatest common divisor divides both, and the quotients are coprime.</summary>
        [Theory]
        [InlineData("x ^ 2 - 1", "x ^ 2 + 2 * x + 1", "x + 1")]
        [InlineData("x ^ 3 - x", "x ^ 2 - x", "x ^ 2 - x")]
        [InlineData("x ^ 2 - y ^ 2", "x ^ 2 - 2 * x * y + y ^ 2", "x - y")]
        [InlineData("(x + y) * (x - y)", "(x + y) ^ 2", "x + y")]
        public void AGreatestCommonDivisorDividesBoth(string left, string right, string expected)
        {
            var divisor = MathS.Polynomials.Gcd(left, right);
            Assert.NotNull(divisor);
            Assert.Equal(Integer.Create(0), (divisor! - expected.ToEntity()).Expand().Simplify());
        }

        /// <summary>
        /// The resultant vanishes exactly where the two polynomials have a common root in the
        /// eliminated variable — checked by substituting the values that make it vanish back
        /// into the pair and solving.
        /// </summary>
        [Fact]
        public void TheResultantVanishesWhereThereIsACommonRoot()
        {
            // Res_x(x^2 - 1, x - a) = a^2 - 1, so the two share a root exactly at a = +-1.
            var resultant = MathS.Polynomials.Resultant("x ^ 2 - 1", "x - a", "x");
            Assert.NotNull(resultant);
            foreach (var value in new Entity[] { 1, -1 })
            {
                Assert.Equal(Integer.Create(0), resultant!.Substitute("a", value).Simplify());
                // and the shared root really is there
                var shared = "x ^ 2 - 1".ToEntity().SolveEquation("x");
                Assert.Contains(value, ((Set.FiniteSet)shared.InnerSimplified).ToArray());
            }
            foreach (var value in new Entity[] { 0, 2, -3 })
                Assert.NotEqual(Integer.Create(0), resultant!.Substitute("a", value).Simplify());
        }

        /// <summary>
        /// The discriminant vanishes exactly where the polynomial has a repeated root, and its
        /// sign counts the real roots of a cubic.
        /// </summary>
        [Theory]
        [InlineData("x ^ 2 - 2 * x + 1", 0)]
        [InlineData("x ^ 2 - 1", 1)]
        [InlineData("x ^ 2 + 1", -1)]
        [InlineData("x ^ 3 - 3 * x + 1", 1)]     // three real roots
        [InlineData("x ^ 3 - 2", -1)]            // one real root
        [InlineData("x ^ 3 - 3 * x + 2", 0)]     // a repeated root at 1
        public void TheDiscriminantHasTheSignItShould(string input, int sign)
        {
            var discriminant = MathS.Polynomials.Discriminant(input, "x");
            Assert.NotNull(discriminant);
            var value = (Real)discriminant!.EvalNumerical().RealPart;
            Assert.Equal(sign, value.EDecimal.Sign);
        }

        /// <summary>
        /// The square-free part has the same roots as what it came from, and each of them
        /// once — so it is what the original is divided by the greatest common divisor with
        /// its derivative, and its own discriminant no longer vanishes.
        /// </summary>
        [Theory]
        [InlineData("(x - 1) ^ 3 * (x + 2) ^ 2")]
        [InlineData("x ^ 4 - 2 * x ^ 2 + 1")]
        [InlineData("(x ^ 2 + 1) ^ 2")]
        public void ASquareFreePartHasNoRepeatedRoot(string input)
        {
            var part = MathS.Polynomials.SquareFreePart(input, "x");
            Assert.NotNull(part);
            var discriminant = MathS.Polynomials.Discriminant(part!, "x");
            Assert.NotNull(discriminant);
            Assert.NotEqual(Integer.Create(0), discriminant!.Simplify());
            // Every root of the original is a root of the square-free part.
            foreach (var root in (Set.FiniteSet)input.ToEntity().SolveEquation("x").InnerSimplified)
                Assert.Equal(Integer.Create(0), part!.Substitute("x", root).Simplify());
        }

        /// <summary>
        /// A polynomial in several variables is factored by Kronecker's substitution, written in
        /// mixed radix: with radices <c>d_i + 1</c> and place values <c>s_0 = 1</c>,
        /// <c>s_(i+1) = s_i * (d_i + 1)</c>, the map sending a monomial to
        /// <c>t^(sum of e_i * s_i)</c> writes each exponent as one digit of a numeral, so it is
        /// injective on every monomial that can appear in the polynomial or in any of its
        /// factors. A factorisation of the one-variable image reads back digit by digit, and each
        /// subset of its irreducible factors names a candidate. Every candidate is checked by
        /// exact division, so the failure mode is a refusal and not a wrong answer.
        /// </summary>
        /// <remarks>
        /// <c>x ^ 2 - y ^ 2</c> is the case #746 item 43 names. The three- and four-variable rows
        /// are the generalisation: the exponent vector is a numeral whatever its length, and only
        /// the image's degree — a product of the radices, not a sum — decides what fits.
        /// Compared numerically, for the reason the content test above gives.
        /// </remarks>
        [Theory]
        [InlineData("x ^ 2 - y ^ 2", 2)]
        [InlineData("x ^ 2 + 2 * x * y + y ^ 2", 1)]
        [InlineData("x ^ 3 - y ^ 3", 2)]
        [InlineData("x ^ 2 * y ^ 2 - 1", 2)]
        [InlineData("x ^ 4 - y ^ 4", 3)]
        [InlineData("x ^ 2 - y ^ 2 + 2 * x + 1", 2)]
        [InlineData("x ^ 2 - (y + z) ^ 2", 2)]
        [InlineData("x ^ 2 + 2 * x * y + y ^ 2 - z ^ 2", 2)]
        [InlineData("x * y - x - y + 1", 2)]
        [InlineData("(x + y) * (x + z + w)", 2)]
        [InlineData("(x + y) * (x + z) * (x + w)", 3)]
        public void APolynomialInSeveralVariablesIsFactored(string input, int distinctFactors)
        {
            var expr = input.ToEntity();
            var factored = MathS.Polynomials.Factor(expr, "x");
            Assert.NotNull(factored);
            Assert.NotEqual(expr, factored);

            // As many distinct factors as the mathematics has, so a partial factorisation
            // reported as a whole one fails rather than passing quietly.
            Assert.Equal(distinctFactors, CountFactors(factored!));

            var variables = expr.Vars.Concat(factored!.Vars).Distinct().ToArray();
            var random = new Random(20260825);
            for (var trial = 0; trial < 20; trial++)
            {
                Entity before = expr, after = factored;
                foreach (var variable in variables)
                {
                    Entity value = Math.Round(random.NextDouble() * 6 - 3, 4);
                    before = before.Substitute(variable, value);
                    after = after.Substitute(variable, value);
                }
                Assert.Equal(
                    before.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    after.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    9);
            }
        }

        private static int CountFactors(Entity product)
            => product is Mulf(var left, var right) ? CountFactors(left) + CountFactors(right) : 1;

        /// <summary>
        /// The image's degree is a <b>product</b> of the radices and not a sum, so the ceiling
        /// closes quickly as variables are added — and where it closes the answer is a refusal.
        /// </summary>
        /// <remarks>
        /// Every row here does factor mathematically and is declined anyway, which is the shape
        /// of every limit in this path: a refusal is a possible answer and a wrong one is not.
        /// The degrees are <c>(2, 2, 1, 1)</c> and <c>(4, 1, 1, 1, 1)</c>, giving images of
        /// degree 35 and 79 against an <c>IntegerPolynomial.MaxDegree</c> of 32; the third is
        /// two variables and past it on its own.
        /// </remarks>
        /// <summary>
        /// A polynomial whose primitive part does not factor still has its content taken out, and
        /// an irreducible one comes back as itself rather than as a refusal.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The substitution answers "this does not factor" by *proving* it: a factorisation into
        /// two parts of positive degree in the main variable maps to a splitting of the
        /// one-variable image, and every splitting of the image is one of the subsets the
        /// recombination tries. Reporting that proof as <see langword="null"/> threw the content
        /// away with it — <c>x * y + y * z</c> was refused although <c>y * (x + z)</c> is a
        /// factorisation this path had already found half of.
        /// </para>
        /// <para>
        /// It also disagreed with the one-variable path, which has always handed back
        /// <c>Factor("x ^ 2 + 1", "x")</c> as <c>x ^ 2 + 1</c>.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData("x * y + y * z", "y * (x + z)")]
        [InlineData("a * x + a * y + a * z", "a * (x + y + z)")]
        [InlineData("x ^ 2 * y - y ^ 3", "y * (x + y) * (x - y)")]
        [InlineData("x ^ 2 + y ^ 2", "x ^ 2 + y ^ 2")]
        [InlineData("x + y", "x + y")]
        [InlineData("y * (x ^ 2 + y ^ 2)", "y * (x ^ 2 + y ^ 2)")]
        [InlineData("x ^ 2 - a", "x ^ 2 - a")]
        [InlineData("x * y + z", "x * y + z")]
        public void AnIrreducibleRestIsAnAnswerAndKeepsItsContent(string input, string expected)
        {
            var factored = MathS.Polynomials.Factor(input.ToEntity(), "x");
            Assert.NotNull(factored);

            // Numerically rather than as a string, for the reason the content test above gives:
            // Simplify does not prove a factored form equal to its expansion.
            var expr = input.ToEntity();
            var wanted = expected.ToEntity();
            var variables = expr.Vars.Concat(factored!.Vars).Concat(wanted.Vars).Distinct().ToArray();
            var random = new Random(20260825);
            for (var trial = 0; trial < 20; trial++)
            {
                Entity got = factored, want = wanted;
                foreach (var variable in variables)
                {
                    Entity value = Math.Round(random.NextDouble() * 6 - 3, 4);
                    got = got.Substitute(variable, value);
                    want = want.Substitute(variable, value);
                }
                Assert.Equal(
                    want.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    got.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    9);
            }
        }

        /// <summary>
        /// The rows that used to sit above, moved here because they factor now — which is what
        /// that test's remark asked for when it said it pins the refusal and not the limit.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Hensel lifting along an evaluation homomorphism, in two variables. The substitution's
        /// image over-factors — <c>x ^ 7 - y ^ 7</c> becomes <c>t ^ 7 (1 - t ^ 49)</c>, whose
        /// factors are cyclotomic — and an evaluation image does not: at <c>y = 1</c> it is
        /// <c>x ^ 7 - 1</c>, which has the two factors the answer has.
        /// </para>
        /// <para>
        /// Checked by <b>value</b> rather than by spelling. What matters is that the product is
        /// the polynomial and that it is a product at all; which arrangement of terms comes back
        /// is the polynomial representation's business, and asserting on it would make this fail
        /// for a reason that is not about factoring.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData("x ^ 7 - y ^ 7", 2)]
        [InlineData("x ^ 6 - y ^ 6", 4)]
        [InlineData("(x ^ 8 + y) * (x ^ 8 + 3 * y)", 2)]
        [InlineData("(x ^ 2 + y ^ 5) * (x ^ 2 - y ^ 5)", 2)]
        [InlineData("x ^ 12 - y ^ 12", 6)]
        [InlineData("x ^ 2 - y ^ 2", 2)]
        [InlineData("x ^ 3 - y ^ 3", 2)]
        public void AnEvaluationImageIsLiftedBackToAFactorisation(string input, int pieces)
        {
            var factored = MathS.Polynomials.Factor(input.ToEntity(), "x");
            Assert.NotNull(factored);
            // It is a product, so something was actually found.
            Assert.IsType<Mulf>(factored);
            // And the product is the polynomial it came from -- which the factoriser itself
            // checks by exact division, so this is the same claim made a second way.
            Assert.Equal(Integer.Zero, (factored! - input.ToEntity()).Simplify());
            Assert.Equal(pieces, Mulf.LinearChildren(factored!).Count());
        }

        /// <summary>
        /// A leading coefficient in <c>x</c> that is a polynomial in <c>y</c> — Wang's
        /// leading-coefficient problem, which the lift declined until it was made not to arise.
        /// </summary>
        /// <remarks>
        /// <c>L^(n-1) f(z/L, y)</c> is a polynomial and is monic in <c>z</c>, so it has a
        /// constant leading coefficient and the lift already handles it; a factor comes back as
        /// <c>h(L·x, y)</c> with the content that substitution introduced divided out. Checked
        /// by value: the product has to be the polynomial, whatever spelling comes back.
        /// </para>
        /// <para>
        /// <b>Every row is past the substitution's reach as well as having a leading coefficient
        /// that is a polynomial</b>, and both conditions are needed for the row to be about this.
        /// A small one like <c>(y x + 1)(x + y)</c> has the leading coefficient but Kronecker
        /// already factors it, so it would pass without any of this code — which is what the
        /// first version of this test was made of.
        /// </remarks>
        [Theory]
        [InlineData("(y * x3 + 1) * (x4 - y3)", 2)]
        [InlineData("(y * x + 1) * (x7 - y7)", 3)]
        [InlineData("(y * x2 + 1) * (x6 - y6)", 5)]
        public void ALeadingCoefficientThatIsAPolynomialIsMadeMonic(string input, int pieces)
        {
            var factored = MathS.Polynomials.Factor(input.ToEntity(), "x");
            Assert.NotNull(factored);
            Assert.IsType<Mulf>(factored);
            Assert.Equal(Integer.Zero, (factored! - input.ToEntity()).Simplify());
            Assert.Equal(pieces, Mulf.LinearChildren(factored!).Count());
        }

        /// <summary>
        /// The monicisation's own bound. It multiplies by <c>L^(n-1-i)</c>, so the degree it
        /// adds is <c>(n-1)·deg L</c>, and past twelve the cost stops being milliseconds:
        /// this row factors correctly in <b>63 seconds</b> unbounded and refuses in three.
        /// </summary>
        /// <remarks>
        /// A refusal is a legitimate answer and a sixty-three second one is not, which is the
        /// same judgement <c>MaxImageFactors</c> makes about the recombination. The row is here
        /// rather than deleted so that a bound which stops being needed fails rather than
        /// quietly costing reach.
        /// </remarks>
        [Theory]
        [InlineData("(y2 * x3 + 1) * (x5 - y2)")]
        public void PastTheMonicisationsGrowthBoundItRefuses(string input)
            => Assert.Null(MathS.Polynomials.Factor(input.ToEntity(), "x"));

        /// <summary>
        /// The property a factorisation has: multiplying the factors gives back what was
        /// factored. Nothing checked it, and a constant content was being dropped — `4x² - 4y²`
        /// came back as `(x + y)(x - y)`.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1092">#1092</a>
        /// </summary>
        /// <remarks>
        /// Everything else in this layer is checked by exact division, but that check is on the
        /// individual <i>factors</i>; nothing compared the **assembled product** against the
        /// input, so a constant lost during assembly was lost silently. This asserts on the
        /// value rather than on the spelling, which is the only way to catch that.
        /// </remarks>
        [Theory]
        [InlineData("4 * x^2 - 4 * y^2")]
        [InlineData("2 * x^2 - 2 * y^2")]
        [InlineData("6 * x^4 - 6 * y^4")]
        [InlineData("2 * x^7 - 2 * y^7")]
        [InlineData("3 * x * y + 3 * y")]
        [InlineData("5 * x^2 * y - 5 * y")]
        [InlineData("x^2 - y^2")]
        [InlineData("x^12 - y^12")]
        [InlineData("x^2 + y^2 + 1")]
        [InlineData("-2 * x^2 + 2 * y^2")]
        [InlineData("x^2/2 - y^2/2")]
        public void AFactorisationMultipliesBackToWhatItFactored(string input)
        {
            var original = input.ToEntity();
            var factored = MathS.Polynomials.Factor(original, "x");
            Assert.NotNull(factored);
            Assert.Equal(Integer.Zero, (factored! - original).Simplify());
        }

        /// <summary>
        /// <c>Factorize</c> reaches inside a product the rules already made, so a numeric content
        /// taken out does not strand the remainder unfactored.
        /// </summary>
        /// <remarks>
        /// The rules take the content out and hand back <c>2 * (x ^ 3 - 1)</c>; the polynomial
        /// layer is what splits the remainder. Declining a product outright — which is how this
        /// first shipped — keeps the rules' answers but leaves exactly the shape
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1018">#1018</a> is about
        /// sitting inside one. Each factor is asked separately instead, so every factor the rules
        /// found survives and the ones they could not split do.
        /// </remarks>
        [Theory]
        [InlineData("2 * x^3 - 2", 3)]
        [InlineData("3 * x^6 - 3", 5)]
        [InlineData("5 * x^7 - 5", 3)]
        [InlineData("y * (x^3 - 1)", 3)]
        public void FactorizeReachesInsideAProductTheRulesMade(string input, int pieces)
        {
            var factored = input.ToEntity().Factorize();
            Assert.Equal(pieces, Mulf.LinearChildren(factored).Count());
            // And it is still the polynomial it factored, which is the property #1092 was about.
            Assert.Equal(Integer.Zero, (factored - input.ToEntity()).Simplify());
        }

        /// <summary>
        /// Three or more variables, past the substitution's reach: the ones other than
        /// <c>x</c> are substituted into one, in mixed radix, and the lift in two variables
        /// factors what that leaves.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every row was refused before. <c>x ^ 3 - (y + z) ^ 3</c> stood in a test of its own
        /// whose remark said a row that starts factoring belongs in a theory like this one, and
        /// the two products of linear factors stood in <c>PastTheSubstitutionsCeilingItRefuses</c>.
        /// The substitution there took in <c>x</c> as well and over-factored; here <c>x</c> is
        /// left alone, so the image is a polynomial in <c>x</c> and one other variable, and an
        /// evaluation image of that has the factors the answer has.
        /// </para>
        /// <para>
        /// Checked by value and by the number of pieces, as the two-variable rows above are:
        /// the product has to be the polynomial, and it has to be a product of as many
        /// irreducible factors as the polynomial has.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData("x ^ 3 - (y + z) ^ 3", 2)]
        [InlineData("x ^ 6 - (y + z) ^ 6", 4)]
        [InlineData("x ^ 10 - (y + z) ^ 10", 4)]
        [InlineData("x ^ 4 - (y * z) ^ 4", 3)]
        [InlineData("x ^ 8 - (y * z) ^ 8", 4)]
        [InlineData("(x + y + z + w) * (x - y)", 2)]
        [InlineData("(x + y) * (x + z) * (x + w) * (x + v)", 4)]
        [InlineData("(x + y + z) * (x - y + 2 * z) * (x + w)", 3)]
        [InlineData("(x ^ 3 + y ^ 2 * z + w) * (x ^ 2 - y * z ^ 2 + 1)", 2)]
        [InlineData("(x ^ 5 + y ^ 4 * z ^ 4 + y + z) * (x ^ 3 - y ^ 3 * z ^ 3 + 2)", 2)]
        [InlineData("(2 * x + y) * (3 * x - z * w) * (x + y * w)", 3)]
        public void ThreeOrMoreVariablesAreLiftedAsTwo(string input, int pieces)
        {
            var factored = MathS.Polynomials.Factor(input.ToEntity(), "x");
            Assert.NotNull(factored);
            Assert.IsType<Mulf>(factored);
            AgreeAtWholePoints(input.ToEntity(), factored!);
            Assert.Equal(pieces,
                Mulf.LinearChildren(factored!).Count(piece => piece.ContainsNode("x")));
        }

        /// <summary>
        /// A polynomial in three variables that the lift shows does not factor is handed back,
        /// which is an answer: a factorisation would survive the substitution, each factor
        /// keeping its degree in <c>x</c>, so an image that does not factor is a proof.
        /// </summary>
        [Theory]
        [InlineData("x ^ 2 + y ^ 2 + z ^ 2")]
        [InlineData("x ^ 2 * y ^ 2 * z ^ 2 + 1")]
        [InlineData("x ^ 3 + y * z + w")]
        public void ThreeVariablesThatDoNotFactorAreAnswered(string input)
        {
            var answered = MathS.Polynomials.Factor(input.ToEntity(), "x");
            Assert.NotNull(answered);
            Assert.IsNotType<Mulf>(answered);
            AgreeAtWholePoints(input.ToEntity(), answered!);
        }

        /// <summary>
        /// Two polynomials agreeing exactly at whole-number points, which is how a product of
        /// degree ten is compared with its expansion: <c>Simplify</c> of the difference does not
        /// come back as <c>0</c> at that size, and exact evaluation does not round.
        /// </summary>
        private static void AgreeAtWholePoints(Entity expected, Entity actual)
        {
            var variables = expected.Vars.Concat(actual.Vars).Distinct().ToArray();
            var random = new Random(20261125);
            for (var trial = 0; trial < 8; trial++)
            {
                Entity left = expected, right = actual;
                foreach (var variable in variables)
                {
                    Entity value = random.Next(-9, 10);
                    left = left.Substitute(variable, value);
                    right = right.Substitute(variable, value);
                }
                Assert.Equal(left.Evaled, right.Evaled);
            }
        }

        /// <summary>
        /// The bound on that substitution: its image's degree in the variable standing for the
        /// others is the product of their radices less one, and one exponent of the
        /// representation holds 127. <c>x ^ 7 - (y + z ^ 3) ^ 7</c> has degree 7 in <c>y</c>
        /// and 21 in <c>z</c>, so the image's degree is <c>8 * 22 - 1 = 175</c>.
        /// </summary>
        [Theory]
        [InlineData("x ^ 7 - (y + z ^ 3) ^ 7")]
        public void PastTheSubstitutionsCeilingItRefuses(string input)
            => Assert.Null(MathS.Polynomials.Factor(input.ToEntity(), "x"));

        /// <summary>
        /// The main variable is a parameter and not a convention: the same polynomial factored
        /// with respect to another variable is the same factorisation.
        /// </summary>
        [Fact]
        public void TheMainVariableIsAParameter()
        {
            var inX = MathS.Polynomials.Factor("x ^ 2 - y ^ 2".ToEntity(), "x");
            var inY = MathS.Polynomials.Factor("x ^ 2 - y ^ 2".ToEntity(), "y");
            Assert.NotNull(inX);
            Assert.NotNull(inY);
            Assert.Equal(inX!.Expand().Simplify(), inY!.Expand().Simplify());
        }

        /// <summary>
        /// The square-free part is <c>p / gcd(p, dp/dx)</c> whatever ring the coefficients live
        /// in, so it is not univariate for any reason but the representation it used to be
        /// written against. Each case here is built from known factors, and the answer has to be
        /// their product with every multiplicity dropped to one.
        /// </summary>
        /// <remarks>
        /// Compared numerically rather than as a string, and up to the content: the univariate
        /// path drops it — <c>SquareFreePart("4 * x ^ 2", "x")</c> is <c>x</c>, not <c>4 * x</c> —
        /// so <c>x ^ 2 * y ^ 2</c> is <c>x</c> for the same reason, with <c>y ^ 2</c> as the
        /// content. That is the existing convention rather than a new one.
        /// </remarks>
        [Theory]
        [InlineData("(x - y) ^ 2 * (x + y)", "(x - y) * (x + y)")]
        [InlineData("(x - y) ^ 3", "x - y")]
        [InlineData("(x + a) ^ 2 * (x + b)", "(x + a) * (x + b)")]
        [InlineData("x ^ 2 * y ^ 2", "x")]
        [InlineData("y ^ 2 * x ^ 2 * (x + 1)", "x * (x + 1)")]
        public void TheSquareFreePartIsTakenWhereTheCoefficientsArePolynomials(
            string input, string expected)
        {
            var part = MathS.Polynomials.SquareFreePart(input, "x");
            Assert.NotNull(part);
            var wanted = expected.ToEntity();
            var variables = wanted.Vars.Concat(part!.Vars).Distinct().ToArray();
            var random = new Random(20260825);
            for (var trial = 0; trial < 20; trial++)
            {
                Entity left = wanted, right = part;
                foreach (var variable in variables)
                {
                    Entity value = Math.Round(random.NextDouble() * 6 - 3, 4);
                    left = left.Substitute(variable, value);
                    right = right.Substitute(variable, value);
                }
                Assert.Equal(
                    left.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    right.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    9);
            }
        }

        /// <summary>
        /// Where the coefficients are polynomials and there is still nothing to say, the answer
        /// is a refusal rather than a guess.
        /// </summary>
        [Theory]
        [InlineData("y")]                            // constant in x
        [InlineData("sin(x) + 1")]                   // not a polynomial
        [InlineData("sin(y) * x ^ 2")]               // the coefficient is not a polynomial either
        public void ASquareFreePartOutsideTheLayerIsRefused(string input)
            => Assert.Null(MathS.Polynomials.SquareFreePart(input, "x"));

        /// <summary>
        /// A request the layer cannot settle is refused with <see langword="null"/>, and never
        /// answered with the input as though it were the result. Handing <c>x * y + y</c> back
        /// from a factorisation would say that <c>y * (x + 1)</c> does not exist, which is a
        /// wrong answer and not a graceful failure.
        /// </summary>
        /// <remarks>
        /// The rows are the two ways of not being asked a question this layer can answer: it is
        /// not a polynomial, or it is past the factoriser's degree bound. Returning the input
        /// where the substitution has *proved* the polynomial does not factor is a different
        /// thing and is asserted separately — see
        /// <see cref="AnIrreducibleRestIsAnAnswerAndKeepsItsContent"/>. What must never happen is
        /// the input coming back from a question that was never settled, which is what these
        /// rows pin.
        /// </remarks>
        [Theory]
        [InlineData("sin(x) + 1")]                   // not a polynomial
        [InlineData("sin(y) * x ^ 2 + 1")]           // nor is a coefficient of one
        [InlineData("x ^ 33 - 1")]                   // past the degree bound of the factoriser
        public void FactorisationRefusesRatherThanReturningTheInput(string input)
            => Assert.Null(MathS.Polynomials.Factor(input, "x"));

        /// <summary>
        /// A polynomial whose coefficients in <c>x</c> are polynomials themselves is refused by
        /// the factoriser, which works over the rationals -- but some of them do not need a bigger
        /// ring at all, only their common divisor taken out first. <c>x * y + y</c> is the example
        /// the refusal test above used to carry, with a comment saying that handing it back would
        /// claim <c>y * (x + 1)</c> does not exist.
        /// </summary>
        /// <remarks>
        /// Checked as a value rather than as a string: which arrangement of a product comes back
        /// is the printer's business, and asserting one pins something this test is not about.
        /// The factorisation is required to be a product that is not the input, and to agree with
        /// the input numerically -- <c>Simplify</c> does not prove <c>y * x * (x + 1)</c> equal to
        /// <c>x ^ 2 * y + x * y</c>, and the two are equal.
        /// </remarks>
        [Theory]
        [InlineData("x * y + y")]
        [InlineData("x ^ 2 * y + x * y")]
        [InlineData("a * x ^ 2 + a * x")]
        [InlineData("2 * x * y + 2 * y")]
        [InlineData("x ^ 2 * y ^ 2 - y ^ 2")]
        [InlineData("x ^ 3 * y - x * y")]
        [InlineData("x * y * z + y * z")]
        public void TheContentIsTakenOutSoTheRestCanBeFactorised(string input)
        {
            var expr = input.ToEntity();
            var factored = MathS.Polynomials.Factor(expr, "x");
            Assert.NotNull(factored);
            Assert.NotEqual(expr, factored);
            var variables = expr.Vars.Concat(factored!.Vars).Distinct().ToArray();
            var random = new Random(20260825);
            for (var trial = 0; trial < 20; trial++)
            {
                Entity before = expr, after = factored;
                foreach (var variable in variables)
                {
                    Entity value = Math.Round(random.NextDouble() * 6 - 3, 4);
                    before = before.Substitute(variable, value);
                    after = after.Substitute(variable, value);
                }
                Assert.Equal(
                    before.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    after.EvalNumerical().RealPart.EDecimal.ToDouble(),
                    9);
            }
        }

        /// <summary>Nine variables is one more than the packed monomial has room for.</summary>
        [Fact]
        public void NineVariablesAreRefused()
        {
            var eight = "a + b + c + d + f + g + h + k";
            var nine = eight + " + m";
            Assert.NotNull(MathS.Polynomials.Gcd(eight, eight + " + 1"));
            Assert.Null(MathS.Polynomials.Gcd(nine, nine + " + 1"));
            Assert.Null(MathS.Polynomials.Resultant(nine, nine + " + 1", "a"));
            Assert.Null(MathS.Polynomials.Discriminant(nine + " + a ^ 2", "a"));
        }

        /// <summary>
        /// Degree 128 is one past what a packed monomial can hold, so everything that goes
        /// through the multivariate representation declines rather than truncating.
        /// </summary>
        [Fact]
        public void ADegreeBeyondTheBoundIsRefused()
        {
            var degree128 = string.Join(" + ",
                Enumerable.Range(0, 129).Select(power => power == 0 ? "1" : $"x ^ {power}"));
            Assert.Null(MathS.Polynomials.Factor(degree128, "x"));
            Assert.Null(MathS.Polynomials.Discriminant(degree128, "x"));
            Assert.Null(MathS.Polynomials.SquareFreePart(degree128, "x"));
            Assert.Null(MathS.Polynomials.Gcd(degree128, degree128 + " + 1"));
        }

        /// <summary>Something that is not a polynomial at all is refused everywhere.</summary>
        [Fact]
        public void SomethingThatIsNotAPolynomialIsRefused()
        {
            Assert.Null(MathS.Polynomials.Gcd("sin(x)", "x"));
            Assert.Null(MathS.Polynomials.Resultant("sin(x)", "x", "x"));
            Assert.Null(MathS.Polynomials.Discriminant("1 / x", "x"));
            Assert.Null(MathS.Polynomials.SquareFreePart("e ^ x", "x"));
        }
    }
}
