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
    /// Quotients of polynomials whose denominator is of degree three or more. A linear or
    /// quadratic denominator was answered in one piece, but nothing read anything above
    /// that, so 1/(x^3 + 1) had no antiderivative at all. Named in the issue's own list of
    /// what is missing: https://github.com/asc-community/AngouriMath/issues/233.
    /// Each answer is checked by differentiating it back and comparing at points, since
    /// what matters is that it is an antiderivative and not what form it is written in.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class PartialFractionsTest
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
                Assert.Equal(expected, actual, 7);
            }
        }

        // The denominator is split at one rational root at a time, and what is left over is
        // a smaller problem of the same kind until its denominator is a quadratic.
        [Theory]
        [InlineData("1 / (x ^ 3 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("x / (x ^ 3 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 3 - x)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 3 + x)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 4 - 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("x / (x ^ 4 - 1)", new[] { 0.3, 1.7, 3.2 })]
        [InlineData("(x ^ 2 + 1) / (x ^ 3 - x)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void ADenominatorOfDegreeThreeOrMore(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // Whether the denominator arrives factored or multiplied out makes no difference:
        // it is read as a polynomial either way.
        [Theory]
        [InlineData("1 / ((x - 1) * (x - 2) * (x - 3))", new[] { 0.3, 1.7, 3.6, 5.2 })]
        [InlineData("1 / (x ^ 3 - 6 * x ^ 2 + 11 * x - 6)", new[] { 0.3, 1.7, 3.6 })]
        [InlineData("(x + 1) / (x ^ 3 - x ^ 2 - 2 * x)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void FactoredOrMultipliedOutAlike(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // The shapes this sits next to have to keep working, in particular the linear and
        // quadratic denominators that are answered in one piece and must not be split.
        [Theory]
        [InlineData("1 / (x ^ 2 + 1)", new[] { 0.3, 1.7 })]
        [InlineData("1 / (x ^ 2 - 1)", new[] { 0.3, 3.2 })]
        [InlineData("1 / (x + 1)", new[] { 0.3, 1.7 })]
        [InlineData("x / (x ^ 2 + 1)", new[] { 0.3, 1.7 })]
        [InlineData("x / (x ^ 2 + 2 * x + 5)", new[] { 0.3, 1.7 })]
        [InlineData("1 / x", new[] { 0.3, 1.7 })]
        [InlineData("x ^ 2", new[] { 0.3, 1.7 })]
        [InlineData("sin(x)", new[] { 0.3, 1.7 })]
        [InlineData("x * e ^ x", new[] { 0.3, 1.7 })]
        public void NeighbouringFormsAreUnaffected(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// A denominator that factors over the rationals with no rational root anywhere in
        /// it. The step at a root cannot get a foothold on any of these, and each one splits
        /// into quadratics that the rule for a linear numerator over a quadratic already
        /// integrates. https://github.com/asc-community/AngouriMath/issues/919
        /// </summary>
        [Theory]
        [InlineData("1 / (x ^ 4 + 3 * x ^ 2 + 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("x / (x ^ 4 + 3 * x ^ 2 + 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("(x + 1) / (x ^ 4 + 3 * x ^ 2 + 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / ((x ^ 2 + 1) * (x ^ 2 + 2))", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 4 + 4)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void ADenominatorThatFactorsWithNoRationalRoot(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // A numerator that shares a factor with the denominator still has to come out right:
        // (x^2 + 1) cancels here, so one of the two numerators the split produces is zero.
        [Theory]
        [InlineData("(x ^ 2 + 1) / (x ^ 4 + 3 * x ^ 2 + 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void ANumeratorThatCancels(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        // A rational root and an irreducible quadratic in the same denominator, which needs
        // both steps: x^5 + ... + 1 is (x + 1)(x^2 + x + 1)(x^2 - x + 1), so the root at -1
        // comes off first and what is left has no root to divide out.
        [Theory]
        [InlineData("1 / (x ^ 5 + x ^ 4 + x ^ 3 + x ^ 2 + x + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void ARootAndAnIrreducibleFactorTogether(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// A biquadratic denominator that is irreducible over the rationals but factors over
        /// the reals, which is the remaining case
        /// <a href="https://github.com/asc-community/AngouriMath/issues/233">#233</a> names:
        /// <c>x^4 + 1</c> is <c>(x^2 - sqrt(2)x + 1)(x^2 + sqrt(2)x + 1)</c>, and both halves
        /// are read by the rule for a linear numerator over a quadratic.
        /// </summary>
        [Theory]
        [InlineData("x ^ 2 / (x ^ 4 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 4 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("x ^ 3 / (x ^ 4 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("(x ^ 3 + 1) / (x ^ 4 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("(x ^ 2 + x) / (x ^ 4 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        // A leading coefficient other than one is divided out rather than refused.
        [InlineData("1 / (2 * x ^ 4 + 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (3 * x ^ 4 + 12)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        public void ABiquadraticThatFactorsOnlyOverTheReals(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// The other shape a biquadratic takes, where <c>p^2 - 4q</c> is positive so there are
        /// two real roots in <c>x^2</c> and the factors are the even <c>(x^2 + u)(x^2 + v)</c>.
        /// A negative <c>q</c> puts one factor either side of zero -- <c>x^4 - 2</c> is
        /// <c>(x^2 - sqrt(2))(x^2 + sqrt(2))</c> -- so one half integrates to a logarithm and
        /// the other to an arctangent, which is what makes it worth testing next to the pair
        /// above rather than folded into them.
        /// </summary>
        [Theory]
        [InlineData("1 / (x ^ 4 + 3 * x ^ 2 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("x ^ 2 / (x ^ 4 + 3 * x ^ 2 + 1)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 4 - 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("x ^ 2 / (x ^ 4 - 2)", new[] { 0.3, 1.7, 3.2, -2.4 })]
        [InlineData("1 / (x ^ 4 - 5 * x ^ 2 + 5)", new[] { 0.3, 3.2, -2.4 })]
        public void ABiquadraticWithTwoRealRootsInTheSquare(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// What has nothing to split into: a denominator that is irreducible and not biquadratic.
        /// The step over the reals reaches a biquadratic only, so a quartic with an odd power in
        /// it is answered whole, its logarithms in a sum over its roots
        /// (<a href="https://github.com/asc-community/AngouriMath/issues/1285">#1285</a>).
        /// <c>x^2/(x^4 + 1)</c> used to be on this list and is answered above by that step.
        /// <c>1/(x^4 + 2x^2 + 1)</c> was here too, as a power of a single irreducible with no
        /// coprime pair to split into -- it is <c>(x^2 + 1)^2</c>, which the Hermite reduction
        /// answers once the repeated factor is written, and the denominator is now written that
        /// way first; see <c>RationalIntegralsTest.ARepeatedFactorTheSpellingHides</c>.
        /// </summary>
        [Theory]
        [InlineData("1 / (x ^ 3 + x ^ 2 + x + 2)")]
        [InlineData("1 / (x ^ 4 + x ^ 3 + 1)")]
        [InlineData("1 / (x ^ 4 + x + 1)")]
        public void WhatCannotBeSplitIsASumOverItsRoots(string integrand)
        {
            Assert.Contains(integrand.ToEntity().Integrate("x").Nodes, node => node is Entity.SumOverSetf);
            AssertIsAntiderivative(integrand, 0.3, 1.7, 3.2, -2.4);
        }

        /// <summary>
        /// The guard that keeps the search cheap, which nothing widening the split must undo:
        /// this factorises into <c>x^4 + x + 1</c>, an irreducible quartic with an odd power in
        /// it that no split reads, and the whole point of reading the factorisation is that
        /// finding that out costs one factorisation rather than a search of every half of every
        /// split. The quartic's roots do answer it, but in a second pass that starts only once
        /// the first has declined, so the first pass's decline is still inside the time asserted
        /// here.
        /// </summary>
        /// <remarks>
        /// The integrand here used to be <c>(1 - x^4)/(1 + x^4 + x^8)</c>, whose quartic is
        /// <c>x^4 - x^2 + 1</c> — <em>biquadratic</em>, and so read by
        /// <c>TrySplitBiquadraticOverTheReals</c>. That one is answered now rather than declined,
        /// which is the test below; the guard it was written for is real and still needs a case
        /// that actually reaches it.
        /// </remarks>
        [Fact]
        public void FindingThatNothingSplitsStaysCheap()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var answer = "(1 - x ^ 4) / ((1 + x ^ 2) * (x ^ 4 + x + 1))".ToEntity().Integrate("x");
            Assert.True(clock.Elapsed < System.TimeSpan.FromSeconds(10), $"took {clock.Elapsed}");
            Assert.Contains(answer.Nodes, node => node is Entity.SumOverSetf);
            AssertIsAntiderivative("(1 - x ^ 4) / ((1 + x ^ 2) * (x ^ 4 + x + 1))", 0.3, 1.7, 3.2, -2.4);
        }

        /// <summary>
        /// And the case that guard used to be written against is now answered, because its
        /// quartic is biquadratic and there is a rule for that. Asserted as a value rather than
        /// as a shape, since what it comes out as says nothing about whether it is right.
        /// </summary>
        [Fact]
        public void ABiquadraticFactorIsSplitRatherThanDeclined()
        {
            var integrand = "(1 - x ^ 4) / (1 + x ^ 4 + x ^ 8)".ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            foreach (var at in new[] { 0.23, 0.61, 1.05, 2.3 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = integrand.Substitute("x", at).EvalNumerical();
                var difference = System.Math.Abs((double)(got - want).RealPart);
                Assert.True(difference / System.Math.Max(1.0, System.Math.Abs((double)want.RealPart)) < 1e-9,
                    $"d/dx of the antiderivative is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
