//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

namespace AngouriMath
{
    partial record Entity
    {
        #region Simple

        partial record Matrix
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => x == this ? value : Elementwise(c => c.Substitute(x, value));
        }

        partial record Sumf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Augend.Substitute(x, value), Addend.Substitute(x, value));
        }

        partial record Minusf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Minuend.Substitute(x, value), Subtrahend.Substitute(x, value));
        }

        partial record Mulf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Multiplier.Substitute(x, value), Multiplicand.Substitute(x, value));
        }

        partial record Divf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Dividend.Substitute(x, value), Divisor.Substitute(x, value));
        }

        partial record Modf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Dividend.Substitute(x, value), Divisor.Substitute(x, value));
        }

        partial record Sinf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Cosf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Secantf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Cosecantf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Tanf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Cotanf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Logf
        {
            /// <inheritdoc/>
            // The base of ln is Euler's number standing in the operator's own definition, not a
            // mention of the name e, so substituting for e leaves it alone: ln(x) at e = 3 is
            // still ln(x), as sum(ln(x), e, 1, 2) already was 2 * ln(x). Compared by reference,
            // which is what Constant.EulerIntrinsic exists for; log(e, x), where the writer did
            // name e, still becomes log(3, x).
            // https://github.com/asc-community/AngouriMath/issues/994
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value
                 : New(ReferenceEquals(Base, Constant.EulerIntrinsic) ? Base : Base.Substitute(x, value), Antilogarithm.Substitute(x, value));
        }

        partial record Powf
        {
            /// <inheritdoc/>
            // As for ln: the base of exp is not a mention of e.
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value
                 : New(ReferenceEquals(Base, Constant.EulerIntrinsic) ? Base : Base.Substitute(x, value), Exponent.Substitute(x, value));
        }

        partial record Arcsinf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Arccosf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Arctanf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Arccotanf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Arcsecantf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Arccosecantf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Factorialf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Binomialf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Upper.Substitute(x, value), Lower.Substitute(x, value));
        }

        partial record Signumf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Erff
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Iversonf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Alephf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Index.Substitute(x, value));
        }

        partial record Erfcf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Erfif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Eif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Lif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Sif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Cif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Shif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Chif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Absf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Floorf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Ceilf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Roundf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Minf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Maxf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Gcdf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Lcmf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Boolean
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : this;
        }

        partial record Notf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Andf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Orf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Xorf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Impliesf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Assumption.Substitute(x, value), Conclusion.Substitute(x, value));
        }

        partial record Equalsf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Greaterf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record GreaterOrEqualf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Lessf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record LessOrEqualf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
        }

        partial record Set
        {
            partial record FiniteSet
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : Apply(c => c.Substitute(x, value));
            }

            partial record Interval
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => New(Left.Substitute(x, value), Right.Substitute(x, value));
            }

            partial record SpecialSet
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : this;
            }

            partial record Unionf
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
            }

            partial record Intersectionf
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
            }

            partial record SetMinusf
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : New(Left.Substitute(x, value), Right.Substitute(x, value));
            }

            partial record Inf
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : New(Element.Substitute(x, value), SupSet.Substitute(x, value));
            }

            partial record Subsetf
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : New(Sub.Substitute(x, value), Super.Substitute(x, value));
            }

            partial record Powersetf
            {
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                    => x == this ? value : New(Argument.Substitute(x, value));
            }

            partial record IndexedSetOperation
            {
                /// <inheritdoc/>
                // Bound throughout, as under a quantifier: nothing is substituted for the name,
                // and a value that mentions it is kept out of its reach by renaming.
                public override Entity Substitute(Entity x, Entity value)
                {
                    if (this == x)
                        return value;
                    if (x.ContainsNode(Var))
                        return this;
                    if (value.ContainsNode(Var) && Var is Variable bound)
                    {
                        var fresh = Variable.CreateUnique(this + value, bound.Name);
                        return New(fresh, Over.Substitute(bound, fresh).Substitute(x, value), Body.Substitute(bound, fresh).Substitute(x, value));
                    }
                    return New(Var, Over.Substitute(x, value), Body.Substitute(x, value));
                }
            }
        }

        partial record Phif
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Primef
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Valuationf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value), Prime.Substitute(x, value));
        }

        partial record Dividesf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Divisor.Substitute(x, value), Dividend.Substitute(x, value));
        }

        partial record Congruentf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Left.Substitute(x, value), Right.Substitute(x, value), Modulus.Substitute(x, value));
        }

        partial record Quantifier
        {
            /// <inheritdoc/>
            // The name is bound throughout, so substituting for it -- or for anything that
            // mentions it, since every occurrence inside is the bound one -- substitutes nothing.
            // A value that mentions the name would be captured by it, so the name is changed
            // first: forall x in RR : x < y at y = x is forall x_1 in RR : x_1 < x, the same
            // statement under either name.
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                if (x.ContainsNode(Var))
                    return this;
                if (value.ContainsNode(Var) && Var is Variable bound)
                {
                    var fresh = Variable.CreateUnique(this + value, bound.Name);
                    return New(fresh, Over.Substitute(bound, fresh).Substitute(x, value), Body.Substitute(bound, fresh).Substitute(x, value));
                }
                return New(Var, Over.Substitute(x, value), Body.Substitute(x, value));
            }
        }

        partial record Cardf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Argument.Substitute(x, value));
        }

        partial record Providedf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => this == x ? value : New(Expression.Substitute(x, value), Predicate.Substitute(x, value));
        }

        partial record Piecewise
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => x == this ? value : Apply(c => c.New(c.Expression.Substitute(x, value), c.Predicate.Substitute(x, value)));
        }

        #endregion

        partial record Application
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
                => x == this ? value : New(Expression.Substitute(x, value), Arguments.Map(arg => arg.Substitute(x, value)));
        }

        #region Local variable preserved
        partial record Set
        {
            partial record ConditionalSet
            {
                // TODO: it might be optimized
                /// <inheritdoc/>
                public override Entity Substitute(Entity x, Entity value)
                {
                    if (this == x)
                        return value;
                    var replacement = Variable.CreateTemp((x + value + Predicate + Var).Vars);

                    // { x | x > a } -> { temp_1 | temp_1 > a }
                    var tempSubstituted = Predicate.Substitute(Var, replacement);

                    // a = 0 -> { temp_1 | temp_1 > a } -> { temp_1 | temp_1 > 0 }
                    // x = 0 -> { temp_1 | temp_1 > a } -> { temp_1 | temp_1 > a }
                    var subs = tempSubstituted.Substitute(x, value);

                    // { temp_1 | temp_1 > a } -> { x | x > a }
                    var postSubs = subs.Substitute(replacement, Var);

                    return New(Var, postSubs);
                }
            }
        }

        partial record Integralf
        {
            // TODO: it might be optimized
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);

                // integrate(x ^ 2 + a, x) -> integrate(temp_1 ^ 2 + a, temp_1)
                var tempSubstituted = Expression.Substitute(Var, replacement);

                // a = 0 -> integrate(temp_1 ^ 2 + a, temp_1) -> integrate(temp_1 ^ 2 + 0, temp_1)
                // x = 0 -> integrate(temp_1 ^ 2 + a, temp_1) -> integrate(temp_1 ^ 2 + a, temp_1)
                var subs = tempSubstituted.Substitute(x, value);

                // integrate(temp_1 ^ 2 + a, temp_1) -> integrate(x ^ 2 + a, temp_1)
                var postSubs = subs.Substitute(replacement, Var);

                return New(postSubs, Var, Range is var (from, to) ? (from.Substitute(x, value), to.Substitute(x, value)) : null);
            }
        }

        partial record Summationf
        {
            /// <inheritdoc/>
            /// <remarks>
            /// The index is <b>bound</b>, so a substitution from outside must not reach it. Renamed
            /// to a fresh variable first, exactly as <see cref="Integralf"/> does; without this
            /// `sum(i, i, 1, n).Substitute("i", 5)` would rewrite the index itself.
            /// </remarks>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);
                var tempSubstituted = Expression.Substitute(Var, replacement);
                var subs = tempSubstituted.Substitute(x, value);
                var postSubs = subs.Substitute(replacement, Var);
                return New(postSubs, Var, From.Substitute(x, value), To.Substitute(x, value));
            }
        }

        partial record SumOverSetf
        {
            /// <inheritdoc/>
            // The variable is bound; renamed first, as a summation's index is.
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);
                var renamed = Expression.Substitute(Var, replacement).Substitute(x, value).Substitute(replacement, Var);
                return New(renamed, Var, Over.Substitute(x, value));
            }
        }

        partial record Maximumf
        {
            /// <inheritdoc/>
            // The variable is bound; renamed first, as a summation's index is.
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);
                var renamed = Expression.Substitute(Var, replacement).Substitute(x, value).Substitute(replacement, Var);
                return New(renamed, Var, Over.Substitute(x, value));
            }
        }

        partial record Minimumf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);
                var renamed = Expression.Substitute(Var, replacement).Substitute(x, value).Substitute(replacement, Var);
                return New(renamed, Var, Over.Substitute(x, value));
            }
        }

        partial record Argmaxf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);
                var renamed = Expression.Substitute(Var, replacement).Substitute(x, value).Substitute(replacement, Var);
                return New(renamed, Var, Over.Substitute(x, value));
            }
        }

        partial record Argminf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);
                var renamed = Expression.Substitute(Var, replacement).Substitute(x, value).Substitute(replacement, Var);
                return New(renamed, Var, Over.Substitute(x, value));
            }
        }

        partial record Productf
        {
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);
                var tempSubstituted = Expression.Substitute(Var, replacement);
                var subs = tempSubstituted.Substitute(x, value);
                var postSubs = subs.Substitute(replacement, Var);
                return New(postSubs, Var, From.Substitute(x, value), To.Substitute(x, value));
            }
        }

        partial record Derivativef
        {
            // TODO: it might be optimized
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);

                // derive(x ^ 2 + a, x) -> derive(temp_1 ^ 2 + a, temp_1)
                var tempSubstituted = Expression.Substitute(Var, replacement);

                // a = 0 -> derive(temp_1 ^ 2 + a, temp_1) -> derive(temp_1 ^ 2 + 0, temp_1)
                // x = 0 -> derive(temp_1 ^ 2 + a, temp_1) -> derive(temp_1 ^ 2 + a, temp_1)
                var subs = tempSubstituted.Substitute(x, value);

                // derive(temp_1 ^ 2 + a, temp_1) -> derive(x ^ 2 + a, temp_1)
                var postSubs = subs.Substitute(replacement, Var);

                return New(postSubs, Var);
            }
        }

        partial record Limitf
        {
            // TODO: it might be optimized
            /// <inheritdoc/>
            public override Entity Substitute(Entity x, Entity value)
            {
                if (this == x)
                    return value;
                var replacement = Variable.CreateTemp((x + value + Expression + Var).Vars);

                // derive(x ^ 2 + a, x) -> derive(temp_1 ^ 2 + a, temp_1)
                var tempSubstituted = Expression.Substitute(Var, replacement);

                // a = 0 -> derive(temp_1 ^ 2 + a, temp_1) -> derive(temp_1 ^ 2 + 0, temp_1)
                // x = 0 -> derive(temp_1 ^ 2 + a, temp_1) -> derive(temp_1 ^ 2 + a, temp_1)
                var subs = tempSubstituted.Substitute(x, value);

                // derive(temp_1 ^ 2 + a, temp_1) -> derive(x ^ 2 + a, temp_1)
                var postSubs = subs.Substitute(replacement, Var);

                var dst = Destination.Substitute(x, value);

                return New(postSubs, Var, dst, ApproachFrom);
            }
        }

        partial record Lambda
        {
            /// <inheritdocs/>
            public override Entity Substitute(Entity x, Entity value)
                => Unit.Flow switch
                { 
                    _ when this == x => value,
                    _ when x == Parameter => this,
                    _ when value.Vars.Contains(Parameter) =>
                        Unit.Flow
                            .Let(out var newVar, Variable.CreateUniqueAlphabetFirst(this + value))
                            .ReplaceWith(new Lambda(newVar, Body.Substitute(Parameter, newVar).Substitute(x, value))),
                    _ => New(Parameter, Body.Substitute(x, value))
                };
        }

        #endregion
    }
}
