//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// An equation the solvers exhaust themselves on, against one that has been shown to
    /// have no roots. Both were the empty set.
    /// https://github.com/asc-community/AngouriMath/issues/1036
    /// </summary>
    /// <remarks>
    /// The empty set is a positive mathematical claim, so it is only available where
    /// something established it. Where nothing did, the answer is the equation as a set
    /// builder: it names the same set, and asserts of it only what is known.
    /// </remarks>
    [Trait("Area", "Algebra")]
    public sealed class UnsolvedEquationTest
    {
        /// <summary>An equation no solver settles comes back as a condition, not as no roots.</summary>
        [Theory]
        [InlineData("x6 + x y + 1 = 0")]
        [InlineData("sin(x) + x + y = 0")]
        [InlineData("e^x + x^2 + y = 0")]
        [InlineData("x y + sin(x) ^ 2 + ln(x) = 0")]
        public void ExhaustedIsNotEmpty(string equation)
        {
            var answer = equation.ToEntity().Solve("x");
            Assert.False(answer.IsSetEmpty, $"{equation} answered the empty set");
            Assert.IsType<ConditionalSet>(answer);
        }

        /// <summary>
        /// The same for the entry point that takes the equation as an expression read as
        /// equal to zero, which reaches the solver by its own route.
        /// </summary>
        [Theory]
        [InlineData("x6 + x y + 1")]
        [InlineData("sin(x) + x + y")]
        public void ExhaustedIsNotEmptyThroughSolveEquation(string expression)
        {
            Assert.False(expression.ToEntity().SolveEquation("x").IsSetEmpty);
            Assert.False(MathS.SolveEquation(expression.ToEntity(), "x").IsSetEmpty);
        }

        /// <summary>
        /// An equation shown to have no roots keeps the empty set. <c>e^x</c> is never zero
        /// and <c>abs(x)</c> is never negative, and in both cases the solver reached that by
        /// inverting rather than by running out of ideas.
        /// </summary>
        [Theory]
        [InlineData("e^x = 0")]
        [InlineData("abs(x) = -1")]
        public void ImpossibleIsStillEmpty(string equation)
        {
            var answer = equation.ToEntity().Solve("x");
            Assert.IsType<FiniteSet>(answer);
            Assert.True(answer.IsSetEmpty, $"{equation} stopped answering the empty set");
        }

        /// <summary>
        /// The two are distinguishable, which is the whole point: one is empty and the other
        /// is not, where before both were <c>{ }</c>.
        /// </summary>
        [Fact]
        public void ExhaustedAndImpossibleDisagree()
        {
            var exhausted = "x6 + x y + 1 = 0".ToEntity().Solve("x");
            var impossible = "e^x = 0".ToEntity().Solve("x");
            Assert.NotEqual(exhausted.IsSetEmpty, impossible.IsSetEmpty);
        }

        /// <summary>
        /// The condition admits a root that the empty set denied. <c>x^6 + x*y + 1 = 0</c>
        /// has six roots for every <c>y</c>; at <c>y = -2</c> one of them is 1.
        /// </summary>
        [Fact]
        public void TheConditionAdmitsARootTheEmptySetDenied()
        {
            var answer = Assert.IsType<ConditionalSet>("x6 + x y + 1 = 0".ToEntity().Solve("x"));
            var atTheRoot = answer.Predicate.Substitute("y", -2).Substitute("x", 1).Simplify();
            Assert.Equal(Entity.Boolean.True, atTheRoot);
        }

        /// <summary>
        /// Newton's method is asked from finitely many starting points inside a bounded
        /// region, so finding nothing is a fact about the search. With it turned off there
        /// is nothing numerical left either, and both of these were <c>{ }</c>.
        /// </summary>
        [Theory]
        [InlineData("x5 + 3x + 1 = 0")]
        [InlineData("sin(x) * x - 3 = 0")]
        [InlineData("2 ^ (x sin(x)) + 4 ^ (x sin(x)) + c = 0")]
        [InlineData("x + sqrt(x^0.1 + a) + c = 0")]
        [InlineData("(x + 6)^(1/6) + x + x3 + a = 0")]
        [InlineData("sqrt(x + 1) + sqrt(x + 2) + a + x = 0")]
        public void WithoutNewtonTheAnswerIsTheEquation(string equation)
        {
            using var _ = MathS.Settings.AllowNewton.Set(false);
            var answer = equation.ToEntity().Solve("x");
            Assert.False(answer.IsSetEmpty, $"{equation} answered the empty set");
            Assert.IsType<ConditionalSet>(answer);
        }

        /// <summary>
        /// And where Newton does find roots they are still the answer, so nothing that
        /// answered before stops answering.
        /// </summary>
        [Theory]
        [InlineData("x5 + 3x + 1 = 0")]
        [InlineData("sin(x) * x - 3 = 0")]
        public void NewtonStillAnswers(string equation)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.False(answer.IsSetEmpty);
        }

        /// <summary>
        /// A conjunction with an unsettled side must not name a root. Intersecting a finite
        /// set with a condition keeps an element whose membership could not be decided, so
        /// <c>{ 1 }</c> here would say 1 is a root of <c>x^6 + x*y + 1</c>, which it is only
        /// at <c>y = -2</c>.
        /// </summary>
        [Fact]
        public void AConjunctionDoesNotNameARootItCannotCheck()
        {
            var answer = "x6 + x y + 1 = 0 and x - 1 = 0".ToEntity().Solve("x");
            Assert.IsType<ConditionalSet>(answer);
            Assert.False(answer.TryContains(1, out var contains) && contains,
                "the conjunction asserted that 1 is a solution");
        }

        /// <summary>A conjunction of two settled sides still intersects them.</summary>
        [Fact]
        public void ASettledConjunctionStillIntersects()
        {
            var answer = Assert.IsType<FiniteSet>(
                "(x - 3) * (x - 6) = 0 and (x - 3) * (x - 7) = 0".ToEntity().Solve("x"));
            Assert.Equal(new FiniteSet(3), answer);
        }

        /// <summary>
        /// A disjunction is a union, and a union with an unsettled side is exactly right:
        /// the roots that were found, together with the ones nothing settled.
        /// </summary>
        [Fact]
        public void ADisjunctionKeepsTheRootsItFound()
        {
            var answer = "x6 + x y + 1 = 0 or x - 1 = 0".ToEntity().Solve("x");
            Assert.True(answer.TryContains(1, out var contains) && contains,
                "the disjunction lost the root it had");
        }

        /// <summary>
        /// An equation that solves is untouched, including the one that has every complex
        /// number as a root and the one that has none because it says nothing about x.
        /// </summary>
        [Theory]
        [InlineData("x2 - 4 = 0", 2)]
        [InlineData("x2 + 1 = 0", 2)]
        [InlineData("(x - 1)(x2 - 3) = 0", 3)]
        [InlineData("sin(x) = 1/2", 2)]
        public void SolvingIsUnchanged(string equation, int rootCount)
            // A family counts once, by its principal member.
            => Assert.Equal(rootCount, equation.ToEntity().Solve("x").Members(0).Count);
    }
}
