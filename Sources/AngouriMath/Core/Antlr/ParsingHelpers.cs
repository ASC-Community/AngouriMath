//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Antlr
{
    internal static class ParsingHelpers
    {
        internal static Matrix TryBuildingMatrix(List<Entity> elements)
        {
            if (!elements.Any())
                return MathS.Vector(elements.ToArray());
            var first = elements.First();
            if (first is not Matrix { IsVector: true } firstVec)
                return MathS.Vector(elements.ToArray());
            var tb = new MatrixBuilder(firstVec.RowCount);
            foreach (var row in elements)
            {
                if (row is not Matrix { IsVector: true } rowVec)
                    return MathS.Vector(elements.ToArray());
                if (rowVec.RowCount != firstVec.RowCount)
                    return MathS.Vector(elements.ToArray());
                tb.Add(rowVec);
            }
            return tb.ToMatrix() ?? throw new AngouriBugException("Should've been checked already");
        }

        // det, transpose, dot, cross and resultant are computed from what they are applied to, and
        // there is no node for any of them: det(A) of a symbol A has nothing to be. So an argument
        // has to be a matrix written out, or an expression that is one once its operations are
        // carried out, as 2 [[1, 2], [3, 4]] is.
        // https://github.com/asc-community/AngouriMath/issues/2147
        private static Matrix MatrixArgument(string function, Entity argument)
            => argument as Matrix ?? argument.InnerSimplified as Matrix
                ?? throw new InvalidArgumentParseException(
                    $"{function} is computed from the entries of a matrix or a vector written out, " +
                    $"as in {function}([[1, 2], [3, 4]]), and {argument} is not one");

        /// <summary>The entries of a vector, written as a column or as a row.</summary>
        private static Entity[] Components(string function, Entity argument)
        {
            var vector = MatrixArgument(function, argument);
            if (vector.ColumnCount == 1)
                return Enumerable.Range(0, vector.RowCount).Select(i => vector[i, 0]).ToArray();
            if (vector.RowCount == 1)
                return Enumerable.Range(0, vector.ColumnCount).Select(j => vector[0, j]).ToArray();
            throw new InvalidArgumentParseException(
                $"{function} is taken of vectors, and {vector} is a {vector.RowCount} by {vector.ColumnCount} matrix");
        }

        /// <summary><c>det(M)</c>: the determinant of a square matrix.</summary>
        internal static Entity Determinant(Entity argument)
        {
            var matrix = MatrixArgument("det", argument);
            return matrix.Determinant ?? throw new InvalidArgumentParseException(
                $"det is taken of a square matrix, and {matrix} is {matrix.RowCount} by {matrix.ColumnCount}");
        }

        /// <summary><c>transpose(M)</c>, which <c>]T</c> writes only after a bracket.</summary>
        internal static Entity Transposed(Entity argument)
            => MatrixArgument("transpose", argument).T;

        /// <summary>
        /// <c>dot(a, b)</c>: the sum of the products of the components, without conjugating either
        /// side, as SymPy's <c>dot</c> and Mathematica's <c>Dot</c> are.
        /// </summary>
        internal static Entity Dot(Entity left, Entity right)
        {
            var a = Components("dot", left);
            var b = Components("dot", right);
            if (a.Length != b.Length)
                throw new InvalidArgumentParseException(
                    $"dot is taken of two vectors of one length, and these have {a.Length} and {b.Length} components");
            return Enumerable.Range(0, a.Length).Select(i => a[i] * b[i]).Aggregate((sum, term) => sum + term);
        }

        /// <summary>
        /// <c>cross(a, b)</c>, the vector product in three dimensions, and <c>cross(a)</c>, the vector
        /// in the plane that <c>a</c> turns into through a right angle anticlockwise: <c>(-a_2, a_1)</c>,
        /// as Mathematica's <c>Cross</c> of one vector is. So <c>dot(cross(a), b)</c> is the plane's
        /// pseudo-scalar product <c>a_1 b_2 - a_2 b_1</c>.
        /// </summary>
        internal static Entity Cross(List<Entity> arguments)
        {
            if (arguments.Count == 1)
            {
                var a = Components("cross", arguments[0]);
                if (a.Length != 2)
                    throw new InvalidArgumentParseException(
                        $"cross of one vector is its turn through a right angle in the plane, and this vector has {a.Length} components");
                return MathS.Vector(-a[1], a[0]);
            }
            var u = Components("cross", arguments[0]);
            var v = Components("cross", arguments[1]);
            if (u.Length != 3 || v.Length != 3)
                throw new InvalidArgumentParseException(
                    $"cross of two vectors is the vector product in three dimensions, and these have {u.Length} and {v.Length} components");
            return MathS.Vector(
                u[1] * v[2] - u[2] * v[1],
                u[2] * v[0] - u[0] * v[2],
                u[0] * v[1] - u[1] * v[0]);
        }

        /// <summary><c>resultant(p, q, x)</c>, eliminating <c>x</c> between two polynomials.</summary>
        internal static Entity Resultant(Entity left, Entity right, Entity eliminate)
        {
            if (eliminate is not Variable { IsConstant: false } variable)
                throw new InvalidArgumentParseException(
                    $"resultant's third argument is the variable to eliminate, and {eliminate} is not a variable");
            return MathS.Polynomials.Resultant(left, right, variable) ?? throw new InvalidArgumentParseException(
                $"resultant is taken of two polynomials in {variable} with rational coefficients, and {left} and {right} are not both");
        }

        /// <summary>
        /// The <see cref="Number.Rational"/> that a quotient of two integer literals denotes, or
        /// the node unchanged where it denotes none.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A <see cref="Number.Rational"/> prints as <c>7/2</c> and there is no rational literal
        /// in the grammar, so re-parsing gave a <see cref="Divf"/> and the round trip was not an
        /// identity — the value survived and the node did not. That is
        /// https://github.com/asc-community/AngouriMath/issues/946's neighbour,
        /// https://github.com/asc-community/AngouriMath/issues/873, answered there with "if you
        /// have two integers on both sides of the division, it is reasonable to try to parse it
        /// as a rational".
        /// </para>
        /// <para>
        /// <b>A quotient that reduces to an integer is left alone</b>, deliberately. Parsing is
        /// not simplification: turning <c>4/2</c> into <c>2</c> would discard what the caller
        /// wrote, and <c>4/2</c> already round-trips, being a <see cref="Divf"/> before and after.
        /// Only the non-integer case is what a <see cref="Number.Rational"/> can print as, so only
        /// it is what the round trip needs.
        /// </para>
        /// <para>
        /// This agrees with the normalisation rather than anticipating it:
        /// <c>Divf(1, 2).InnerSimplified</c> was already a <see cref="Number.Rational"/>, so the
        /// parser was the one step that disagreed.
        /// </para>
        /// <para>
        /// The codomain is not read here and not carried. Every caller either has no codomain to
        /// carry — the sweep over a finished tree, where an annotation cannot have reached a
        /// quotient — or is <c>domain(...)</c> itself, which applies its own afterwards.
        /// </para>
        /// </remarks>
        internal static Entity RationalLiteral(Entity node)
        {
            if (node is not Divf(Number.Integer numerator, Number.Integer denominator))
                return node;
            if (denominator.EInteger.IsZero)
                return node;
            var value = Number.Rational.Create(numerator.EInteger, denominator.EInteger);
            return value is Number.Integer ? node : value;
        }
    }
}
