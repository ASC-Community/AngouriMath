//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core.Exceptions;

namespace AngouriMath
{
    partial record Entity
    {

        /// <summary>
        /// This node represents all possible values a boolean node might be of
        /// </summary>
        public sealed partial record Boolean(bool Value) : Statement
        {
            /// <summary>
            /// One of the Boolean's states, which also behaves as Entity
            /// That is, hangable
            /// </summary>
            [ConstantField] public static readonly Boolean True = new Boolean(true);

            /// <summary>
            /// One of the Boolean's states, which also behaves as Entity
            /// That is, hangable
            /// </summary>
            [ConstantField] public static readonly Boolean False = new Boolean(false);

            /// <summary>
            /// This conversation is 100% free, no need to manually choose between
            /// <see cref="True"/> and <see cref="False"/>
            /// </summary>
            /// <param name="b">To convert from</param>
            public static implicit operator bool(Boolean b) => b.Value;

            /// <summary>
            /// Creates a Boolean at 0 cost
            /// No need to manually choose between <see cref="True"/> and <see cref="False"/>
            /// </summary>
            /// <param name="value">
            /// From which to create
            /// </param>
            public static Boolean Create(bool value) => value ? True : False; // to avoid reallocation

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) 
                => func(this);
            internal override Priority Priority => Priority.Leaf;
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => Array.Empty<Entity>();

            /// <summary>
            /// Use this when parsing one boolean value
            /// </summary>
            /// <param name="expr">A string to parse from</param>
            /// <param name="dst">Where to store the result</param>
            /// <returns>
            /// true if the parsing completed successfully, 
            /// false otherwise
            /// </returns>
            public static bool TryParse(string expr, out Boolean dst)
            {
                switch (expr.ToLower())
                {
                    case "false":
                        dst = False;
                        return true;
                    case "true":
                        dst = True;
                        return true;
                }
                dst = False;
                return false;
            }

            /// <summary>
            /// Unlike <see cref="TryParse"/> this will throw a
            /// <see cref="ParseException"/> if parsing is not successful
            /// </summary>
            public static Boolean Parse(string expr)
                => TryParse(expr, out var res) ? res : throw new CannotParseInstanceException(typeof(Boolean), expr);
        }

        #region Logical gates

        /// <summary>
        /// Whatever its argument is, the result will be inverted
        /// </summary>
        public sealed partial record Notf(Entity Argument) : Statement, IUnaryNode
        {
            internal override Priority Priority => Priority.Negation;

            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            private Notf New(Entity negated) =>
                ReferenceEquals(Argument, negated) ? this : new(negated) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };

        }

        /// <summary>
        /// Is true iff both operands are true
        /// </summary>
        public sealed partial record Andf(Entity Left, Entity Right) : Statement, IBinaryNode
        {
            internal override Priority Priority => Priority.Conjunction;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            private Andf New(Entity left, Entity right) =>
                ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        /// <summary>
        /// Is true iff at least one operand is true,
        /// </summary>
        public sealed partial record Orf(Entity Left, Entity Right) : Statement, IBinaryNode
        {
            internal override Priority Priority => Priority.Disjunction;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            private Orf New(Entity left, Entity right) =>
                ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        /// <summary>
        /// Is true iff one operand is true
        /// </summary>
        public sealed partial record Xorf(Entity Left, Entity Right) : Statement, IBinaryNode
        {
            internal override Priority Priority => Priority.XDisjunction;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            private Xorf New(Entity left, Entity right) =>
                ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        /// <summary>
        /// Is true iff assumption is false or conclusion is true
        /// </summary>
        public sealed partial record Impliesf(Entity Assumption, Entity Conclusion) : Statement, IBinaryNode
        {
            internal override Priority Priority => Priority.Implication;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Assumption;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Conclusion;

            private Impliesf New(Entity assumption, Entity conclusion) =>
                ReferenceEquals(Assumption, assumption) && ReferenceEquals(Conclusion, conclusion) ? this : new(assumption, conclusion) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Assumption.Replace(func), Conclusion.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Assumption, Conclusion };
        }

        #endregion

        #region Equality/inequality operators

        /// <summary>
        /// It is true if left and right are equal
        /// </summary>
        public sealed partial record Equalsf(Entity Left, Entity Right) : ComparisonSign, IBinaryNode
        {
            internal override Priority Priority => Priority.Equal;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            internal Equalsf New(Entity left, Entity right)
                => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new Equalsf(left, right);
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        /// <summary>
        /// It is true iff both parts are numeric and real, and left number is greater
        /// than the right one
        /// It is false iff both parts are numeric and real, and left number is less or equal 
        /// the right one
        /// It is NaN/unsimplified otherwise.
        /// </summary>
        public sealed partial record Greaterf(Entity Left, Entity Right) : ComparisonSign, IBinaryNode
        {
            internal override Priority Priority => Priority.GreaterThan;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            internal Greaterf New(Entity left, Entity right)
                => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        /// <summary>
        /// It is true iff both parts are numeric and real, and left number is greater
        /// than the right one or equal to it
        /// It is false iff both parts are numeric and real, and left number is less 
        /// the right one
        /// It is NaN/unsimplified otherwise.
        /// </summary>
        public sealed partial record GreaterOrEqualf(Entity Left, Entity Right) : ComparisonSign, IBinaryNode
        {
            internal override Priority Priority => Priority.GreaterThan;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            internal GreaterOrEqualf New(Entity left, Entity right)
                => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        /// <summary>
        /// It is true iff both parts are numeric and real, and left number is less
        /// than the right one
        /// It is false iff both parts are numeric and real, and left number is greater or equal 
        /// the right one
        /// It is NaN/unsimplified otherwise.
        /// </summary>
        public sealed partial record Lessf(Entity Left, Entity Right) : ComparisonSign, IBinaryNode
        {
            internal override Priority Priority => Priority.GreaterThan;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            internal Lessf New(Entity left, Entity right)
                => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        /// <summary>
        /// It is true iff both parts are numeric and real, and left number is less
        /// than the right one or equal to it
        /// It is false iff both parts are numeric and real, and left number is greater
        /// the right one
        /// It is NaN/unsimplified otherwise.
        /// </summary>
        public sealed partial record LessOrEqualf(Entity Left, Entity Right) : ComparisonSign, IBinaryNode
        {
            internal override Priority Priority => Priority.GreaterThan;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Left;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Right;

            internal LessOrEqualf New(Entity left, Entity right)
                => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new(left, right) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right };
        }

        #endregion

        #region Set statements
        partial record Set
        {
            /// <summary>
            /// This node represents whether the given element is in the set
            /// </summary>
            public sealed partial record Inf(Entity Element, Entity SupSet) : Statement, IBinaryNode
            {
                internal override Priority Priority => Priority.ContainsIn;

                /// <inheritdoc/>
                public Entity NodeFirstChild => Element;

                /// <inheritdoc/>
                public Entity NodeSecondChild => SupSet;

                internal Inf New(Entity element, Entity supSet)
                    => ReferenceEquals(Element, element) && ReferenceEquals(SupSet, supSet) ? this : new(element, supSet) { Codomain = Codomain };
                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Element.Replace(func), SupSet.Replace(func)));
                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Element, SupSet };
            }

            /// <summary>
            /// The statement that every member of <see cref="Sub"/> is a member of
            /// <see cref="Super"/>: <c>A subset B</c>, or <c>A ⊆ B</c>. The relation between sets
            /// that <c>in</c> is between an element and a set, and a different one: <c>{1, 2}</c>
            /// is a subset of <c>{1, 2, 3}</c> and not an element of it. Decided from the shapes
            /// of the two sets, by the set algebra, member by member for a finite set, and
            /// through <c>forall x in A : x in B</c> where a bound reads as a comparison; two
            /// sets are equal exactly when each is a subset of the other, which is how <c>=</c>
            /// between sets is decided. A statement about sets: <c>NaN</c> where a side is a
            /// number or a truth value.
            /// https://github.com/asc-community/AngouriMath/issues/1409
            /// </summary>
            public sealed partial record Subsetf(Entity Sub, Entity Super) : Statement, IBinaryNode
            {
                internal override Priority Priority => Priority.Subset;

                /// <inheritdoc/>
                public Entity NodeFirstChild => Sub;

                /// <inheritdoc/>
                public Entity NodeSecondChild => Super;

                internal Subsetf New(Entity sub, Entity super)
                    => ReferenceEquals(Sub, sub) && ReferenceEquals(Super, super) ? this : new(sub, super) { Codomain = Codomain };
                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Sub.Replace(func), Super.Replace(func)));
                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Sub, Super };
            }
        }
        #endregion

        #region Number theory
        /// <summary>
        /// The <c>n</c>-th prime, <c>prime(n)</c>: <c>prime(1)</c> is <c>2</c>, <c>prime(25)</c>
        /// is <c>97</c>. An enumeration of <c>PP</c> in its order, which a set does not carry, so
        /// a function of its own -- SymPy's <c>prime(n)</c>. Defined on the positive whole numbers.
        /// https://github.com/asc-community/AngouriMath/issues/1450
        /// </summary>
        public sealed partial record Primef(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal Primef New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The <c>p</c>-adic valuation, <c>valuation(n, p)</c>: the exponent of the prime
        /// <c>p</c> in <c>n</c>, so <c>valuation(12, 2)</c> is <c>2</c> and <c>valuation(12, 5)</c>
        /// is <c>0</c>; <c>+oo</c> at <c>n = 0</c>, by the convention every text uses. Sullivan and
        /// Mackey's Ex 5.5.1 writes <c>n = 2^m (2 l + 1)</c> with <c>m</c> the 2-adic valuation, and
        /// <c>n = product(p^valuation(n, p), p in PP)</c> is what a factorisation says.
        /// https://github.com/asc-community/AngouriMath/issues/1450
        /// </summary>
        public sealed partial record Valuationf(Entity Argument, Entity Prime) : Function, IBinaryNode
        {
            /// <inheritdoc/>
            public Entity NodeFirstChild => Argument;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Prime;

            internal Valuationf New(Entity argument, Entity prime)
                => ReferenceEquals(Argument, argument) && ReferenceEquals(Prime, prime) ? this : new(argument, prime) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Argument.Replace(func), Prime.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument, Prime };
        }

        /// <summary>
        /// This node represents the Euler totient function (phi)
        /// </summary>
        public sealed partial record Phif(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal Phif New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The Möbius function <c>mobius(n)</c>, <c>μ(n)</c>: 0 where a square of a prime divides the positive integer <c>n</c>, and <c>(-1)^k</c> where <c>n</c> is a product of <c>k</c> distinct primes, so <c>μ(1) = 1</c>.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record Mobiusf(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal Mobiusf New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The number of distinct prime factors <c>primenu(n)</c>, <c>ω(n)</c>, of a positive integer: <c>ω(12) = 2</c> and <c>ω(1) = 0</c>.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record PrimeNuf(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal PrimeNuf New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The number of prime factors <c>primeomega(n)</c>, <c>Ω(n)</c>, of a positive integer, counted with multiplicity: <c>Ω(12) = 3</c>.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record PrimeOmegaf(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal PrimeOmegaf New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The prime-counting function <c>primepi(x)</c>, <c>π(x)</c>: how many primes are at most the real number <c>x</c>, so <c>π(10) = 4</c>.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record PrimePif(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal PrimePif New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }

        /// <summary>
        /// The divisor function <c>divisorsigma(k, n)</c>, <c>σ_k(n)</c>: the sum of the <c>k</c>-th powers of
        /// the positive divisors of the positive integer <c>n</c>, so <c>σ_0</c> counts them and <c>σ_1</c> sums
        /// them. The order first, as the notation and Mathematica's <c>DivisorSigma[k, n]</c> read.
        /// </summary>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/2182</remarks>
        public sealed partial record DivisorSigmaf(Entity Order, Entity Argument) : Function, IBinaryNode
        {
            /// <inheritdoc/>
            public Entity NodeFirstChild => Order;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Argument;

            internal DivisorSigmaf New(Entity order, Entity argument)
                => ReferenceEquals(Order, order) && ReferenceEquals(Argument, argument) ? this : new(order, argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Order.Replace(func), Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Order, Argument };
        }

        /// <summary>
        /// This node represents whether the first argument divides the second: <c>a divides b</c>
        /// is the statement that <c>b</c> is a whole multiple of <c>a</c>, which for integers is
        /// <c>b mod a = 0</c>, and <c>0 divides b</c> exactly when <c>b</c> is 0. A statement
        /// about integers: over anything else it is <c>NaN</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1212
        /// </summary>
        public sealed partial record Dividesf(Entity Divisor, Entity Dividend) : Statement, IBinaryNode
        {
            internal override Priority Priority => Priority.Divides;

            /// <inheritdoc/>
            public Entity NodeFirstChild => Divisor;

            /// <inheritdoc/>
            public Entity NodeSecondChild => Dividend;

            internal Dividesf New(Entity divisor, Entity dividend)
                => ReferenceEquals(Divisor, divisor) && ReferenceEquals(Dividend, dividend) ? this : new(divisor, dividend) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Divisor.Replace(func), Dividend.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Divisor, Dividend };
        }

        /// <summary>
        /// The statement that <c>a</c> and <c>b</c> are congruent modulo <c>n</c>: written
        /// <c>a = b (mod n)</c>, or <c>a ≡ b (mod n)</c>, and meaning <c>n divides a - b</c>. A
        /// relation between integers with the modulus written once at the end, as mathematics
        /// writes it — not the remainder operator <c>a mod n</c>, which is a number, and which the
        /// reference this follows refuses to write in this sense at all. The modulus may be any
        /// integer: modulo <c>0</c> it is equality, and its sign is immaterial, exactly as in
        /// <c>n divides a - b</c>. A statement about integers: <c>NaN</c> elsewhere.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// </summary>
        public sealed partial record Congruentf(Entity Left, Entity Right, Entity Modulus) : Statement
        {
            internal override Priority Priority => Priority.Congruent;

            internal Congruentf New(Entity left, Entity right, Entity modulus)
                => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) && ReferenceEquals(Modulus, modulus)
                    ? this : new(left, right, modulus) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Left.Replace(func), Right.Replace(func), Modulus.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Left, Right, Modulus };
        }
        #endregion

        #region Sets
        /// <summary>
        /// This node represents the cardinality of a set: <c>card(S)</c> is the number of
        /// elements of <c>S</c>. Counted for a finite set whose elements are numbers, and for
        /// an interval whose ends are; left as written for an infinite set, which has a
        /// cardinality this library has no number for, and for a set whose elements are not
        /// yet known to be distinct.
        /// https://github.com/asc-community/AngouriMath/issues/1212
        /// </summary>
        public sealed partial record Cardf(Entity Argument) : Function, IUnaryNode
        {
            /// <inheritdoc/>
            public Entity NodeChild => Argument;

            internal Cardf New(Entity argument)
                   => ReferenceEquals(argument, Argument) ? this : new(argument) { Codomain = Codomain };

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(New(Argument.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        #endregion

        #region Quantifiers
        /// <summary>
        /// A quantified statement: <c>forall x in S : P</c>, <c>exists x in S : P</c> and
        /// <c>exists! x in S : P</c>. A binder, like a set builder: the name is bound throughout
        /// the body and the set, and the statement is a function of what else the body mentions.
        /// The quantification set is mandatory, as Sullivan and Mackey's proofs book insists — a
        /// statement is quantified over something, and <c>forall x : x^2 &gt;= 0</c> is true of
        /// the reals and false of the complex numbers.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// https://github.com/asc-community/AngouriMath/issues/225
        /// </summary>
        public abstract partial record Quantifier(Entity Var, Entity Over, Entity Body) : Statement
        {
            /// <summary>The name this quantifier binds.</summary>
            public Entity Var { get; init; } = Binding.Of(Var).Name;

            /// <summary>The set the bound name ranges over.</summary>
            public Entity Over { get; init; } = Binding.Of(Var).In(Over);

            /// <summary>The statement made of every, or some, member of the set.</summary>
            public Entity Body { get; init; } = Binding.Of(Var).In(Body);

            internal override Priority Priority => Priority.Quantifier;

            /// <summary>The same quantifier over another name, set and body.</summary>
            internal abstract Quantifier New(Entity var, Entity over, Entity body);

            /// <summary>The keyword this quantifier is written with: <c>forall</c>, <c>exists</c> or <c>exists!</c>.</summary>
            internal abstract string Keyword { get; }

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Var, Over.Replace(func), Body.Replace(func)));

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Var, Over, Body };
        }

        /// <summary>
        /// <c>forall x in S : P</c>: every member of <c>S</c> satisfies <c>P</c>. True of the empty
        /// set. See <see cref="Quantifier"/>.
        /// </summary>
        public sealed partial record Forallf(Entity Var, Entity Over, Entity Body) : Quantifier(Var, Over, Body)
        {
            internal override Quantifier New(Entity var, Entity over, Entity body)
                => ReferenceEquals(Var, var) && ReferenceEquals(Over, over) && ReferenceEquals(Body, body)
                    ? this : new Forallf(var, over, body) { Codomain = Codomain };
            internal override string Keyword => "forall";
        }

        /// <summary>
        /// <c>exists x in S : P</c>: some member of <c>S</c> satisfies <c>P</c>. False of the empty
        /// set. See <see cref="Quantifier"/>.
        /// </summary>
        public sealed partial record Existsf(Entity Var, Entity Over, Entity Body) : Quantifier(Var, Over, Body)
        {
            internal override Quantifier New(Entity var, Entity over, Entity body)
                => ReferenceEquals(Var, var) && ReferenceEquals(Over, over) && ReferenceEquals(Body, body)
                    ? this : new Existsf(var, over, body) { Codomain = Codomain };
            internal override string Keyword => "exists";
        }

        /// <summary>
        /// <c>exists! x in S : P</c>: exactly one member of <c>S</c> satisfies <c>P</c>, which is
        /// <c>exists x in S : P and forall y in S : P(y) implies y = x</c>. See <see cref="Quantifier"/>.
        /// </summary>
        public sealed partial record ExistsUniquef(Entity Var, Entity Over, Entity Body) : Quantifier(Var, Over, Body)
        {
            internal override Quantifier New(Entity var, Entity over, Entity body)
                => ReferenceEquals(Var, var) && ReferenceEquals(Over, over) && ReferenceEquals(Body, body)
                    ? this : new ExistsUniquef(var, over, body) { Codomain = Codomain };
            internal override string Keyword => "exists!";
        }
        #endregion

    }
}
