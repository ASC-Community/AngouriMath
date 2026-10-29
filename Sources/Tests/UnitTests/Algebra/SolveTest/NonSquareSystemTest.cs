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
    /// Systems with more or fewer equations than unknowns that are not linear over the
    /// rationals, which <see cref="ParametricSystemTest"/> answers. Each of these raised
    /// <c>WrongNumberOfArgumentsException</c>, and is now answered or declined as
    /// <see langword="null"/>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class NonSquareSystemTest
    {
        private static Entity.Matrix? Solve(string[] equations, string[] variables)
            => MathS.Equations(equations.Select(e => e.ToEntity()).ToArray())
                .Solve(variables.Select(v => (Entity.Variable)v).ToArray());

        /// <summary>
        /// Each row, substituted into each equation, is zero at several values of the parameters
        /// and symbols left in it, negative ones among them. That is checked numerically rather
        /// than by simplifying, which is how the solver checks it: <c>sqrt(t^2) - t</c> is zero at
        /// every positive <c>t</c> and at no negative one. An <c>n_1</c>, which a periodic root
        /// brings and which stands for a whole number, is sampled at whole numbers.
        /// </summary>
        private static void HoldsAtSampledValues(string[] equations, string[] variables, Entity.Matrix solutions)
        {
            var samples = new[] { 1.37, -1.7, 2.3, -0.61 };
            var wholeSamples = new[] { 2, -1, 3, 0 };
            for (var row = 0; row < solutions.RowCount; row++)
                foreach (var text in equations)
                {
                    Entity residual = text.ToEntity();
                    for (var column = 0; column < variables.Length; column++)
                        residual = residual.Substitute(variables[column], solutions[row, column]);
                    var symbols = residual.Vars.ToList();
                    for (var shift = 0; shift < samples.Length; shift++)
                    {
                        var pinned = residual;
                        for (var i = 0; i < symbols.Count; i++)
                            pinned = symbols[i].Name.StartsWith("n_") && !equations.Any(equation => equation.ToEntity().Vars.Contains(symbols[i]))
                                ? pinned.Substitute(symbols[i], wholeSamples[(shift + i) % wholeSamples.Length])
                                : pinned.Substitute(symbols[i], samples[(shift + i) % samples.Length]);
                        var value = Assert.IsAssignableFrom<Entity.Number.Complex>(pinned.Evaled);
                        Assert.False(value.IsNaN, $"{residual} at {pinned}");
                        Assert.True(((Entity.Number.Real)value.Abs()).EDecimal.ToDouble() < 1e-9, $"{residual} at {pinned} is {value}");
                    }
                }
        }

        /// <summary>
        /// Fewer equations than unknowns, not linear in them or with a symbol for a coefficient:
        /// an unknown that is left free is a parameter, and a root with two values is two rows.
        /// </summary>
        [Theory]
        [InlineData(new[] { "x^2 + y^2 - 1" }, new[] { "x", "y" }, 2)]
        [InlineData(new[] { "x*y - 1" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "a*x + y - 1" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "sin(x) + y" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "e^x - y", "y - z" }, new[] { "x", "y", "z" }, 1)]
        [InlineData(new[] { "x^2 + y - 1" }, new[] { "x", "y", "z" }, 1)]
        public void AShortSystemIsAFamily(string[] equations, string[] variables, int rows)
        {
            var solutions = Solve(equations, variables);
            Assert.NotNull(solutions);
            Assert.Equal(rows, solutions!.RowCount);
            Assert.Equal(variables.Length, solutions.ColumnCount);
            HoldsAtSampledValues(equations, variables, solutions);
        }

        /// <summary>
        /// A family a periodic root brings a whole-number parameter to, <c>n_1</c>, which holds for
        /// every whole <c>n_1</c> and is answered: <c>sin(x) = 1/2</c> in <c>x</c> and <c>y</c> is
        /// the two families <c>(pi/6 + 2 pi n_1, t_1)</c> and <c>(pi - pi/6 + 2 pi n_1, t_1)</c>.
        /// </summary>
        [Theory]
        [InlineData(new[] { "sin(x) - 1/2" }, new[] { "x", "y" }, 2)]
        [InlineData(new[] { "cos(x) - 1" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "tan(x) - 1" }, new[] { "x", "y" }, 1)]
        [InlineData(new[] { "sin(x) - 1/2", "y - x" }, new[] { "x", "y", "z" }, 2)]
        public void AFamilyWithAWholeNumberParameterIsAnswered(string[] equations, string[] variables, int rows)
        {
            var solutions = Solve(equations, variables);
            Assert.NotNull(solutions);
            Assert.Equal(rows, solutions!.RowCount);
            Assert.Contains(solutions.Vars, name => name.Name.StartsWith("n_"));
            HoldsAtSampledValues(equations, variables, solutions);
        }

        /// <summary>
        /// More equations than unknowns, which the triangularisation does not take because they
        /// are not polynomial. The last drops <c>x = -1</c>, where <c>sin(-pi/2) - 1</c> is -2.
        /// </summary>
        [Theory]
        [InlineData(new[] { "sin(x)", "x" }, new[] { "x" }, new[] { "0" })]
        [InlineData(new[] { "e^x - 1", "x + y", "y" }, new[] { "x", "y" }, new[] { "0", "0" })]
        [InlineData(new[] { "sin(x) - y", "cos(x) - y", "x - pi/4" }, new[] { "x", "y" }, new[] { "pi/4", "sqrt(2)/2" })]
        [InlineData(new[] { "x^2 - 1", "sin(pi*x/2) - 1" }, new[] { "x" }, new[] { "1" })]
        public void AnOverdeterminedSystemIsSolved(string[] equations, string[] variables, string[] solution)
        {
            var solutions = Solve(equations, variables);
            Assert.NotNull(solutions);
            Assert.Equal(1, solutions!.RowCount);
            for (var column = 0; column < variables.Length; column++)
                Assert.Equal(Entity.Number.Integer.Create(0), (solutions[0, column] - solution[column]).Simplify());
        }

        /// <summary>
        /// What the elimination finds and cannot show to hold. <c>sqrt(y) = x</c> is solved for
        /// <c>y</c> as <c>x^2</c>, no solution where <c>x</c> is negative. The coefficient of
        /// <c>y</c> in the second is zero without being written so, and dividing by it gives a
        /// family that <c>Simplify</c> calls zero provided it is not zero. The third has solutions
        /// only where <c>a + b = 2c</c>.
        /// </summary>
        [Theory]
        [InlineData(new[] { "sqrt(y) - x" }, new[] { "x", "y" })]
        [InlineData(new[] { "x^2 - ((a + 1)^2 - a^2 - 2*a - 1)*y" }, new[] { "x", "y" })]
        [InlineData(new[] { "x + y - a", "x - y - b", "x - c" }, new[] { "x", "y" })]
        public void WhatCannotBeShownToHoldIsDeclined(string[] equations, string[] variables)
            => Assert.Null(Solve(equations, variables));
    }
}
