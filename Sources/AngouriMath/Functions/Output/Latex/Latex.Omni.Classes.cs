//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Set
        {
            partial record FiniteSet
            {
                /// <inheritdoc/>
                private protected override string LatexizeNode()
                    => IsSetEmpty ? @"\emptyset" : $@"\left\{{ {string.Join(", ", Elements.Select(c => c.Latexize()))} \right\}}";
            }

            partial record Interval
            {
                /// <inheritdoc/>
                // NOTE: Comma is used as the separator following standard mathematical notation (ISO 80000-2).
                // Some regional variants use semicolon, but comma is more universally recognized.
                private protected override string LatexizeNode()
                {
                    var left = LeftClosed ? "[" : "(";
                    var right = RightClosed ? "]" : ")";
                    return @"\left" + left + Left.Latexize() + ", " + Right.Latexize() + @"\right" + right;
                }
            }

            partial record ConditionalSet
            {
                /// <inheritdoc/>
                private protected override string LatexizeNode()
                    => DeclaredMembership is var (set, rest)
                        ? $@"\left\{{ {Var.Latexize()} \in {set.Latexize()} : {rest.Latexize()} \right\}}"
                        : $@"\left\{{ {Var.Latexize()} : {Predicate.Latexize()} \right\}}";
            }

            partial record SpecialSet
            {
                /// <inheritdoc/>
                private protected override string LatexizeNode()
                    => $@"\mathbb{{{Stringize()[0]}}}";

                // MathWorld's spellings, https://mathworld.wolfram.com/N.html: the star and the
                // plus are superscripts on the blackboard Z, so the printed form is the input
                // form with the letter doubled.
                partial record NonNegativeIntegers
                {
                    /// <inheritdoc/>
                    private protected override string LatexizeNode() => @"\mathbb{Z}^{*}";
                }

                partial record PositiveIntegers
                {
                    /// <inheritdoc/>
                    private protected override string LatexizeNode() => @"\mathbb{Z}^{+}";
                }

                partial record Primes
                {
                    /// <inheritdoc/>
                    private protected override string LatexizeNode() => @"\mathbb{P}";
                }
            }

            partial record Unionf
            {
                /// <inheritdoc/>
                // CSharpMath.Evaluation reads \cup and \setminus at one precedence level and folds
                // them to the left, exactly as the grammar here does, so a \setminus on the right
                // needs bracketing or it comes back as the outer operator. Union with union is
                // associative and stays flat.
                private protected override string LatexizeNode()
                    => $@"{Left.Latexize(Left.LatexPriority < LatexPriority)} \cup {Right.Latexize(Right.LatexPriority < LatexPriority || Right is SetMinusf)}";
            }

            partial record Intersectionf
            {
                /// <inheritdoc/>
                private protected override string LatexizeNode()
                    => $@"{Left.Latexize(Left.LatexPriority < LatexPriority)} \cap {Right.Latexize(Right.LatexPriority < LatexPriority)}";
            }

            partial record SetMinusf
            {
                /// <inheritdoc/>
                // Not associative, and sharing a precedence level with union: anything at that
                // level on the right is bracketed, the same rule \frac's text form follows.
                private protected override string LatexizeNode()
                    => $@"{Left.Latexize(Left.LatexPriority < LatexPriority)} \setminus {Right.Latexize(Right.LatexPriority <= LatexPriority)}";
            }

            partial record Inf
            {
                /// <inheritdoc/>
                // \in is left-folded by CSharpMath too, and membership is not associative.
                private protected override string LatexizeNode()
                    => $@"{Element.Latexize(Element.LatexPriority < LatexPriority)} \in {SupSet.Latexize(SupSet.LatexPriority <= LatexPriority)}";
            }

            partial record Subsetf
            {
                /// <inheritdoc/>
                private protected override string LatexizeNode()
                    => $@"{Sub.Latexize(Sub.LatexPriority < LatexPriority)} \subseteq {Super.Latexize(Super.LatexPriority <= LatexPriority)}";
            }

            partial record Powersetf
            {
                /// <inheritdoc/>
                private protected override string LatexizeNode()
                    => $@"\mathcal{{P}}\left({Argument.Latexize()}\right)";
            }

            partial record IndexedSetOperation
            {
                /// <inheritdoc/>
                private protected override string LatexizeNode()
                    => $@"\big{(this is IndexedUnionf ? "cup" : "cap")}_{{{Var.Latexize()} \in {Over.Latexize()}}} {Body.Latexize(Body.LatexPriority < Priority.SetOperation)}";
            }
        }

        partial record Providedf
        {
            /// <inheritdoc/>
            // Right-folding, so the left operand is the one that needs bracketing -- see the
            // remark on Stringize.
            private protected override string LatexizeNode() => $@"{Expression.Latexize(Expression.LatexPriority <= LatexPriority)} \quad \text{{for}} \quad {Predicate.Latexize(Predicate.LatexPriority < LatexPriority)}";
        }

        partial record Piecewise
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => @"\begin{cases}" +
                string.Join(@"\\",
                Cases.Select(c =>
                {
                    if (c.Predicate == Boolean.True)
                        return $@"{c.Expression.Latexize(c.Expression.LatexPriority < Priority.Provided)} & \text{{otherwise}}";
                    return $@"{c.Expression.Latexize(c.Expression.LatexPriority < Priority.Provided)} & \text{{for }} {c.Predicate.Latexize(c.Predicate.LatexPriority <= Priority.Provided)}"; // "for" used in https://mathworld.wolfram.com/Derivative.html
                }
                ))
                +
                @"\end{cases}";
        }

        // det and tr as operators, the transpose as an upright T, as ISO 80000-2 writes them, and an
        // entry as a subscript. https://github.com/asc-community/AngouriMath/issues/2147
        partial record Determinantf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\det\left({Argument.Latexize()}\right)";
        }

        partial record Transposef
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"{{{Argument.Latexize(Argument.Priority < Priority.Func)}}}^{{\mathrm{{T}}}}";
        }

        partial record Tracef
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\operatorname{{tr}}\left({Argument.Latexize()}\right)";
        }

        partial record MatrixEntryf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"{{{Argument.Latexize(Argument.Priority < Priority.Func)}}}_{{{Row.Latexize()},{Column.Latexize()}}}";
        }

        partial record Matrix
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
            {
                if (IsVector)
                {
                    var sb = new StringBuilder();
                    sb.Append(@"\begin{bmatrix}");
                    sb.Append(string.Join(@" \\ ", InnerMatrix.Iterate().Select(k => k.Value.Latexize())));
                    sb.Append(@"\end{bmatrix}");
                    return sb.ToString();
                }
                {
                    var sb = new StringBuilder();
                    sb.Append(@"\begin{bmatrix}");
                    var lines = new List<string>();
                    for (int x = 0; x < RowCount; x++)
                    {
                        var items = new List<string>();

                        for (int y = 0; y < ColumnCount; y++)
                            items.Add(this[x, y].Latexize());

                        var line = string.Join(" & ", items);
                        lines.Add(line);
                    }
                    sb.Append(string.Join(@" \\ ", lines));
                    sb.Append(@"\end{bmatrix}");
                    return sb.ToString();
                }
            }
        }

        partial record Application
        {
            /// <inheritdoc/>
            // NOTE: Application represents curried function application. Multiple arguments
            // are rendered as separate applications: f(x)(y) rather than f(x, y)
            private protected override string LatexizeNode()
            {
                var result = new StringBuilder(Expression.Latexize(Expression.LatexPriority < LatexPriority));
                foreach (var arg in Arguments)
                {
                    result.Append(@"\left(").Append(arg.Latexize()).Append(@"\right)");
                }
                return result.ToString();
            }
        }

        partial record Lambda
        {
            /// <inheritdoc/>
            // NOTE: \mapsto (↦) links the function variable with its output while
            // \rightarrow (→) links the function domain with its codomain. See https://math.stackexchange.com/a/651240, https://math.stackexchange.com/a/936591
            private protected override string LatexizeNode()
                => Parameter.Latexize() + @" \mapsto " + Body.Latexize(Body.LatexPriority < LatexPriority);
        }
    }
}
