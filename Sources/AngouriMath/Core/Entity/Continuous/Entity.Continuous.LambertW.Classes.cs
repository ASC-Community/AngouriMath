//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// The Lambert W function <c>W_k(z)</c>, the solution <c>w</c> of <c>w e^w = z</c> on the branch
        /// <c>k</c>, a whole number: <c>lambertw(z)</c> is the principal branch and <c>lambertw(k, z)</c>
        /// the branch <c>k</c>, as Mathematica's <c>ProductLog</c> is.
        /// </summary>
        /// <remarks>
        /// The branches are those of Corless, Gonnet, Hare, Jeffrey and Knuth (1996): <c>W_0</c> is real
        /// on <c>[-1/e, +oo)</c> and <c>W_(-1)</c> on <c>[-1/e, 0)</c>, and both are <c>-1</c> at <c>-1/e</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </remarks>
        public sealed partial record LambertWf(Entity Branch, Entity Argument) : Function, IBinaryNode
        {
            internal LambertWf New(Entity branch, Entity argument) =>
                ReferenceEquals(Branch, branch) && ReferenceEquals(Argument, argument) ? this : new(branch, argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public Entity NodeFirstChild => Branch;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Argument;

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Branch.Replace(func), Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Branch, Argument };
        }
    }
}
