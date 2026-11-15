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
        partial record Number
        {
            partial record Complex
            {
                internal override string ToSymPy()
                {
                    if (ImaginaryPart == 0)
                        return RealPart.ToSymPy();
                    if (RealPart == 0)
                        return $"{ImaginaryPart.ToSymPy()} * sympy.I";
                    return $"{RealPart.ToSymPy()} + {ImaginaryPart.ToSymPy()} * sympy.I";
                }
            }

            partial record Real
            {
                /// <summary>
                /// A finite real prints as its decimal, which Python reads as a float. The three
                /// non-finite ones have no such reading: this library spells them <c>NaN</c>,
                /// <c>+oo</c> and <c>-oo</c>, and <c>ToSympyCode</c>'s preamble binds a name only
                /// for each free <em>variable</em>, so they arrived in the generated program as
                /// bare names and it stopped with <c>NameError: name 'NaN' is not defined</c>.
                /// SymPy's own spellings are used instead.
                /// https://github.com/asc-community/AngouriMath/issues/909
                /// </summary>
                internal override string ToSymPy()
                    => this switch
                    {
                        { IsFinite: true } => StringizeAscii(),
                        { IsNaN: true } => "sympy.nan",
                        { IsNegative: true } => "-sympy.oo",
                        _ => "sympy.oo",
                    };
            }

            partial record Rational
            {
                // The closing parenthesis was missing, so every expression carrying a non-integer
                // rational -- which is most of what a CAS hands back -- emitted Python that would
                // not even parse: `sympy.Rational(1, 2` is `SyntaxError: '(' was never closed`.
                // Nothing caught it because nothing runs the generated code.
                // https://github.com/asc-community/AngouriMath/issues/909
                internal override string ToSymPy()
                    => $"sympy.Rational({Numerator.ToSymPy()}, {Denominator.ToSymPy()})";
            }

            partial record Integer
            {
                internal override string ToSymPy()
                    => StringizeAscii();
            }
        }
    }
}
