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
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// <see cref="EquationSystem.SolveWithParameters"/>: the rows of a system's answer and the
    /// parameters they are written in, each with the set it ranges over.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// </summary>
    /// <remarks>
    /// Every answer here is checked by giving each parameter values from its set -- whole ones for
    /// a parameter over <c>ZZ</c>, which is all an <c>n_1</c> promises -- and putting each row back
    /// into each equation. A set that is wider than the one the family holds over fails that.
    /// </remarks>
    [Trait("Area", "Algebra")]
    public sealed class SystemParametersTest
    {
        private static ParametrizedSolutions Solved(string[] equations, params string[] unknowns)
        {
            var solved = MathS.Equations(equations.Select(equation => equation.ToEntity()))
                .SolveWithParameters(unknowns.Select(unknown => (Variable)unknown).ToArray());
            Assert.NotNull(solved);
            Assert.Equal(MathS.Equations(equations.Select(equation => equation.ToEntity()))
                .Solve(unknowns.Select(unknown => (Variable)unknown).ToArray()), solved!.Rows);
            // Solved again, the answer is equal, the list compared by its elements.
            Assert.Equal(solved, MathS.Equations(equations.Select(equation => equation.ToEntity()))
                .SolveWithParameters(unknowns.Select(unknown => (Variable)unknown).ToArray()));
            Holds(solved, equations, unknowns);
            return solved;
        }

        /// <summary>
        /// Two values of each parameter from its set, and each row put back into each equation
        /// at each pair of choices.
        /// </summary>
        private static void Holds(ParametrizedSolutions solved, string[] equations, string[] unknowns)
        {
            foreach (var choice in new[] { 0, 1 })
            {
                var values = solved.Parameters.Select((pair, index) => (pair.Parameter, Value: SampleOf(pair.Domain, index + choice))).ToList();
                for (var row = 0; row < solved.Rows.RowCount; row++)
                {
                    var point = new Dictionary<Variable, Entity>();
                    for (var column = 0; column < unknowns.Length; column++)
                    {
                        var entry = solved.Rows[row, column];
                        foreach (var (parameter, value) in values)
                            entry = entry.Substitute(parameter, value);
                        if (entry is Providedf(var bare, var condition))
                        {
                            Assert.Equal(Boolean.True, condition.Evaled);
                            entry = bare;
                        }
                        point[unknowns[column]] = entry;
                    }
                    foreach (var equation in equations)
                    {
                        // A symbol of the system's own, as k in 2x - 4y = k, at one value.
                        var substituted = equation.ToEntity().Substitute(point);
                        foreach (var symbol in substituted.Vars.ToList())
                            substituted = substituted.Substitute(symbol, Rational.Create(3, 7));
                        var residual = substituted.EvalNumerical();
                        Assert.True(residual.Abs() < 1e-9, $"{equation} at row {row} is {residual}");
                    }
                }
            }
        }

        private static Entity SampleOf(Set domain, int index)
        {
            if (domain == MathS.Sets.Z)
                return new[] { 2, -3, 5 }[index % 3];
            if (domain is Set.Interval(var left, _, var right, _))
                return Rational.Create(1 + index % 2, 3) * (right - left) + left;
            Assert.True(domain == MathS.Sets.R || domain == MathS.Sets.C, $"no sample of {domain}");
            return new[] { Rational.Create(7, 5), Rational.Create(-13, 10), Rational.Create(9, 4) }[index % 3];
        }

        private static (Variable, Set) In(string parameter, Set domain) => (parameter, domain);

        /// <summary>The issue's own example, with its one parameter over every number.</summary>
        [Fact]
        public void TheExampleTheIssueNames()
        {
            var solved = Solved(new[] { "2x - 4y - 12" }, "x", "y");
            Assert.Equal(MathS.Matrix(new Entity[,] { { "6 + 2 * t_1", "t_1" } }), solved.Rows);
            Assert.Equal(new[] { In("t_1", MathS.Sets.C) }, solved.Parameters);
        }

        /// <summary>
        /// A whole-number parameter beside a free one: <c>n_1</c> is said to be in <c>ZZ</c>,
        /// which nothing in the rows themselves says.
        /// </summary>
        [Fact]
        public void APeriodicFamilyNamesItsWholeNumberParameter()
        {
            var solved = Solved(new[] { "sin(x) - 1/2" }, "x", "y");
            Assert.Equal(MathS.Matrix(new Entity[,]
            {
                { "pi / 6 + 2 * pi * n_1", "t_1" },
                { "5/6 * pi + 2 * pi * n_1", "t_1" },
            }), solved.Rows);
            Assert.Equal(new[] { In("n_1", MathS.Sets.Z), In("t_1", MathS.Sets.C) }, solved.Parameters);
        }

        [Fact]
        public void ALinearFreedomAndAPeriodTogether()
        {
            var solved = Solved(new[] { "x + y - 1", "sin(z)" }, "x", "y", "z");
            Assert.Equal(new[] { In("t_1", MathS.Sets.C), In("n_1", MathS.Sets.Z) }, solved.Parameters);
        }

        /// <summary>A parameter shared by every column, since each unknown is the one before.</summary>
        [Fact]
        public void APeriodReachingAnotherEquationIsOneParameter()
        {
            var solved = Solved(new[] { "tan(x) - 1", "y - x" }, "x", "y");
            Assert.Equal(MathS.Matrix(new Entity[,] { { "pi / 4 + pi * n_1", "pi / 4 + pi * n_1" } }), solved.Rows);
            Assert.Equal(new[] { In("n_1", MathS.Sets.Z) }, solved.Parameters);
        }

        [Fact]
        public void AnAnswerWithoutParametersHasNone()
        {
            var solved = Solved(new[] { "x + y - 3", "x - y - 1" }, "x", "y");
            Assert.Empty(solved.Parameters);
        }

        [Fact]
        public void ASymbolOfTheSystemIsNotAParameter()
        {
            var solved = Solved(new[] { "2x - 4y - k" }, "x", "y");
            Assert.Equal(new[] { In("t_1", MathS.Sets.C) }, solved.Parameters);
        }

        /// <summary>A system with no solutions is <see langword="null"/>, as <c>Solve</c> answers it.</summary>
        [Fact]
        public void ASystemWithoutSolutionsIsNull()
            => Assert.Null(MathS.Equations("x + y - 1", "x + y - 2").SolveWithParameters("x", "y"));

        [Fact]
        public void ADeclinedSystemIsNull()
            => Assert.Null(MathS.Equations("x * e^x - 1").SolveWithParameters("x", "y"));

        /// <summary>
        /// Two equations that each bring a parameter of their own, and do not reach each other,
        /// have two parameters. As one name, <c>(pi/6 + 2 pi n_1, pi/6 + 2 pi n_1)</c>, the
        /// answer said <c>x - y</c> is a whole number of periods and left out
        /// <c>(pi/6, 13 pi/6)</c>.
        /// </summary>
        [Fact]
        public void IndependentPeriodsAreTwoParameters()
        {
            var solved = Solved(new[] { "sin(x) - 1/2", "sin(y) - 1/2" }, "x", "y");
            Assert.Equal(4, solved.Rows.RowCount);
            Assert.Equal((Entity)"pi / 6 + 2 * pi * n_1", solved.Rows[0, 0]);
            Assert.Equal((Entity)"pi / 6 + 2 * pi * n_2", solved.Rows[0, 1]);
            Assert.Equal(new[] { In("n_1", MathS.Sets.Z), In("n_2", MathS.Sets.Z) }, solved.Parameters);
        }

        [Fact]
        public void ThreeIndependentPeriodsAreThreeParameters()
        {
            var solved = Solved(new[] { "tan(x) - 1", "tan(y) - 1", "tan(z) - 1" }, "x", "y", "z");
            Assert.Equal(MathS.Matrix(new Entity[,] { { "pi / 4 + pi * n_1", "pi / 4 + pi * n_2", "pi / 4 + pi * n_3" } }), solved.Rows);
        }

        /// <summary>
        /// The parameter of <c>floor(x) = 1</c> ranges over <c>[0, 1)</c>, said where it is made,
        /// and two floors have one each.
        /// </summary>
        [Fact]
        public void AFloorsParameterRangesOverTheUnitInterval()
        {
            var solved = Solved(new[] { "floor(x) - 1", "floor(y) - 2" }, "x", "y");
            var unit = MathS.Sets.Interval(0, true, 1, false);
            Assert.Equal(new[] { In("t_1", unit), In("t_2", unit) }, solved.Parameters);
        }

        [Fact]
        public void TheArgumentOfAGivenModulusIsReal()
        {
            var solved = Solved(new[] { "abs(x) - 1", "abs(y) - 2" }, "x", "y");
            Assert.Equal(new[] { In("r_1", MathS.Sets.R), In("r_2", MathS.Sets.R) }, solved.Parameters);
        }

        /// <summary>
        /// Rows are separate families: <c>t_1</c> is <c>y</c> in the rows where <c>sin(x)</c> is
        /// zero and <c>x</c> where <c>cos(y)</c> is, over every number in both.
        /// </summary>
        [Fact]
        public void ANameInTwoRowsHasOneSet()
        {
            var solved = Solved(new[] { "sin(x) * cos(y)" }, "x", "y");
            Assert.Equal(new[] { In("n_1", MathS.Sets.Z), In("t_1", MathS.Sets.C) }, solved.Parameters);
        }

        [Fact]
        public void OverTheRealsAFreeUnknownIsReal()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            var solved = MathS.Equations("2x - 4y - 12").SolveWithParameters("x", "y");
            Assert.NotNull(solved);
            Assert.Equal(new[] { In("t_1", MathS.Sets.R) }, solved!.Parameters);
        }

        [Fact]
        public void AConjunctionIsSolvedWithItsParameters()
        {
            var solved = MathS.SolveSystemWithParameters("sin(x) = 1/2 and y - x = 0", "x", "y");
            Assert.NotNull(solved);
            Assert.Equal(MathS.SolveSystem("sin(x) = 1/2 and y - x = 0", "x", "y"), solved!.Rows);
            Assert.Equal(new[] { In("n_1", MathS.Sets.Z) }, solved.Parameters);
            Assert.Null(MathS.SolveSystemWithParameters("x + y = 3 and x > 0", "x", "y"));
        }
    }
}
