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
            internal override string ToSymPy() =>
                Augend.ToSymPy(Augend.Priority < Priority.Sum) + " + " + Addend.ToSymPy(Addend.Priority < Priority.Sum);
        }

        public partial record Minusf
        {
            internal override string ToSymPy() =>
                Minuend.ToSymPy(Minuend.Priority < Priority.Minus) + " - " + Subtrahend.ToSymPy(Subtrahend.Priority <= Priority.Minus);
        }

        public partial record Mulf
        {
            internal override string ToSymPy() =>
                Multiplier.ToSymPy(Multiplier.Priority < Priority.Mul) + " * " + Multiplicand.ToSymPy(Multiplicand.Priority < Priority.Mul);
        }

        /// <summary>
        /// The operand written so that SymPy does the arithmetic rather than Python.
        /// </summary>
        /// <remarks>
        /// Python's <c>/</c> and its <c>**</c> with a negative exponent are float operations on two
        /// integers, so an exact value would leave here inexact: <c>1 / 2</c> is <c>0.5</c> there,
        /// and <c>sympify(0.5)</c> is a <c>Float</c> and not <c>Rational(1, 2)</c> — the exactness
        /// is gone before SymPy sees the expression, and nothing downstream recovers it. Making one
        /// operand a SymPy integer is enough, since its operators sympify the other side.
        /// <para/>
        /// Only a pair of integers needs this. With a symbol anywhere in the shape SymPy's own
        /// operators already take over, and <c>+</c>, <c>-</c>, <c>*</c> and a non-negative
        /// <c>**</c> are exact on Python integers, whose precision is unbounded.
        /// https://github.com/asc-community/AngouriMath/issues/911
        /// </remarks>
        private static string ToSymPyExactly(Entity operand)
            => $"sympy.Integer({operand.ToSymPy()})";

        public partial record Divf
        {
            internal override string ToSymPy() =>
                (Dividend is Number.Integer && Divisor is Number.Integer
                    ? ToSymPyExactly(Dividend)
                    : Dividend.ToSymPy(Dividend.Priority < Priority.Div))
                + " / " + Divisor.ToSymPy(Divisor.Priority <= Priority.Div);
        }

        public partial record Modf
        {
            internal override string ToSymPy() =>
                "sympy.Mod(" + Dividend.ToSymPy() + ", " + Divisor.ToSymPy() + ")";
        }

        public partial record Logf
        {
            internal override string ToSymPy() => "sympy.log(" + Antilogarithm.ToSymPy() + ", " + Base.ToSymPy() + ")";
        }

        public partial record Powf
        {
            internal override string ToSymPy() =>
                Exponent == 0.5m
                ? "sympy.sqrt(" + Base.ToSymPy() + ")"
                : (Base is Number.Integer && Exponent is Number.Integer { IsNegative: true }
                    ? ToSymPyExactly(Base)
                    : Base.ToSymPy(Base.Priority < Priority.Pow))
                  + " ** " + Exponent.ToSymPy(Exponent.Priority < Priority.Pow);
        }

        public partial record Signumf
        {
            internal override string ToSymPy()
                => $@"sympy.sign({Argument.ToSymPy()})";
        }

        public partial record Erff
        {
            internal override string ToSymPy()
                => $@"sympy.erf({Argument.ToSymPy()})";
        }

        public partial record Ref
        {
            internal override string ToSymPy() => $"sympy.re({Argument.ToSymPy()})";
        }

        public partial record Imf
        {
            internal override string ToSymPy() => $"sympy.im({Argument.ToSymPy()})";
        }

        public partial record DoubleFactorialf
        {
            internal override string ToSymPy() => $"sympy.factorial2({Argument.ToSymPy()})";
        }

        public partial record Argf
        {
            internal override string ToSymPy() => $"sympy.arg({Argument.ToSymPy()})";
        }

        public partial record Conjugatef
        {
            internal override string ToSymPy() => $"sympy.conjugate({Argument.ToSymPy()})";
        }

        public partial record Iversonf
        {
            internal override string ToSymPy()
                => $@"sympy.Piecewise((1, {Argument.ToSymPy()}), (0, True))";
        }

        // SymPy has no sizes of sets, so an aleph goes as a symbol of its own.
        public partial record Alephf
        {
            internal override string ToSymPy()
                => $"sympy.Symbol(\"aleph_{Index.Stringize()}\")";
        }

        public partial record Erfcf
        {
            internal override string ToSymPy()
                => $@"sympy.erfc({Argument.ToSymPy()})";
        }

        public partial record Erfif
        {
            internal override string ToSymPy()
                => $@"sympy.erfi({Argument.ToSymPy()})";
        }

        public partial record Eif
        {
            internal override string ToSymPy()
                => $@"sympy.Ei({Argument.ToSymPy()})";
        }

        public partial record Lif
        {
            internal override string ToSymPy()
                => $@"sympy.li({Argument.ToSymPy()})";
        }

        public partial record Polygammaf
        {
            internal override string ToSymPy()
                => Order is Number.Integer { IsZero: true }
                ? $@"sympy.digamma({Argument.ToSymPy()})"
                : $@"sympy.polygamma({Order.ToSymPy()}, {Argument.ToSymPy()})";
        }

        public partial record IncompleteGammaf
        {
            internal override string ToSymPy()
                => $@"sympy.uppergamma({Parameter.ToSymPy()}, {LowerLimit.ToSymPy()})";
        }

        public partial record Sif
        {
            internal override string ToSymPy()
                => $@"sympy.Si({Argument.ToSymPy()})";
        }

        public partial record Cif
        {
            internal override string ToSymPy()
                => $@"sympy.Ci({Argument.ToSymPy()})";
        }

        public partial record Shif
        {
            internal override string ToSymPy()
                => $@"sympy.Shi({Argument.ToSymPy()})";
        }

        public partial record Chif
        {
            internal override string ToSymPy()
                => $@"sympy.Chi({Argument.ToSymPy()})";
        }

        public partial record Absf
        {
            internal override string ToSymPy()
                => $@"sympy.Abs({Argument.ToSymPy()})";
        }

        public partial record Floorf
        {
            internal override string ToSymPy()
                => $@"sympy.floor({Argument.ToSymPy()})";
        }

        public partial record Ceilf
        {
            // SymPy spells it in full; `ceil` is not a name it has.
            internal override string ToSymPy()
                => $@"sympy.ceiling({Argument.ToSymPy()})";
        }

        public partial record Roundf
        {
            /// <remarks>
            /// SymPy has no symbolic round -- `RoundFunction` is an abstract base and
            /// raises, and `.round()` is a method on a concrete number rather than a
            /// function of an expression. So this is built out of what SymPy does have.
            ///
            /// It is deliberately not `sympy.floor(x + 1/2)`, which is the obvious
            /// translation and is wrong at every tie: that sends 1/2 to 1 and 5/2 to 3,
            /// where rounding half to even gives 0 and 2. The correction below is exactly
            /// the tie case -- when the fractional part is a half and the candidate is odd,
            /// step down to the even neighbour -- and was checked against SymPy's own
            /// `Rational.round()` on ties, non-ties and negatives.
            /// </remarks>
            internal override string ToSymPy()
            {
                var arg = Argument.ToSymPy();
                var candidate = $"sympy.floor({arg} + sympy.Rational(1, 2))";
                return $"sympy.Piecewise(({candidate} - 1, "
                     + $"sympy.Eq(sympy.frac({arg}), sympy.Rational(1, 2)) & sympy.Eq(sympy.Mod({candidate}, 2), 1)), "
                     + $"({candidate}, True))";
            }
        }

        public partial record Minf
        {
            internal override string ToSymPy()
                => $@"sympy.Min({Left.ToSymPy()}, {Right.ToSymPy()})";
        }

        public partial record Maxf
        {
            internal override string ToSymPy()
                => $@"sympy.Max({Left.ToSymPy()}, {Right.ToSymPy()})";
        }

        public partial record Gcdf
        {
            internal override string ToSymPy()
                => $@"sympy.gcd({Left.ToSymPy()}, {Right.ToSymPy()})";
        }

        public partial record Lcmf
        {
            internal override string ToSymPy()
                => $@"sympy.lcm({Left.ToSymPy()}, {Right.ToSymPy()})";
        }

        public partial record Binomialf
        {
            internal override string ToSymPy()
                => $@"sympy.binomial({Upper.ToSymPy()}, {Lower.ToSymPy()})";
        }

        public partial record FallingFactorialf
        {
            internal override string ToSymPy() => $"sympy.ff({Argument.ToSymPy()}, {Order.ToSymPy()})";
        }

        public partial record RisingFactorialf
        {
            internal override string ToSymPy() => $"sympy.rf({Argument.ToSymPy()}, {Order.ToSymPy()})";
        }

        public partial record CatalanNumberf
        {
            internal override string ToSymPy() => $"sympy.catalan({Argument.ToSymPy()})";
        }

        public partial record HarmonicNumberf
        {
            internal override string ToSymPy()
                => Order == Integer.One ? $"sympy.harmonic({Argument.ToSymPy()})" : $"sympy.harmonic({Argument.ToSymPy()}, {Order.ToSymPy()})";
        }

        public partial record Phif
        {
            internal override string ToSymPy() => $"sympy.totient({Argument.ToSymPy()})";
        }

        public partial record Primef
        {
            internal override string ToSymPy() => $"sympy.prime({Argument.ToSymPy()})";
        }

        public partial record Mobiusf
        {
            internal override string ToSymPy() => $"sympy.mobius({Argument.ToSymPy()})";
        }

        public partial record PrimeNuf
        {
            internal override string ToSymPy() => $"sympy.primenu({Argument.ToSymPy()})";
        }

        public partial record PrimeOmegaf
        {
            internal override string ToSymPy() => $"sympy.primeomega({Argument.ToSymPy()})";
        }

        public partial record PrimePif
        {
            internal override string ToSymPy() => $"sympy.primepi({Argument.ToSymPy()})";
        }

        public partial record DivisorSigmaf
        {
            // SymPy takes the argument first and the order second.
            internal override string ToSymPy() => $"sympy.divisor_sigma({Argument.ToSymPy()}, {Order.ToSymPy()})";
        }

        public partial record Valuationf
        {
            internal override string ToSymPy() => $"sympy.multiplicity({Prime.ToSymPy()}, {Argument.ToSymPy()})";
        }

        public partial record Cardf
        {
            // SymPy has no cardinality function; a finite set answers len, and an infinite one
            // raises, which is the same refusal this node makes by staying as written.
            internal override string ToSymPy() => $"len({Argument.ToSymPy()})";
        }

        public partial record Factorialf
        {
            internal override string ToSymPy() => "sympy.factorial(" + Argument.ToSymPy() + ")";
        }
    }
}
