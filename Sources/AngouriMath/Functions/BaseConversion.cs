//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using System;
using PeterO.Numbers;

namespace AngouriMath.Functions
{
    using static Entity;
    internal static class BaseConversion
    {
        /// <summary>
        /// The digits, to base 36, as the catalogue's <c>to_base</c> and <c>from_base</c> and the MCP server's
        /// <c>am_represent</c> offer: the table stopped at F, so base 17 threw and <c>G</c> could not be read.
        /// https://github.com/asc-community/AngouriMath/issues/2343,
        /// https://github.com/asc-community/AngouriMath/issues/2344
        /// </summary>
        [ConstantField] internal static readonly string ALPHABET_TOCHAR = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>The value of <paramref name="digit"/> in base <paramref name="N"/>, a letter in either case.</summary>
        private static int DigitValue(char digit, int N)
        {
            var value = digit switch
            {
                >= '0' and <= '9' => digit - '0',
                >= 'A' and <= 'Z' => digit - 'A' + 10,
                >= 'a' and <= 'z' => digit - 'a' + 10,
                _ => -1,
            };
            if (value < 0 || value >= N)
                throw new InvalidNumericSystemException($"'{digit}' is not a digit in base {N}");
            return value;
        }

        private static void CheckBase(int N)
        {
            if (N < 2 || N > ALPHABET_TOCHAR.Length)
                throw new InvalidNumericSystemException(
                    $"The base has to be from 2 to {ALPHABET_TOCHAR.Length}, since that is how many digits there are, and {N} was given");
        }

        /// <summary>Transforms an integer to the corresponding base (in string)</summary>
        internal static string IntToBaseN(EInteger num, int N)
        {
            if (num < 0)
                throw new AngouriBugException("Error in IntToBaseN");
            if (num.IsZero)
                return "0";
            var res = new System.Text.StringBuilder();
            while (num > 0)
            {
                res.Append(ALPHABET_TOCHAR[(num % N).ToInt32Checked()]);
                num /= N;
            }
            Reverse(res);
            return res.ToString();
            // https://stackoverflow.com/a/47944113/5429648
            static void Reverse(System.Text.StringBuilder sb)
            {
                char t;
                int end = sb.Length - 1;
                int start = 0;

                while (end - start > 0)
                {
                    t = sb[end];
                    sb[end] = sb[start];
                    sb[start] = t;
                    start++;
                    end--;
                }
            }
        }

        /// <summary>
        /// How many base-<paramref name="N"/> fractional digits carry as much information as
        /// the currently configured decimal precision, i. e. ceil(precision / log10(N)).
        /// Used to bound expansions that never terminate.
        /// </summary>
        private static int MaxFractionalDigits(int N)
        {
            var precision = MathS.Settings.DecimalPrecisionContext.Value.Precision;
            // An unlimited context reports a precision of 0; fall back to the default.
            var digits = precision.IsZero ? 100 : precision.ToInt32Checked();
            return (int)Math.Ceiling(digits / Math.Log10(N));
        }

        /// <summary>Transforms a floating number, but this number should be in [0; 1]</summary>
        /// <remarks>
        /// Multiplying an <see cref="EDecimal"/> by an integer is exact and never grows its
        /// scale, so the digits are produced exactly and the sequence is eventually periodic.
        /// A terminating expansion ends on its own; a repeating one (e. g. 0.125 in base 5,
        /// which is 0.0303...) is cut off at <see cref="MaxFractionalDigits"/> digits.
        /// </remarks>
        internal static string FloatToBaseN(EDecimal num /*should be < 1*/, int N)
        {
            if (num.GreaterThan(EDecimal.One) || num.IsNegative)
                throw new AngouriBugException("Error in FloatToBaseN");
            var res = new System.Text.StringBuilder();
            var maxDigits = MaxFractionalDigits(N);
            while (!num.IsZero && res.Length < maxDigits)
            {
                num = num.Multiply(N);

                EInteger intPart;
                (intPart, num) = num.SplitDecimal();
                res.Append(ALPHABET_TOCHAR[intPart.ToInt32Checked()]);
            }
            return res.ToString();
        }

        /// <summary>
        /// if a number is A + B where A is integer and B is in [0; 1], it performs operations
        /// for A and B separately and then concatenates
        /// </summary>
        internal static string ToBaseN(EDecimal num, int N)
        {
            CheckBase(N);
            string sign = num.IsNegative ? "-" : "";
            num = num.Abs();
            var (intPart, floatPart) = num.SplitDecimal();

            string rightPart = !floatPart.IsZero ? "." + FloatToBaseN(floatPart, N) : "";
            string leftPart = sign + IntToBaseN(intPart, N);

            return leftPart + rightPart;
        }

        /// <summary>
        /// A rational in base <paramref name="N"/>, each digit of the fraction by long division of its numerator by its
        /// denominator: exact, so that <c>1/7</c> in base 7 is <c>0.1</c>, where through its decimal, rounded to the
        /// precision and the rounding multiplied up digit by digit, it ran to 119 digits and ended on a wrong one. A
        /// repeating expansion is cut where <see cref="FloatToBaseN"/> cuts it.
        /// https://github.com/asc-community/AngouriMath/issues/2344
        /// </summary>
        internal static string ToBaseN(ERational num, int N)
        {
            CheckBase(N);
            var sign = num.IsNegative ? "-" : "";
            num = num.Abs();
            var whole = num.Numerator.Divide(num.Denominator);
            var remainder = num.Numerator.Remainder(num.Denominator);
            if (remainder.IsZero)
                return sign + IntToBaseN(whole, N);
            var digits = new System.Text.StringBuilder();
            var maxDigits = MaxFractionalDigits(N);
            while (!remainder.IsZero && digits.Length < maxDigits)
            {
                remainder *= N;
                digits.Append(ALPHABET_TOCHAR[remainder.Divide(num.Denominator).ToInt32Checked()]);
                remainder = remainder.Remainder(num.Denominator);
            }
            return sign + IntToBaseN(whole, N) + "." + digits;
        }

        /// <summary>
        /// The whole number written in <paramref name="num"/>, which holds no sign and no point. By Horner's rule in
        /// <see cref="EInteger"/>: added up in an <c>int</c>, with <c>(int)Math.Pow(N, i)</c> for the place value, a sum
        /// past 2^31 wrapped and a place value past it saturated, so <c>FromBaseN("80000000", 16)</c> was
        /// <c>-2147483648</c>. https://github.com/asc-community/AngouriMath/issues/2343
        /// </summary>
        internal static EInteger IntFromBaseN(string num, int N)
        {
            var res = EInteger.Zero;
            foreach (var digit in num)
                res = res * N + DigitValue(digit, N);
            return res;
        }

        /// <summary>if num is ABC, the initial number was 0.ABC, as the exact quotient of ABC by N^3</summary>
        internal static ERational FloatFromBaseN(string num, int N)
            => ERational.Create(IntFromBaseN(num, N), EInteger.FromInt32(N).Pow(num.Length));

        /// <summary>
        /// Performs operations on both the integer and floating parts of a number and concatenates
        /// </summary>
        internal static EDecimal FromBaseN(string num, int N)
        {
            CheckBase(N);
            if (num.Length == 0 || num == "-")
                throw new InvalidNumericSystemException($"'{num}' has no digits");
            int sign = num[0] == '-' ? -1 : 1;
            num = num[0] == '-' ? num.Substring(1) : num;
            int pos = num.IndexOf('.');
            string leftPart;
            string rightPart;
            if (pos != -1)
            {
                leftPart = num.Substring(0, pos);
                rightPart = num.Substring(pos + 1);
            }
            else
            {
                leftPart = num;
                rightPart = "";
            }
            // Summed exactly and rounded once, into the precision context.
            var value = ERational.FromEInteger(IntFromBaseN(leftPart, N));
            if (rightPart != "")
                value = value.Add(FloatFromBaseN(rightPart, N));
            return (sign < 0 ? value.Negate() : value).ToEDecimal(MathS.Settings.DecimalPrecisionContext);
        }
    }
}
