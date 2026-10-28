//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        public partial record Sumf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                Augend.Latexize(Augend.LatexPriority < LatexPriority)
                + (Addend.Latexize(Addend.LatexPriority < LatexPriority) is var addend && addend.StartsWith("-")
                    ? addend : "+" + addend);
        }

        public partial record Minusf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                Minuend.Latexize(Minuend.LatexPriority < LatexPriority)
                // Subtracting something written with a leading minus is adding the rest of it:
                // `1/100 - (-4)(b - a)` printed as `\frac{1}{100}--4 \left(b-a\right)`, two
                // minus signs in a row, where the sum above already folds `a + (-b)` to `a-b`.
                + (Subtrahend.Latexize(Subtrahend.LatexPriority <= LatexPriority) is var subtrahend && subtrahend.StartsWith("-")
                    ? "+" + subtrahend.Substring(1) : "-" + subtrahend);
        }

        public partial record Mulf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
            {
                var longArray = GatherProducts(this).ToArray();
                return longArray.AggregateIndexed("",
                    (prevOut, index, currIn) =>
                    {
                        switch (index)
                        {
                            case 0:
                                return currIn switch
                                {
                                    // -1, -2, 2i, i, -i, -2i etc. in the front and not (1+i) etc.
                                    Number { LatexPriority: Priority.Sum } and not Complex { RealPart.IsZero: false, ImaginaryPart.IsZero: false } =>
                                        currIn.Latexize(false),
                                    _ => currIn.Latexize(currIn.LatexPriority < LatexPriority)
                                };
                            case 1:
                                if (longArray[index - 1] is Integer(-1))
                                    return $"-{currIn.Latexize(currIn.LatexPriority < LatexPriority || currIn is Modf)}"; // display "-1 * x * y" as "-x \cdot y", only for the first -1
                                break;
                        }
                        // A factor other than the first that is a `mod` needs brackets: \bmod sits
                        // at this precedence level and is not associative, so "x \cdot y \bmod z"
                        // reads as "(x y) mod z". 2 * (3 mod 2) is 2 and (2 * 3) mod 2 is 0.
                        var currOut = currIn.Latexize(currIn.LatexPriority < LatexPriority || currIn is Modf);

                        return (longArray[index - 1], currIn) switch // whether we use juxtaposition and omit \cdot
                        {
                            // NOTE: upright text are to be interpreted as a whole while italic text are to be interpreted as individual characters.
                            // Therefore, constants formatted as upright text, and multi-character variables are not considered for juxtaposition.

                            // Don't juxtapose upright variables with numbers like displaying "var2" for "var*2" since "var2" may be interpreted as one variable.
                            // Also, don't produce upright "ei" (one variable with two chars) for e*i, or "ei^2" for e*i^2.
                            // but "e (2+i)" and "e (2+i)^2" are fine with the parentheses - so we have the LatexPriority check.
                            (Variable { IsLatexUprightFormatted: true }
                                or Complex { ImaginaryPart.IsZero: false, LatexPriority: >= Priority.Mul } /* don't combine upright "i" with an upright variable*/,
                             Variable { IsLatexUprightFormatted: true } or Number { LatexPriority: >= Priority.Mul }
                                or Factorialf(Number { LatexPriority: Priority.Leaf } or Variable { IsLatexUprightFormatted: true })
                                or DoubleFactorialf(Number { LatexPriority: Priority.Leaf } or Variable { IsLatexUprightFormatted: true })
                                or Powf(Number { LatexPriority: Priority.Leaf } or Variable { IsLatexUprightFormatted: true }
                                        or Factorialf(Number { LatexPriority: Priority.Leaf } or Variable { IsLatexUprightFormatted: true })
                                        or DoubleFactorialf(Number { LatexPriority: Priority.Leaf } or Variable { IsLatexUprightFormatted: true }), _)) => false,
                            // 2 * (3/4) instead of 2 (3/4) which is a mixed number (= 2 + 3/4)
                            (Number { LatexPriority: Priority.Leaf }, { LatexPriority: Priority.Div }) => false,
                            // 2 * 3 instead of 2 3 (= 23), 2 * 3^4 instead of 2 3^4 (= 23^4), but "(2+i) 2", "2 (2+i)" and "2 (2+i)^2" are fine with the parentheses - so we have the LatexPriority check.
                            (_, Number { LatexPriority: >= Priority.Mul } or Factorialf(Number { LatexPriority: Priority.Leaf })
                                or DoubleFactorialf(Number { LatexPriority: Priority.Leaf })
                                or Powf(Number { LatexPriority: Priority.Leaf } or Factorialf(Number { LatexPriority: Priority.Leaf })
                                        or DoubleFactorialf(Number { LatexPriority: Priority.Leaf }), _)) => false, // Keep the \cdot in "f(x) \cdot -2" "f(x) \cdot 2i" "f(x) \cdot -2i"
                            (var left, var right) => left.LatexPriority >= right.LatexPriority &&
                                !(left.LatexPriority == Priority.Div && right.LatexPriority == Priority.Div) // Without \cdot, the fraction lines may appear too closely together.
                        } ? $@"{prevOut} {currOut}" : $@"{prevOut} \cdot {currOut}";
                    });

                static IEnumerable<Entity> GatherProducts(Entity expr)
                    => expr switch
                    {
                        Mulf(var a, var b) => GatherProducts(a).Concat(GatherProducts(b)),
                        var other => [other]
                    };
            }
        }

        public partial record Divf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                @"\frac{" + Dividend.Latexize() + "}{" + Divisor.Latexize() + "}";
        }

        public partial record Modf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                Dividend.Latexize(Dividend.Priority < Priority) + @" \bmod " + Divisor.Latexize(Divisor.Priority <= Priority);
        }

        public partial record Logf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                Base == 10
                ? @"\log\left(" + Antilogarithm.Latexize() + @"\right)"
                : Base == MathS.e
                ? @"\ln\left(" + Antilogarithm.Latexize() + @"\right)"
                : @"\log_{" + Base.Latexize() + @"}\left(" + Antilogarithm.Latexize() + @"\right)";
        }

        public partial record Powf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
            {
                if (Exponent is Rational { ERational: { Numerator: var numerator, Denominator: var denominator } }
                    and not Integer)
                {
                    var str =
                        @"\sqrt" + (denominator.Equals(2) ? "" : "[" + denominator + "]")
                        + "{" + Base.Latexize() + "}";
                    var abs = numerator.Abs();
                    if (!abs.Equals(EInteger.One))
                        str += "^{" + abs + "}";
                    if (numerator < 0)
                        str = @"\frac{1}{" + str + "}";
                    return str;
                }
                else
                {
                    return "{" + Base.Latexize(Base.LatexPriority <= LatexPriority) + "}^{" + Exponent.Latexize() + "}";
                }
            }
        }

        public partial record DoubleFactorialf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                Argument.Latexize(Argument.LatexPriority <= LatexPriority) + "!!";
        }

        public partial record Factorialf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() =>
                Argument.Latexize(Argument.LatexPriority <= LatexPriority) + "!";
        }

        partial record Signumf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{sgn}}\left({Argument.Latexize()}\right)";
        }

        partial record Erff
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{erf}}\left({Argument.Latexize()}\right)";
        }

        partial record Ref
        {
            // Upright, as ISO 80000-2 writes Re z, rather than the fraktur \Re.
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => @"\operatorname{Re}\left(" + Argument.Latexize() + @"\right)";
        }

        partial record Imf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => @"\operatorname{Im}\left(" + Argument.Latexize() + @"\right)";
        }

        partial record Argf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => @"\arg\left(" + Argument.Latexize() + @"\right)";
        }

        partial record Conjugatef
        {
            // The bar over the argument, as every text on complex numbers writes it.
            // https://github.com/asc-community/AngouriMath/issues/2145
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\overline{{{Argument.Latexize()}}}";
        }

        partial record Iversonf
        {
            // The double bracket of Iverson's notation, drawn as two brackets pulled together:
            // a one-element vector prints as a bmatrix and an interval with a comma, so neither
            // is read back as this. It needs no package.
            // https://github.com/asc-community/AngouriMath/issues/1478
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"[\![{Argument.Latexize()}]\!]";
        }

        partial record Alephf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\aleph_{{{Index.Latexize()}}}";
        }

        partial record Erfcf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{erfc}}\left({Argument.Latexize()}\right)";
        }

        partial record Erfif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{erfi}}\left({Argument.Latexize()}\right)";
        }

        partial record Eif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{Ei}}\left({Argument.Latexize()}\right)";
        }

        partial record Lif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{li}}\left({Argument.Latexize()}\right)";
        }

        partial record Polygammaf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => Order is Number.Integer { IsZero: true }
                ? $@"\psi\left({Argument.Latexize()}\right)"
                : $@"\psi^{{({Order.Latexize()})}}\left({Argument.Latexize()}\right)";
        }

        partial record IncompleteGammaf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\Gamma\left({Parameter.Latexize()}, {LowerLimit.Latexize()}\right)";
        }

        partial record Sif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{Si}}\left({Argument.Latexize()}\right)";
        }

        partial record Cif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{Ci}}\left({Argument.Latexize()}\right)";
        }

        partial record Shif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{Shi}}\left({Argument.Latexize()}\right)";
        }

        partial record Chif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\operatorname{{Chi}}\left({Argument.Latexize()}\right)";
        }

        partial record Absf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\left|{Argument.Latexize()}\right|";
        }

        partial record Floorf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\left\lfloor{{{Argument.Latexize()}}}\right\rfloor";
        }

        partial record Ceilf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\left\lceil{{{Argument.Latexize()}}}\right\rceil";
        }

        partial record Roundf
        {
            // The nearest-integer brackets: a floor on the left, a ceiling on the right.
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\left\lfloor{{{Argument.Latexize()}}}\right\rceil";
        }

        partial record Minf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\min\left({Left.Latexize()}, {Right.Latexize()}\right)";
        }

        partial record Maxf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\max\left({Left.Latexize()}, {Right.Latexize()}\right)";
        }

        partial record Gcdf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\gcd\left({Left.Latexize()}, {Right.Latexize()}\right)";
        }

        partial record Lcmf
        {
            /// <inheritdoc/>
            // LaTeX has \gcd and no \lcm, so the standard spelling is the operator name.
            private protected override string LatexizeNode()
                => $@"\operatorname{{lcm}}\left({Left.Latexize()}, {Right.Latexize()}\right)";
        }

        partial record Binomialf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode()
                => $@"\binom{{{Upper.Latexize()}}}{{{Lower.Latexize()}}}";
        }

        partial record Primef
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\operatorname{{prime}}\left({Argument.Latexize()}\right)";
        }

        // A Greek letter that could be a variable is wrapped in \operatorname, as phi is, since
        // mu * (n + 1) draws as \mu \left(n+1\right). https://github.com/asc-community/AngouriMath/issues/2182
        partial record Mobiusf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\operatorname{{\mu}}\left({Argument.Latexize()}\right)";
        }

        partial record PrimeNuf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\operatorname{{\omega}}\left({Argument.Latexize()}\right)";
        }

        partial record PrimeOmegaf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\operatorname{{\Omega}}\left({Argument.Latexize()}\right)";
        }

        partial record PrimePif
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\operatorname{{\pi}}\left({Argument.Latexize()}\right)";
        }

        partial record DivisorSigmaf
        {
            /// <inheritdoc/>
            private protected override string LatexizeNode() => $@"\operatorname{{\sigma}}_{{{Order.Latexize()}}}\left({Argument.Latexize()}\right)";
        }

        partial record Valuationf
        {
            /// <inheritdoc/>
            // The textbook's v_p(n), the prime as a subscript.
            private protected override string LatexizeNode() => $@"v_{{{Prime.Latexize()}}}\left({Argument.Latexize()}\right)";
        }

        partial record Phif
        {
            /// <inheritdoc/>
            // NOTE: \operatorname is used here to distinguish the phi function from variables, consistent with sgn and other functions.
            private protected override string LatexizeNode() => $@"\operatorname{{\varphi}}\left({Argument.Latexize()}\right)";
        }

        partial record Cardf
        {
            /// <inheritdoc/>
            // The prefix `\#`, matching the canonical `#`. Not |S|, whose bars read back as a
            // modulus, and a modulus of a set is not this.
            private protected override string LatexizeNode() => $@"\#{Argument.Latexize(Argument.LatexPriority <= LatexPriority)}";
        }
    }
}
