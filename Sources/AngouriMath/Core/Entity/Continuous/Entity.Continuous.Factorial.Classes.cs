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

        /// <summary>
        /// The harmonic number <c>harmonicnumber(n)</c>, <c>H_n = 1 + 1/2 + ... + 1/n</c>, and the
        /// generalized one <c>harmonicnumber(n, m)</c>, <c>H_n^(m) = 1 + 1/2^m + ... + 1/n^m</c>.
        /// </summary>
        /// <remarks>
        /// Off the whole numbers it is <c>ζ(m) - ζ(m, n + 1)</c>, as Mathematica's <c>HarmonicNumber</c>
        /// is: <c>ψ(n + 1) - ψ(1)</c> for <c>m = 1</c>, which is <c>2 - 2 ln 2</c> at <c>n = 1/2</c>. It
        /// has no value at a negative integer <c>n</c>, a pole of the Hurwitz zeta function, but for a
        /// whole order <c>m &lt;= 0</c>, where it is a polynomial in <c>n</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </remarks>
        public sealed partial record HarmonicNumberf(Entity Argument, Entity Order) : Function, IBinaryNode
        {
            /// <inheritdoc/>
            public Entity NodeFirstChild => Argument;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Order;

            internal HarmonicNumberf New(Entity argument, Entity order) =>
                ReferenceEquals(Argument, argument) && ReferenceEquals(Order, order) ? this : new(argument, order) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func), Order.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument, Order };
        }

        /// <summary>
        /// The Catalan number <c>catalannumber(n)</c>, <c>C_n = binomial(2n, n) / (n + 1)</c>: 1, 1, 2, 5,
        /// 14 from <c>n = 0</c>, the number of ways to bracket a product of <c>n + 1</c> factors.
        /// </summary>
        /// <remarks>
        /// Off the whole numbers it is <c>4^n Γ(n + 1/2) / (√π Γ(n + 2))</c>, as Mathematica's
        /// <c>CatalanNumber</c> and SymPy's <c>catalan</c> are: <c>-1/2</c> at <c>-1</c>, <c>0</c> at
        /// every whole number below it, and no value at a negative half-integer, a pole of Γ(n + 1/2).
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </remarks>
        public sealed partial record CatalanNumberf(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal CatalanNumberf New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The falling factorial <c>fallingfactorial(x, n)</c>, <c>x</c> to the <c>n</c> falling: <c>x (x - 1) ... (x - n + 1)</c> for a whole <c>n &gt;= 0</c>, in the notation <c>x^{\underline{n}}</c> of Graham, Knuth and Patashnik's <i>Concrete Mathematics</i>.
        /// </summary>
        /// <remarks>
        /// For a negative whole <c>n</c> it is <c>1 / ((x + 1) (x + 2) ... (x - n))</c>, the book's (2.52), and for any other <c>n</c> it is <c>Γ(x + 1) / Γ(x - n + 1)</c>, as Mathematica's <c>FactorialPower</c> is.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </remarks>
        public sealed partial record FallingFactorialf(Entity Argument, Entity Order) : Function, IBinaryNode
        {
            /// <inheritdoc/>
            public Entity NodeFirstChild => Argument;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Order;

            internal FallingFactorialf New(Entity argument, Entity order) =>
                ReferenceEquals(Argument, argument) && ReferenceEquals(Order, order) ? this : new(argument, order) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func), Order.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument, Order };
        }

        /// <summary>
        /// The rising factorial <c>risingfactorial(x, n)</c>, <c>x</c> to the <c>n</c> rising: <c>x (x + 1) ... (x + n - 1)</c> for a whole <c>n &gt;= 0</c>, in the notation <c>x^{\overline{n}}</c> of Graham, Knuth and Patashnik's <i>Concrete Mathematics</i>.
        /// </summary>
        /// <remarks>
        /// For a negative whole <c>n</c> it is <c>1 / ((x - 1) (x - 2) ... (x + n))</c>, and for any other <c>n</c> it is <c>Γ(x + n) / Γ(x)</c>, as Mathematica's <c>Pochhammer</c> is. The name <c>pochhammer</c> is refused, since the Pochhammer symbol is the falling factorial to some authors.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </remarks>
        public sealed partial record RisingFactorialf(Entity Argument, Entity Order) : Function, IBinaryNode
        {
            /// <inheritdoc/>
            public Entity NodeFirstChild => Argument;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Order;

            internal RisingFactorialf New(Entity argument, Entity order) =>
                ReferenceEquals(Argument, argument) && ReferenceEquals(Order, order) ? this : new(argument, order) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func), Order.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument, Order };
        }
    }
}
