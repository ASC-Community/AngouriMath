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
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// A system with more equations than unknowns, some of them multiples of others, was refused. The
    /// elimination solved one equation for the last unknown and put the root into the rest, which left
    /// <c>2x - 2x</c>. That still mentions <c>x</c>, solving it for <c>x</c> gave <c>CC</c>, and an unknown
    /// whose equations gave no roots ended the elimination as undecided. An equation that is zero for every
    /// value constrains nothing, and is dropped.
    /// https://github.com/asc-community/AngouriMath/issues/2601
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARedundantEquationConstrainsNothingTest
    {
        private static Matrix? Solve(string[] equations, string[] variables)
            => MathS.Equations(equations.Select(e => e.ToEntity()).ToArray())
                .Solve(variables.Select(v => (Variable)v).ToArray());

        [Theory]
        [InlineData(new[] { "x - y", "2x - 2y", "3x - 3y" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "x + 2y - 3", "2x + 4y - 6", "3x + 6y - 9" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "x + y + z - 1", "2x + 2y + 2z - 2", "3x + 3y + 3z - 3" }, new[] { "x", "y", "z" }, 2)]
        [InlineData(new[] { "sin(x) - y", "2 sin(x) - 2y", "3 sin(x) - 3y" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "x - y", "2x - 2y", "x + y - 2" }, new[] { "x", "y" }, 0)]
        // Was [[0, 0, 0]], one point of a curve of solutions: x = t, y = -t/(1 + t), z = x y.
        [InlineData(new[] { "x y - z", "2 x y - 2 z", "x + y + z" }, new[] { "x", "y", "z" }, 1)]
        public void IsAnsweredAsTheFamilyOfTheRest(string[] equations, string[] variables, int freedom)
        {
            var solutions = Solve(equations, variables);
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions!.RowCount);
            var unknowns = variables.Select(v => (Variable)v).ToHashSet();
            var parameters = solutions.Nodes.OfType<Variable>().Where(v => !unknowns.Contains(v)).Distinct().ToList();
            // As many parameters as the system leaves free: none is lost, and none is made up.
            Assert.Equal(freedom, parameters.Count);
            var samples = new[] { 1.37, -1.7, 2.3, -0.61 };
            foreach (var text in equations)
            {
                Entity residual = text.ToEntity();
                for (var column = 0; column < variables.Length; column++)
                    residual = residual.Substitute(variables[column], solutions[0, column]);
                for (var shift = 0; shift < samples.Length; shift++)
                {
                    var pinned = residual;
                    for (var i = 0; i < parameters.Count; i++)
                        pinned = pinned.Substitute(parameters[i], samples[(shift + i) % samples.Length]);
                    var value = Assert.IsAssignableFrom<Number.Complex>(pinned.Evaled);
                    Assert.True(((Number.Real)value.Abs()).EDecimal.ToDouble() < 1e-9, $"{residual} at {pinned} is {value}");
                }
            }
        }

        // A copy dropped is not an inconsistency dropped: 0 = 1 is left after x = y, and there is no solution.
        [Theory]
        [InlineData(new[] { "x - y", "2x - 2y", "x - y - 1" }, new[] { "x", "y" })]
        [InlineData(new[] { "x + y + z - 1", "2x + 2y + 2z - 2", "x + y + z - 2" }, new[] { "x", "y", "z" })]
        [InlineData(new[] { "sin(x) - y", "2 sin(x) - 2y", "sin(x) - y - 1" }, new[] { "x", "y" })]
        public void AnInconsistentOneStillHasNoSolution(string[] equations, string[] variables)
            => Assert.Null(Solve(equations, variables));
    }
}
