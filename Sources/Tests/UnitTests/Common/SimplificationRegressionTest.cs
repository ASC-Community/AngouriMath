//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using PeterO.Numbers;
using System.Linq;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Regression tests for simplification, factoring and integration.
    /// Each test names the issue it locks down, so a future refactor that
    /// reintroduces the bug fails loudly.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class SimplificationRegressionTest
    {
        // https://github.com/asc-community/AngouriMath/issues/205
        // Radicands were never reduced to a square-free part, so like radicals could
        // never be collected: sqrt(12) + sqrt(27) stayed as written instead of
        // 5*sqrt(3). Pulling perfect powers out from under the root makes the terms
        // share a radical, and the existing collection rules take it from there.
        [Theory]
        [InlineData("sqrt(12) + sqrt(27)", "5 * sqrt(3)")]
        [InlineData("sqrt(8) + sqrt(2)", "3 * sqrt(2)")]
        [InlineData("sqrt(18) + sqrt(8)", "5 * sqrt(2)")]
        [InlineData("sqrt(50) - sqrt(2)", "4 * sqrt(2)")]
        [InlineData("sqrt(20) + sqrt(45) + sqrt(5)", "6 * sqrt(5)")]
        [InlineData("cbrt(54) + cbrt(2)", "4 * cbrt(2)")]
        public void Issue205_LikeRadicalsCollect(string input, string expected)
        {
            var simplified = input.ToEntity().Simplify();
            // Compared by difference, not by form: the collected result comes out as
            // `sqrt(3) * 5` where the expectation reads `5 * sqrt(3)`.
            Assert.Equal(Entity.Number.Integer.Create(0),
                (simplified - expected.ToEntity()).Simplify());
            // ...but it must genuinely have collected into a single term, which is the
            // point of the issue.
            Assert.DoesNotContain("+", simplified.Stringize());
            Assert.DoesNotContain("-", simplified.Stringize());
        }

        // https://github.com/asc-community/AngouriMath/issues/281
        // A standalone surd used to keep the form it was written in, because Simplify
        // picks between candidates by a complexity metric and by that metric `sqrt(12)`
        // beats `2 * sqrt(3)`. The metric is the wrong judge here: the reduced radicand
        // is the canonical form every other system prints -- sympy, Mathematica and
        // Maxima all answer 2*sqrt(3) -- and it is what makes two surds comparable
        // without simplifying their difference. So the reduction now happens where the
        // power itself is built rather than being offered as a candidate.
        [Theory]
        [InlineData("sqrt(12)", "2 * sqrt(3)")]
        [InlineData("sqrt(18)", "3 * sqrt(2)")]
        [InlineData("sqrt(1/2)", "sqrt(2) / 2")]
        [InlineData("(2 ^ 3 * 5 ^ 7) ^ (1/3)", "50 * 5 ^ (1/3)")]
        [InlineData("12 ^ (5/2)", "288 * sqrt(3)")]
        // A negative exponent is the same question asked of the reciprocal, and it has to give
        // the same answer: 1/sqrt(2) and sqrt(1/2) are one number and printed differently only
        // because one of them was declined.
        [InlineData("1 / sqrt(2)", "sqrt(2) / 2")]
        [InlineData("2 ^ (-1/2)", "sqrt(2) / 2")]
        [InlineData("1 / sqrt(12)", "sqrt(3) / 6")]
        [InlineData("1 / sqrt(8)", "sqrt(2) / 4")]
        [InlineData("1 / cbrt(2)", "4 ^ (1/3) / 2")]
        [InlineData("x / sqrt(2)", "x * sqrt(2) / 2")]
        [InlineData("8 ^ (-1/3)", "1/2")]
        public void Issue281_PerfectPowersComeOutFromUnderTheRoot(string input, string expected) =>
            Assert.Equal(Entity.Number.Integer.Create(0),
                (input.ToEntity().Simplify() - expected.ToEntity()).Simplify());

        // A negative radicand is not reduced: the extraction would have to choose a branch,
        // and which branch a negative base takes is already settled elsewhere -- the odd
        // roots take the real one, so (-8)^(1/3) is -2 and not 1 + i*sqrt(3). Reducing the
        // radicand under an even root would be picking that same choice by accident.
        [Fact]
        public void Issue281_NegativeRadicandsAreNotReduced() =>
            Assert.Equal("sqrt(-12)", "(-12) ^ (1/2)".ToEntity().Simplify().Stringize());

        // Whatever form comes out, it must still be the same number.
        [Theory]
        [InlineData("sqrt(8)")]
        [InlineData("sqrt(12) + sqrt(27)")]
        [InlineData("sqrt(1000)")]
        [InlineData("cbrt(54)")]
        [InlineData("sqrt(2)")]     // already square-free, must not change
        [InlineData("sqrt(4)")]     // perfect square
        [InlineData("1 / sqrt(8)")] // negative exponent
        public void Issue205_ReductionPreservesValue(string input)
        {
            var before = input.ToEntity().EvalNumerical().RealPart.EDecimal.ToDouble();
            var after = input.ToEntity().Simplify().EvalNumerical().RealPart.EDecimal.ToDouble();
            Assert.Equal(before, after, 10);
        }

        [Theory]
        [InlineData("sqrt(2)")]
        [InlineData("sqrt(3)")]
        [InlineData("sqrt(6)")]
        public void Issue205_SquareFreeRadicalsAreLeftAlone(string input) =>
            Assert.Equal(input.ToEntity(), input.ToEntity().Simplify());

        // https://github.com/asc-community/AngouriMath/issues/403
        // Fractions over a common denominator were cross-multiplied into d*d and left to
        // the simplifier to cancel back down. Since the rule calls Simplify on both halves
        // from inside Simplify's own pass, each further fraction squared the denominator
        // before anything cancelled: two terms took 25 ms, three never returned.
        [Theory]
        [InlineData("1 / (x + y + z) * dx + 1 / (x + y + z) * dy + 1 / (x + y + z) * dz", "(dx + dy + dz) / (x + y + z)")]
        [InlineData("a / (x + y) + b / (x + y) + c / (x + y)", "(a + b + c) / (x + y)")]
        [InlineData("a / (x + y + z) + b / (x + y + z) + c / (x + y + z) + d / (x + y + z)", "(a + b + c + d) / (x + y + z)")]
        public void Issue403_FractionsOverACommonDenominatorCollapse(string input, string expected) =>
            Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        // The general case still brings unlike denominators together, which needs the
        // numerator fully simplified to reach 0.
        [Fact]
        // The condition is the square-free part of (b - 1)(1 - b), which vanishes at the same
        // points, since #2137 offers the rational canonical form as a candidate.
        public void Issue403_UnlikeDenominatorsStillCombine() =>
            Assert.Equal("0 provided not b - 1 = 0".ToEntity(),
                "(a - 1) / (1 - b) - (1 - a) / (b - 1)".ToEntity().Simplify());

        // Three separate defects sat on top of each other in the integrator,
        // so each gets its own test below.
        //
        // 1. SolveAsPolynomialTerm, SolveLogarithmic and SolveBySubstitution recursed
        //    without handing on `integrateByParts`, so a solver re-enabled integration by
        //    parts one level below the call that had switched it off. By parts calls back
        //    into the dispatcher, so that is a cycle: x * ln(x) rode it into a stack
        //    overflow, and sin(x)^2 and cos(x)^2 spun past 20 seconds.
        [Theory]
        [InlineData("x * ln(x)")]
        [InlineData("sin(x) ^ 2")]
        [InlineData("cos(x) ^ 2")]
        public void IntegrationTerminates(string integrand)
        {
            var task = System.Threading.Tasks.Task.Run(() => integrand.ToEntity().Integrate("x"));
            Assert.True(task.Wait(LimitTermination.Guard),
                $"integrating {integrand} did not terminate");
        }

        // 2. Substitution candidates were not required to depend on x. `ln(e)`, picked out
        //    of any integrand carrying a logarithm, has derivative 0, and dividing by it
        //    gave a NaN that contained no x -- so it passed the "substitution succeeded"
        //    test and was returned as the antiderivative.
        [Theory]
        [InlineData("x * ln(x)")]
        [InlineData("ln(x)")]
        [InlineData("x * e ^ x")]
        public void AntiderivativesAreFreeOfNaN(string integrand) =>
            Assert.DoesNotContain("NaN", integrand.ToEntity().Integrate("x").Stringize());

        // 3. For a logarithm times a polynomial, by parts differentiated the polynomial,
        //    which leaves the logarithm to integrate and puts the original integral back
        //    in front of the solver. Differentiating the logarithm terminates instead.
        [Theory]
        [InlineData("x * ln(x)")]
        [InlineData("x ^ 2 * ln(x)")]
        [InlineData("ln(x) * x")]
        // and the orderings that already worked must keep working
        [InlineData("x * e ^ x")]
        [InlineData("x * sin(x)")]
        [InlineData("x ^ 2 * e ^ x")]
        public void IntegrationByPartsAnswersAreCorrect(string integrand)
        {
            var antiderivative = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", antiderivative.Stringize());
            // Differentiating back must return the integrand. The residual may carry the
            // domain conditions of the antiderivative, which are not what is under test.
            var residual = (antiderivative.Differentiate("x") - integrand.ToEntity()).Simplify();
            while (residual is Entity.Providedf(var inner, _)) residual = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), residual);
        }

        // https://github.com/asc-community/AngouriMath/issues/531
        // The factoring rules only ever looked at two adjacent terms of the sum tree, so a
        // sum of four was left half-factored at a*(c + d) + b*c + b*d.
        [Theory]
        [InlineData("a * c + a * d + b * c + b * d", "(a + b) * (c + d)")]
        [InlineData("a * b + a * c + a * d", "a * (b + c + d)")]
        [InlineData("x * a + x * b + y * a + y * b + z * a + z * b", "(a + b) * (x + y + z)")]
        public void Issue531_TermsCollectOverTheWholeSum(string input, string expected) =>
            Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        // Whatever form comes out, it has to be the same expression.
        [Theory]
        [InlineData("a * c + a * d + b * c + b * d")]
        [InlineData("x * a + x * b + y * a + y * b + z * a + z * b")]
        [InlineData("a * b + a * c + a * d")]
        public void Issue531_CollectionPreservesValue(string input)
        {
            var difference = (input.ToEntity() - input.ToEntity().Simplify()).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        // https://github.com/asc-community/AngouriMath/issues/178
        // Factorize left its own workings in the answer: the difference-of-squares rule
        // halved the exponents without checking they were even, so it fired again on the
        // linear factors it had just produced and turned x^2 - y^2 into
        // (sqrt(x) - sqrt(y)) * (sqrt(x) + sqrt(y)) * (x^1 + y^1). Nothing simplified the
        // result afterwards either, which is where the x^1 and the sqrt(4) survived.
        [Theory]
        [InlineData("4 * x ^ 2 - 4 * y ^ 2", "4 * (x - y) * (x + y)")]
        [InlineData("x ^ 2 - y ^ 2", "(x - y) * (x + y)")]
        [InlineData("x ^ 2 - 4", "(x - 2) * (x + 2)")]
        [InlineData("x ^ 4 - y ^ 4", "(x - y) * (x + y) * (x ^ 2 + y ^ 2)")]
        // Compared as printed text: what is under test is the form of the answer, and
        // the tree differs from the parsed expectation only in how the product associates.
        public void Issue178_FactorizeLeavesNoResidue(string input, string expected) =>
            Assert.Equal(expected, input.ToEntity().Factorize().Stringize());

        // Odd exponents must not be halved -- that is what introduced the radicals.
        [Fact]
        public void Issue178_OddPowersAreNotSplit() =>
            Assert.Equal("x ^ 3 - y ^ 5".ToEntity(), "x ^ 3 - y ^ 5".ToEntity().Factorize());

        [Theory]
        [InlineData("4 * x ^ 2 - 4 * y ^ 2")]
        [InlineData("x ^ 4 - y ^ 4")]
        [InlineData("x ^ 6 - y ^ 6")]
        [InlineData("x ^ 2 - 4")]
        public void Issue178_FactorizationPreservesValue(string input)
        {
            var difference = (input.ToEntity() - input.ToEntity().Factorize()).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        // https://github.com/asc-community/AngouriMath/issues/55
        // A quotient of polynomials only ever reached lowest terms where the common
        // factor was one the pattern rules could see written out. In one variable the
        // factoring rules found most of them; in several there was nothing at all, so
        // (x + y)^2 / ((x - y)(x + y)) came back long-divided as
        // 1 + (2y^2 + 2xy)/(x^2 - y^2) -- still carrying the factor it should have lost.
        [Theory]
        [InlineData("(x ^ 2 + 2 * x * y + y ^ 2) / (x ^ 2 - y ^ 2)", "(x + y) / (x - y)")]
        [InlineData("(x ^ 2 - 2 * x * y + y ^ 2) / (x ^ 2 - y ^ 2)", "(x - y) / (x + y)")]
        [InlineData("(a ^ 2 - b ^ 2) / (a ^ 2 + 2 * a * b + b ^ 2)", "(a - b) / (a + b)")]
        [InlineData("(x ^ 2 * y - y ^ 3) / (x * y + y ^ 2)", "x - y")]
        [InlineData("(x ^ 2 * y ^ 2 - 1) / (x * y - 1)", "x * y + 1")]
        [InlineData("(x ^ 3 - y ^ 3) / (x ^ 2 - y ^ 2)", "(x ^ 2 + x * y + y ^ 2) / (x + y)")]
        public void Issue55_MultivariateQuotientsReachLowestTerms(string input, string expected)
        {
            var simplified = input.ToEntity().Simplify();
            var bare = simplified;
            while (bare is Entity.Providedf(var inner, _)) bare = inner;

            // Equal as expressions, wherever both are defined...
            var difference = (bare - expected.ToEntity()).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);

            // ...and no bigger than the reduced form, which is what the issue is about:
            // the answer it complains of is the same number too, just not in lowest terms.
            Assert.True(bare.Nodes.Count() <= expected.ToEntity().Nodes.Count(), bare.Stringize());
        }

        // Cancelling widens the domain -- at x = -y the quotient was 0/0 where the
        // reduced form is 0 -- so the condition has to travel with the answer.
        [Fact]
        public void Issue55_CancellationKeepsTheDomainCondition()
        {
            var simplified = "(x ^ 2 + 2 * x * y + y ^ 2) / (x ^ 2 - y ^ 2)".ToEntity().Simplify();
            var atRemovedFactor = simplified.Substitute("x", 1).Substitute("y", -1).Evaled;
            Assert.Equal(MathS.NaN, atRemovedFactor);
        }

        // A common divisor that is not there must not be found. These are all coprime,
        // and the point of the check is that nothing is cancelled out of them.
        [Theory]
        [InlineData("(x + y) / (x - y)")]
        [InlineData("(x ^ 2 + y ^ 2) / (x + y)")]
        [InlineData("(x + 1) / (y + 1)")]
        public void Issue55_CoprimeQuotientsAreLeftAlone(string input) =>
            Assert.Equal(input.ToEntity(), input.ToEntity().Simplify());

        // The strongest thing that can be said about a computed divisor is that the
        // quotient still takes the same values. Sampled at whole points, where a divisor
        // that did not really divide shows up as a different number.
        [Theory]
        [InlineData("(x ^ 2 + 2 * x * y + y ^ 2) / (x ^ 2 - y ^ 2)")]
        [InlineData("(x ^ 3 - y ^ 3) / (x ^ 2 - y ^ 2)")]
        [InlineData("(a ^ 2 - b ^ 2) / (a ^ 2 + 2 * a * b + b ^ 2)")]
        [InlineData("(x ^ 4 - y ^ 4) / (x ^ 2 - 2 * x * y + y ^ 2)")]
        [InlineData("(x ^ 3 + x ^ 2 * y - x * y ^ 2 - y ^ 3) / (x ^ 2 - y ^ 2)")]
        [InlineData("(x ^ 2 * y ^ 2 - 1) / (x * y - 1)")]
        [InlineData("(6 * x ^ 2 - 6 * y ^ 2) / (4 * x + 4 * y)")]
        public void Issue55_CancellationPreservesValue(string input)
        {
            var original = input.ToEntity();
            var simplified = original.Simplify();
            var variables = original.Vars.OrderBy(variable => variable.Name).ToArray();
            foreach (var (first, second) in new[] { (3, 5), (7, 2), (-4, 9), (11, -6) })
            {
                Entity Bind(Entity expr) =>
                    expr.Substitute(variables[0], first).Substitute(variables[1], second);
                Assert.Equal(Bind(original).EvalNumerical(), Bind(simplified).EvalNumerical());
            }
        }

        // Dividing the integrand by a derivative that is zero gives NaN, and NaN
        // contains no x, which is exactly the test for a successful ubstitution --
        // so it was returned as the answer. The first guard caught the cases where
        // the derivative is visibly zero; these two are only zero after simplification,
        // since d/dx (sin(x)^2 + cos(x)^2) is written as 2sin(x)cos(x) - 2cos(x)sin(x).
        [Theory]
        [InlineData("sin(x) ^ 2 + cos(x) ^ 2")]
        [InlineData("x / (x + 1) + 1 / (x + 1)")]
        [InlineData("x * ln(x)")]
        public void AntiderivativesNeverContainNaN(string integrand) =>
            Assert.DoesNotContain("NaN", integrand.ToEntity().Integrate("x").Stringize());

        // https://github.com/asc-community/AngouriMath/issues/569
        // https://github.com/asc-community/AngouriMath/issues/179
        // The trigonometric tables were only ever read forwards, so sin(pi/4) was sqrt(2)/2
        // but arcsin(sqrt(2)/2) stayed as written. arctan was the exception, and only at
        // +-1, which is why arctan(1) already answered pi/4.
        [Theory]
        [InlineData("arcsin(1/2)", "pi / 6")]
        [InlineData("arcsin(sqrt(2) / 2)", "pi / 4")]
        [InlineData("arcsin(sqrt(3) / 2)", "pi / 3")]
        [InlineData("arcsin(1)", "pi / 2")]
        [InlineData("arcsin(0)", "0")]
        [InlineData("arccos(1/2)", "pi / 3")]
        [InlineData("arccos(0)", "pi / 2")]
        [InlineData("arccos(1)", "0")]
        [InlineData("arctan(sqrt(3))", "pi / 3")]
        [InlineData("arctan(sqrt(3) / 3)", "pi / 6")]
        [InlineData("arccotan(1)", "pi / 4")]
        [InlineData("arccotan(sqrt(3))", "pi / 6")]
        // The 5ths, 8ths, 10ths and 12ths of the circle, which are in the forward table too
        [InlineData("arcsin((sqrt(5) - 1) / 4)", "pi / 10")]
        [InlineData("arcsin(sqrt(2 - sqrt(2)) / 2)", "pi / 8")]
        [InlineData("arcsin((sqrt(6) - sqrt(2)) / 4)", "pi / 12")]
        [InlineData("arctan(2 - sqrt(3))", "pi / 12")]
        [InlineData("arctan(sqrt(2) - 1)", "pi / 8")]
        [InlineData("arctan(sqrt(5 - 2 * sqrt(5)))", "pi / 5")]
        // Both functions are odd, and arccos/arccotan take the complement
        [InlineData("arcsin(-1/2)", "-pi / 6")]
        [InlineData("arctan(-sqrt(3))", "-pi / 3")]
        [InlineData("arccos(-1/2)", "2 * pi / 3")]
        [InlineData("arccos(-1)", "pi")]
        public void Issue569_InverseTrigonometryReadsTheTableBackwards(string input, string expected) =>
            Assert.Equal(Entity.Number.Integer.Create(0),
                (input.ToEntity().Simplify() - expected.ToEntity()).Simplify());

        // A value that is merely near a table entry is not that entry, and one that is not in
        // the table at all has no closed form to give. `arcsin(0.4999999)` in particular must
        // not come back pi/6: the double comparison is a sieve, not the answer.
        [Theory]
        [InlineData("arcsin(4999999/10000000)", "arcsin")]
        [InlineData("arcsin(1/3)", "arcsin")]
        [InlineData("arctan(2)", "arctan")]
        [InlineData("arcsin(x)", "arcsin")]
        [InlineData("arcsin(2)", "arcsin")]           // outside the real branch
        public void Issue569_NonTableValuesAreLeftAlone(string input, string stillThere)
        {
            var simplified = input.ToEntity().Simplify().Stringize();
            Assert.Contains(stillThere, simplified);
            Assert.DoesNotContain("pi", simplified);
        }

        // And the angle that comes back must be the angle that was asked for.
        [Theory]
        [InlineData("arcsin(1/2)")]
        [InlineData("arccos(1/2)")]
        [InlineData("arctan(sqrt(3))")]
        [InlineData("arccotan(1)")]
        [InlineData("arcsin(-sqrt(2) / 2)")]
        [InlineData("arccos(-sqrt(3) / 2)")]
        public void Issue569_RecognisedAnglesKeepTheirValue(string input)
        {
            var before = input.ToEntity().EvalNumerical().RealPart.EDecimal.ToDouble();
            var after = input.ToEntity().Simplify().EvalNumerical().RealPart.EDecimal.ToDouble();
            Assert.Equal(before, after, 12);
        }

        // A negative factor under the line was taken out by multiplying by it and inverting
        // what was left, so a / (-b * c) came back as -(b * (c / a)) -- the reciprocal of the
        // right answer. Simplify keeps whichever candidate is shortest, so the inverted form
        // won wherever it was smaller, which is wherever the numerator was 1.
        //
        // Checked numerically as well as symbolically: an inverted answer differs from the
        // original by a factor, and subtracting the two can simplify to something that only
        // looks like zero if the same wrong rule fires on the difference.
        [Theory]
        [InlineData("(1/x) / (-1 - 1/x)", 2.0, -1.0 / 3)]
        [InlineData("(1/x) / (-1 - 1/x^2)", 2.0, -0.4)]
        [InlineData("1 / (x * (-1 - 1/x))", 2.0, -1.0 / 3)]
        [InlineData("(1/x) / (-1 - y^2/x^2)", 2.0, -0.4)]
        [InlineData("1 / (-2 * x)", 2.0, -0.25)]
        public void ANegativeFactorUnderTheLineStaysUnderIt(string input, double at, double expected)
        {
            var simplified = input.ToEntity().Simplify();
            var value = simplified.Substitute("x", at).Substitute("y", 1).EvalNumerical();
            Assert.True(System.Math.Abs(((Entity.Number.Complex)value).RealPart.EDecimal.ToDouble() - expected) < 1e-9,
                $"{input} simplified to {simplified.Stringize()}, which is {value.Stringize()} at x = {at}");
        }

        // Cancelling a quotient of factorials can leave a sum -- (x + 1)! / x! is x + 1 --
        // and SmartExpandOver, which is documented not to take a sum, was handed the result
        // and threw AngouriBugException out of a public method.
        // https://github.com/asc-community/AngouriMath/issues/817
        //
        // Asserted as a value rather than as a string: expanding must not change what the
        // expression is worth, and the point it is checked at is one where the factorials
        // are defined and the quotient is a whole number.
        [Theory]
        [InlineData("(x + 1)! / x!", 4, 5)]
        [InlineData("(x + 2)! / x!", 4, 30)]
        [InlineData("(x + 3)! / x!", 4, 210)]
        [InlineData("x! / (x + 1)!", 4, 0.2)]
        [InlineData("(x + 1)! / x! + y", 4, 9)]
        [InlineData("((x + 1)! / x!) * (a + b)", 4, 40)]
        public void ExpandKeepsTheValueOfAQuotientOfFactorials(string input, int at, double expected)
        {
            var expanded = input.ToEntity().Expand();
            var value = expanded.Substitute("x", at).Substitute("y", 4).Substitute("a", 3).Substitute("b", 5).EvalNumerical();
            var real = ((Entity.Number.Complex)value).RealPart.EDecimal.ToDouble();
            Assert.True(System.Math.Abs(real - expected) < 1e-9,
                $"{input} expanded to {expanded.Stringize()}, which is {value.Stringize()} at x = {at} rather than {expected}");
        }

        // The cancellation is what makes the expansion possible at all, so the answer should
        // no longer mention a factorial where the two cancel completely.
        [Theory]
        [InlineData("(x + 1)! / x!")]
        [InlineData("(x + 2)! / x!")]
        [InlineData("(x + y + 1)! / (x + y)!")]
        public void ExpandCancelsAQuotientOfFactorialsRatherThanLeavingIt(string input)
            => Assert.DoesNotContain(input.ToEntity().Expand().Nodes, node => node is Entity.Factorialf);

        // https://github.com/asc-community/AngouriMath/issues/876
        // There was one excluded-middle rule and it matched the negation on the left operand
        // only. `or` is commutative, so the same proposition had two answers depending on
        // which side it was written: `not (x < 0) or (x < 0)` was True while
        // `(x < 0) or not (x < 0)` was left as written. A bare variable hid it, because the
        // boolean minimiser reduces those whichever way round they are — it takes a
        // comparison, which the minimiser does not treat as an atom, to see it.
        //
        // The rows here are the propositions that hold unconditionally, which is why they can
        // be pinned to True outright: equality and set membership are decided everywhere on
        // the complex plane, and a boolean variable stands for something with a truth value.
        // The order comparisons that were also listed here moved to
        // <see cref="AnExhaustivePairOfComparisonsHoldsOverTheReals"/>, because `i < 0` has no
        // truth value and True is not their answer over the default codomain.
        [Theory]
        [InlineData("not (a = b) or (a = b)")]
        [InlineData("(a = b) or not (a = b)")]
        [InlineData("not (x in RR) or (x in RR)")]
        [InlineData("(x in RR) or not (x in RR)")]
        [InlineData("not p or p")]
        [InlineData("p or not p")]
        public void ExcludedMiddleHoldsWhicheverOperandCarriesTheNegation(string input)
            => Assert.Equal(Entity.Boolean.True, input.ToEntity().Simplify());

        // https://github.com/asc-community/AngouriMath/issues/876 §2
        // An order comparison need not have a truth value: the default codomain is
        // Domain.Complex and `i < 0` is NaN. The rules that decide two comparisons of the same
        // pair of operands did not say so — `x < 0 and x >= 0` reduced to False, which is the
        // answer over the reals, while at x = i the statement is NaN. So Simplify did not
        // commute with Substitute, silently. The reduction is right; what was missing is the
        // condition it holds under.
        //
        // This is asserted as a commutation rather than against a printed form so that it
        // holds whatever shape the condition takes.
        [Theory]
        [InlineData("x < 0 and x >= 0")]
        [InlineData("x > 0 and x <= 0")]
        [InlineData("x < 0 or x >= 0")]
        [InlineData("x <= 0 or x > 0")]
        [InlineData("x < x")]
        [InlineData("x >= x")]
        [InlineData("not (x < 0) or (x < 0)")]
        [InlineData("(x < 0) or not (x < 0)")]
        public void DecidingAPairOfComparisonsKeepsItsValueOffTheRealLine(string input)
        {
            var original = input.ToEntity();
            var atI = original.Substitute("x", "i").Evaled;
            // Stated rather than assumed: the whole point is that there is nothing to decide
            // here, so a change that gave `i < 0` a truth value would make this test vacuous.
            Assert.Equal(MathS.NaN, atI);
            Assert.Equal(atI, original.Simplify().Substitute("x", "i").Evaled);
        }

        // https://github.com/asc-community/AngouriMath/issues/880
        // `x < 0 and x = 0` was a row of the theory above until evaluation became Kleene, and it
        // no longer belongs there: at x = i one conjunct is *decidably* false -- `i = 0` is False,
        // not NaN -- and `False and u` is False whatever `u` is. So there is something to decide
        // here, and the value is False rather than NaN.
        //
        // Which left the pair disagreeing the other way round from #876 for a while: evaluation
        // settled the conjunction everywhere, while Simplify answered `False provided x in RR`,
        // whose condition is unnecessary for this row -- the reduction needs one conjunct to be
        // false, not both operands to be real. Evaluation now decides an equality against a
        // strict sign of the same quantity itself (https://github.com/asc-community/AngouriMath/issues/1414),
        // so the two agree: False, everywhere.
        [Fact]
        public void AConjunctionWithOneFalseConjunctIsFalseOffTheRealLineToo()
        {
            var original = "x < 0 and x = 0".ToEntity();
            Assert.Equal(Entity.Boolean.False, original.Substitute("x", "i").Evaled);
            Assert.Equal(Entity.Boolean.False, original.Simplify());
            Assert.Equal(Entity.Boolean.False, original.Simplify().Substitute("x", "i").Evaled);
        }

        // https://github.com/asc-community/AngouriMath/issues/876 §3
        // The unsatisfiable conjunction was decided and the valid disjunction was not, so the
        // library took the half of excluded middle that is unsound off the real line and
        // skipped the half that is sound on it. Over the reals both are decided outright, and
        // nothing else in the library read MathS.Settings.Codomain to find that out.
        [Theory]
        [InlineData("x < 0 or x >= 0")]
        [InlineData("x <= 0 or x > 0")]
        [InlineData("x <= 0 or x >= 0")]
        [InlineData("x > 0 or x <= 0")]
        [InlineData("x >= x")]
        [InlineData("not (x < 0) or (x < 0)")]
        [InlineData("(x < 0) or not (x < 0)")]
        [InlineData("not (x <= 0) or (x <= 0)")]
        [InlineData("(x <= 0) or not (x <= 0)")]
        public void AnExhaustivePairOfComparisonsHoldsOverTheReals(string input)
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal(Entity.Boolean.True, input.ToEntity().Simplify());
        }

        [Theory]
        [InlineData("x < 0 and x >= 0")]
        [InlineData("x > 0 and x <= 0")]
        [InlineData("x < 0 and x = 0")]
        [InlineData("x < x")]
        public void AContradictoryPairOfComparisonsFailsOverTheReals(string input)
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal(Entity.Boolean.False, input.ToEntity().Simplify());
        }

        // https://github.com/asc-community/AngouriMath/issues/884
        // arcsin is a left inverse of sin only on [-pi/2, pi/2], and the three siblings
        // likewise only on their principal intervals. The rewrite was unconditional, so
        // arcsin(sin(3)) simplified to 3 where the value is pi - 3 -- a wrong answer at an
        // ordinary real point, not a missing one.
        //
        // Asserted numerically because the point of the bug is the value: comparing the
        // simplified form against the original at a point outside the principal interval
        // is the only assertion that fails for the right reason.
        [Theory]
        [InlineData("arcsin(sin(x))", "3")]
        [InlineData("arcsin(sin(x))", "pi")]
        [InlineData("arcsin(sin(x))", "-2")]
        [InlineData("arccos(cos(x))", "4")]
        [InlineData("arccos(cos(x))", "-1")]
        [InlineData("arctan(tan(x))", "2")]
        [InlineData("arctan(tan(x))", "-2")]
        [InlineData("arccotan(cotan(x))", "-1")]
        public void SimplifyingAnInverseOfItsOwnFunctionKeepsTheValueOffThePrincipalBranch(
            string expression, string at)
        {
            var original = expression.ToEntity();
            var simplified = original.Simplify();
            var before = original.Substitute("x", at.ToEntity()).EvalNumerical();
            var after = simplified.Substitute("x", at.ToEntity()).EvalNumerical();
            Assert.True(Magnitude(before - after) < 1e-20,
                $"{expression} simplified to {simplified.Stringize()}, which at x = {at} is "
                + $"{after.Stringize()} rather than {before.Stringize()}");
        }

        // The cancellation is still wanted where the argument is inside the principal
        // interval, and there it has to stay exact rather than becoming a decimal. The
        // expectation is simplified too because "1/2" parses as a Divf rather than a
        // Rational (https://github.com/asc-community/AngouriMath/issues/873), and Entity
        // equality is structural; a decimal answer still fails, which is what this pins.
        [Theory]
        [InlineData("arcsin(sin(1/2))", "1/2")]
        [InlineData("arccos(cos(1/2))", "1/2")]
        [InlineData("arctan(tan(1/2))", "1/2")]
        [InlineData("arcsin(sin(0))", "0")]
        [InlineData("arctan(tan(-1/3))", "-1/3")]
        public void SimplifyingAnInverseOfItsOwnFunctionStaysExactOnThePrincipalBranch(
            string expression, string expected)
        {
            Assert.Equal(expected.ToEntity().Simplify(), expression.ToEntity().Simplify());
        }

        // The other direction composes the *right* inverse and needs no assumption at all:
        // sin(arcsin(z)) is z wherever arcsin(z) is defined. Pinned so that guarding the
        // unsound half does not take the sound half with it.
        [Theory]
        [InlineData("sin(arcsin(x))")]
        [InlineData("cos(arccos(x))")]
        [InlineData("tan(arctan(x))")]
        [InlineData("cotan(arccotan(x))")]
        public void ComposingAFunctionOverItsOwnInverseIsStillTheIdentity(string input)
        {
            Assert.Equal("x".ToEntity(), input.ToEntity().Simplify());
        }

        static double Magnitude(Entity difference) =>
            ((System.Numerics.Complex)difference.EvalNumerical()).Magnitude;

        // https://github.com/asc-community/AngouriMath/issues/902
        // log_b(a^c) = c * log_b(a) needs c * ln(a) to stay inside the strip Im in (-pi, pi] that
        // ln maps onto, and it was applied to anything at all. ln(e^x) came back as x, which at
        // x = 3*pi*i is 9.42i where the expression is pi*i -- e^(3*pi*i) being -1. The rewrite
        // wins on complexity, so it is what an ordinary caller gets.
        [Theory]
        [InlineData("ln(e^x)")]
        [InlineData("log(2, 2^x)")]
        [InlineData("ln(x^2)")]
        [InlineData("log(2, x^2)")]
        public void AnExponentIsNotPulledOutOfALogarithmOverAnUndecidedArgument(string expression) =>
            Assert.Equal(expression.ToEntity(), expression.ToEntity().Simplify());

        // The value is the point, so it is the value that is checked: at 3*pi*i the two forms
        // differ by the full turn the principal branch discards.
        [Fact]
        public void TheLogarithmOfAPowerKeepsItsValueOffTheRealLine()
        {
            var original = "ln(e^x)".ToEntity();
            var at = "3 * pi * i".ToEntity();
            Assert.Equal(original.Substitute("x", at).EvalNumerical(),
                original.Simplify().Substitute("x", at).EvalNumerical());
        }

        // Where both sides are decidable it still fires, and a real reading is enough to decide
        // it: under Domain.Real the exponent is real by the reading itself.
        [Theory]
        [InlineData("ln(e^3)", "3")]
        [InlineData("log(2, 2^5)", "5")]
        [InlineData("ln(e^(1/2))", "1/2")]
        public void AnExponentIsPulledOutWhereTheArgumentIsDecidable(string expression, string expected) =>
            Assert.Equal(expected.ToEntity().Simplify(), expression.ToEntity().Simplify());

        [Fact]
        public void ARealReadingDecidesTheExponent()
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            Assert.Equal("x".ToEntity(), "ln(e^x)".ToEntity().Simplify());
        }

        // https://github.com/asc-community/AngouriMath/issues/890
        // log_b(1) is ln(1)/ln(b), which is 0/ln(b) -- so 0 for every base except 1, where it
        // is 0/0. The rewrite answered 0 for any base at all, so log(1, 1) was 0 where every
        // division by zero in this library is NaN.
        [Fact]
        public void LogarithmOfOneIsZeroOnlyWhereTheBaseIsNotOne()
        {
            Assert.Equal(MathS.NaN, "log(1, 1)".ToEntity().Simplify());
            Assert.Equal(MathS.NaN, "log(1, 1)".ToEntity().EvalNumerical());
        }

        [Theory]
        [InlineData("log(2, 1)")]
        [InlineData("log(1/2, 1)")]
        [InlineData("log(e, 1)")]
        public void LogarithmOfOneStillCollapsesForAnOrdinaryBase(string expression) =>
            Assert.Equal(Entity.Number.Integer.Create(0), expression.ToEntity().Simplify());

        // A symbolic base cannot be placed away from 1, so the answer carries the condition --
        // and here a condition is right, because at b = 1 the expression really is undefined
        // rather than merely different.
        [Fact]
        public void LogarithmOfOneOverASymbolCarriesItsCondition()
        {
            var simplified = "log(x, 1)".ToEntity().Simplify();
            Assert.NotEqual(Entity.Number.Integer.Create(0), simplified);
            Assert.Equal(MathS.NaN, simplified.Substitute("x", 1).EvalNumerical());
        }

        // log_b(0) is ln(0)/ln(b) = -oo/ln(b), whose sign follows the sign of ln(b): -oo above
        // 1 and +oo between 0 and 1. It answered -oo for every base.
        [Theory]
        [InlineData("log(2, 0)", "-oo")]
        [InlineData("log(3, 0)", "-oo")]
        [InlineData("log(1/2, 0)", "+oo")]
        [InlineData("log(1/3, 0)", "+oo")]
        public void LogarithmOfZeroFollowsTheSignOfItsBase(string expression, string expected) =>
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());

        // For a base whose side of 1 cannot be read there is no signed answer to give, so the
        // node is left as written rather than answered with one of the two.
        [Fact]
        public void LogarithmOfZeroOverASymbolIsLeftAlone() =>
            Assert.Equal("log(x, 0)".ToEntity(), "log(x, 0)".ToEntity().Simplify());

        // This library's arccotan is arctan(1/x) extended with arccotan(0) = pi/2, so its
        // range is (-pi/2, pi/2] and not the (0, pi) some texts use. #884 guarded
        // arccotan(cotan(x)) with [0, pi] on the assumption it was the latter, which left the
        // wrong answer in place above pi/2 -- arccotan(cotan(2)) is -1.1416 and simplified to
        // 2 -- and refused the rewrite below zero, where it is correct.
        // The argument has to be a number *before* Simplify runs, because that is what the
        // guard reads: a symbolic argument leaves the node alone and would pass whatever the
        // interval said. Every one of these is a composition whose value the rewrite must not
        // change, at points inside and outside each principal range.
        [Theory]
        [InlineData("arccotan(cotan(2))")]
        [InlineData("arccotan(cotan(3))")]
        [InlineData("arccotan(cotan(-2))")]
        [InlineData("arccotan(cotan(-1/2))")]
        [InlineData("arccotan(cotan(1/2))")]
        [InlineData("arcsin(sin(3))")]
        [InlineData("arcsin(sin(-3))")]
        [InlineData("arcsin(sin(1/2))")]
        [InlineData("arccos(cos(4))")]
        [InlineData("arccos(cos(-1))")]
        [InlineData("arccos(cos(2))")]
        [InlineData("arctan(tan(2))")]
        [InlineData("arctan(tan(-2))")]
        [InlineData("arctan(tan(1/2))")]
        public void CancellingAnInverseOverANumberKeepsTheValue(string expression)
        {
            var original = expression.ToEntity();
            var simplified = original.Simplify();
            Assert.True(Magnitude(original.EvalNumerical() - simplified.EvalNumerical()) < 1e-20,
                $"{expression} simplified to {simplified.Stringize()}, which is "
                + $"{simplified.EvalNumerical().Stringize()} rather than "
                + $"{original.EvalNumerical().Stringize()}");
        }

        // Inside the range it must still cancel, and exactly. -1/2 is in it and 2 is not,
        // which is the half the old interval had backwards.
        [Theory]
        [InlineData("arccotan(cotan(1/2))", "1/2")]
        [InlineData("arccotan(cotan(-1/2))", "-1/2")]
        [InlineData("arccotan(cotan(-1))", "-1")]
        public void SimplifyingArccotanOfCotanStaysExactInsideItsRange(string expression, string expected)
        {
            Assert.Equal(expected.ToEntity().Simplify(), expression.ToEntity().Simplify());
        }

        // arctan(x) + arccotan(x) is pi/2 for x >= 0 and -pi/2 for x < 0, by the same range.
        // It was pi/2 unconditionally.
        [Theory]
        [InlineData("3", "pi / 2")]
        [InlineData("1/2", "pi / 2")]
        [InlineData("0", "pi / 2")]
        [InlineData("-3", "-pi / 2")]
        [InlineData("-1/2", "-pi / 2")]
        public void ArctanPlusArccotanFollowsTheSignOfItsArgument(string at, string expected)
        {
            var sum = $"arctan({at}) + arccotan({at})".ToEntity().Simplify();
            Assert.True(Magnitude(sum - expected.ToEntity()) < 1e-20,
                $"arctan({at}) + arccotan({at}) simplified to {sum.Stringize()}, not {expected}");
        }

        // A symbolic argument has no decidable sign, so the sum is left as written rather than
        // answered for one sign of it.
        [Fact]
        public void ArctanPlusArccotanOfASymbolIsLeftAlone()
        {
            var simplified = "arctan(x) + arccotan(x)".ToEntity().Simplify();
            Assert.NotEqual(MathS.pi / 2, simplified);
            Assert.NotEqual(-MathS.pi / 2, simplified);
        }

        // https://github.com/asc-community/AngouriMath/issues/881
        // |x| is x where the argument is a non-negative real and -x where it is negative, which
        // is the definition of abs rather than an identity needing an assumption. Only a Number
        // folded, so abs(-sqrt(6)) and abs(-pi) came back exactly as written, and a concrete
        // quadratic inequality was answered with abs(-sqrt(6)) / 2 as an endpoint.
        [Theory]
        [InlineData("abs(-sqrt(6))", "sqrt(6)")]
        [InlineData("abs(sqrt(6))", "sqrt(6)")]
        [InlineData("abs(-pi)", "pi")]
        [InlineData("abs(-e)", "e")]
        [InlineData("abs(1 - sqrt(2))", "sqrt(2) - 1")]
        [InlineData("abs(-sqrt(6)) / 2", "sqrt(6) / 2")]
        public void AbsoluteValueFoldsWhereTheSignOfTheArgumentIsDecidable(string expression, string expected)
        {
            var simplified = expression.ToEntity().Simplify();
            Assert.DoesNotContain(simplified.Nodes, node => node is Entity.Absf);
            Assert.True(Magnitude(simplified - expected.ToEntity()) < 1e-20,
                $"{expression} simplified to {simplified.Stringize()}, not {expected}");
        }

        // An argument off the real line is neither itself nor its negation under abs: sqrt(-4)
        // is 2i, whose magnitude is 2. So the sign is read off the value, and a value that is
        // not real does not answer the question.
        [Theory]
        [InlineData("abs(sqrt(-4))", "2")]
        [InlineData("abs(-sqrt(-4))", "2")]
        public void AbsoluteValueOffTheRealLineIsTheMagnitude(string expression, string expected)
        {
            var simplified = expression.ToEntity().Simplify();
            Assert.True(Magnitude(simplified - expected.ToEntity()) < 1e-20,
                $"{expression} simplified to {simplified.Stringize()}, not {expected}");
        }

        // A symbol has no decidable sign, so nothing is assumed about it: abs(-a) is not a, and
        // it is not -a either. It stays as written.
        [Fact]
        public void AbsoluteValueOfASymbolIsLeftAlone()
        {
            var simplified = "abs(-a)".ToEntity().Simplify();
            Assert.Contains(simplified.Nodes, node => node is Entity.Absf);
        }
        // https://github.com/asc-community/AngouriMath/issues/892
        // |sgn(z)| and sgn(|z|) are 1 for every z except 0, where both are 0 -- sgn(0) is 0,
        // which the comment above Signumf.InnerSimplify already said. Both rewrites answered 1
        // for any argument at all, so a symbolic one gave a value that is wrong at one point.
        //
        // A condition would be the wrong fix: at z = 0 the expression is defined and equal to
        // 0, so `1 provided not z = 0` would trade a wrong value for a wrong domain.
        [Theory]
        [InlineData("abs(sgn(x))")]
        [InlineData("sgn(abs(x))")]
        [InlineData("abs(sgn(x + y))")]
        public void ComposingAbsAndSignumOverASymbolIsLeftAlone(string expression) =>
            Assert.NotEqual(Entity.Number.Integer.Create(1), expression.ToEntity().Simplify());

        // Where the argument's vanishing can be decided, the answer is still given, and it is
        // the right one at zero.
        [Theory]
        [InlineData("abs(sgn(0))", "0")]
        [InlineData("sgn(abs(0))", "0")]
        [InlineData("abs(sgn(2))", "1")]
        [InlineData("sgn(abs(-3))", "1")]
        [InlineData("abs(sgn(-1/2))", "1")]
        [InlineData("sgn(abs(i))", "1")]
        public void ComposingAbsAndSignumOverANumberIsDecided(string expression, string expected) =>
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());

        // The value is what the bug was, so it is the value that is asserted: at x = 0 both
        // compositions are 0, and a rewrite to 1 is off by one there.
        [Theory]
        [InlineData("abs(sgn(x))")]
        [InlineData("sgn(abs(x))")]
        public void ComposingAbsAndSignumKeepsItsValueAtZero(string expression)
        {
            var original = expression.ToEntity();
            var simplified = original.Simplify();
            Assert.Equal(original.Substitute("x", 0).EvalNumerical(),
                simplified.Substitute("x", 0).EvalNumerical());
        }

        // https://github.com/asc-community/AngouriMath/issues/882
        // -(a - b) was turned into b - a for a whole expression and not for the same expression
        // inside another node, because the step came from Expand -- which is offered for the root
        // only and does not descend into an exponent, a function's argument or a matrix. So what a
        // caller got depended on where the subexpression sat. As a rule it runs everywhere.
        //
        // The printed form is the bug here, which is why it is what these assert: every one of
        // these was already the right *number*, written the long way round.
        [Theory]
        [InlineData("-(5 - sqrt(-11))", "sqrt(-11) - 5")]
        [InlineData("-(5 - sqrt(-11)) + y", "sqrt(-11) - 5 + y")]
        [InlineData("2 ^ (-(5 - sqrt(-11)))", "2 ^ (sqrt(-11) - 5)")]
        [InlineData("sgn(-(5 - sqrt(-11)))", "sgn(sqrt(-11) - 5)")]
        [InlineData("[[-(5 - sqrt(-11)), 1]]", "[[sqrt(-11) - 5, 1]]")]
        [InlineData("-(a - b)", "b - a")]
        public void ANegatedDifferenceIsTurnedRoundWhereverItSits(string expression, string expected) =>
            Assert.Equal(expected, expression.ToEntity().Simplify().Stringize());

        // And it is the same number afterwards, which the printed forms above do not check.
        [Theory]
        [InlineData("-(a - b)")]
        [InlineData("2 ^ (-(a - b))")]
        [InlineData("sgn(-(a - b))")]
        public void TurningANegatedDifferenceRoundKeepsItsValue(string expression)
        {
            var original = expression.ToEntity();
            var simplified = original.Simplify();
            foreach (var (a, b) in new[] { (2, 7), (-3, 5), (0, 0), (4, -1) })
                Assert.Equal(
                    original.Substitute("a", a).Substitute("b", b).EvalNumerical(),
                    simplified.Substitute("a", a).Substitute("b", b).EvalNumerical());
        }

        // The same issue's second half: Expand read its argument as a sum, and a matrix is not one,
        // so a matrix left by the "too complicated, return what came in" exit. Factorize and
        // Differentiate both descend, being built out of rewrite rules, so Expand was the odd one
        // out rather than matrices being held back deliberately.
        [Theory]
        [InlineData("[[(x+1)^2, 1]]", "[[1 + 2 * x + x ^ 2, 1]]")]
        [InlineData("[[(x+1)^2, (y+2)^2], [1, x]]", "[[1 + 2 * x + x ^ 2, 4 + 4 * y + y ^ 2], [1, x]]")]
        public void ExpandDescendsIntoAMatrix(string expression, string expected) =>
            Assert.Equal(expected, expression.ToEntity().Expand().Stringize());

        // What a user meets it through: EquationSystem.Solve returns a Matrix, so every entry of a
        // system's answer was left in the form the solver happened to build it in. Asserted by
        // node count rather than by form, since the point is that the entries got shorter.
        [Fact]
        public void ASystemsAnswerSimplifiesEntryByEntry()
        {
            var answer = MathS.Equations("x2 + y", "y - x - 3").Solve("x", "y");
            Assert.NotNull(answer);
            var simplified = answer!.Simplify();
            Assert.True(simplified.Complexity < answer.Complexity,
                $"the system's answer simplified to {simplified.Stringize()}, which is no shorter "
                + $"than the {answer.Stringize()} it came from");
        }

        // https://github.com/asc-community/AngouriMath/issues/880
        // A connective is no longer strict in NaN. `false and u` is false and `true or u` is true
        // whatever `u` is, so an operand with no truth value does not absorb an answer the truth
        // table settles without it. Simplify already answered this way -- `true or (true and
        // (x < 0))` is True -- while evaluation answered NaN, so the two contradicted each other.
        //
        // `i < 0` is the undefined operand throughout: the default codomain is Domain.Complex and
        // the complex numbers are not ordered.
        [Theory]
        [InlineData("(i < 0) and false", "false")]
        [InlineData("false and (i < 0)", "false")]
        [InlineData("(i < 0) or true", "true")]
        [InlineData("true or (i < 0)", "true")]
        [InlineData("false implies (i < 0)", "true")]
        [InlineData("(i < 0) implies true", "true")]
        public void AConnectiveSettlesWhatItsTruthTableSettles(string expression, string expected) =>
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Evaled);

        // And what the table does not settle stays unsettled: NaN means "this does not exist", so
        // a connective may not invent a value for it either.
        [Theory]
        [InlineData("(i < 0) and true")]
        [InlineData("(i < 0) or false")]
        [InlineData("not (i < 0)")]
        [InlineData("(i < 0) xor (i < 0)")]
        [InlineData("(i < 0) xor true")]
        [InlineData("(i < 0) implies false")]
        public void AConnectiveInventsNothingItCannotSettle(string expression) =>
            Assert.Equal(MathS.NaN, expression.ToEntity().Evaled);

        // Arithmetic stays strict, which is the reason this is opted into per node rather than
        // done in the shared helper for everything: a rule for a zero factor exists, and NaN * 0
        // must not reach it.
        [Theory]
        [InlineData("(0/0) * 0")]
        [InlineData("(0/0) + 1")]
        [InlineData("(0/0) - (0/0)")]
        [InlineData("(0/0) ^ 0")]
        public void ArithmeticIsStillStrictInNaN(string expression) =>
            Assert.Equal(MathS.NaN, expression.ToEntity().Evaled);

        // https://github.com/asc-community/AngouriMath/issues/897
        // A number is not a truth value, so a connective over one has no answer to give: 0 is not
        // false and 1 is not true. It used to report the same category error three different ways
        // -- NaN where the operator had to look at the number, a correct-by-short-circuit False
        // where it did not, and a rewritten `not 0` for `true xor 0` -- so what a caller got
        // depended on which operator and which operand position. NaN is the worst of the three: it
        // says the expression does not exist, where the truth is that it is not a proposition.
        [Theory]
        [InlineData("false and 0")]
        [InlineData("true and 0")]
        [InlineData("0 and true")]
        [InlineData("true or 0")]
        [InlineData("false or 0")]
        [InlineData("false xor 0")]
        [InlineData("true xor 0")]
        [InlineData("not 0")]
        [InlineData("0 xor 0")]
        [InlineData("true implies 0")]
        [InlineData("0 implies true")]
        [InlineData("true and 1")]
        [InlineData("true and 1/2")]
        [InlineData("true and i")]
        public void AConnectiveOverANumberHasNoAnswer(string expression)
        {
            var evaled = expression.ToEntity().Evaled;
            Assert.NotEqual(MathS.NaN, evaled);
            Assert.IsNotType<Entity.Boolean>(evaled);
            Assert.False(expression.ToEntity().EvaluableBoolean);
        }

        // A caller who insists on a boolean still gets the type error, at the layer where insisting
        // happens -- which is why declining here loses nothing that mattered.
        [Fact]
        public void InsistingOnABooleanOverANumberStillThrows() =>
            Assert.Throws<AngouriMath.Core.Exceptions.CannotEvalException>(
                () => "true and 0".ToEntity().EvalBoolean());

        // The distinction this rests on, and the one that makes it not a retreat from #880: NaN is
        // how this library spells "no truth value", and a connective settles what it can with one.
        // A number is a value of the wrong sort. The two must not be treated alike.
        [Theory]
        [InlineData("(0/0) and false", "false")]
        [InlineData("(i < 0) and false", "false")]
        [InlineData("(0/0) or true", "true")]
        public void AnUndefinedTruthValueIsNotANumberForThisPurpose(string expression, string expected) =>
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Evaled);

        // Nor is a symbol: a bare variable is Domain.Any, so it may yet be a truth value and the
        // absorbing rules still apply to it.
        [Theory]
        [InlineData("false and x", "false")]
        [InlineData("true or x", "true")]
        public void ASymbolMayStillBeATruthValue(string expression, string expected) =>
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Evaled);

        // The contradiction this removes, stated as the commutation it broke.
        [Theory]
        [InlineData("true or (true and (x < 0))")]
        [InlineData("false and (x < 0)")]
        public void SimplifyAndEvaluationAgreeOffTheRealLine(string input)
        {
            var original = input.ToEntity();
            Assert.Equal(original.Substitute("x", "i").Evaled,
                original.Simplify().Substitute("x", "i").Evaled);
        }

        // https://github.com/asc-community/AngouriMath/issues/721
        // log_b(b) is 1 wherever it is defined, and the rewrite carried `provided x > 0` --
        // the real-domain condition, written out inside the rule rather than read from the
        // node. It was wrong in both directions at once: at x = -3 the logarithm is 1 and
        // the simplification was NaN, and at x = 1 the logarithm is NaN and the
        // simplification was 1.
        [Theory]
        [InlineData("-3")]
        [InlineData("-1/2")]
        [InlineData("2")]
        [InlineData("1")]
        [InlineData("0")]
        public void ALogarithmOfItsOwnBaseKeepsItsValueOffThePositiveReals(string at)
        {
            var original = "log(x, x)".ToEntity();
            var simplified = original.Simplify();
            Assert.Equal(original.Substitute("x", at.ToEntity()).Evaled,
                simplified.Substitute("x", at.ToEntity()).Evaled);
        }

        // The contradiction #890 reported, which is the same defect seen from the domain
        // side: the library gave the expression a value and declared it undefined
        // everywhere at the same time.
        [Theory]
        [InlineData("log(-3, -3)")]
        [InlineData("log(2, -8)")]
        [InlineData("log(-2, 8)")]
        public void ALogarithmWithNegativeArgumentsIsNotDeclaredUndefined(string input)
        {
            var expression = input.ToEntity();
            Assert.NotEqual(MathS.NaN, expression.Evaled);
            Assert.Equal(Entity.Boolean.True, expression.DomainCondition.Evaled);
        }

        // Which of the two conditions applies is the reading, as it already was for arcsin
        // and its siblings. A negative base is admitted over the complex plane and refused
        // over the reals; 0 and 1 are refused under either.
        [Theory]
        [InlineData("-3", true)]
        [InlineData("2", true)]
        [InlineData("1", false)]
        [InlineData("0", false)]
        public void TheLogarithmsDomainOverTheComplexPlaneAdmitsANegativeBase(string at, bool defined)
        {
            var condition = "log(x, x)".ToEntity().DomainCondition;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False,
                condition.Substitute("x", at.ToEntity()).Evaled);
        }

        [Theory]
        [InlineData("-3", false)]
        [InlineData("2", true)]
        [InlineData("1", false)]
        [InlineData("0", false)]
        public void TheLogarithmsDomainOverTheRealsStillRefusesANegativeBase(string at, bool defined)
        {
            var condition = "log(x, x)".ToEntity().WithCodomain(Domain.Real).DomainCondition;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False,
                condition.Substitute("x", at.ToEntity()).Evaled);
        }
    }
}
