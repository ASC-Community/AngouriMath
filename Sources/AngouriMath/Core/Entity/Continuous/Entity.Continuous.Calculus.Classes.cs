//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// A node of derivative
        /// </summary>
        /// <remarks>
        /// Negative iterations convert to integrals.
        /// </remarks>
        public sealed partial record Derivativef(Entity Expression, Entity Var, int Iterations) : CalculusOperator(Expression, Var)
        {
            /// <summary>Reuse the cache by returning the same object if possible</summary>
            private Derivativef New(Entity expression, Entity var) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var)
                ? this : new(expression, var, Iterations) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, Iterations };
        }

        /// <summary>
        /// A node of integral
        /// </summary>
        public sealed partial record Integralf(Entity Expression, Entity Var, (Entity from, Entity to)? Range) : CalculusOperator(Expression, Var)
        {
            /// <summary>The interval integrated over, or <see langword="null"/> for an indefinite integral.</summary>
            public (Entity from, Entity to)? Range { get; init; } = Range is var (from, to)
                ? (Binding.Of(Var).In(from), Binding.Of(Var).In(to))
                : Range;

            /// <summary>Reuse the cache by returning the same object if possible</summary>
            private Integralf New(Entity expression, Entity var, (Entity from, Entity to)? range) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var)
                && (range is null && Range is null || range is var (newFrom, newTo) && Range is var (oldFrom, oldTo)
                    && ReferenceEquals(newFrom, oldFrom) && ReferenceEquals(newTo, oldTo))
                ? this : new(expression, var, range) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, Range is var (from, to) ? (from.Replace(func), to.Replace(func)) : null));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => Range is var (from, to) ? [Expression, Var, from, to] : [Expression, Var];
        }

        /// <summary>
        /// A summation over a bound variable: <c>sum(f, i, from, to)</c> is
        /// <c>f(from) + … + f(to)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A <see cref="CalculusOperator"/> rather than a <see cref="Sumf"/> of many terms,
        /// because the bounds may be symbolic — <c>sum(i, i, 1, n)</c> has no finite expansion
        /// and is still a well-formed expression. Where they are concrete integers it expands,
        /// which is what makes the common case useful without the general case being wrong.
        /// </para>
        /// <para>
        /// The index is <b>bound</b>: it is the summation index, not a free variable of
        /// the expression, so substituting it from outside must not reach inside. That is the
        /// property <c>ConditionalSet</c> already has and the one
        /// <a href="https://github.com/asc-community/AngouriMath/issues/878">#878</a> was about
        /// getting wrong.
        /// </para>
        /// <para><a href="https://github.com/asc-community/AngouriMath/issues/248">#248</a></para>
        /// </remarks>
        public sealed partial record Summationf(Entity Expression, Entity Var, Entity From, Entity To) : CalculusOperator(Expression, Var)
        {
            /// <summary>The first value of the index.</summary>
            public Entity From { get; init; } = Binding.Of(Var).In(From);

            /// <summary>The last value of the index, inclusive.</summary>
            public Entity To { get; init; } = Binding.Of(Var).In(To);

            /// <summary>Reuse the cache by returning the same object if possible</summary>
            private Summationf New(Entity expression, Entity var, Entity from, Entity to) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var)
                && ReferenceEquals(From, from) && ReferenceEquals(To, to)
                ? this : new(expression, var, from, to) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, From.Replace(func), To.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, From, To };
        }

        /// <summary>
        /// A product over a bound variable: <c>product(f, i, from, to)</c> is
        /// <c>f(from) · … · f(to)</c>. See <see cref="Summationf"/>, which it mirrors exactly.
        /// </summary>
        /// <remarks><a href="https://github.com/asc-community/AngouriMath/issues/248">#248</a></remarks>
        public sealed partial record Productf(Entity Expression, Entity Var, Entity From, Entity To) : CalculusOperator(Expression, Var)
        {
            /// <summary>The first value of the index.</summary>
            public Entity From { get; init; } = Binding.Of(Var).In(From);

            /// <summary>The last value of the index, inclusive.</summary>
            public Entity To { get; init; } = Binding.Of(Var).In(To);

            /// <summary>Reuse the cache by returning the same object if possible</summary>
            private Productf New(Entity expression, Entity var, Entity from, Entity to) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var)
                && ReferenceEquals(From, from) && ReferenceEquals(To, to)
                ? this : new(expression, var, from, to) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, From.Replace(func), To.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, From, To };
        }

        /// <summary>
        /// A sum over a set: <c>sum(f(x), x in S)</c> is the sum of <c>f(x)</c> over the members
        /// <c>x</c> of <c>S</c>, each counted once. A binder, like <see cref="Summationf"/>, whose
        /// range is a set rather than two bounds, and so has no index and no order: the sum over
        /// the roots of a polynomial is <c>sum(f(w), w in { w : p(w) = 0 })</c>, which needs no
        /// first root.
        /// https://github.com/asc-community/AngouriMath/issues/1285
        /// </summary>
        public sealed partial record SumOverSetf(Entity Expression, Entity Var, Entity Over) : CalculusOperator(Expression, Var)
        {
            /// <summary>The set the variable ranges over.</summary>
            public Entity Over { get; init; } = Binding.Of(Var).In(Over);

            private SumOverSetf New(Entity expression, Entity var, Entity over) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var) && ReferenceEquals(Over, over)
                ? this : new(expression, var, over) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, Over.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, Over };
        }

        /// <summary>
        /// A node of limit
        /// </summary>
        public sealed partial record Limitf(Entity Expression, Entity Var, Entity Destination, ApproachFrom ApproachFrom) : CalculusOperator(Expression, Var)
        {
            /// <summary>What the variable approaches.</summary>
            public Entity Destination { get; init; } = Binding.Of(Var).In(Destination);

            /// <summary>Reuse the cache by returning the same object if possible</summary>
            private Limitf New(Entity expression, Entity var, Entity destination, ApproachFrom approachFrom) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var) && ReferenceEquals(Destination, destination)
                && ApproachFrom == approachFrom ? this : new(expression, var, destination, approachFrom) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var.Replace(func), Destination.Replace(func), ApproachFrom));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, Destination };
        }

        /// <summary>
        /// The largest value of an expression as a variable ranges over a set:
        /// <c>max(f(t), t in S)</c>. A binder, like <see cref="Summationf"/>: the variable is
        /// bound throughout the expression and the set.
        /// https://github.com/asc-community/AngouriMath/issues/1212
        /// </summary>
        public sealed partial record Maximumf(Entity Expression, Entity Var, Entity Over) : CalculusOperator(Expression, Var)
        {
            /// <summary>The set the variable ranges over.</summary>
            public Entity Over { get; init; } = Binding.Of(Var).In(Over);

            private Maximumf New(Entity expression, Entity var, Entity over) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var) && ReferenceEquals(Over, over)
                ? this : new(expression, var, over) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, Over.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, Over };
        }

        /// <summary>
        /// The smallest value of an expression as a variable ranges over a set:
        /// <c>min(f(t), t in S)</c>. See <see cref="Maximumf"/>.
        /// </summary>
        public sealed partial record Minimumf(Entity Expression, Entity Var, Entity Over) : CalculusOperator(Expression, Var)
        {
            /// <summary>The set the variable ranges over.</summary>
            public Entity Over { get; init; } = Binding.Of(Var).In(Over);

            private Minimumf New(Entity expression, Entity var, Entity over) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var) && ReferenceEquals(Over, over)
                ? this : new(expression, var, over) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, Over.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, Over };
        }

        /// <summary>
        /// The set of points at which an expression takes its largest value over a set:
        /// <c>argmax(f(t), t in S)</c>. See <see cref="Maximumf"/>.
        /// </summary>
        public sealed partial record Argmaxf(Entity Expression, Entity Var, Entity Over) : CalculusOperator(Expression, Var)
        {
            /// <summary>The set the variable ranges over.</summary>
            public Entity Over { get; init; } = Binding.Of(Var).In(Over);

            private Argmaxf New(Entity expression, Entity var, Entity over) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var) && ReferenceEquals(Over, over)
                ? this : new(expression, var, over) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, Over.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, Over };
        }

        /// <summary>
        /// The set of points at which an expression takes its smallest value over a set:
        /// <c>argmin(f(t), t in S)</c>. See <see cref="Maximumf"/>.
        /// </summary>
        public sealed partial record Argminf(Entity Expression, Entity Var, Entity Over) : CalculusOperator(Expression, Var)
        {
            /// <summary>The set the variable ranges over.</summary>
            public Entity Over { get; init; } = Binding.Of(Var).In(Over);

            private Argminf New(Entity expression, Entity var, Entity over) =>
                ReferenceEquals(Expression, expression) && ReferenceEquals(Var, var) && ReferenceEquals(Over, over)
                ? this : new(expression, var, over) { Codomain = Codomain };
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(New(Expression.Replace(func), Var, Over.Replace(func)));
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new[] { Expression, Var, Over };
        }
    }
}
