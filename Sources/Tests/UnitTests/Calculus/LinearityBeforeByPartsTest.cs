//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A product with a sum inside it -- <c>sin(a+f*x)^4 * (5 - 6*sin(a+f*x)^2)</c> -- is the
    /// same function as the expanded difference, which answers in milliseconds. Written
    /// factored it did not finish in twenty seconds, because the only rule that would
    /// distribute it ran behind integration by parts and the search spent the whole budget
    /// first. https://github.com/asc-community/AngouriMath/issues/779
    /// <para/>
    /// Every answer is checked by differentiating it back and comparing at points: what
    /// matters is that it is an antiderivative, not what form it is written in.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LinearityBeforeByPartsTest
    {
        /// <summary>
        /// Generous enough that a slow machine will not fail it, and far below the twenty
        /// seconds this shape used to spend. Asserts termination rather than speed -- a
        /// regression here is a hang, and a hang would otherwise wedge the run instead of
        /// failing it.
        /// </summary>
        private static readonly TimeSpan Budget = LimitTermination.Guard;

        private static Entity IntegrateWithinBudget(string integrand)
        {
            Entity? answer = null;
            var task = Task.Run(() => answer = integrand.ToEntity().Integrate("x"));
            Assert.True(task.Wait(Budget), $"{integrand} did not finish within {Budget}");
            return answer!;
        }

        private static void AssertIsAntiderivative(string integrand, params double[] points)
        {
            var antiderivative = IntegrateWithinBudget(integrand);
            Assert.DoesNotContain("integral(", antiderivative.Stringize());
            var f = integrand.ToEntity().Substitute("a", 0.7).Substitute("f", 1.3);
            var derivative = antiderivative
                .Substitute("C", 0).Substitute("a", 0.7).Substitute("f", 1.3)
                .Differentiate("x");
            foreach (var point in points)
            {
                var expected = f.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.Equal(expected, actual, 7);
            }
        }

        /// <summary>
        /// The integrand from the issue, and the neighbours that make the point: written as a
        /// difference it always answered, and the factored form is the same function.
        /// </summary>
        [Theory]
        [InlineData("sin(a + f * x) ^ 4 * (5 - 6 * sin(a + f * x) ^ 2)", new[] { 0.4, 1.6, -0.9 })]
        [InlineData("5 * sin(a + f * x) ^ 4 - 6 * sin(a + f * x) ^ 6", new[] { 0.4, 1.6, -0.9 })]
        [InlineData("sin(a + f * x) ^ 2 * (1 + sin(a + f * x) ^ 2)", new[] { 0.4, 1.6 })]
        public void AProductWithASumInsideIsDistributed(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// Linearity is not specific to trigonometry, and these are the shapes most likely to
        /// be disturbed by moving it in front of integration by parts.
        /// </summary>
        [Theory]
        [InlineData("x * (1 + x)", new[] { 0.4, 1.6, -2.2 })]
        [InlineData("(x + 1) * (x + 2)", new[] { 0.4, 1.6, -2.2 })]
        [InlineData("cos(x) * (1 + x)", new[] { 0.4, 1.6, -2.2 })]
        [InlineData("e ^ x * (1 + x)", new[] { 0.4, 1.6, -2.2 })]
        public void OrdinaryProductsOverSumsStillAnswer(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// Integration by parts is still reached. Splitting returns nothing unless *every*
        /// term integrates, so a sum that only comes out whole falls through to it -- which is
        /// what makes putting linearity first unable to cost an answer.
        /// </summary>
        [Theory]
        [InlineData("x * cos(x)", new[] { 0.4, 1.6, -2.2 })]
        [InlineData("x * ln(x)", new[] { 0.4, 1.6 })]
        [InlineData("x ^ 2 * e ^ x", new[] { 0.4, 1.6, -2.2 })]
        public void ByPartsIsStillReached(string integrand, double[] points) =>
            AssertIsAntiderivative(integrand, points);

        /// <summary>
        /// These were pinned here as a *separate* known gap, declined rather than hung, so
        /// that they would not be read as the defect this file is about: two powers of one
        /// base were never merged, so <c>x^2 / x</c> was declined though it is <c>x</c>.
        /// <para/>
        /// That gap is fixed, and the assertion is inverted rather than deleted -- it is the
        /// evidence that distributing over a sum and merging the powers compose, which is
        /// the one thing neither issue could show on its own. Distributing
        /// <c>sin(x)^4 * (5 - 6 sin(x)^2)</c> is what *produces* <c>sin(x)^4 * (-6) * sin(x)^2</c>,
        /// and the merge is what makes that answerable.
        /// https://github.com/asc-community/AngouriMath/issues/781
        /// </summary>
        [Theory]
        [InlineData("sin(x) ^ 4 * (5 - 6 * sin(x) ^ 2)")]
        [InlineData("sin(x) ^ 4 * (-6) * sin(x) ^ 2")]
        [InlineData("x ^ 2 * (x + 1 / x)")]
        [InlineData("x ^ 2 / x")]
        public void PowersOfOneBaseAreNowMerged(string integrand) =>
            Assert.DoesNotContain("integral(", IntegrateWithinBudget(integrand).Stringize());
    }
}
