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
        /// The p-norm of a vector, <c>norm(v, p) = (|v_1|^p + ... + |v_n|^p)^(1/p)</c> for <c>p &gt;= 1</c>, and the
        /// largest of the moduli <c>|v_i|</c> at <c>p = +oo</c>. The 2-norm is the absolute value of the vector,
        /// <c>abs(v)</c>, and is computed as that. Below <c>p = 1</c> it is not a norm, the triangle inequality failing,
        /// and has no value. A vector written out is taken when simplified; the node stays where the vector or
        /// <c>p</c> is not known.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record Normf(Entity Argument, Entity Order) : Function, IBinaryNode
        {
            internal Normf New(Entity argument, Entity order) =>
                ReferenceEquals(Argument, argument) && ReferenceEquals(Order, order) ? this : new(argument, order) { Codomain = Codomain };

            /// <inheritdoc/>
            public Entity NodeFirstChild => Argument;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Order;

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func), Order.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument, Order };
        }
    }
}
