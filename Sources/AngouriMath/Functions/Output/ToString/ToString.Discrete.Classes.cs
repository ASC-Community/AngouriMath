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
        partial record Boolean
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => ((bool)this).ToString();
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Notf
        {
            /// <inheritdoc/>
            // Under MathS.Settings.UnicodeOutput the negation of an equality is a ≠, printed here as
            // the <> it is read as, with the brackets an equality's own operands take.
            // https://github.com/asc-community/AngouriMath/issues/1242
            private protected override string StringizeNode()
                => SpelledForUnicode && Argument is Equalsf { PrintsItsCodomain: false } equality
                ? $"{equality.Left.Stringize(equality.Left.Priority <= equality.Priority)} <> {equality.Right.Stringize(equality.Right.Priority <= equality.Priority)}"
                : $"not {Argument.Stringize(Argument.Priority < Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Andf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority < Priority)} and {Right.Stringize(Right.Priority < Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Orf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority < Priority)} or {Right.Stringize(Right.Priority < Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Xorf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority < Priority)} xor {Right.Stringize(Right.Priority < Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Impliesf
        {
            /// <inheritdoc/>
            // The grammar folds `implies` to the left, like everything but `^` and `provided`, so
            // it is the *conclusion* that needs bracketing when it is an implication of its own.
            // The two are not interchangeable: `false implies (true implies false)` is true, and
            // `(false implies true) implies false` is false, so printing the first without its
            // brackets said the opposite of what it meant.
            private protected override string StringizeNode()
                => $"{Assumption.Stringize(Assumption.Priority < Priority)} implies {Conclusion.Stringize(Conclusion.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Dividesf
        {
            /// <inheritdoc/>
            // Folded to the left like `in`, and not associative: `(a divides b) divides c` asks
            // whether a truth value divides c.
            private protected override string StringizeNode()
                => $"{Divisor.Stringize(Divisor.Priority < Priority)} divides {Dividend.Stringize(Dividend.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Quantifier
        {
            /// <inheritdoc/>
            // The body runs to the end of the line, so it is never bracketed; the set is, where
            // it is not something `in` reads.
            private protected override string StringizeNode()
                => $"{Keyword} {Var.Stringize()} in {Over.Stringize(Over.Priority < Priority.SetOperation)} : {Body.Stringize()}";
        }

        // A record synthesizes ToString for itself unless it declares one, whatever its base
        // declares, so each of the three says it prints as its Stringize.
        partial record Forallf
        {
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Existsf
        {
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record ExistsUniquef
        {
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Congruentf
        {
            /// <inheritdoc/>
            // With the modulus once at the end, and `≡` under MathS.Settings.UnicodeOutput
            // (https://github.com/asc-community/AngouriMath/issues/1242). Not chained: two
            // congruences print as the conjunction they are.
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority <= Priority)} {(SpelledForUnicode ? "≡" : "=")} {Right.Stringize(Right.Priority <= Priority)} (mod {Modulus.Stringize(Modulus.Priority < Priority.SetOperation)})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Equalsf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority <= Priority)} = {Right.Stringize(Right.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Greaterf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority <= Priority)} > {Right.Stringize(Right.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record GreaterOrEqualf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority <= Priority)} >= {Right.Stringize(Right.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Lessf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority <= Priority)} < {Right.Stringize(Right.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record LessOrEqualf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => $"{Left.Stringize(Left.Priority <= Priority)} <= {Right.Stringize(Right.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }
    }
}
