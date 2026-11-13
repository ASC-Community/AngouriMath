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
        partial record Set
        {
            partial record FiniteSet
            {
                /// <inheritdoc/>
                private protected override string StringizeNode()
                    => $"{{ {string.Join(", ", Elements.Select(c => c.Stringize()))} }}";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record Interval
            {
                /// <inheritdoc/>
                private protected override string StringizeNode()
                {
                    var left = LeftClosed ? "[" : "(";
                    var right = RightClosed ? "]" : ")";
                    return left + Left.Stringize() + "; " + Right.Stringize() + right;
                }
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record ConditionalSet
            {
                /// <inheritdoc/>
                private protected override string StringizeNode()
                    => DeclaredMembership is var (set, rest)
                        ? $"{{ {Var.Stringize()} in {set.Stringize()} : {rest.Stringize()} }}"
                        : $"{{ {Var.Stringize()} : {Predicate.Stringize()} }}";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record SpecialSet
            {
                partial record Booleans
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "BB";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }

                partial record Integers
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "ZZ";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }

                partial record NonNegativeIntegers
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "ZZ*";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }

                partial record Primes
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "PP";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }

                partial record PositiveIntegers
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "ZZ+";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }

                partial record Rationals
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "QQ";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }

                partial record Reals
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "RR";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }

                partial record Complexes
                {
                    /// <inheritdoc/>
                    private protected override string StringizeNode() => "CC";
                    /// <inheritdoc/>
                    public override string ToString() => Stringize();
                }
            }

            partial record Unionf
            {
                /// <inheritdoc/>
                // Union is associative, so a union on the right may stay unbracketed -- but `\/`
                // and `\` share one precedence level and are folded by one loop in the grammar, so
                // a *set difference* on the right must be bracketed or it is re-read as the outer
                // operator: `{ 1, 2 } \/ ({ 3 } \ { 1, 2 })` is { 1, 2, 3 } and
                // `({ 1, 2 } \/ { 3 }) \ { 1, 2 }` is { 3 }.
                private protected override string StringizeNode()
                    => $@"{Left.Stringize(Left.Priority < Priority)} \/ {Right.Stringize(Right.Priority < Priority || Right is SetMinusf)}";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record Intersectionf
            {
                /// <inheritdoc/>
                private protected override string StringizeNode()
                    => $@"{Left.Stringize(Left.Priority < Priority)} /\ {Right.Stringize(Right.Priority < Priority)}";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record SetMinusf
            {
                /// <inheritdoc/>
                // Set difference is not associative, and it shares its precedence level with
                // union, so anything at that level on the right needs bracketing -- the same rule
                // `-` and `/` follow. `{1,2,3} \ ({2,3} \ {3})` is { 1, 3 } where
                // `({1,2,3} \ {2,3}) \ {3}` is { 1 }.
                private protected override string StringizeNode()
                    => $@"{Left.Stringize(Left.Priority < Priority)} \ {Right.Stringize(Right.Priority <= Priority)}";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record Inf
            {
                /// <inheritdoc/>
                // `in` is folded to the left and is not associative: `(a in b) in c` asks whether
                // a truth value is an element of c, `a in (b in c)` whether a is an element of one.
                private protected override string StringizeNode()
                    => $@"{Element.Stringize(Element.Priority < Priority)} in {SupSet.Stringize(SupSet.Priority <= Priority)}";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record Subsetf
            {
                /// <inheritdoc/>
                // The ASCII spelling; `⊆` is read by the parser and waits for a Unicode output
                // flag (#1242). Not associative, for the same reason as `in`.
                private protected override string StringizeNode()
                    => $@"{Sub.Stringize(Sub.Priority < Priority)} subset {Super.Stringize(Super.Priority <= Priority)}";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record Powersetf
            {
                /// <inheritdoc/>
                private protected override string StringizeNode()
                    => $"powerset({Argument.Stringize()})";
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record IndexedSetOperation
            {
                /// <inheritdoc/>
                private protected override string StringizeNode()
                    => $"{Keyword}({Body.Stringize()}, {Var.Stringize()} in {Over.Stringize(Over.Priority < Priority.SetOperation)})";
            }

            // ToString on each sealed record rather than on the base: a record's generated
            // PrintMembers calls ToString on its members, and an override on the base is what
            // the generated one calls back into.
            partial record IndexedUnionf
            {
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }

            partial record IndexedIntersectionf
            {
                /// <inheritdoc/>
                public override string ToString() => Stringize();
            }
        }

        

        partial record Providedf
        {
            /// <inheritdoc/>
            // `provided` is the one infix operator the grammar folds to the *right*, so this is
            // the mirror of the `implies` rule: the flat form is read as
            // `a provided (b provided c)`, and it is the left operand that has to say when it is
            // an attached condition of its own. `(x provided p) provided q` printed flat came
            // back as `x provided (p provided q)` -- the same value, since both are `x` exactly
            // when `p` and `q` hold, and a different expression, which is what the round trip is
            // about.
            private protected override string StringizeNode() => $@"{Expression.Stringize(Expression.Priority <= Priority.Provided)} provided {Predicate.Stringize(Predicate.Priority < Priority.Provided)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Piecewise
        {
            /// <inheritdoc/>
            // piecewise(a provided p, b provided q). The `if` spelling is not in the grammar,
            // and neither is a bare comma-separated list, so a printed piecewise came back
            // either as a product with `if` read as an undeclared variable, or as nothing at
            // all once there was more than one case.
            private protected override string StringizeNode()
                => "piecewise(" + string.Join(", ",
                    Cases.Select(n => n.Expression.Stringize(n.Expression.Priority < Priority)
                                      + " provided "
                                      + n.Predicate.Stringize(n.Predicate.Priority < Priority))) + ")";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        // https://github.com/asc-community/AngouriMath/issues/2147
        partial record Determinantf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"det({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Transposef
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"transpose({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Tracef
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"trace({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record MatrixEntryf
        {
            // The brackets follow a name or a call directly, and anything looser is put in
            // parentheses first, so that (A + B)[1, 2] is not read as A + B[1, 2].
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Argument.Stringize(Argument.Priority < Priority.Func)}[{Row.Stringize()}, {Column.Stringize()}]";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Matrix
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => "[" +
                    string.Join(", ",
                        IsVector switch
                        {
                            true => this.Select(c => c.ToString()),
                            false => this.Select(c => $"[{string.Join(", ", ((Matrix)c).T)}]"),
                        }) +
                   "]";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Application
        {
            /// <inheritdoc/>
            // apply(f, a, b), for the same reason: juxtaposition is not application in the
            // grammar, and `(x -> x + 1) 2` came back as a power.
            private protected override string StringizeNode()
                => "apply(" + Expression.Stringize() + ", " +
                    string.Join(", ", Arguments.Select(arg => arg.Stringize())) + ")";

            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Lambda
        {
            /// <inheritdoc/>
            // lambda(p, body), because that is what the parser reads. The arrow spelling is
            // not in the grammar at all, and `->` there is the implication operator, so a
            // printed lambda used to come back as an implication.
            private protected override string StringizeNode()
                => "lambda(" + Parameter.Stringize() + ", " + Body.Stringize() + ")";

            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }
    }
}
