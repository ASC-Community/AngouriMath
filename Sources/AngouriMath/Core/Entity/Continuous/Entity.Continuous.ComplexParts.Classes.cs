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
        /// The complex conjugate <c>conjugate(z)</c>, also written <c>conj(z)</c>: <c>a - b i</c> for
        /// <c>z = a + b i</c>.
        /// </summary>
        /// <remarks>
        /// An involution that fixes the real numbers and sends <c>i</c> to <c>-i</c>, and a field
        /// automorphism, so it is carried through sums, differences, products, quotients and whole
        /// powers. It is not carried through a root or a logarithm: their branch cuts lie on the
        /// negative real axis, where the conjugate of the principal value is not the principal value
        /// of the conjugate, as <c>sqrt(-1)</c> is <c>i</c> and its conjugate <c>-i</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2145
        /// </remarks>
        public sealed partial record Conjugatef(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            private Conjugatef New(Entity arg) =>
                ReferenceEquals(Argument, arg) ? this : new(arg) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The principal argument <c>arg(z)</c>: the angle of <c>z</c> from the positive real axis,
        /// in <c>(-pi, pi]</c>, so that <c>z = |z| e^(i arg(z))</c>.
        /// </summary>
        /// <remarks>
        /// <c>arg(-1)</c> is <c>pi</c>, the end of the interval that is included, as the principal
        /// logarithm's imaginary part is. <c>arg(0)</c> has no value: every angle fits
        /// <c>0 = 0 e^(i t)</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2145
        /// </remarks>
        public sealed partial record Argf(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            private Argf New(Entity arg) =>
                ReferenceEquals(Argument, arg) ? this : new(arg) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
    }
}
