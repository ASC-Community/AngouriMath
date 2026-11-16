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
        /// The beta function <c>B(a, b) = Γ(a) Γ(b)/Γ(a + b)</c> (DLMF 5.12.1), written <c>beta(a, b)</c>. It has no
        /// value where <c>a</c> or <c>b</c> is <c>0, -1, -2, ...</c>, unless <c>a + b</c> is one as well and the other
        /// argument is a whole number <c>n &gt;= 1</c>: there it is the value at that point of
        /// <c>B(n, b) = (n - 1)!/(b (b + 1) ... (b + n - 1))</c>, a rational function of <c>b</c>, so that
        /// <c>B(1, b) = 1/b</c> holds wherever either side has a value. Where <c>a + b</c> is <c>0, -1, -2, ...</c> and
        /// neither argument is, it is <c>0</c>.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record Betaf(Entity First, Entity Second) : Function, IBinaryNode
        {
            internal Betaf New(Entity first, Entity second) =>
                ReferenceEquals(First, first) && ReferenceEquals(Second, second) ? this : new(first, second) { Codomain = Codomain };

            /// <inheritdoc/>
            public Entity NodeFirstChild => First;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Second;

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(First.Replace(func), Second.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { First, Second };
        }
    }
}
