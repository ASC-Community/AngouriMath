//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation that does not mention the unknown has either no solutions or every
    /// solution, and which of the two is decided by whether what is left is zero. Where
    /// that is not decidable, the answer was the empty set -- which asserts that no
    /// <c>x</c> satisfies it, and every <c>x</c> does whenever the two sides happen to be
    /// equal. https://github.com/asc-community/AngouriMath/issues/278
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class EquationWithoutTheUnknownTest
    {
        /// <summary>
        /// The predicate the answer carries must be true exactly where the equation is.
        /// Every free symbol is given each of a handful of values and the two are compared
        /// as booleans; an assignment where either side does not decide is skipped, and the
        /// count that did decide is asserted to be non-zero so that a vacuous pass is not
        /// mistaken for agreement.
        /// </summary>
        private static void AssertAgreesWithTheEquation(Entity predicate, Entity equation)
        {
            var symbols = equation.Vars.Where(v => v != "x").ToList();
            var compared = 0;
            foreach (var value in new Entity[] { -2, 0, 1, 2, "1/2".ToEntity() })
            {
                Entity left = predicate, right = equation;
                foreach (var symbol in symbols)
                {
                    left = left.Substitute(symbol, value);
                    right = right.Substitute(symbol, value);
                }
                bool expected, actual;
                try
                {
                    expected = right.EvalBoolean();
                    actual = left.EvalBoolean();
                }
                catch { continue; }
                compared++;
                Assert.True(expected == actual,
                    $"{equation.Stringize()} at {value.Stringize()}: the answer's condition "
                    + $"{predicate.Stringize()} says {actual} where the equation says {expected}");
            }
            Assert.True(compared > 0,
                $"{equation.Stringize()}: no assignment decided, so the condition was not checked");
        }

        /// <summary>Decidably unequal: no x satisfies it.</summary>
        [Theory]
        [InlineData("3 = 5")]
        [InlineData("2 + 2 = 5")]
        [InlineData("sqrt(4) = 3")]
        public void AFalseStatementHasNoSolutions(string equation)
            => Assert.Equal(Entity.Set.Empty, equation.ToEntity().Solve("x").InnerSimplified);

        /// <summary>
        /// Decidably equal: every x satisfies it. <c>a = a</c> and <c>a + b = b + a</c> are
        /// the cases that evaluation alone does not settle -- neither side is a number --
        /// so the residual is simplified before the question is called undecidable.
        /// </summary>
        [Theory]
        [InlineData("3 = 3")]
        [InlineData("2 + 2 = 4")]
        [InlineData("a = a")]
        [InlineData("a + b = b + a")]
        public void ATrueStatementIsSatisfiedByEverything(string equation)
            => Assert.Equal(MathS.Sets.C, equation.ToEntity().Solve("x").InnerSimplified);

        /// <summary>
        /// Not decidable either way, which is what #278 asks for: the answer is the
        /// condition itself, as a set of every <c>x</c> subject to it.
        /// </summary>
        [Theory]
        [InlineData("a = b")]
        [InlineData("sin(a) = 0")]
        [InlineData("a ^ 2 = 2")]
        public void AnUndecidableStatementAnswersWithItsCondition(string equation)
        {
            var solved = equation.ToEntity().Solve("x").InnerSimplified;
            var conditional = Assert.IsType<Entity.Set.ConditionalSet>(solved);
            Assert.Equal((Entity)"x".ToEntity(), conditional.Var);
            // The condition must agree with the equation, not merely be present: it is what
            // the answer claims, and a wrong one here is a wrong answer that looks careful.
            // Compared by truth value at concrete assignments rather than symbolically --
            // asserting that Simplify can prove two booleans equal would be testing the
            // simplifier's reach on `and`/`or`, not the solver's answer.
            AssertAgreesWithTheEquation(conditional.Predicate, equation.ToEntity());
        }

        /// <summary>
        /// The unknown is bound by the limit rather than free in it, so this reduces to
        /// <c>y^2 - 2</c> and is the undecidable case above -- every <c>x</c> is a solution
        /// when <c>y</c> is a square root of 2, and none otherwise.
        /// </summary>
        /// <remarks>
        /// This case used to sit in <c>SolveOneEquation.TestInvertNodes</c> expecting zero
        /// roots. It was moved here rather than updated in place because that test verifies
        /// a *count* of roots through a helper that requires a <c>FiniteSet</c>, and the
        /// right answer here is not a finite set.
        /// </remarks>
        [Fact]
        public void ALimitBindsTheUnknownAndLeavesTheEquationWithout()
        {
            var solved = "limit(x, x, y)2 - 2".ToEntity().SolveEquation("x").InnerSimplified;
            var conditional = Assert.IsType<Entity.Set.ConditionalSet>(solved);
            AssertAgreesWithTheEquation(conditional.Predicate, "y ^ 2 = 2".ToEntity());
        }
    }
}
