//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text;
using AngouriMath.Functions.Algebra;
using static AngouriMath.Entity;

namespace AngouriMath.Core
{
    /// <summary>
    /// A class for systems of equations. Is not part of AM's ecosystem,
    /// that is, it is not an Entity, just an intermediate class.
    /// It is a system of arbitrary equations, not only those linear. However,
    /// it is not a system of <see cref="Statement"/>s.
    /// </summary>
    public sealed class EquationSystem : ILatexizeable
    {
        private readonly IEnumerable<Entity> equations;

        /// <summary>
        /// Each equation is an expression equal to zero, or an equality <c>a = b</c>, which is read as
        /// <c>a - b = 0</c>, as <see cref="MathS.SolveEquation"/> reads one in a single variable.
        /// A conjunction of equations, <c>x + y = 3 and x - y = 1</c>, is read as the equations
        /// it joins; one with any other conjunct is passed on as it is.
        /// After having created a system of equations, you may solve or latexize it.
        /// </summary>
        /// <param name="equations">
        /// Any <see cref="IEnumerable{T}"/> parameter, such as <see cref="List{T}"/> or <see cref="System.Array"/>.
        /// </param>
        /// <example>
        /// <code>
        /// var eq = new EquationSystem(new Entity[] {
        ///     "x + sin(y)2 + 3",
        ///     "y - a"
        /// });
        /// </code>
        /// </example>
        public EquationSystem(IEnumerable<Entity> equations) => this.equations = equations.SelectMany(AsExpressionsEqualToZero).ToList();

        /// <summary>
        /// Each equation is an expression equal to zero, or an equality <c>a = b</c>, which is read as
        /// <c>a - b = 0</c>, as <see cref="MathS.SolveEquation"/> reads one in a single variable.
        /// A conjunction of equations, <c>x + y = 3 and x - y = 1</c>, is read as the equations
        /// it joins; one with any other conjunct is passed on as it is.
        /// After having created a system of equations, you may solve or latexize it.
        /// </summary>
        /// <param name="equations">
        /// An <see cref="System.Array"/>
        /// </param>
        /// <example>
        /// <code>
        /// var eq = new EquationSystem(
        ///     "x + sin(y)2 + 3",
        ///     "y - a"
        /// );
        /// </code>
        /// </example>
        public EquationSystem(params Entity[] equations) => this.equations = equations.SelectMany(AsExpressionsEqualToZero).ToList();

        // An equality was passed on as it stood and the system answered that it had no solution:
        // `x + y = 3` and `x - y = 1` gave null, where `x + y - 3` and `x - y - 1` give `[[2, 1]]`.
        // https://github.com/asc-community/AngouriMath/issues/1673
        private static Entity AsAnExpressionEqualToZero(Entity equation)
            => equation is Equalsf(var left, var right) ? left - right : equation;

        // A conjunction of equations was passed on as one equation and the system had no
        // solution: x + y = 3 and x - y = 1 gave null, where the two equations listed give
        // [[2, 1]]. A conjunction is its conjuncts, where each is an equation; one with any other
        // conjunct -- an inequality, a membership -- is passed on as it was, since solving the
        // equations alone would answer a different question.
        // https://github.com/asc-community/AngouriMath/issues/2450
        private static IEnumerable<Entity> AsExpressionsEqualToZero(Entity equation)
            => equation is Andf && EquationsOf(equation, out _) is { } conjuncts
                ? conjuncts
                : new[] { AsAnExpressionEqualToZero(equation) };

        /// <summary>
        /// The equations of <paramref name="system"/>, each an expression equal to zero, where it
        /// is a conjunction of equations -- nested or not, and one equation is a conjunction of
        /// one -- or an expression, which is read as equal to zero. <see langword="null"/>, with
        /// the first conjunct that is not an equation in <paramref name="notAnEquation"/>, where
        /// any conjunct is something else: an inequality, a membership, a disjunction, a
        /// negation or a boolean. Leaving one out would answer a different question.
        /// </summary>
        /// <remarks>
        /// An equality whose side is itself a statement, <c>(x = 1) = (y = 2)</c>, is an
        /// equivalence of statements rather than an equation in numbers, and is not one either.
        /// https://github.com/asc-community/AngouriMath/issues/2450
        /// </remarks>
        internal static List<Entity>? EquationsOf(Entity system, out Entity? notAnEquation)
        {
            notAnEquation = null;
            if (system is not Statement)
                return new List<Entity> { system };
            var equations = new List<Entity>();
            var pending = new Stack<Entity>();
            pending.Push(system);
            while (pending.Count > 0)
            {
                switch (pending.Pop())
                {
                    case Andf(var left, var right):
                        pending.Push(right);
                        pending.Push(left);
                        break;
                    case Equalsf(var left, var right) when left is not Statement && right is not Statement:
                        equations.Add(left - right);
                        break;
                    case var other:
                        notAnEquation = other;
                        return null;
                }
            }
            return equations;
        }

        /// <summary>
        /// Solves a system of equations, if possible.
        /// </summary>
        /// <param name="vars">
        /// The unknowns, as many as there are equations or not. Where there are more, or where a
        /// system leaves one free, it becomes a parameter in the answer: <c>x^2 + y^2 = 1</c> for
        /// <c>x</c> and <c>y</c> is two rows, <c>x = t_1</c> with <c>y</c> the positive and the
        /// negative square root of <c>1 - t_1^2</c>.
        /// </param>
        /// <returns>
        /// If there exists at least one solution, returns a solution matrix
        /// The first axis of the matrix corresponds to the number of solutions,
        /// the second one corresponds to the number of variables.
        /// 
        /// The i-th column of the matrix represents the possible values of the i-th variable.
        /// 
        /// If no solution was found, returns a null.
        /// </returns>
        /// <exception cref="Exceptions.NotSufficientlySupportedException">
        /// The work the call may spend ran out before the system was solved: the budget in
        /// <see cref="MathS.Settings.Budget"/> where it is set, and the solver's own where it
        /// is not. This is "not settled", not
        /// "no solution"; raise the budget to allow more.
        /// </exception>
        public Matrix? Solve(params Variable[] vars) => EquationSolver.SolveSystem(equations, vars);

        /// <returns>
        /// Latexized version of the system. It adds
        /// zero equality to all the provided expressions
        /// </returns>
        public string Latexize()
        {
            using var enumerator = equations.GetEnumerator();
            if (!enumerator.MoveNext())
                return string.Empty;
            var firstEquation = enumerator.Current.Latexize() + " = 0";
            if (!enumerator.MoveNext())
                return firstEquation;
            var sb = new StringBuilder(@"\begin{cases}").Append(firstEquation);
            do sb.Append(@"\\").Append(enumerator.Current.Latexize()).Append(" = 0");
            while (enumerator.MoveNext());
            sb.Append(@"\end{cases}");
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToString() => string.Join("\n", equations.Select(c => c.Stringize() + " = 0"));
    }
}