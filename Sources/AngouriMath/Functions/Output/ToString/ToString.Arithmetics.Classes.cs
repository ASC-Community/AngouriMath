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
        public partial record Sumf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                Augend.Stringize(Augend.Priority < Priority) + " + " + Addend.Stringize(Addend.Priority < Priority);
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Minusf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                Minuend.Stringize(Minuend.Priority < Priority) + " - " + Subtrahend.Stringize(Subtrahend.Priority <= Priority);
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Mulf
        {
            /// <inheritdoc/>
            // Multiplication is associative, so a product or a quotient on the right may stay
            // unbracketed -- but `mod` shares this precedence level and is not associative, so a
            // `mod` on the right must be bracketed or it is re-read as the outer operator:
            // `2 * (3 mod 2)` is 2 and `(2 * 3) mod 2` is 0.
            private protected override string StringizeNode() =>
                (Multiplier is Integer(-1) && !MathS.Diagnostic.OutputExplicit ? "-"
                    : Multiplier.Stringize(Multiplier.Priority < Priority) + " * ")
                + Multiplicand.Stringize(Multiplicand.Priority < Priority || Multiplicand is Modf);
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Divf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                Dividend.Stringize(Dividend.Priority < Priority) + " / " + Divisor.Stringize(Divisor.Priority <= Priority);
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Modf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                Dividend.Stringize(Dividend.Priority < Priority) + " mod " + Divisor.Stringize(Divisor.Priority <= Priority);
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Logf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => Base == MathS.e ?
                $"ln({Antilogarithm.Stringize()})"
                :
                $"log({Base.Stringize()}, {Antilogarithm.Stringize()})";

            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Powf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() =>
                Exponent == 0.5m
                ? "sqrt(" + Base.Stringize() + ")"
                // The base takes <=, the exponent takes <, which is the mirror of the rule the
                // left-associative operators above use: ^ groups to the right, so it is the
                // *left* operand that needs bracketing when it is a power of its own.
                // (2 ^ 3) ^ 2 printed as 2 ^ 3 ^ 2 before, which is 512 where the expression
                // printed is 64.
                : Base.Stringize(Base.Priority <= Priority) + " ^ " + Exponent.Stringize(Exponent.Priority < Priority);
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record CatalanNumberf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"catalannumber({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record HarmonicNumberf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => Order == Integer.One ? $@"harmonicnumber({Argument.Stringize()})" : $@"harmonicnumber({Argument.Stringize()}, {Order.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Phif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"phi({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Primef
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"prime({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Mobiusf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"mobius({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record PrimeNuf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"primenu({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record PrimeOmegaf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"primeomega({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record PrimePif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"primepi({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record DivisorSigmaf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"divisorsigma({Order.Stringize()}, {Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Valuationf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $@"valuation({Argument.Stringize()}, {Prime.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        partial record Cardf
        {
            /// <inheritdoc/>
            // Canonical is the prefix `#`; `card( )` is accepted on input. The argument takes
            // parentheses only where it binds looser than a function call, so `#S`, `#{ 1, 2 }`
            // and `#(A \/ B)` but `#S` inside `#S + 1`.
            private protected override string StringizeNode() => $"#{Argument.Stringize(Argument.Priority <= Priority)}";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Signumf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"sgn({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Erff
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"erf({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Ref
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"Re({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Imf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"Im({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Argf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"arg({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Conjugatef
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"conjugate({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Iversonf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"iverson({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Alephf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"aleph({Index.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Erfcf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"erfc({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Erfif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"erfi({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Eif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"Ei({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Lif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"li({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Polygammaf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode()
                => Order is Number.Integer { IsZero: true }
                ? $"digamma({Argument.Stringize()})"
                : $"polygamma({Order.Stringize()}, {Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record IncompleteGammaf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"gamma({Parameter.Stringize()}, {LowerLimit.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Sif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"Si({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Cif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"Ci({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Shif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"Shi({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Chif
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"Chi({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Absf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"abs({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Floorf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"floor({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Ceilf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"ceil({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Roundf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"round({Argument.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Minf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"min({Left.Stringize()}, {Right.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Maxf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"max({Left.Stringize()}, {Right.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Gcdf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"gcd({Left.Stringize()}, {Right.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Lcmf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"lcm({Left.Stringize()}, {Right.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Binomialf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"binomial({Upper.Stringize()}, {Lower.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record FallingFactorialf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"fallingfactorial({Argument.Stringize()}, {Order.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record RisingFactorialf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => $"risingfactorial({Argument.Stringize()}, {Order.Stringize()})";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record DoubleFactorialf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => Argument.Stringize(Argument.Priority <= Priority) + "!!";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }

        public partial record Factorialf
        {
            /// <inheritdoc/>
            private protected override string StringizeNode() => Argument.Stringize(Argument.Priority <= Priority) + "!";
            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }
    }
}
