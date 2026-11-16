//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Functions.Algebra;

using System;
using System.Collections.Generic;
using System.Linq;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// The derivative of <paramref name="expr"/> with respect to <paramref name="over"/>,
        /// taken <paramref name="times"/> times, by changing variables rather than by renaming.
        /// </summary>
        /// <remarks>
        /// Differentiating with respect to something that is a function of one variable is
        /// <c>(df/dx) / (dg/dx)</c> — the chain rule read backwards, which is the change of
        /// variables <c>z = g(x)</c> without having to invert <c>g</c>. It needs no occurrence of
        /// <paramref name="over"/> in <paramref name="expr"/>: <c>d(x ^ 2)/d(x + 1)</c> is
        /// <c>2x</c>. <see langword="null"/> where the question has no answer this way — a
        /// <paramref name="over"/> that does not vary in <paramref name="x"/>, or a derivative the
        /// library cannot take.
        /// <para>
        /// The derivative of an unknown function, <c>derivative(apply(f, x), x)</c>, is taken: it
        /// is the value <c>f'(x)</c>, not a refusal. So <c>x f(x)</c> over <c>f(x)</c> is
        /// <c>(f + x f') / f'</c>, defined where <c>f' ≠ 0</c>, which is where <c>f(x)</c> can
        /// stand for <c>x</c> at all.
        /// https://github.com/asc-community/AngouriMath/issues/230
        /// </para>
        /// </remarks>
        private static Entity? ChangeOfVariable(Entity expr, Entity over, Variable x, int times)
        {
            var dOver = over.Differentiate(x).InnerSimplified;
            if (IsUntaken(dOver) || dOver == 0)
                return null;
            var result = expr;
            for (var taken = 0; taken < times; taken++)
            {
                var dResult = result.Differentiate(x);
                if (IsUntaken(dResult))
                    return null;
                result = (dResult / dOver).InnerSimplified;
            }
            return result;
        }

        /// <summary>
        /// A derivative the library could not take, as opposed to the derivative of an unknown
        /// function, <c>derivative(apply(f, x), x)</c>, which is the answer <c>f'(x)</c>.
        /// </summary>
        private static bool IsUntaken(Entity derivative)
            => derivative is Derivativef { Expression: not Application { Expression: Variable } };

        /// <summary>
        /// The one variable <paramref name="over"/> varies in, or <see langword="null"/> where it
        /// varies in none or in more than one. The name of an unknown function is not a variable
        /// it varies in: <c>apply(f, x)</c> varies in <c>x</c>, and <c>f</c> names what it is.
        /// </summary>
        private static Variable? TheVariableItVariesIn(Entity over)
        {
            Variable? found = null;
            foreach (var variable in over.Vars)
            {
                if (!IsOnlyAFunctionName(over, variable))
                {
                    if (found is not null)
                        return null;
                    found = variable;
                }
            }
            return found;

            static bool IsOnlyAFunctionName(Entity expr, Variable name)
                => expr == name ? false
                : expr is Application(var head, var arguments)
                    ? (head == name || IsOnlyAFunctionName(head, name)) && arguments.All(argument => IsOnlyAFunctionName(argument, name))
                    : expr.DirectChildren.All(child => IsOnlyAFunctionName(child, name));
        }

        public partial record Derivativef
        {
            // The derivative operator is always defined symbolically, even though
            // the resulting expression may be undefined at certain points.
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoAndTArguments(Expression, Var, Iterations,
                    (a, b, c) => (a, b, c) switch
                    {
                        // InnerDifferentiate, not the public Differentiate: the test below is
                        // "did the derivative come back unresolved", and the public overload
                        // simplifies each pass, so asking it would simplify the very
                        // Derivativef this is deciding about and arrive back here unchanged.
                        // https://github.com/asc-community/AngouriMath/issues/1002
                        //
                        // Nor where the derivative comes back holding this very node: the sign
                        // of sqrt(x) - 1 is flat where x > 0 and unevaluated elsewhere, a
                        // piecewise whose second case is this node, and simplifying that case
                        // would ask again without end.
                        (var expr, Variable var, int asInt)
                            when expr.InnerDifferentiate(var, asInt) is var res and not Derivativef
                            && !res.ContainsNode(new Derivativef(expr, var, asInt))
                            => Core.Binding.Written(var, res.InnerSimplified(isExact)),
                        (var expr, Variable var, int asInt) => null,
                        (Application, _, _) => null,
                        // Differentiating with respect to a subexpression rather than a variable
                        // -- https://github.com/asc-community/AngouriMath/issues/230. The
                        // subexpression has to be something that can *vary*: with a number there,
                        // `derivative(x ^ 3, 3)` renamed the exponent and answered
                        // `ln(x) * x ^ 3`, which is the derivative of a question nobody asked.
                        // https://github.com/asc-community/AngouriMath/issues/964
                        //
                        // Where the whole expression can be written in terms of the subexpression,
                        // naming it and differentiating over the name is exact: sin(x) ^ 3 is
                        // z ^ 3, and d(z ^ 3)/dz is 3z ^ 2 wherever z is. That is this arm, and
                        // the guard is that nothing of the subexpression's own variables is left
                        // over -- without it, `derivative(x * (x + 1), x + 1)` renamed one factor,
                        // read the other as independent of the name, and answered `x` where the
                        // change of variables gives 2x + 1.
                        (var expr, Entity otherExpr, int asInt)
                            when otherExpr.Vars.Count > 0
                            && Variable.CreateTemp(otherExpr.Vars) is var tempVar
                            && expr.Substitute(otherExpr, tempVar) is var tempSubstituted
                            && !otherExpr.Vars.Any(tempSubstituted.ContainsNode)
                            && tempSubstituted.InnerDifferentiate(tempVar, asInt) is var res and not Derivativef
                            => res.Substitute(tempVar, otherExpr).InnerSimplified(isExact),
                        // Otherwise it is a change of variables and not a rename, and the
                        // subexpression does not need to occur at all: with z = x + 1, x ^ 2 is
                        // (z - 1) ^ 2 and its derivative is 2(z - 1) = 2x. df/dg is
                        // (df/dx) / (dg/dx), which is that without having to invert g.
                        // https://github.com/asc-community/AngouriMath/pull/990
                        //
                        // One variable, which an unknown function applied to it, f(x), varies in as
                        // x does; with two, as in x * y, the derivative depends on how they move
                        // together and is not determined.
                        (var expr, Entity otherExpr, int asInt)
                            when TheVariableItVariesIn(otherExpr) is { } x
                            && ChangeOfVariable(expr, otherExpr, x, asInt) is { } res
                            => res.InnerSimplified(isExact),
                        _ => null
                    },
                    (@this, a, b, _) => ((Derivativef)@this).New(a, b), isExact);
        }
        
        public partial record PartialDerivativef
        {
            // Defined symbolically, as the derivative is.
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <summary>
            /// Whether <paramref name="node"/> is a slot of its own: an unknown function applied,
            /// <c>f(x)</c>, or a derivative of one in a variable, <c>f'(x)</c>.
            /// </summary>
            internal static bool IsASlot(Entity node)
                => node is Application { Expression: Variable }
                || node is Derivativef { Expression: Application { Expression: Variable }, Var: Variable, Iterations: > 0 };

            /// <summary>
            /// The derivative of <paramref name="expr"/> in the slot <paramref name="slot"/>,
            /// <paramref name="times"/> times, with the other slots held fixed; <see langword="null"/>
            /// where that cannot be taken.
            /// </summary>
            /// <remarks>
            /// Every slot is renamed to a fresh variable, the derivatives first, so that
            /// <c>f'(x)</c> is not read as a function of <c>f(x)</c>: renaming <c>f(x)</c> inside it
            /// would make it the derivative of a variable. An unknown function of the slot,
            /// <c>g(f(x))</c>, is not another slot but a function of this one, and is not renamed.
            /// The renaming is a substitution, which respects a binder, and what still mentions the
            /// slot after it -- under a binder of its variable, or inside a derivative node over the
            /// slot -- has no answer here, nor does a derivative of something built from it that the
            /// library could not take.
            /// </remarks>
            private static Entity? InTheSlot(Entity expr, Entity slot, int times)
            {
                var taken = new HashSet<Variable>(expr.Vars.Concat(slot.Vars));
                Variable Fresh()
                {
                    var fresh = Variable.CreateTemp(taken);
                    taken.Add(fresh);
                    return fresh;
                }
                var over = Fresh();
                var others = new List<(Entity Slot, Variable Name)>();
                var renamed = expr;
                foreach (var derivativesFirst in new[] { true, false })
                    foreach (var node in expr.Nodes.Where(node => IsASlot(node) && node is Derivativef == derivativesFirst
                        && !(node is Application && node != slot && node.ContainsNode(slot))).Distinct().ToList())
                    {
                        if (node == slot)
                            renamed = renamed.Substitute(node, over);
                        else
                        {
                            var name = Fresh();
                            others.Add((node, name));
                            renamed = renamed.Substitute(node, name);
                        }
                    }
                if (renamed.ContainsNode(slot))
                    return null;
                var result = renamed;
                for (var i = 0; i < times; i++)
                {
                    result = result.Differentiate(over);
                    if (result.Nodes.Any(node => node is Derivativef { Var: var v } derivative && v == over && !IsASlot(derivative)))
                        return null;
                }
                result = result.Substitute(over, slot);
                foreach (var (other, name) in others)
                    result = result.Substitute(name, other);
                return result;
            }

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoAndTArguments(Expression, Var, Iterations,
                    (a, b, c) => (a, b, c) switch
                    {
                        (_, _, < 1) => null,
                        // Over a variable it is the derivative: every other variable held fixed,
                        // and what depends on the variable following it, as SymPy's diff over a
                        // symbol does.
                        (var expr, Variable var, int asInt)
                            => new Derivativef(expr, var, asInt).InnerSimplified(isExact),
                        (var expr, var slot, int asInt)
                            when IsASlot(slot) && InTheSlot(expr, slot, asInt) is { } res
                            => res.InnerSimplified(isExact),
                        _ => null
                    },
                    (@this, a, b, _) => ((PartialDerivativef)@this).New(a, b), isExact);
        }

        public partial record Integralf
        {
            // The integral operator is always defined symbolically, even though
            // the antiderivative may not exist in closed form or may be undefined at certain points.
            private protected override Entity IntrinsicCondition => Boolean.True;

            private static Entity? ConditionallySimplified(Entity e, bool isExact) => e is Integralf ? null : e.InnerSimplified(isExact);

            /// <summary>
            /// The definite integral, or the node as written where the integrand can jump and
            /// the split at its jumps declined. Not <see langword="null"/> there: null hands the
            /// expression to the expansion below, which distributes the integral into the
            /// cases of a piecewise and under a <c>provided</c> whether or not their
            /// conditions mention the integration variable -- and where they do, that is an
            /// answer with the variable still in it. See BreakpointIntegration.
            /// </summary>
            private static Entity? DefiniteOrDeclined(Entity expr, Variable var, Entity from, Entity to, bool isExact)
            {
                var result = expr.Integrate(var, from, to);
                if (result is not Integralf)
                    return result.InnerSimplified(isExact);
                return Functions.Algebra.BreakpointIntegration.HasABreak(expr, var) ? result : null;
            }

            /// <summary>
            /// The antiderivative of <paramref name="expr"/> with respect to
            /// <paramref name="over"/>, by changing variables rather than by renaming.
            /// </summary>
            /// <remarks>
            /// With <c>z = g(x)</c>, the integral of <c>f</c> over <c>z</c> is the integral of
            /// <c>f (dg/dx)</c> over <c>x</c>, which needs no occurrence of
            /// <paramref name="over"/> in <paramref name="expr"/> and no inverse of <c>g</c>.
            /// <see langword="null"/> where there is no answer this way.
            /// </remarks>
            private static Entity? ChangeOfVariable(Entity expr, Entity over)
            {
                var x = over.Vars[0];
                var dOver = over.Differentiate(x).InnerSimplified;
                if (dOver is Derivativef || dOver == 0)
                    return null;
                return (expr * dOver).Integrate(x) is var res and not Integralf ? res : null;
            }
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoAndTArguments(Expression, Var, Range,
                    (a, b, c) => (a, b, c) switch
                    {
                        (var expr, Variable var, var (from, to)) => Core.Binding.Written(var, DefiniteOrDeclined(expr, var, from, to, isExact)),
                        // The rename, under the same guard as in Derivativef above: exact only
                        // where nothing of the subexpression's own variables is left over.
                        // https://github.com/asc-community/AngouriMath/issues/964
                        (var expr, var otherExpr, var (from, to))
                            when otherExpr.Vars.Count > 0
                            && Variable.CreateTemp(otherExpr.Vars) is var tempVar
                            && expr.Substitute(otherExpr, tempVar) is var tempSubstituted
                            && !otherExpr.Vars.Any(tempSubstituted.ContainsNode)
                            && tempSubstituted.Integrate(tempVar, from, to) is var res => ConditionallySimplified(res.Substitute(tempVar, otherExpr), isExact),
                        (var expr, Variable var, null) => Core.Binding.Written(var, ConditionallySimplified(expr.Integrate(var), isExact)),
                        (var expr, var otherExpr, null)
                            when otherExpr.Vars.Count > 0
                            && Variable.CreateTemp(otherExpr.Vars) is var tempVar
                            && expr.Substitute(otherExpr, tempVar) is var tempSubstituted
                            && !otherExpr.Vars.Any(tempSubstituted.ContainsNode)
                            && tempSubstituted.Integrate(tempVar) is var res => ConditionallySimplified(res.Substitute(tempVar, otherExpr), isExact),
                        // Otherwise a change of variables, as the derivative does: with z = g(x),
                        // the integral over z is the integral of f (dg/dx) over x. Indefinite
                        // only -- a range is stated in z, and converting it needs g inverted,
                        // which is a separate question.
                        // https://github.com/asc-community/AngouriMath/pull/990
                        (var expr, var otherExpr, null)
                            when otherExpr.Vars.Count is 1
                            && ChangeOfVariable(expr, otherExpr) is { } res => ConditionallySimplified(res, isExact),
                        _ => null
                    },
                    (@this, a, b, c) => ((Integralf)@this).New(a, b, c), isExact);
        }


        partial record Maximumf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => Functions.ExtremumOverSet.Value(Expression, Var, Over, largest: true) ?? this;
        }

        partial record Minimumf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => Functions.ExtremumOverSet.Value(Expression, Var, Over, largest: false) ?? this;
        }

        partial record Argmaxf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => Functions.ExtremumOverSet.Points(Expression, Var, Over, largest: true) ?? this;
        }

        partial record Argminf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => Functions.ExtremumOverSet.Points(Expression, Var, Over, largest: false) ?? this;
        }

        partial record SumOverSetf
        {
            /// <summary>
            /// A sum over a set is a well-formed expression whatever the set, as
            /// <see cref="Summationf"/> is whatever its bounds; adding its terms up is an
            /// evaluation, not a claim about where it is defined.
            /// </summary>
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            /// <remarks>
            /// Where the terms are not added up, the sum is still written in its simplest parts, so
            /// that a derivative taken term by term reads as one: <c>2 y w</c> and not
            /// <c>0 * y ^ 2 + 2 * y ^ 1 * 1 * w</c>.
            /// </remarks>
            protected override Entity InnerSimplify(bool isExact)
                => Functions.SumOverSet.Value(Expression, Var, Over, isExact)
                    ?? New(Expression.InnerSimplified, Var, Over.InnerSimplified);
        }

        partial record ProductOverSetf
        {
            /// <summary>
            /// A product over a set is a well-formed expression whatever the set, as a sum over a set
            /// is; multiplying its factors out is an evaluation, not a claim about where it is defined.
            /// </summary>
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            /// <remarks>
            /// The factors are multiplied out where the sum's terms would be added up -- a listed set
            /// of numbers, distinct by value, and the roots of a polynomial the solver writes all of
            /// -- and the product is left as written, in its simplest parts, where they would not.
            /// https://github.com/asc-community/AngouriMath/issues/2449
            /// </remarks>
            protected override Entity InnerSimplify(bool isExact)
                => Functions.SumOverSet.Value(Expression, Var, Over, isExact, product: true)
                    ?? New(Expression.InnerSimplified, Var, Over.InnerSimplified);
        }

        // TODO: rewrite this part too
        public partial record Summationf
        {
            /// <summary>
            /// A summation is a well-formed expression whatever its bounds, so it is defined
            /// everywhere its expression is; the expansion below is an evaluation, not a
            /// definedness claim.
            /// </summary>
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <summary>
            /// How many terms are worth writing out. A sum of a thousand terms is a correct
            /// expansion and a useless expression, and the cost is paid by everything downstream
            /// that then walks it.
            /// </summary>
            internal const int MaxExpandedTerms = 100;

            /// <inheritdoc/>
            /// <remarks>
            /// The expansion first, because it answers any summand at all where the range is
            /// short, and the closed form only a polynomial one. What the closed form then
            /// reaches is the two cases the expansion declines: a symbolic bound, and a concrete
            /// range too long to write out — where it is not a fallback but the better answer,
            /// since it computes rather than expands.
            ///
            /// A count comes before all of them: a sum of an Iverson bracket is the number of
            /// indices its statement holds for, which is computed however long the range is, and in
            /// a nested sum the count of the inner one is what the outer one sums.
            /// </remarks>
            protected override Entity InnerSimplify(bool isExact) =>
                Functions.IversonSum.ClosedForm(Expression, Var, From, To)
                ?? Expanded(this, Expression, Var, From, To, static (a, b) => a + b, 0, isExact)
                ?? Functions.PolynomialSummation.ClosedForm(Expression, Var, From, To)
                ?? Functions.ExponentialSeries.ClosedForm(Expression, Var, From, To)
                ?? Functions.BinomialSum.ClosedForm(Expression, Var, From, To)
                ?? Functions.BinomialIdentities.ClosedForm(Expression, Var, From, To)
                ?? Functions.FactorialSum.ClosedForm(Expression, Var, From, To)
                ?? Functions.GeometricSeries.ClosedForm(Expression, Var, From, To)
                ?? Functions.PolynomialGeometricSeries.ClosedForm(Expression, Var, From, To)
                // Last: a series that converges is summed above, and only what none of them
                // answered is asked whether it diverges.
                ?? Functions.DivergentSeries.ClosedForm(Expression, Var, From, To)
                ?? this;

            /// <summary>
            /// Writes the operator out where its bounds are concrete integers and there are few
            /// enough terms, and returns null otherwise -- an unevaluated operator rather than a
            /// wrong or enormous answer.
            /// </summary>
            internal static Entity? Expanded(Entity self, Entity expression, Entity var, Entity from, Entity to,
                Func<Entity, Entity, Entity> combine, Entity empty, bool isExact)
            {
                if (var is not Variable index)
                    return null;
                if (from.Evaled is not Integer lower || to.Evaled is not Integer upper)
                    return null;
                if (!lower.EInteger.CanFitInInt32() || !upper.EInteger.CanFitInInt32())
                    return null;
                var lo = lower.EInteger.ToInt32Checked();
                var hi = upper.EInteger.ToInt32Checked();
                // An empty range is the operator's identity: nothing summed is 0, nothing
                // multiplied is 1. Stated rather than left to the loop, which would return the
                // accumulator's initial value by accident and read as a coincidence.
                if (hi < lo)
                    return empty;
                if ((long)hi - lo + 1 > MaxExpandedTerms)
                    return null;
                var accumulated = empty;
                for (var i = lo; i <= hi; i++)
                    accumulated = combine(accumulated, expression.Substitute(index, i));
                return accumulated.InnerSimplified;
            }
        }

        public partial record Productf
        {
            /// <summary>See <see cref="Summationf"/>; a product is defined wherever its expression is.</summary>
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            /// <remarks>
            /// The expansion first and the closed form after, as for the sum — and the closed
            /// form is narrower here, a product having no linearity to take a general polynomial
            /// apart with. A monomial body is what separates.
            /// </remarks>
            protected override Entity InnerSimplify(bool isExact) =>
                Summationf.Expanded(this, Expression, Var, From, To, static (a, b) => a * b, 1, isExact)
                ?? Functions.MonomialProduct.ClosedForm(Expression, Var, From, To)
                ?? Functions.MonomialProduct.OfAPower(Expression, Var, From, To)
                ?? this;
        }

        public partial record Limitf
        {
            // The limit operator is always defined symbolically, even though
            // the limit may not exist (returns NaN/undefined) for certain functions.
            private protected override Entity IntrinsicCondition => Boolean.True;
            
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoAndTArguments(
                    Expression, Destination, (v: Var, ap: ApproachFrom),
                    (expr, dest, vap) => vap.v switch
                    {
                        Variable v when expr.Limit(v, dest, vap.ap) is var res and not Limitf => res.InnerSimplified(isExact),
                        _ => null
                    },
                    (@this, expr, dest, vap) => ((Limitf)@this).New(expr, vap.v, dest, vap.ap), isExact);
        }
    }
}
