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
        /// The Hurwitz zeta function <c>zeta(s, a)</c>, <c>sum_(n >= 0) 1/(n + a)^s</c> for <c>Re s > 1</c> and
        /// continued to every <c>s</c> but its pole at <c>1</c>, with <c>a</c> anything but <c>0, -1, -2, ...</c>
        /// (DLMF 25.11.1). The Riemann zeta function <c>zeta(s)</c> is <c>zeta(s, 1)</c>, and is this node with a
        /// <see cref="Parameter"/> of <c>1</c>.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record Zetaf(Entity Argument, Entity Parameter) : Function, IBinaryNode
        {
            internal Zetaf New(Entity argument, Entity parameter) =>
                ReferenceEquals(Argument, argument) && ReferenceEquals(Parameter, parameter) ? this : new(argument, parameter) { Codomain = Codomain };

            /// <summary>Whether this is the Riemann zeta function, the Hurwitz one at <c>a = 1</c>.</summary>
            internal bool IsRiemann => Parameter == Integer.One;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Argument;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Parameter;

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func), Parameter.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument, Parameter };
        }
    }
}
