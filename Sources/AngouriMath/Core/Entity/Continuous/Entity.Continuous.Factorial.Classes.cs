//
// Copyright (c) 2019-2022 Angouri.
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
        /// A node of factorial
        /// </summary>
        public sealed partial record Factorialf(Entity Argument) : Function, IUnaryNode
        {
            /// <summary>Reuse the cache by returning the same object if possible</summary>
            private Factorialf New(Entity argument) => ReferenceEquals(Argument, argument) ? this : new(argument) { Codomain = Codomain };
            // This is still a function for pattern replacement
            internal override Priority Priority => Priority.Factorial;

            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The double factorial <c>n!!</c>: the product of the positive integers up to <c>n</c> with its
        /// parity, <c>n (n - 2) (n - 4) ...</c>, which is not <c>(n!)!</c>.
        /// </summary>
        /// <remarks>
        /// Defined on the integers but the negative even ones: <c>0!!</c> and <c>(-1)!!</c> are the empty
        /// product <c>1</c>, and below them <c>n!! = (n + 2)!!/(n + 2)</c>, so <c>(-3)!!</c> is
        /// <c>-1</c> and <c>(-5)!!</c> is <c>1/3</c>. Off the integers it has no value: the extension
        /// through the gamma function does not give <c>0!! = 1</c>, so no extension is the double
        /// factorial's. https://github.com/asc-community/AngouriMath/issues/2182
        /// </remarks>
        public sealed partial record DoubleFactorialf(Entity Argument) : Function, IUnaryNode
        {
            private DoubleFactorialf New(Entity argument) => ReferenceEquals(Argument, argument) ? this : new(argument) { Codomain = Codomain };
            // Written as the factorial is, a postfix after its argument.
            internal override Priority Priority => Priority.Factorial;

            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// A node of the binomial coefficient <c>binomial(n, k)</c>, "n choose k".
        /// </summary>
        /// <remarks>
        /// A node rather than <c>n!/(k! (n - k)!)</c>: that quotient is correct and useless as a
        /// spelling -- it loses the integrality, it is undefined for a negative <c>n</c> where
        /// the falling factorial <c>n (n - 1) ... (n - k + 1)/k!</c> is not, and no rule can
        /// recognise Pascal's identity or Vandermonde's in a ratio of three factorials. The
        /// falling factorial is the definition for a whole <c>k</c>, whatever <c>n</c> is; a
        /// whole <c>k</c> below zero gives <c>0</c>; and for a <c>k</c> that is not whole the
        /// value is <c>Γ(n + 1)/(Γ(k + 1) Γ(n - k + 1))</c>, which has no value where <c>n</c>
        /// is a negative whole number. What cannot be settled is left as this node.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// https://github.com/asc-community/AngouriMath/issues/809
        /// </remarks>
        public sealed partial record Binomialf(Entity Upper, Entity Lower) : Function, IBinaryNode
        {
            /// <inheritdoc/>
            public Entity NodeFirstChild => Upper;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Lower;

            private Binomialf New(Entity upper, Entity lower) =>
                ReferenceEquals(Upper, upper) && ReferenceEquals(Lower, lower) ? this : new(upper, lower) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Upper.Replace(func), Lower.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Upper, Lower };
        }
    }
}
