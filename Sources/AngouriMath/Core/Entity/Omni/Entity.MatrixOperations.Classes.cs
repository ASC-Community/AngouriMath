//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;

namespace AngouriMath
{
    // The operations on a matrix or on vectors that an expression can name before the matrix is known. A
    // matrix written out is computed from its entries as soon as it is parsed, as it was without
    // these nodes; a node is what is left where the argument is not yet a matrix -- a name that
    // stands for one, det(A) -- and it is computed once the argument becomes a matrix, by
    // substitution or simplification. A variable carries no type yet
    // (https://github.com/asc-community/AngouriMath/issues/1237), so these nodes are the only place
    // a name is read as a matrix: anywhere else it is still a number to the rules.
    // https://github.com/asc-community/AngouriMath/issues/2147
    partial record Entity
    {
        /// <summary>
        /// The determinant <c>det(A)</c> of a square matrix.
        /// </summary>
        /// <remarks>
        /// Computed when <see cref="Argument"/> is a matrix, by <see cref="Matrix.Determinant"/>. A
        /// matrix that is not square has no determinant, and the node is left as it is rather than
        /// given a value.
        /// </remarks>
        public sealed partial record Determinantf(Entity Argument) : Function
        {
            private Determinantf New(Entity arg) =>
                ReferenceEquals(Argument, arg) ? this : new(arg) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The transpose <c>transpose(A)</c>, the matrix whose rows are the columns of <c>A</c>.
        /// A vector is a column, so its transpose is a row, as <c>]T</c> writes it.
        /// </summary>
        public sealed partial record Transposef(Entity Argument) : Function
        {
            private Transposef New(Entity arg) =>
                ReferenceEquals(Argument, arg) ? this : new(arg) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The trace <c>trace(A)</c> of a square matrix, the sum of its diagonal entries.
        /// </summary>
        /// <remarks>
        /// Only of a square matrix: <see cref="Matrix.Trace"/> sums the diagonal of any matrix,
        /// but the trace is defined for the square ones, as it is the sum of the eigenvalues.
        /// </remarks>
        public sealed partial record Tracef(Entity Argument) : Function
        {
            private Tracef New(Entity arg) =>
                ReferenceEquals(Argument, arg) ? this : new(arg) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The entry <c>A[i, j]</c> in row <c>i</c> and column <c>j</c> of a matrix, counted from 1
        /// as <c>a_ij</c> is.
        /// </summary>
        /// <remarks>
        /// Computed when <see cref="Argument"/> is a matrix and both indices are whole numbers in
        /// its range. An index out of the range or not whole names no entry, and the node is left
        /// as it is rather than given a value.
        /// </remarks>
        public sealed partial record MatrixEntryf(Entity Argument, Entity Row, Entity Column) : Function
        {
            private MatrixEntryf New(Entity arg, Entity row, Entity column) =>
                ReferenceEquals(Argument, arg) && ReferenceEquals(Row, row) && ReferenceEquals(Column, column)
                    ? this : new(arg, row, column) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Argument.Replace(func), Row.Replace(func), Column.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument, Row, Column };
        }

        /// <summary>
        /// The scalar product <c>dot(a, b)</c> of two vectors of one length: the sum of the products of their components,
        /// neither side conjugated, as SymPy's <c>dot</c> and Mathematica's <c>Dot</c> are.
        /// </summary>
        /// <remarks>
        /// Computed when both operands are vectors, each a column or a row. Vectors of different lengths, or a matrix that
        /// is not a vector, have no scalar product, and the node is left as it is rather than given a value.
        /// https://github.com/asc-community/AngouriMath/issues/105
        /// </remarks>
        public sealed partial record DotProductf(Entity Left, Entity Right) : Function
        {
            private DotProductf New(Entity left, Entity right) =>
                ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };

            /// <summary>
            /// The sum of the products of the components, or null where the two are not vectors of one length. Written as
            /// it was computed when parsed: <c>dot([1, 2], [3, 4])</c> is <c>1 * 3 + 2 * 4</c>.
            /// </summary>
            internal static Entity? Of(Matrix left, Matrix right)
                => VectorComponents(left) is { } a && VectorComponents(right) is { } b && a.Length == b.Length
                    ? Enumerable.Range(0, a.Length).Select(i => a[i] * b[i]).Aggregate((sum, term) => sum + term)
                    : null;
        }

        /// <summary>
        /// The vector product <c>cross(a, b)</c> of two vectors in three dimensions, a column. It is not commutative:
        /// <c>cross(b, a)</c> is <c>-cross(a, b)</c>, and the operands are never put in another order.
        /// </summary>
        /// <remarks>
        /// Computed when both operands are vectors of three components, each a column or a row. Anything else has no vector
        /// product, and the node is left as it is rather than given a value. <c>cross(a)</c> of one vector in the plane is
        /// computed from a vector written out only, and has no node.
        /// https://github.com/asc-community/AngouriMath/issues/105
        /// </remarks>
        public sealed partial record CrossProductf(Entity Left, Entity Right) : Function
        {
            private CrossProductf New(Entity left, Entity right) =>
                ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };

            /// <summary>
            /// The vector product, a column, or null where the two are not vectors of three components. Written as it was
            /// computed when parsed: <c>cross([1, 2, 3], [4, 5, 6])</c> is <c>[2 * 6 - 3 * 5, 3 * 4 - 1 * 6, 1 * 5 - 2 * 4]</c>.
            /// </summary>
            internal static Entity? Of(Matrix left, Matrix right)
                => VectorComponents(left) is { Length: 3 } u && VectorComponents(right) is { Length: 3 } v
                    ? MathS.Vector(
                        u[1] * v[2] - u[2] * v[1],
                        u[2] * v[0] - u[0] * v[2],
                        u[0] * v[1] - u[1] * v[0])
                    : null;
        }

        /// <summary>The components of a vector, written as a column or as a row, or null where the matrix is neither.</summary>
        internal static Entity[]? VectorComponents(Matrix vector)
            => vector.ColumnCount == 1 ? Enumerable.Range(0, vector.RowCount).Select(i => vector[i, 0]).ToArray()
                : vector.RowCount == 1 ? Enumerable.Range(0, vector.ColumnCount).Select(j => vector[0, j]).ToArray()
                : null;
    }
}
