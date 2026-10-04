//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Functions.Algebra;

using System;

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
        /// <paramref name="over"/> that does not vary in <c>x</c>, or a derivative the library
        /// cannot take.
        /// </remarks>
        private static Entity? ChangeOfVariable(Entity expr, Entity over, int times)
        {
            var x = over.Vars[0];
            var dOver = over.Differentiate(x).InnerSimplified;
            if (dOver is Derivativef || dOver == 0)
                return null;
            var result = expr;
            for (var taken = 0; taken < times; taken++)
            {
                var dResult = result.Differentiate(x);
                if (dResult is Derivativef)
                    return null;
                result = (dResult / dOver).InnerSimplified;
            }
            return result;
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
                        (var expr, Variable var, int asInt)
                            when expr.InnerDifferentiate(var, asInt) is var res and not Derivativef
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
                        (var expr, Entity otherExpr, int asInt)
                            when otherExpr.Vars.Count is 1
                            && ChangeOfVariable(expr, otherExpr, asInt) is { } res
                            => res.InnerSimplified(isExact),
                        _ => null
                    },
                    (@this, a, b, _) => ((Derivativef)@this).New(a, b), isExact);
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
