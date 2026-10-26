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
    /// Square systems where solving one equation widens it, so the elimination finds rows that
    /// are no solution: <c>sqrt(y) = x</c> is solved for <c>y</c> as <c>x^2</c>, which holds only
    /// where <c>x</c> is not negative. Each was answered with such a row, and the rows are now
    /// put back into the system.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2123">#2123</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class SpuriousSystemRowTest
    {
        private static Entity.Matrix? Solve(params string[] equations)
            => MathS.Equations(equations.Select(e => e.ToEntity()).ToArray()).Solve("x", "y");

        /// <summary>
        /// A row that fails an equation with no symbol left in it is no solution for any value
        /// of anything, and is dropped. <c>[-2, 4]</c> leaves <c>sqrt(4) - (-2) = 4</c>, and
        /// <c>[-1, -1]</c> leaves <c>ln(-1) + ln(-1) = 2 pi i</c>.
        /// </summary>
        [Theory]
        [InlineData("sqrt(y) - x", "y - 4", "2", "4")]
        [InlineData("ln(x) + ln(y)", "x - y", "1", "1")]
        public void ARowThatIsNoSolutionIsDropped(string first, string second, string x, string y)
        {
            var solutions = Solve(first, second);
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions!.RowCount);
            Assert.Equal(Entity.Number.Integer.Create(0), (solutions[0, 0] - x).Simplify());
            Assert.Equal(Entity.Number.Integer.Create(0), (solutions[0, 1] - y).Simplify());
        }

        /// <summary>
        /// <c>[-1, 1]</c> was the only row, and <c>sqrt(1) - (-1)</c> is 2: the system has no
        /// solution, which <see cref="EquationSystem.Solve"/> answers as <see langword="null"/>.
        /// </summary>
        [Fact]
        public void ASystemWhoseOnlyRowIsNoSolutionHasNone()
            => Assert.Null(Solve("sqrt(y) - x", "x + 1"));

        /// <summary>
        /// <c>[-sqrt(a), a]</c> fails at <c>a = 137/100</c> and holds at <c>a = 0</c>, so it
        /// cannot be dropped as no solution, and the system is declined instead of being
        /// answered by the other row alone. A row like it that holds for some values only, as
        /// <c>sqrt(t^2) = t</c> does for positive <c>t</c>, would be lost by dropping it.
        /// </summary>
        [Fact]
        public void ARowThatFailsForSomeValueDeclinesTheSystem()
            => Assert.Null(Solve("sqrt(y) - x", "y - a"));

        /// <summary>
        /// What was answered rightly still is: rows in a symbol of the system's own, and rows in
        /// an integer parameter, which are tried with it at a whole number and not at the value
        /// the system's own symbols are tried at. Each row is checked here at several values of
        /// both, negative ones among them.
        /// </summary>
        [Theory]
        [InlineData("x * y - a", "x - y", 2)]
        [InlineData("x ^ 2 - a", "y - x", 2)]
        [InlineData("sin(x) - y", "y - 1/2", 2)]
        [InlineData("tan(x) - y", "y - 1", 1)]
        [InlineData("sqrt(x) + y", "x - 1", 1)]
        public void ARowThatHoldsIsKept(string first, string second, int rows)
        {
            var solutions = Solve(first, second);
            Assert.NotNull(solutions);
            Assert.Equal(rows, solutions!.RowCount);
            foreach (var at in new[] { -1, 0, 2 })
                for (var row = 0; row < solutions.RowCount; row++)
                    foreach (var equation in new[] { first, second })
                    {
                        var residual = equation.ToEntity().Substitute("x", solutions[row, 0]).Substitute("y", solutions[row, 1]);
                        foreach (var symbol in residual.Vars.ToList())
                            residual = residual.Substitute(symbol, symbol.Name == "a" ? 1.7 + at : at);
                        var value = Assert.IsAssignableFrom<Entity.Number.Complex>(residual.Evaled);
                        Assert.True(((Entity.Number.Real)value.Abs()).EDecimal.ToDouble() < 1e-9, $"{equation} at row {row}, {at}: {value}");
                    }
        }
    }
}
