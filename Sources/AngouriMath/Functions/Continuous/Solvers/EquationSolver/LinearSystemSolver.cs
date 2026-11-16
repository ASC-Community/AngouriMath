//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using System.Diagnostics.CodeAnalysis;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// A linear system with fewer equations than unknowns, answered as the family of all its
    /// solutions: the unknowns a row reduction leaves free become parameters, and the rest are
    /// written in terms of them.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>2x - 4y = 12</c> for <c>x</c> and <c>y</c> is the issue's own example. It has
    /// infinitely many solutions and one degree of freedom, so the answer is a single row
    /// <c>[12/2 + 2t, t]</c> rather than a list of them, and the <c>t</c> in it is a variable
    /// the caller did not name.
    /// </para>
    /// <para>
    /// <b>The answer type did not need changing, which is why this is small.</b> A solution is a
    /// row of <see cref="Matrix"/> whose i-th entry is the i-th unknown's value, and nothing
    /// says those entries may not mention a variable. The same device already carries the
    /// constant of integration out of the ODE solver, and
    /// <see cref="EquationSolver.InSolveSystem"/> already mints one <c>t</c> for the single case
    /// where its recursion bottoms out unconstrained
    /// (<a href="https://github.com/asc-community/AngouriMath/issues/550">#550</a>).
    /// </para>
    /// <para>
    /// <b>Reached only where the count is short</b>, which is the one shape that had no answer
    /// at all — <c>SolveSystem</c> threw <c>WrongNumberOfArgumentsException</c> for it before
    /// reaching any elimination. A square system that is rank-deficient is left to the
    /// eliminator, which already answers it; taking those here as well would change answers that
    /// are not wrong.
    /// </para>
    /// <para>
    /// <b>Rational coefficients on the unknowns, and that is a soundness requirement rather than
    /// a convenience.</b> Row reduction has to decide whether a pivot is zero, and the general
    /// test available here is structural — <c>Matrix.ReducedRowEchelonForm</c> asks
    /// <c>a == 0</c>, which an expression that is zero everywhere without being written as
    /// <c>0</c> fails. Choosing such a pivot divides by zero and produces a wrong family rather
    /// than no answer. Over the rationals the question is decidable, so it is asked there. The
    /// <b>constant</b> term is under no such restriction: it is never a pivot, so
    /// <c>2x - 4y = k</c> is answered with <c>k</c> symbolic.
    /// </para>
    /// </remarks>
    internal static class LinearSystemSolver
    {
        /// <summary>
        /// The family of solutions of <paramref name="equations"/> in <paramref name="variables"/>,
        /// or <see langword="false"/> where this does not apply. A <see langword="true"/> with a
        /// <see langword="null"/> matrix is the answer that the system has no solutions at all.
        /// </summary>
        internal static bool TrySolve(
            IReadOnlyList<Entity> equations, IReadOnlyList<Variable> variables, out Matrix? solution)
        {
            solution = null;
            if (variables.Count == 0 || equations.Count == 0 || equations.Count >= variables.Count)
                return false;

            var width = variables.Count;
            var coefficients = new ERational[equations.Count][];
            var constants = new Entity[equations.Count];
            for (var row = 0; row < equations.Count; row++)
            {
                if (!TryReadLinear(equations[row], variables, out var read, out var offset))
                    return false;
                coefficients[row] = read;
                constants[row] = offset;
            }

            // Ax = b, with b the constant moved across. The coefficients are exact rationals and
            // the right-hand side is whatever the equations had in them.
            for (var row = 0; row < constants.Length; row++)
                constants[row] = (-constants[row]).InnerSimplified;

            var pivotOfRow = new int[equations.Count];
            for (var row = 0; row < pivotOfRow.Length; row++)
                pivotOfRow[row] = -1;
            var rank = Reduce(coefficients, constants, width, pivotOfRow);

            // A row that reduced to 0 = c with c not zero says the system is inconsistent, and
            // that is an answer -- no solutions -- rather than a failure to find one.
            for (var row = rank; row < equations.Count; row++)
                if (constants[row].Evaled is not Integer { IsZero: true })
                {
                    if (constants[row].Evaled is Number { IsExact: true })
                        return true;
                    // A constant that cannot be decided either way is not evidence of
                    // inconsistency, and answering "no solutions" on it would be a wrong answer
                    // rather than an absent one.
                    return false;
                }

            var isPivot = new bool[width];
            for (var row = 0; row < rank; row++)
                isPivot[pivotOfRow[row]] = true;

            // One parameter per free column. Each is minted against the equations *and* the
            // parameters already minted, since CreateUnique compares whole names in use and
            // would otherwise hand back the same name twice.
            var nameSource = Sumf.Sum(equations);
            var values = new Entity[width];
            for (var column = 0; column < width; column++)
                if (!isPivot[column])
                {
                    var parameter = SolutionParameters.Mint(nameSource, "t", SolutionParameters.Free);
                    values[column] = parameter;
                    nameSource += parameter;
                }

            // Back-substitution is a lookup rather than a sweep: the reduction leaves each pivot
            // alone in its column, so a pivot's value is its own row's constant less the free
            // columns of that row.
            for (var row = 0; row < rank; row++)
            {
                var column = pivotOfRow[row];
                Entity value = constants[row];
                for (var other = column + 1; other < width; other++)
                    if (!isPivot[other] && !coefficients[row][other].IsZero)
                        // The coefficient is negated rather than the term subtracted, so that a
                        // negative one reads as `6 + 2 * t_1` and not as `6 - (-2) * t_1`.
                        value += Rational.Create(coefficients[row][other].Negate()) * values[other];
                values[column] = value.InnerSimplified;
            }

            var builder = new MatrixBuilder(width);
            builder.Add(new List<Entity>(values));
            solution = builder.ToMatrix();
            return solution is not null;
        }

        /// <summary>
        /// <paramref name="equation"/> read as <c>c_1 v_1 + ... + c_n v_n + constant</c>, or
        /// <see langword="false"/> where it is not linear in <paramref name="variables"/> with
        /// rational coefficients.
        /// </summary>
        /// <remarks>
        /// Linearity is checked rather than assumed, the way
        /// <see cref="OrdinaryDifferentialEquation"/> checks it: the coefficients are read off by
        /// differentiating, which is only valid if the equation really is linear in them, so the
        /// reading is put back together and compared with what it came from. <c>x * y</c> and
        /// <c>x ^ 2</c> both survive the differentiation and both fail the comparison.
        /// </remarks>
        private static bool TryReadLinear(
            Entity equation, IReadOnlyList<Variable> variables,
            [NotNullWhen(true)] out ERational[]? coefficients, [NotNullWhen(true)] out Entity? constant)
        {
            coefficients = null;
            constant = null;

            var read = new ERational[variables.Count];
            Entity reassembled = Integer.Create(0);
            for (var i = 0; i < variables.Count; i++)
            {
                var derivative = equation.Differentiate(variables[i]).Simplify();
                if (derivative.Evaled is not Rational ratio || derivative.Vars.Any())
                    return false;
                read[i] = ratio.ERational;
                reassembled += Rational.Create(ratio.ERational) * variables[i];
            }

            var withoutVariables = equation;
            foreach (var variable in variables)
                withoutVariables = withoutVariables.Substitute(variable, Integer.Create(0));
            var offset = withoutVariables.Simplify();
            if (variables.Any(variable => offset.ContainsNode(variable)))
                return false;

            if ((reassembled + offset - equation).Simplify() is not Integer { IsZero: true })
                return false;

            coefficients = read;
            constant = offset;
            return true;
        }

        /// <summary>
        /// Reduced row echelon form of <paramref name="coefficients"/>, carrying
        /// <paramref name="constants"/> along, and the rank it came to.
        /// <paramref name="pivotOfRow"/> receives the pivot column of each row below the rank.
        /// </summary>
        private static int Reduce(ERational[][] coefficients, Entity[] constants, int width, int[] pivotOfRow)
        {
            var rank = 0;
            for (var column = 0; column < width && rank < coefficients.Length; column++)
            {
                var pivot = -1;
                for (var row = rank; row < coefficients.Length; row++)
                    if (!coefficients[row][column].IsZero)
                    {
                        pivot = row;
                        break;
                    }
                if (pivot < 0)
                    continue;

                (coefficients[rank], coefficients[pivot]) = (coefficients[pivot], coefficients[rank]);
                (constants[rank], constants[pivot]) = (constants[pivot], constants[rank]);

                var head = coefficients[rank][column];
                for (var other = column; other < width; other++)
                    coefficients[rank][other] = coefficients[rank][other].Divide(head);
                constants[rank] = (constants[rank] / Rational.Create(head)).InnerSimplified;

                for (var row = 0; row < coefficients.Length; row++)
                {
                    if (row == rank || coefficients[row][column].IsZero)
                        continue;
                    var factor = coefficients[row][column];
                    for (var other = column; other < width; other++)
                        coefficients[row][other] =
                            coefficients[row][other].Subtract(factor.Multiply(coefficients[rank][other]));
                    constants[row] =
                        (constants[row] - Rational.Create(factor) * constants[rank]).InnerSimplified;
                }

                pivotOfRow[rank] = column;
                rank++;
            }
            return rank;
        }
    }
}
