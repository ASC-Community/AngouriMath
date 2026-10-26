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
    /// Systems with fewer equations than unknowns, which have infinitely many solutions and are
    /// answered as the one family that describes them all.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A system of that shape used to raise <c>WrongNumberOfArgumentsException</c> — which reads
    /// as "you called this wrongly" for a caller who did nothing wrong. <c>2x - 4y = 12</c> for
    /// <c>x</c> and <c>y</c> is a well-formed question with an infinite answer set, and the
    /// answer is now that set: one row, with a parameter where the system leaves a degree of
    /// freedom.
    /// </para>
    /// <para>
    /// Every case here is checked by substituting the answer back into every equation and
    /// simplifying to zero, with the parameter left symbolic — a family that satisfies the
    /// system only at particular values of its parameter is not a family.
    /// </para>
    /// </remarks>
    [Trait("Area", "Algebra")]
    public sealed class ParametricSystemTest
    {
        /// <summary>
        /// Substitutes the answer back with the parameters still symbolic, so what is checked is
        /// the whole family and not one member of it.
        /// </summary>
        private static Entity.Matrix Satisfies(string[] equations, params string[] variables)
        {
            var solutions = MathS.Equations(equations.Select(e => e.ToEntity()).ToArray())
                .Solve(variables.Select(v => (Entity.Variable)v).ToArray());
            Assert.NotNull(solutions);
            Assert.Equal(variables.Length, solutions!.ColumnCount);

            for (var row = 0; row < solutions.RowCount; row++)
                foreach (var text in equations)
                {
                    Entity residual = text.ToEntity();
                    for (var column = 0; column < variables.Length; column++)
                        residual = residual.Substitute(variables[column], solutions[row, column]);
                    Assert.Equal(Entity.Number.Integer.Create(0), residual.Simplify());
                }
            return solutions;
        }

        /// <summary>The example in the issue's own body, and the shape it asks for.</summary>
        [Fact]
        public void TheExampleTheIssueNames()
        {
            var solutions = Satisfies(new[] { "2*x - 4*y - 12" }, "x", "y");
            Assert.Equal(1, solutions.RowCount);
            // y is the free one and x is written in terms of it, which is `x = 6 + 2t, y = t`.
            Assert.Single(solutions[0, 1].Vars);
            Assert.Equal(solutions[0, 1].Vars.Single(), solutions[0, 0].Vars.Single());
        }

        /// <summary>
        /// The five-unknown system Happypig375 worked out by hand in the thread, whose answer
        /// was <c>x1 = 2s + 3t + 7</c>, <c>x2 = -3s - 2t - 3</c>, <c>x3 = s</c>, <c>x4 = 2t</c>,
        /// <c>x5 = t</c>. Renamed, since a digit suffix does not parse as a variable.
        /// </summary>
        [Fact]
        public void TheFiveUnknownSystemFromTheThread()
        {
            var solutions = Satisfies(
                new[]
                {
                    "p + 2*q + 4*r + s - u - 1",
                    "2*p + 4*q + 8*r + 3*s - 4*u - 2",
                    "p + 3*q + 7*r + 3*u + 2",
                },
                "p", "q", "r", "s", "u");
            Assert.Equal(1, solutions.RowCount);
            // Three equations of rank three over five unknowns leaves two degrees of freedom.
            var parameters = new System.Collections.Generic.HashSet<Entity.Variable>();
            for (var column = 0; column < solutions.ColumnCount; column++)
                foreach (var variable in solutions[0, column].Vars)
                    parameters.Add(variable);
            Assert.Equal(2, parameters.Count);
        }

        /// <summary>One equation short, two short, and several equations with several unknowns.</summary>
        [Theory]
        [InlineData(new[] { "x + y - 3" }, new[] { "x", "y" })]
        [InlineData(new[] { "x + y + z - 1" }, new[] { "x", "y", "z" })]
        [InlineData(new[] { "x + y + z - 1", "x - y" }, new[] { "x", "y", "z" })]
        [InlineData(new[] { "2*x + 3*y - 5*z - 7" }, new[] { "x", "y", "z" })]
        [InlineData(new[] { "x/2 - y/3 - 1" }, new[] { "x", "y" })]
        public void AFamilySatisfiesEveryEquation(string[] equations, string[] variables)
            => Satisfies(equations, variables);

        /// <summary>
        /// The constant term may be anything free of the unknowns, since it is never a pivot and
        /// so never has to be tested for zero. Only the coefficients must be rational.
        /// </summary>
        [Fact]
        public void ASymbolicConstantIsCarriedThrough()
        {
            var solutions = Satisfies(new[] { "2*x - 4*y - k" }, "x", "y");
            Assert.Contains((Entity.Variable)"k", solutions[0, 0].Vars);
        }

        /// <summary>
        /// A short system whose equations contradict each other has no solutions, and that is an
        /// answer rather than a failure to find one — <see langword="null"/>, the same as for a
        /// contradictory square system.
        /// </summary>
        [Fact]
        public void AContradictoryShortSystemHasNoSolutions()
            => Assert.Null(MathS.Equations(new Entity[] { "x + y - 1", "2*x + 2*y - 3" })
                .Solve("x", "y", "z"));

        /// <summary>
        /// The neighbours must answer exactly as they did: a determined system, an
        /// overdetermined but consistent one, and a contradictory one. Only the short count
        /// reaches the new path.
        /// </summary>
        [Fact]
        public void TheOtherShapesAreUnchanged()
        {
            var determined = MathS.Equations(new Entity[] { "x + y - 3", "x - y - 1" }).Solve("x", "y");
            Assert.NotNull(determined);
            Assert.Equal(Entity.Number.Integer.Create(2), determined![0, 0].Simplify());
            Assert.Equal(Entity.Number.Integer.Create(1), determined[0, 1].Simplify());

            var overdetermined = MathS.Equations(new Entity[] { "x + y - 3", "x - y - 1", "2*x - 4" })
                .Solve("x", "y");
            Assert.NotNull(overdetermined);
            Assert.Equal(Entity.Number.Integer.Create(2), overdetermined![0, 0].Simplify());

            Assert.Null(MathS.Equations(new Entity[] { "x - 1", "x - 2" }).Solve("x"));
        }

        /// <summary>
        /// A square system that is rank-deficient is left to the eliminator, which has answered
        /// it since <a href="https://github.com/asc-community/AngouriMath/issues/550">#550</a>.
        /// Taking those here too would change answers that are not wrong.
        /// </summary>
        [Fact]
        public void ARankDeficientSquareSystemIsLeftAlone()
        {
            var solutions = MathS.Equations(new Entity[] { "x + y - 3", "x + y - 3" }).Solve("x", "y");
            Assert.NotNull(solutions);
            Assert.NotEmpty(solutions![0, 0].Vars);
        }
    }
}
