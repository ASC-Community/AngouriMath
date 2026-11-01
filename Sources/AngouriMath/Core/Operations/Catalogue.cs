//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core.Transformations;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Core.Operations
{
    /// <summary>
    /// Every operation the library offers by name, which the terminal and the MCP server take
    /// theirs from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The names are the ones
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1019">#1019</a> item 16
    /// proposes for <c>amcli</c>'s commands in v3: the library's own verbs, so <c>simplify</c>
    /// rather than <c>simp</c> and <c>differentiate</c> rather than <c>diff</c>.
    /// </para>
    /// <para>
    /// An operation is in this list when a surface offers it, or could offer it with nothing
    /// but text in and text out. The building blocks <see cref="Transformation"/> also names,
    /// such as <see cref="Transformation.RationalCanonicalization"/> or
    /// <see cref="Transformation.EqualitySaturation"/>, are not: a caller composes them, and
    /// none answers a question by itself. A test lists every factory of
    /// <see cref="Transformation"/> as one or the other, so that a new one fails the build
    /// until it is placed.
    /// </para>
    /// https://github.com/asc-community/AngouriMath/issues/1684
    /// </remarks>
    internal static class Catalogue
    {
        [ConstantField]
        private static readonly Parameter TheVariable = new("variable", "The variable it is over.");

        // ---------------------------------------------------------------- transformations

        /// <summary><see cref="Entity.Simplify(int)"/>.</summary>
        public static TransformationOperation<Func<Entity, Entity>> Simplify { get; } = new(
            "simplify", "Simplifies, searching the expression's rewritings for the simplest.",
            TransformationRelation.Equivalence, static e => e.Simplify());

        /// <summary><see cref="Entity.InnerSimplified"/>.</summary>
        public static TransformationOperation<Func<Entity, Entity>> Normalize { get; } = new(
            "normalize", "Tidies the expression in one structural pass, without the search simplify makes.",
            TransformationRelation.Equivalence, static e => e.InnerSimplified);

        /// <summary><see cref="Entity.Expand(int)"/>.</summary>
        public static TransformationOperation<Func<Entity, Entity>> Expand { get; } = new(
            "expand", "Multiplies products over sums out.",
            TransformationRelation.Equivalence, static e => e.Expand());

        /// <summary><see cref="Entity.Factorize(int)"/>.</summary>
        public static TransformationOperation<Func<Entity, Entity>> Factorize { get; } = new(
            "factorize", "Gathers common factors out and factors polynomials.",
            TransformationRelation.Equivalence, static e => e.Factorize());

        /// <summary><see cref="Entity.Differentiate(Variable, int)"/>.</summary>
        public static TransformationOperation<Func<Entity, Variable, int, Entity>> Differentiate { get; } = new(
            "differentiate", "Differentiates over a variable, as many times as the order says.",
            TransformationRelation.Derivation, static (e, variable, order) => e.Differentiate(variable, order),
            TheVariable, new Parameter("order", "How many times; a negative order integrates instead."));

        /// <summary><see cref="Entity.Integrate(Variable)"/>.</summary>
        public static TransformationOperation<Func<Entity, Variable, Entity>> Integrate { get; } = new(
            "integrate", "Finds an antiderivative over a variable, with a constant of integration, or leaves the integral unevaluated.",
            TransformationRelation.Derivation, static (e, variable) => e.Integrate(variable),
            TheVariable);

        /// <summary><see cref="Entity.Limit(Variable, Entity, ApproachFrom)"/>.</summary>
        public static TransformationOperation<Func<Entity, Variable, Entity, ApproachFrom, Entity>> Limit { get; } = new(
            "limit", "Takes the limit as a variable approaches a value, or leaves it unevaluated.",
            TransformationRelation.Derivation, static (e, variable, destination, side) => e.Limit(variable, destination, side),
            TheVariable, new Parameter("destination", "The value it approaches."), new Parameter("side", "From the left, from the right, or from both sides."));

        /// <summary><see cref="Entity.Substitute(Entity, Entity)"/>.</summary>
        public static TransformationOperation<Func<Entity, Entity, Entity, Entity>> Substitute { get; } = new(
            "substitute", "Replaces every occurrence of a subexpression with another expression.",
            TransformationRelation.Derivation, static (e, what, with) => e.Substitute(what, with),
            new Parameter("what", "The subexpression to replace."), new Parameter("with", "What to put in its place."));

        // ---------------------------------------------------------------- queries on an expression

        /// <summary><see cref="Entity.Solve(Variable)"/>.</summary>
        public static Query<Func<Entity, Variable, Set>> Solve { get; } = new(
            "solve", "Solves an equation, an inequality or a statement for a variable.",
            static (e, variable) => e.Solve(variable),
            TheVariable);

        /// <summary><see cref="EquationSystem.Solve(Variable[])"/>.</summary>
        public static Query<Func<IEnumerable<Entity>, IReadOnlyList<Variable>, Matrix?>> SolveSystem { get; } = new(
            "solve_system", "Solves equations jointly for several variables, one row of the answer for each solution.",
            static (equations, variables) => MathS.Equations(equations).Solve([.. variables]),
            new Parameter("variables", "The variables to solve for, in the order of the answer's columns."));

        /// <summary><see cref="Entity.Evaled"/>.</summary>
        public static Query<Func<Entity, Entity>> Evaluate { get; } = new(
            "evaluate", "Evaluates as far as it goes: to a number, a boolean or a set where it can, and as an expression where it cannot.",
            static e => e.Evaled);

        /// <summary><see cref="Entity.EvalNumerical"/>.</summary>
        public static Query<Func<Entity, Complex>> EvaluateNumerically { get; } = new(
            "evaluate_numerically", "Evaluates to a complex number, at the working precision.",
            static e => e.EvalNumerical());

        /// <summary><see cref="Entity.Vars"/>.</summary>
        public static Query<Func<Entity, IReadOnlyList<Variable>>> Variables { get; } = new(
            "variables", "Lists the free variables.",
            static e => e.Vars);

        /// <summary><see cref="Entity.Alternate(int)"/>.</summary>
        public static Query<Func<Entity, int, IEnumerable<Entity>>> Alternatives { get; } = new(
            "alternatives", "Lists forms of the expression that simplify finds, simplest first.",
            static (e, level) => e.Alternate(level),
            new Parameter("level", "How hard to look, as simplify's level."));

        /// <summary><see cref="Entity.Latexize()"/>.</summary>
        public static Query<Func<Entity, string>> Latex { get; } = new(
            "latex", "Writes the expression as LaTeX.",
            static e => e.Latexize());

        /// <summary><see cref="MathS.ToSympyCode(Entity)"/>.</summary>
        public static Query<Func<Entity, string>> Sympy { get; } = new(
            "sympy", "Writes the expression as Python code for SymPy.",
            static e => MathS.ToSympyCode(e));

        /// <summary><see cref="MathS.Boolean.BuildTruthTable(Entity, Variable[])"/>.</summary>
        public static Query<Func<Entity, IReadOnlyList<Variable>, Matrix?>> TruthTable { get; } = new(
            "truth_table", "Tabulates a statement over every assignment of true and false to its variables.",
            static (e, variables) => MathS.Boolean.BuildTruthTable(e, [.. variables]),
            new Parameter("variables", "The variables, in the order of the table's columns."));

        /// <summary><see cref="MathS.SolveBooleanTable(Entity, Variable[])"/>.</summary>
        public static Query<Func<Entity, IReadOnlyList<Variable>, Matrix?>> SatisfyingAssignments { get; } = new(
            "satisfying_assignments", "The assignments of true and false to a statement's variables under which it holds, one row each.",
            static (e, variables) => MathS.SolveBooleanTable(e, [.. variables]),
            new Parameter("variables", "The variables, in the order of the rows' columns."));

        /// <summary>
        /// <see cref="MathS.Series.Taylor(Entity, int, ValueTuple{Variable, Entity}[])"/>, or
        /// <see cref="MathS.Series.Maclaurin(Entity, int, Variable[])"/> where there is no point,
        /// with one term more than the degree, since those count terms: <c>sin(x)</c> to degree 1 is
        /// <c>x</c>, and was <c>0</c>. https://github.com/asc-community/AngouriMath/issues/2299
        /// </summary>
        public static Query<Func<Entity, Variable, int, Entity?, Entity>> Series { get; } = new(
            "series", "Expands as a Taylor series in a variable, up to a degree, around a point or around zero.",
            static (e, variable, degree, around) => around is null
                ? MathS.Series.Maclaurin(e, degree + 1, variable)
                : MathS.Series.Taylor(e, degree + 1, (variable, around)),
            TheVariable, new Parameter("degree", "The highest power kept."), new Parameter("around", "The point, or none for zero."));

        // ---------------------------------------------------------------- queries on a number

        /// <summary><see cref="MathS.ToBaseN(Real, int)"/>.</summary>
        public static Query<Func<Real, int, string>> ToBase { get; } = new(
            "to_base", "Writes a real number in a base from 2 to 36.",
            static (number, numberBase) => MathS.ToBaseN(number, numberBase),
            new Parameter("base", "The base, from 2 to 36."));

        /// <summary><see cref="MathS.FromBaseN(string, int)"/>.</summary>
        public static Query<Func<string, int, Real>> FromBase { get; } = new(
            "from_base", "Reads a real number written in a base from 2 to 36.",
            static (digits, numberBase) => MathS.FromBaseN(digits, numberBase),
            new Parameter("base", "The base, from 2 to 36."));

        // ---------------------------------------------------------------- queries on a matrix

        /// <summary><see cref="Matrix.Determinant"/>.</summary>
        public static Query<Func<Matrix, Entity?>> Determinant { get; } = new(
            "determinant", "The determinant of a square matrix, or none for a matrix that is not square.",
            static m => m.Determinant);

        /// <summary><see cref="Matrix.Inverse"/>.</summary>
        public static Query<Func<Matrix, Matrix?>> Inverse { get; } = new(
            "inverse", "The inverse of a square matrix, or none where it has none.",
            static m => m.Inverse);

        /// <summary><see cref="Matrix.T"/>.</summary>
        public static Query<Func<Matrix, Matrix>> Transpose { get; } = new(
            "transpose", "The transpose of a matrix.",
            static m => m.T);

        /// <summary><see cref="Matrix.Rank"/>.</summary>
        public static Query<Func<Matrix, int>> Rank { get; } = new(
            "rank", "The rank of a matrix.",
            static m => m.Rank);

        /// <summary><see cref="Matrix.ReducedRowEchelonForm"/>.</summary>
        public static Query<Func<Matrix, Matrix>> ReducedRowEchelonForm { get; } = new(
            "reduced_row_echelon_form", "A matrix in reduced row echelon form.",
            static m => m.ReducedRowEchelonForm);

        /// <summary><see cref="Matrix.Trace"/>.</summary>
        public static Query<Func<Matrix, Entity>> Trace { get; } = new(
            "trace", "The trace of a square matrix.",
            static m => m.Trace);

        /// <summary><see cref="Matrix.TensorProduct(Matrix, Matrix)"/>.</summary>
        public static Query<Func<Matrix, Matrix, Matrix>> TensorProduct { get; } = new(
            "tensor_product", "The tensor product of two matrices.",
            static (a, b) => Matrix.TensorProduct(a, b),
            new Parameter("other", "The matrix on the right."));

        // ---------------------------------------------------------------- queries on an integer

        /// <summary><see cref="MathS.NumberTheory.Factorize(Integer)"/>.</summary>
        public static Query<Func<Integer, IEnumerable<(Integer Prime, Integer Power)>>> PrimeFactorization { get; } = new(
            "prime_factorization", "The primes that divide an integer, each with its power.",
            static n => MathS.NumberTheory.Factorize(n));

        /// <summary><see cref="MathS.NumberTheory.Phi(Entity)"/>, evaluated.</summary>
        public static Query<Func<Integer, Entity>> Totient { get; } = new(
            "totient", "Euler's totient of an integer: how many positive integers up to it are coprime to it.",
            static n => MathS.NumberTheory.Phi(n).Evaled);

        /// <summary><see cref="MathS.NumberTheory.GreatestCommonDivisor(Integer, Integer)"/>.</summary>
        public static Query<Func<Integer, Integer, Integer>> GreatestCommonDivisor { get; } = new(
            "greatest_common_divisor", "The greatest common divisor of two integers.",
            static (a, b) => MathS.NumberTheory.GreatestCommonDivisor(a, b),
            new Parameter("other", "The second integer."));

        /// <summary><see cref="MathS.NumberTheory.CountDivisors(Integer)"/>.</summary>
        public static Query<Func<Integer, Integer>> DivisorCount { get; } = new(
            "divisor_count", "How many positive divisors an integer has.",
            static n => MathS.NumberTheory.CountDivisors(n));

        /// <summary><see cref="Integer.IsPrime"/>.</summary>
        public static Query<Func<Integer, bool>> IsPrime { get; } = new(
            "is_prime", "Whether an integer is prime.",
            static n => n.IsPrime);

        // ---------------------------------------------------------------- queries on physical dimensions

        [ConstantField]
        private static readonly Parameter TheDimensions = new("dimensions", "The physical dimension of each variable.");

        /// <summary><see cref="MathS.Dimensions.Of(Entity, IReadOnlyDictionary{Variable, Dimension})"/>.</summary>
        public static Query<Func<Entity, IReadOnlyDictionary<Variable, Dimension>, DimensionCheck>> Dimension { get; } = new(
            "dimension", "The physical dimension of an expression from those of its variables, or the part where two dimensions disagree.",
            static (e, dimensions) => MathS.Dimensions.Of(e, dimensions),
            TheDimensions);

        /// <summary><see cref="MathS.Dimensions.DimensionlessGroups(IReadOnlyList{Variable}, IReadOnlyDictionary{Variable, Dimension})"/>.</summary>
        public static Query<Func<IReadOnlyList<Variable>, IReadOnlyDictionary<Variable, Dimension>, IReadOnlyList<Entity>>> DimensionlessGroups { get; } = new(
            "dimensionless_groups", "The independent products of powers of variables that have no dimension: their Buckingham pi groups.",
            static (variables, dimensions) => MathS.Dimensions.DimensionlessGroups(variables, dimensions),
            TheDimensions);

        // ---------------------------------------------------------------- the list

        /// <summary>Every entry above, in the order written.</summary>
        /// <remarks>Last in the file, because static initialisers run in the order they are written.</remarks>
        public static IReadOnlyList<Operation> All { get; } =
        [
            Simplify, Normalize, Expand, Factorize, Differentiate, Integrate, Limit, Substitute,
            Solve, SolveSystem, Evaluate, EvaluateNumerically, Variables, Alternatives, Latex, Sympy, TruthTable, SatisfyingAssignments, Series,
            ToBase, FromBase,
            Determinant, Inverse, Transpose, Rank, ReducedRowEchelonForm, Trace, TensorProduct,
            PrimeFactorization, Totient, GreatestCommonDivisor, DivisorCount, IsPrime,
            Dimension, DimensionlessGroups,
        ];

        /// <summary>The entry named <paramref name="name"/>, or <see langword="null"/> where there is none.</summary>
        /// <param name="name">Its name, such as <c>simplify</c>.</param>
        public static Operation? Find(string name) => All.FirstOrDefault(operation => operation.Name == name);
    }
}
