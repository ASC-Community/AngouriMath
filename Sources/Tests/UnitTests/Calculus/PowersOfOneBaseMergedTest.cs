//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// Two powers of one base sitting in the same product were never merged, so an
    /// integrand that is a power in disguise was declined:
    /// <code>
    ///     x ^ 2 / x                       -> integral(x ^ 2 / x, x)   -- it is x
    ///     sin(x) ^ 4 * (-6) * sin(x) ^ 2  -> declined                 -- it is -6 sin(x)^6
    /// </code>
    /// The rule that merges them exists in <c>Patterns.PowerRules</c>, but it pairs two
    /// sibling nodes, and a constant factor sitting between the two powers makes them
    /// non-siblings. <c>Integrate</c> normalises with <c>InnerSimplified</c>, which does
    /// not run <c>PowerRules</c> at all, so even the sibling case was missed.
    /// https://github.com/asc-community/AngouriMath/issues/781
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class PowersOfOneBaseMergedTest
    {
        /// <summary>Sample points, chosen to include negative x and to avoid the zeros of sin.</summary>
        private static readonly double[] Points = { -1.3, -0.4, 0.25, 0.7, 1.9, 3.3 };

        /// <summary>Relative, because these integrands range over several orders of magnitude.</summary>
        private const double RelativeTolerance = 1e-9;

        private static Entity AssertAnswered(string integrand)
        {
            var antiderivative = integrand.Integrate("x");
            Assert.False(antiderivative is Entity.Integralf,
                $"{integrand} was declined: {antiderivative.Stringize()}");
            return antiderivative;
        }

        /// <summary>
        /// The antiderivative is checked by differentiating it back, rather than against a
        /// printed form -- the point is that an answer exists and is right, not how it is
        /// spelled.
        /// </summary>
        private static void AssertIntegrates(string integrand, string expectedDerivative)
        {
            var difference = (AssertAnswered(integrand).Differentiate("x") - expectedDerivative.ToEntity()).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        /// <summary>
        /// The same check for integrands whose differentiated-back form is a trigonometric
        /// identity that <see cref="Entity.Simplify"/> cannot close -- for those, asserting
        /// a symbolic zero would be testing the simplifier's reach rather than the
        /// integrator's answer. The antiderivative is sampled instead.
        /// </summary>
        private static void AssertIntegratesNumerically(string integrand, string expectedDerivative)
        {
            var derivative = AssertAnswered(integrand).Differentiate("x");
            foreach (var point in Points)
            {
                var actual = derivative.Substitute("x", point).Substitute("C", 0)
                    .EvalNumerical().RealPart.EDecimal.ToDouble();
                var expected = expectedDerivative.ToEntity().Substitute("x", point)
                    .EvalNumerical().RealPart.EDecimal.ToDouble();
                var scale = Math.Max(Math.Max(Math.Abs(expected), Math.Abs(actual)), 1e-12);
                Assert.True(Math.Abs(expected - actual) <= RelativeTolerance * scale,
                    $"{integrand} at x = {point}: differentiated back to {actual}, expected {expected}");
            }
        }

        [Theory]
        [InlineData("x ^ 2 / x", "x")]
        [InlineData("x ^ 2 * (1 / x)", "x")]
        [InlineData("x ^ 3 / x ^ 2", "x")]
        public void PowersOfOneBaseAreMerged(string integrand, string expectedDerivative)
            => AssertIntegrates(integrand, expectedDerivative);

        [Theory]
        [InlineData("sin(x) ^ 4 * (-6) * sin(x) ^ 2", "-6 * sin(x) ^ 6")]
        [InlineData("sin(x) ^ 4 * 3 * sin(x) ^ 2", "3 * sin(x) ^ 6")]
        public void PowersOfOneTrigBaseAreMerged(string integrand, string expectedDerivative)
            => AssertIntegratesNumerically(integrand, expectedDerivative);

        /// <summary>
        /// Every argument other than a bare <c>x</c> already answered before the fix, and
        /// must keep answering. These are the control cases from the issue.
        /// </summary>
        [Theory]
        [InlineData("x ^ 2 * x", "x ^ 3")]
        public void AlreadyAnsweredCasesStillAnswer(string integrand, string expectedDerivative)
            => AssertIntegrates(integrand, expectedDerivative);

        [Theory]
        [InlineData("sin(2 * x) ^ 4 * (-6) * sin(2 * x) ^ 2", "-6 * sin(2 * x) ^ 6")]
        [InlineData("sin(x + 1) ^ 4 * (-6) * sin(x + 1) ^ 2", "-6 * sin(x + 1) ^ 6")]
        public void AlreadyAnsweredTrigCasesStillAnswer(string integrand, string expectedDerivative)
            => AssertIntegratesNumerically(integrand, expectedDerivative);

        /// <summary>
        /// Merging powers of one base must not disturb a product whose factors have
        /// different bases -- the gathering is keyed on the base, and a wrong key here
        /// would silently rewrite unrelated factors together.
        /// </summary>
        [Theory]
        [InlineData("x ^ 2 * a ^ 3")]
        [InlineData("sin(x) ^ 2 * cos(x) ^ 3")]
        public void DifferentBasesAreLeftAlone(string expr)
        {
            var gathered = expr.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, gathered.EqualTo(expr.ToEntity()).Simplify());
        }
    }
}
