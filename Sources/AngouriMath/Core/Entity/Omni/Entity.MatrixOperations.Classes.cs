//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;

namespace AngouriMath
{
    // The four operations on a matrix that an expression can name before the matrix is known. A
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
    }
}
