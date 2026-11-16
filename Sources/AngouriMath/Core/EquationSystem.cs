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

        /// <summary>
        /// Solves the system as <see cref="Solve(Variable[])"/> does, and says what each parameter
        /// in the answer is and which set it ranges over.
        /// </summary>
        /// <param name="vars">The unknowns, as <see cref="Solve(Variable[])"/> takes them.</param>
        /// <returns>
        /// The rows <see cref="Solve(Variable[])"/> answers, in
        /// <see cref="ParametrizedSolutions.Rows"/>, and in
        /// <see cref="ParametrizedSolutions.Parameters"/> each symbol of theirs that is neither an
        /// unknown nor a symbol of the equations, in the order it first appears, with the set it
        /// ranges over: <c>t_1</c> over <c>CC</c> for an unknown nothing constrains (<c>RR</c>
        /// where <see cref="MathS.Settings.Codomain"/> is <see cref="Domain.Real"/>), <c>n_1</c>
        /// over <c>ZZ</c> for a whole number of periods or a branch of the Lambert W function,
        /// <c>r_1</c> over <c>RR</c> for the argument of a number of given modulus, and the
        /// <c>t_1</c> of <c>floor(x) = 1</c> over <c>[0, 1)</c>. The list is empty where the
        /// answer has no parameter. <see langword="null"/> where <see cref="Solve(Variable[])"/>
        /// is, and where a parameter is one this cannot name the set of.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Each row is a family of solutions, one for each value of the parameters it mentions,
        /// taken from their sets; a row need not mention all of them. A name stands for one set
        /// throughout the answer, and within a row two parameters are two names: in
        /// <c>sin(x) = 1/2</c>, <c>sin(y) = 1/2</c> the rows read
        /// <c>(pi/6 + 2 pi n_1, pi/6 + 2 pi n_2)</c>. Rows are separate families, so a
        /// name in two rows is chosen independently in each.
        /// </para>
        /// <para>
        /// A row may say where it holds, as <c>1 + t_1 provided t_1 in RR and t_1 &gt;= 0 and
        /// t_1 &lt; 1</c> does; the set of its parameter is said here either way.
        /// </para>
        /// https://github.com/asc-community/AngouriMath/issues/212
        /// </remarks>
        /// <exception cref="Exceptions.NotSufficientlySupportedException">
        /// As <see cref="Solve(Variable[])"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// var solved = MathS.Equations("sin(x) - 1/2").SolveWithParameters("x", "y");
        /// Console.WriteLine(solved!.Rows);
        /// foreach (var (parameter, domain) in solved.Parameters)
        ///     Console.WriteLine($"{parameter} in {domain}");
        /// </code>
        /// Prints
        /// <code>
        /// [[pi / 6 + 2 * pi * n_1, t_1], [pi - pi / 6 + 2 * pi * n_1, t_1]]
        /// n_1 in ZZ
        /// t_1 in CC
        /// </code>
        /// </example>
        public ParametrizedSolutions? SolveWithParameters(params Variable[] vars)
        {
            var scope = AngouriMath.Functions.Algebra.AnalyticalSolving.SolutionParameters.Open(out var opened);
            try
            {
                if (Solve(vars) is not { } rows)
                    return null;
                var known = new HashSet<Variable>(equations.SelectMany(equation => equation.Vars));
                known.UnionWith(vars);
                var parameters = new List<(Variable Parameter, Set Domain)>();
                for (var row = 0; row < rows.RowCount; row++)
                    for (var column = 0; column < rows.ColumnCount; column++)
                        foreach (var symbol in rows[row, column].Vars)
                        {
                            if (!known.Add(symbol))
                                continue;
                            if (scope.SetOf(symbol) is not { } domain)
                                return null;
                            parameters.Add((symbol, domain));
                        }
                return new ParametrizedSolutions(rows, parameters);
            }
            finally
            {
                if (opened)
                    scope.Close();
            }
        }

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

    /// <summary>
    /// The solutions of a system as families: the rows <see cref="EquationSystem.Solve"/>
    /// answers, and the parameters they are written in, each with the set it ranges over.
    /// See <see cref="EquationSystem.SolveWithParameters"/>.
    /// https://github.com/asc-community/AngouriMath/issues/212
    /// </summary>
    /// <param name="Rows">
    /// A row for each family of solutions and a column for each unknown, as
    /// <see cref="EquationSystem.Solve"/> answers them.
    /// </param>
    /// <param name="Parameters">
    /// Each parameter of <paramref name="Rows"/> and the set it ranges over, in the order the
    /// parameters first appear: <c>(n_1, ZZ)</c>, <c>(t_1, CC)</c>. Empty where no row has one.
    /// </param>
    public sealed record ParametrizedSolutions(Matrix Rows, IReadOnlyList<(Variable Parameter, Set Domain)> Parameters)
    {
        /// <summary>
        /// Whether the rows are equal and the parameters are the same, in the same order, with
        /// the same sets: compared element by element, where a record compares a list by reference.
        /// </summary>
        public bool Equals(ParametrizedSolutions? other)
            => other is not null && Rows == other.Rows && Parameters.SequenceEqual(other.Parameters);

        /// <inheritdoc/>
        public override int GetHashCode()
            => Parameters.Aggregate(Rows.GetHashCode(), (hash, pair) => unchecked(hash * 31 + pair.GetHashCode()));
    }
}