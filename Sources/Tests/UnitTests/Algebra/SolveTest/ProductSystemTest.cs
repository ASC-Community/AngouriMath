//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using System.Linq;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// Systems with an equation that is a product in more than one unknown, which the
    /// elimination solved for one unknown as if the other factors were not zero: <c>x y = 0</c>
    /// for <c>y</c> is <c>0</c>, and the branch <c>x = 0</c> was lost. Each is now solved once
    /// for each factor.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2128">#2128</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ProductSystemTest
    {
        private static Entity.Matrix? Solve(params string[] equations)
            => MathS.Equations(equations.Select(e => e.ToEntity()).ToArray()).Solve("x", "y");

        /// <summary>
        /// Each row satisfies each equation, at several values of what is left in it: an
        /// integer parameter at whole numbers, anything else at values of both signs.
        /// </summary>
        private static void EveryRowHolds(Entity.Matrix solutions, params string[] equations)
        {
            for (var row = 0; row < solutions.RowCount; row++)
                foreach (var equation in equations)
                {
                    var residual = equation.ToEntity().Substitute("x", solutions[row, 0]).Substitute("y", solutions[row, 1]);
                    foreach (var at in new[] { -1, 0, 2 })
                    {
                        var pinned = residual;
                        foreach (var symbol in residual.Vars.ToList())
                            pinned = pinned.Substitute(symbol, symbol.Name.StartsWith("n_") ? at : at + 0.3);
                        var value = Assert.IsAssignableFrom<Entity.Number.Complex>(pinned.Evaled);
                        Assert.True(((Entity.Number.Real)value.Abs()).EDecimal.ToDouble() < 1e-9, $"{equation} at row {row}, {at}: {value}");
                    }
                }
        }

        private static bool HasRow(Entity.Matrix solutions, string x, string y)
            => Enumerable.Range(0, solutions.RowCount).Any(row =>
                (solutions[row, 0] - x).Simplify() == 0 && (solutions[row, 1] - y).Simplify() == 0);

        /// <summary>
        /// The branch the elimination lost: <c>(0, 1)</c> for the first two, where <c>x</c> is
        /// zero, and <c>(1, e)</c> for the third, where <c>x - 1</c> is.
        /// </summary>
        [Theory]
        [InlineData("x * y", "sin(x) + y - 1", 3, "0", "1")]
        [InlineData("x * y", "y - cos(x)", 3, "0", "1")]
        [InlineData("(x - 1) * (y - 2)", "e^x - y", 2, "1", "e")]
        [InlineData("(x - 1) * (y - 2)", "e^x - y", 2, "ln(2)", "2")]
        public void EachFactorIsABranch(string first, string second, int rows, string x, string y)
        {
            var solutions = Solve(first, second);
            Assert.NotNull(solutions);
            Assert.Equal(rows, solutions!.RowCount);
            Assert.True(HasRow(solutions, x, y), $"({x}, {y}) is not among {solutions}");
            EveryRowHolds(solutions, first, second);
        }

        /// <summary>
        /// <c>x e^y = 0</c> and <c>x^2 y = 0</c> hold wherever <c>x</c> is zero, which was
        /// answered as the one point <c>(0, 0)</c>.
        /// </summary>
        [Fact]
        public void ABranchThatLeavesAnUnknownFreeIsAFamily()
        {
            var solutions = Solve("x * e^y", "x^2 * y");
            Assert.NotNull(solutions);
            Assert.Contains(Enumerable.Range(0, solutions!.RowCount),
                row => solutions[row, 0] == 0 && solutions[row, 1] is Entity.Variable);
            EveryRowHolds(solutions, "x * e^y", "x^2 * y");
        }

        /// <summary>
        /// The branch <c>x = 0</c> of <c>x ln(y) = 0</c> with <c>x = y</c> is <c>(0, 0)</c>,
        /// where <c>ln(0)</c> is not a number and the product is not zero.
        /// </summary>
        [Fact]
        public void AFactorZeroWhereAnotherIsUndefinedIsNoSolution()
        {
            var solutions = Solve("x * ln(y)", "x - y");
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions!.RowCount);
            Assert.True(HasRow(solutions, "1", "1"));
        }

        /// <summary>
        /// A branch can leave an unknown that no equation mentions, and so can a caller. It is
        /// a parameter, as in a system with fewer equations than unknowns; the elimination had
        /// no equation to take it from and answered nothing.
        /// </summary>
        [Fact]
        public void AnUnknownNoEquationMentionsIsAParameter()
        {
            var solutions = Solve("x - 1", "x^2 - 1");
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions!.RowCount);
            Assert.Equal(Entity.Number.Integer.Create(1), solutions[0, 0]);
            Assert.IsType<Entity.Variable>(solutions[0, 1]);
        }
    }
}
