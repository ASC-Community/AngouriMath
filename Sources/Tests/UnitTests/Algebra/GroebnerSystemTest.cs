//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using AngouriMath.Functions;
using AngouriMath.Functions.Algebra.Groebner;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// Systems that are triangularised by a Gröbner basis rather than eliminated in
    /// radicals — <a href="https://github.com/asc-community/AngouriMath/issues/860">#860</a>.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class GroebnerSystemTest
    {
        static Variable[] Vars(int count) =>
            Enumerable.Range(0, count).Select(i => (Variable)$"x_{i}").ToArray();

        /// <summary>n unknowns that are a permutation of 1..n, written as n power sums.</summary>
        static Entity[] PowerSums(int n)
        {
            var equations = new Entity[n];
            for (var power = 1; power <= n; power++)
            {
                Entity sum = 0;
                for (var v = 0; v < n; v++)
                    sum += MathS.Pow($"x_{v}", power);
                var target = 0L;
                for (var k = 1; k <= n; k++)
                {
                    var term = 1L;
                    for (var e = 0; e < power; e++) term *= k;
                    target += term;
                }
                equations[power - 1] = sum - target;
            }
            return equations;
        }

        static void AssertEverySolutionSatisfies(Entity[] equations, Variable[] variables, Matrix solutions)
        {
            for (var row = 0; row < solutions.RowCount; row++)
            {
                var substitutions = new Dictionary<Variable, Entity>();
                for (var column = 0; column < variables.Length; column++)
                    substitutions[variables[column]] = solutions[row, column];
                foreach (var equation in equations)
                    Assert.Equal(0, equation.Substitute(substitutions).EvalNumerical());
            }
        }

        /// <summary>
        /// Four coupled equations did not finish in three hundred seconds when each
        /// elimination went through the radical formulas, while four uncoupled ones with 256
        /// solutions took seventeen milliseconds. The cost was the radicals, not the size.
        /// </summary>
        [Theory]
        [InlineData(2, 2)]
        [InlineData(3, 6)]
        [InlineData(4, 24)]
        [InlineData(5, 120)]
        public void CoupledSystemsAreSolved(int variableCount, int expectedSolutions)
        {
            var equations = PowerSums(variableCount);
            var variables = Vars(variableCount);

            var solutions = MathS.Equations(equations).Solve(variables);

            Assert.NotNull(solutions);
            Assert.Equal(expectedSolutions, solutions.RowCount);
            Assert.Equal(variableCount, solutions.ColumnCount);
            AssertEverySolutionSatisfies(equations, variables, solutions);
        }

        /// <summary>
        /// More equations than unknowns used to be refused outright, and a Gröbner basis has
        /// no use for as many of one as the other.
        /// </summary>
        [Fact]
        public void AnOverDeterminedConsistentSystemIsSolved()
        {
            Entity[] equations = { "x^2 + y^2 - 25", "x + y - 7", "x*y - 12" };
            Variable[] variables = { "x", "y" };

            var solutions = MathS.Equations(equations).Solve(variables);

            Assert.NotNull(solutions);
            Assert.Equal(2, solutions.RowCount);
            AssertEverySolutionSatisfies(equations, variables, solutions);
        }

        /// <summary>
        /// The textbook signal for an inconsistent system is that the basis contains a
        /// nonzero constant. It has to arrive as "no solutions" and not as an exception.
        /// </summary>
        [Fact]
        public void AnOverDeterminedInconsistentSystemHasNoSolutions()
        {
            Entity[] equations = { "x^2 + y^2 - 25", "x + y - 7", "x*y - 99" };
            Assert.Null(MathS.Equations(equations).Solve("x", "y"));
        }

        [Fact]
        public void AConsistentSquareSystemIsUnaffected()
        {
            Entity[] equations = { "x^2 + y^2 - 25", "x + y - 7" };
            Variable[] variables = { "x", "y" };

            var solutions = MathS.Equations(equations).Solve(variables);

            Assert.NotNull(solutions);
            Assert.Equal(2, solutions.RowCount);
            AssertEverySolutionSatisfies(equations, variables, solutions);
        }

        /// <summary>
        /// The columns are the variables in the order they were asked for, which the
        /// triangular back-substitution fills in from the last one first.
        /// </summary>
        [Fact]
        public void ColumnsFollowTheOrderTheVariablesWereGivenIn()
        {
            Entity[] equations = { "x - 1", "y - 2" };
            var solutions = MathS.Equations(equations).Solve("x", "y");
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions.RowCount);
            Assert.Equal(1, solutions[0, 0].EvalNumerical());
            Assert.Equal(2, solutions[0, 1].EvalNumerical());

            var swapped = MathS.Equations(equations).Solve("y", "x");
            Assert.NotNull(swapped);
            Assert.Equal(2, swapped[0, 0].EvalNumerical());
            Assert.Equal(1, swapped[0, 1].EvalNumerical());
        }

        /// <summary>
        /// Not a polynomial over Q in these variables, so the Gröbner path must decline and
        /// leave the answer exactly as it was.
        /// </summary>
        [Fact]
        public void ANonPolynomialSystemIsLeftToTheExistingSolver()
        {
            Entity[] equations = { "cos(x2 + 1)^2 + 3y", "y * (-1) + 4cos(x2 + 1)" };
            var solutions = MathS.Equations(equations).Solve("x", "y");
            Assert.NotNull(solutions);
            Assert.Equal(8, solutions.RowCount);
        }

        /// <summary>
        /// A radical solution is still an exact one, and one structural pass is enough to
        /// prove it satisfies the system, so these are in reach too.
        /// </summary>
        [Theory]
        [InlineData("x^2 - 2", "y - x", 2)]
        [InlineData("x^2 - 2", "y^2 - 3", 4)]
        [InlineData("x^2 + y^2 - 4", "x - y", 2)]
        [InlineData("x^2 + x - 1", "y - x^2", 2)]
        [InlineData("x^3 - 2", "y - x", 3)]
        public void SystemsWithIrrationalSolutionsAreSolved(string first, string second, int expected)
        {
            Entity[] equations = { first, second };
            Variable[] variables = { "x", "y" };

            var solutions = MathS.Equations(equations).Solve(variables);

            Assert.NotNull(solutions);
            Assert.Equal(expected, solutions.RowCount);
            AssertEverySolutionSatisfies(equations, variables, solutions);
        }

        /// <summary>
        /// The basis of <c>x * y, x + y^2 - 1</c> in degree-reverse-lexicographic order has
        /// <c>1</c>, <c>y</c> and <c>x</c> under its staircase, and converting it reaches
        /// <c>y^2 = 1 - x</c> with the row for <c>1</c> already standing. The row kept for
        /// <c>y^2</c> is then <c>-x</c>, and recording it as <c>y^2</c> alone made the element
        /// for <c>x</c> come out as <c>x + y^2</c>, whose root <c>(0, 0)</c> is no solution.
        /// </summary>
        [Fact]
        public void TheLexicographicBasisKeepsWhatARowWasReducedBy()
        {
            Variable[] variables = { "x", "y" };
            var index = new Dictionary<Variable, int> { [variables[0]] = 0, [variables[1]] = 1 };
            var generators = new[] { "x * y", "x + y^2 - 1" }
                .Select(text => MultivariatePolynomial.TryParse(text.ToEntity(), index)!)
                .ToList();
            var budget = new GroebnerBudget();
            var basis = Buchberger.Compute(generators, MonomialOrder.DegreeReverseLexicographic, budget);
            Assert.NotNull(basis);
            var lexicographic = Fglm.ToLexicographic(basis!, variables.Length, budget);
            Assert.NotNull(lexicographic);

            var elements = lexicographic!.Select(element => element.ToEntity(variables)).ToList();
            Assert.Equal(2, elements.Count);
            foreach (var expected in new[] { "y^3 - y", "x + y^2 - 1" })
                Assert.Contains(elements, element => (element - expected.ToEntity()).Simplify() == 0);
        }

        /// <summary>
        /// The system the basis above belongs to. With the wrong element the triangularisation
        /// was declined, and the eliminator that answered instead solved <c>x * y = 0</c> for
        /// <c>y</c> as <c>0</c> alone, which kept <c>(1, 0)</c> and lost <c>(0, 1)</c> and
        /// <c>(0, -1)</c>.
        /// </summary>
        [Fact]
        public void ASystemWhoseBasisLeansOnAReducedRowIsSolvedWhole()
        {
            Entity[] equations = { "x * y", "x + y^2 - 1" };
            Variable[] variables = { "x", "y" };

            var solutions = MathS.Equations(equations).Solve(variables);

            Assert.NotNull(solutions);
            Assert.Equal(3, solutions!.RowCount);
            AssertEverySolutionSatisfies(equations, variables, solutions);
            foreach (var (x, y) in new[] { (1, 0), (0, 1), (0, -1) })
                Assert.Contains(Enumerable.Range(0, solutions.RowCount),
                    row => solutions[row, 0] == Number.Integer.Create(x) && solutions[row, 1] == Number.Integer.Create(y));
        }

        /// <summary>
        /// A basis that is not in shape position. Its element <c>x y + 3 x - 5 y - 15</c> is
        /// <c>0</c> at <c>y = -3</c> but was left <c>x * (-3) + 3 * x</c>, which still mentions
        /// <c>x</c>, so it was the equation <c>x</c> was solved from there. That solved to every
        /// number, the triangularisation was declined, and the eliminator lost <c>(5, 0)</c>.
        /// </summary>
        [Fact]
        public void ARootThatZeroesALeadingCoefficientUsesTheNextElement()
        {
            Entity[] equations = { "x * y + 3 * x - 5 * y - 15", "x^2 - 3 * x - 4 * y - 10" };
            Variable[] variables = { "x", "y" };

            var solutions = MathS.Equations(equations).Solve(variables);

            Assert.NotNull(solutions);
            Assert.Equal(3, solutions!.RowCount);
            AssertEverySolutionSatisfies(equations, variables, solutions);
            foreach (var (x, y) in new[] { (5, 0), (1, -3), (2, -3) })
                Assert.Contains(Enumerable.Range(0, solutions.RowCount),
                    row => solutions[row, 0] == Number.Integer.Create(x) && solutions[row, 1] == Number.Integer.Create(y));
        }

        /// <summary>
        /// The answers stay exact rather than being handed back as decimals — which is the
        /// point of triangularising rather than evaluating.
        /// </summary>
        [Fact]
        public void AnIrrationalSolutionComesBackInRadicals()
        {
            var solutions = MathS.Equations(new Entity[] { "x^2 - 2", "y - x" }).Solve("x", "y");
            Assert.NotNull(solutions);
            for (var row = 0; row < solutions.RowCount; row++)
                for (var column = 0; column < solutions.ColumnCount; column++)
                    Assert.DoesNotContain(
                        solutions[row, column].Nodes,
                        node => node is Number.Real and not Number.Rational);
        }

        /// <summary>
        /// Where the univariate's roots are decimals there is nothing to prove an identity
        /// with, so the system is declined — and the declining has to be quick, because an
        /// earlier version proved nothing slowly and cost more than it saved.
        /// </summary>
        [Fact]
        public void APolynomialSystemWithDecimalRootsIsStillAnswered()
        {
            Entity[] equations = { "x3 + 9 x2 y - 10", "y3 + x y2 - 2" };
            var solutions = MathS.Equations(equations).Solve("x", "y");
            Assert.NotNull(solutions);
            Assert.Equal(9, solutions.RowCount);
        }

        /// <summary>
        /// A free variable means infinitely many solutions, which is not something a
        /// triangular basis enumerates — the ideal is not zero-dimensional, so there is no
        /// finite set of standard monomials and the conversion declines. What this path hands
        /// on is therefore unchanged; what happens after it is not.
        /// </summary>
        /// <remarks>
        /// This used to assert a <c>WrongNumberOfArgumentsException</c>, which is what the whole
        /// call raised for a system with fewer equations than unknowns. Such a system is now
        /// answered as the family of all its solutions where it is linear
        /// (<a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>), so the
        /// assertion here is only that this path declines and not that the call fails.
        /// </remarks>
        [Fact]
        public void AnUnderDeterminedSystemIsNotTriangularised()
        {
            Assert.False(Functions.Algebra.Groebner.GroebnerSystemSolver.TrySolve(
                new List<Entity> { "x + y - 3" }, new Entity.Variable[] { "x", "y" }, out _));

            // And the call as a whole answers it, rather than raising as it used to.
            var solutions = MathS.Equations(new Entity[] { "x + y - 3" }).Solve("x", "y");
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions!.RowCount);
            Assert.Equal(2, solutions.ColumnCount);
        }

        /// <summary>
        /// Eight unknowns is the ceiling of the packed representation; nine has to fall
        /// through rather than be truncated.
        /// </summary>
        [Fact]
        public void MoreVariablesThanThePackingHoldsFallsThrough()
        {
            var equations = new Entity[9];
            var variables = new Variable[9];
            for (var i = 0; i < 9; i++)
            {
                variables[i] = $"v_{i}";
                equations[i] = variables[i] - (i + 1);
            }
            var solutions = MathS.Equations(equations).Solve(variables);
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions.RowCount);
        }
    }
}
