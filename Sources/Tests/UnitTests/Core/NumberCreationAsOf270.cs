//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// The making of a number from a machine number as it was at 2.7.0 (840075ab2), copied verbatim, so that what
    /// the library makes now can be compared with it value by value. Only what a test cannot reach is changed: a
    /// <see cref="Real"/> that is not downcast is made with the downcasting off, which is what made it then, and
    /// a <see cref="Complex"/> is described by its parts, whose constructor is the library's own.
    /// </summary>
    internal static class NumberCreationAsOf270
    {
        /// <summary>A real number as the downcasting does not touch it.</summary>
        private static Real NewReal(EDecimal value)
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            return Real.Create(value);
        }

        /// <summary><c>Real.FromMachineNumber</c>.</summary>
        internal static Real FromMachineNumber(EDecimal value) => Create(value, fromMachineNumber: true, EDecimal.One);

        /// <summary>
        /// <c>Complex.FromMachineNumbers</c>: the number, or, where it has an imaginary part, the two parts it is
        /// made of.
        /// </summary>
        internal static (Real? Number, Real? RealPart, Real? ImaginaryPart) FromMachineNumbers(EDecimal real, EDecimal imaginary)
        {
            if (!MathS.Settings.DowncastingEnabled)
                return (null, NewReal(real), NewReal(imaginary));
            Real Part(EDecimal part) => FromMachineNumber(part);
            if (imaginary.IsZero)
                return (Part(real), null, null);
            if (real.IsNaN() || imaginary.IsNaN())
                return (Real.NaN, null, null);
            if (imaginary.IsFinite && imaginary.Abs().LessThan(MathS.Settings.DowncastingTolerance))
                return (Part(real), null, null);
            else
                return (null, Part(real), Part(imaginary));
        }

        private static Real Create(EDecimal value, bool fromMachineNumber, EDecimal cancelledScale)
        {
            if (!MathS.Settings.DowncastingEnabled)
                return NewReal(value);

            if (!value.IsFinite)
                return NewReal(value);
            // Decided cheaply before the hundred-digit splitting and the search below:
            // a value that is not near any small rational -- which is what an
            // evaluation produces at almost every node -- is a Real and nothing else.
            // https://github.com/asc-community/AngouriMath/issues/1338
            if (!MayBeASmallRational(value, MathS.Settings.FloatToRationalIterCount, MathS.Settings.DowncastingToleranceAsDouble))
                return NewReal(value);
            var (intPart, intRest) = value.SplitDecimal();
            // If the difference between value & round(value) is zero (see Number.IsZero), we consider value as an integer.
            // Onto zero only as a residual of the cancellation that produced it, below the tolerance times the
            // scale of what cancelled; a negative one reaches zero by rounding up.
            var tolerance = MathS.Settings.DowncastingTolerance;
            bool IsAResidual() => value.IsZero || !cancelledScale.IsZero && value.Abs().LessThan(tolerance.Multiply(cancelledScale.Abs()));
            if (intRest.LessThan(tolerance) && (!intPart.IsZero || IsAResidual()))
                return Integer.Create(intPart);
            if (intRest.GreaterThan(1 - tolerance) && (!intPart.Increment().IsZero || IsAResidual()))
                return Integer.Create(intPart.Increment());

            var attempt = FindRationalWithin(value, fromMachineNumber ? MathS.Settings.PrecisionErrorZeroRange : tolerance);
            if (attempt is null ||
                attempt.ERational.Numerator.Abs() > MathS.Settings.MaxAbsNumeratorOrDenominatorValue ||
                attempt.ERational.Denominator.Abs() > MathS.Settings.MaxAbsNumeratorOrDenominatorValue)
                return NewReal(value);
            else
                return attempt;
        }


        /// <summary>
        /// <see cref="FindRational(EDecimal, int)"/>, accepting a level of the continued fraction whose
        /// remainder is below its integer part times <paramref name="tolerance"/>.
        /// </summary>
        internal static Rational? FindRationalWithin(EDecimal num, EDecimal tolerance, int iterCount = int.MinValue)
        {
            if (iterCount is int.MinValue)
                iterCount = MathS.Settings.FloatToRationalIterCount;
            if (iterCount <= 0)
                return null;
            if (!num.IsFinite)
                return null;
            // The search below is a continued fraction in hundred-digit arithmetic, and
            // it is run on the result of every arithmetic operation on a Real, so that
            // 0.5 comes back as 1/2: twenty microseconds a time, most of the cost of a
            // numerical evaluation (https://github.com/asc-community/AngouriMath/issues/1338).
            // Almost every result is not a small rational, and that is decided first in
            // a double and then a double-double, whose thirty-two digits are enough to
            // say so reliably; only a value the cheap search does not rule out reaches
            // the exact one.
            if (!MayBeASmallRational(num, iterCount))
                return null;
            return FindRationalExactly(num, iterCount, tolerance);
        }

        /// <summary>The continued fraction in the decimal's own arithmetic, level by level.</summary>
        internal static Rational? FindRationalExactly(EDecimal num, int iterCount, EDecimal tolerance)
        {
            if (iterCount <= 0)
                return null;
            var sign = num.Sign;
            num *= sign;
            var (intPart, rest) = num.SplitDecimal();
            if (intPart > MathS.Settings.MaxAbsNumeratorOrDenominatorValue)
                return null;
            // Whether the continued fraction has terminated is a question about the
            // digits of intPart, so the tolerance is relative to it. Number.IsZero
            // compares against PrecisionErrorZeroRange outright, which at the top of
            // the recursion -- where intPart is 0 and rest is the whole number --
            // declared every value below 1e-16 to be the integer 0. That is how
            // 1e-20 came to parse as 0. Below the top, intPart is at least 1 (rest
            // was inverted to get here), so the test stays as permissive as it was.
            if (rest.Abs().LessThan(EDecimal.FromEInteger(intPart)
                    .Multiply(tolerance, MathS.Settings.DecimalPrecisionContext)))
                return Integer.Create(sign * intPart);
            else
            {
                var inv = Entity.Number.CtxDivide(EDecimal.One, rest);
                var rat = FindRationalExactly(inv, iterCount - 1, tolerance);
                if (rat is null)
                    return null;
                // Through Create, so that a whole number is an Integer: a value just below one is 0 + 1/1, and
                // came back as the Rational 1/1, which is not equal to 1.
                // https://github.com/asc-community/AngouriMath/issues/2069
                return Rational.Create(intPart * sign + sign / rat.ERational);
            }
        }

        /// <summary>
        /// Whether <paramref name="num"/> may be within the exact search's tolerance of a
        /// rational with a numerator and a denominator within
        /// <see cref="MathS.Settings.MaxAbsNumeratorOrDenominatorValue"/>, decided by the
        /// same continued fraction in <see cref="double"/> and, where the double cannot
        /// tell, in a double-double. A <see langword="false"/> is reliable; a
        /// <see langword="true"/> is only a reason to run the exact search.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The exact search accepts a level whose remainder is below the integer part
        /// times its tolerance, at most <see cref="MathS.Settings.PrecisionErrorZeroRange"/>,
        /// ten to the minus sixteen, unless a caller has set it higher. In a double the remainder at a level carries an error of the value's
        /// representation error, two to the minus fifty-two, times the square of that
        /// level's convergent denominator; the tolerance here is the exact one widened by
        /// that bound, so a value the exact search would accept is never refused, and
        /// where the bound has grown past a tenth the double can no longer tell. A
        /// double-double -- a pair of doubles, a hundred and six bits -- starts from an
        /// error of ten to the minus twenty-eight, which the square of any denominator
        /// within the bound leaves below ten to the minus twelve, so it decides every
        /// value the double could not. A false yes costs the exact search, which then
        /// refuses; it was what every value paid.
        /// </para>
        /// <para>
        /// Both are read off the mantissa's leading bits and the exponent directly:
        /// PeterO's own conversions to a double or a decimal round the whole
        /// hundred-digit mantissa and cost two to three microseconds, as much as the
        /// arithmetic they were meant to spare. The decimal ran fifteen divisions of
        /// <see cref="decimal"/> where the double-double runs fifteen of a few
        /// floating-point operations each, and a value near a short decimal -- a
        /// polynomial at 0.37000000000001234 is near one at every node -- paid two
        /// microseconds in it.
        /// </para>
        /// </remarks>
        internal static bool MayBeASmallRational(EDecimal num, int iterCount, double integerTolerance = 0)
        {
            var bound = EDecimal.FromEInteger(MathS.Settings.MaxAbsNumeratorOrDenominatorValue.Value).ToDouble();
            // In a double first, which is a third of a microsecond and refuses most
            // values; what it does not refuse, the same search in the double-double
            // the value was read as; and only then the exact one.
            if (!TryReadAsDoubleDouble(num, out var high, out var low))
                return true;
            if (high < 0)
                (high, low) = (-high, -low);
            // Within the caller's tolerance of an integer -- Real.Create's, which
            // rounds a residual of exact cancellation such as sin(pi)'s onto zero.
            var nearestInteger = Math.Round(high);
            if (Math.Abs(high - nearestInteger) <= integerTolerance)
                return true;
            if (!ContinuedFractionMayTerminate(high, iterCount, bound))
                return false;
            // A yes from the double is only a maybe: its tolerance is the exact one
            // widened by its error, and by the twelfth level of a value like sin 1
            // that error is a hundredth, so the remainder there was within it of the
            // next integer about once in a hundred values -- each of which then paid
            // the fifty microseconds of the exact search to be refused. The
            // double-double's error at that level is ten to the minus twelve, and it
            // decides. https://github.com/asc-community/AngouriMath/issues/1338
            return ContinuedFractionMayTerminate(high, low, iterCount, bound);
        }

        /// <summary>
        /// The continued fraction of <paramref name="value"/> in a double: whether it may
        /// reach a level the exact search accepts within <paramref name="iterCount"/>
        /// levels and with every convergent denominator within <paramref name="bound"/>;
        /// a yes as well where the double's error -- its representation error times the
        /// square of a convergent's denominator -- has grown past a tenth before the
        /// question was settled.
        /// </summary>
        private static bool ContinuedFractionMayTerminate(double value, int iterCount, double bound)
        {
            if (value > 1e300)
                return false;
            // The convergents' denominators, k_n = a_n k_(n-1) + k_(n-2) from k_(-2) = 1 and
            // k_(-1) = 0, to stop once they pass the bound; and the absolute error the
            // level's remainder carries, the representation error of the value to begin
            // with, divided by the square of each fractional part inverted on the way
            // (the derivative of 1/f) and one rounding of each division. Checked with
            // this epsilon on twelve thousand rationals within the bound and on as many
            // values within ten to the minus ten of one against the exact search: none
            // refused.
            const double epsilon = 1e-15;   // the reading's error, an ulp, and margin
            double previousDenominator = 1, denominator = 0;
            var rest = value;
            var error = epsilon * value + 1e-300;
            for (var i = 0; i < iterCount; i++)
            {
                var integral = Math.Floor(rest);
                if (integral > bound)
                    return false;
                var fractional = rest - integral;
                var nextDenominator = integral * denominator + previousDenominator;
                if (nextDenominator > bound)
                    return false;
                if (error > 0.1)
                    return true;
                // Within the error of a remainder the exact search accepts, or of the
                // next integer, where the true integral part is one more and the true
                // remainder small.
                if (fractional <= integral * 1e-16 + error || 1 - fractional <= error || (integral == 0 && fractional == 0))
                    return true;
                error = error / (fractional * fractional) + epsilon / fractional;
                (previousDenominator, denominator) = (denominator, nextDenominator);
                rest = 1 / fractional;
            }
            return false;
        }

        /// <summary>
        /// The same in a double-double <paramref name="high"/> + <paramref name="low"/>,
        /// whose error stays below deciding for every denominator within the bound.
        /// </summary>
        private static bool ContinuedFractionMayTerminate(double high, double low, int iterCount, double bound)
        {
            // The reading's relative error -- the mantissa past its hundred and sixth
            // bit, the powers of ten from the table, together a rounding of the
            // hundred and fourth bit for each of the digits of the exponent (see
            // ScalingSteps), and the products' own -- and a division's.
            const double epsilonRead = 1e-28;
            const double epsilonDivide = 1e-31;
            double previousDenominator = 1, denominator = 0;
            var error = epsilonRead * high + 1e-300;
            for (var i = 0; i < iterCount; i++)
            {
                var integral = Math.Floor(high);
                if (integral == high && low < 0)
                    integral -= 1;
                if (integral > bound)
                    return false;
                var (fractional, fractionalLow) = Subtract(high, low, integral);
                var nextDenominator = integral * denominator + previousDenominator;
                if (nextDenominator > bound)
                    return false;
                if (error > 0.1)
                    return true;
                if (fractional <= integral * 1e-16 + error || 1 - fractional <= error || (integral == 0 && fractional == 0))
                    return true;
                error = error / (fractional * fractional) + epsilonDivide / fractional;
                (previousDenominator, denominator) = (denominator, nextDenominator);
                (high, low) = Reciprocal(fractional, fractionalLow);
            }
            return false;
        }

        // Double-double arithmetic: a value as the unevaluated sum of two doubles, the
        // low one within half an ulp of the high one, which is a hundred and six bits.
        // Dekker's error-free transformations, without a fused multiply-add, which
        // netstandard2.0 does not have.

        /// <summary>The sum and its rounding error, exactly.</summary>
        private static (double Sum, double Error) TwoSum(double a, double b)
        {
            var sum = a + b;
            var bVirtual = sum - a;
            var error = (a - (sum - bVirtual)) + (b - bVirtual);
            return (sum, error);
        }

        /// <summary>The sum and its rounding error, where |a| is at least |b|.</summary>
        private static (double Sum, double Error) QuickTwoSum(double a, double b)
        {
            var sum = a + b;
            return (sum, b - (sum - a));
        }

        /// <summary>The product and its rounding error, exactly.</summary>
        private static (double Product, double Error) TwoProduct(double a, double b)
        {
            var product = a * b;
            const double splitter = 134217729.0;   // 2^27 + 1
            var t = splitter * a;
            var aHigh = t - (t - a);
            var aLow = a - aHigh;
            t = splitter * b;
            var bHigh = t - (t - b);
            var bLow = b - bHigh;
            var error = ((aHigh * bHigh - product) + aHigh * bLow + aLow * bHigh) + aLow * bLow;
            return (product, error);
        }

        /// <summary>A double-double minus a double.</summary>
        private static (double High, double Low) Subtract(double high, double low, double subtrahend)
        {
            var (sum, error) = TwoSum(high, -subtrahend);
            return QuickTwoSum(sum, error + low);
        }

        /// <summary>A double-double times a double-double.</summary>
        private static (double High, double Low) Multiply(double aHigh, double aLow, double bHigh, double bLow)
        {
            var (product, error) = TwoProduct(aHigh, bHigh);
            return QuickTwoSum(product, error + aHigh * bLow + aLow * bHigh);
        }

        /// <summary>One over a double-double, by two Newton corrections of the double's quotient.</summary>
        private static (double High, double Low) Reciprocal(double high, double low)
        {
            var quotient = 1 / high;
            // The remainder 1 - quotient * (high + low), in a double-double.
            var (product, productError) = TwoProduct(quotient, high);
            var (remainder, remainderError) = TwoSum(1, -product);
            remainderError -= productError + quotient * low;
            var correction = (remainder + remainderError) / high;
            return QuickTwoSum(quotient, correction);
        }

        /// <summary>
        /// Ten to every power from -<see cref="PowersOfTenRange"/> to
        /// <see cref="PowersOfTenRange"/> as double-doubles, the positive ones by
        /// repeated multiplication -- an error within a rounding of the hundred and
        /// fourth bit each, so under two to the minus ninety-five at the end of the
        /// table -- and the negative ones their reciprocals.
        /// </summary>
        private const int PowersOfTenRange = 512;
        /// <summary>
        /// How many times the table's range the reading covers: a power of ten in the
        /// table carries a rounding of the hundred and sixth bit for each of the
        /// multiplications that built it, so the reading's error grows with the
        /// exponent, and the ten to the minus twenty-eight the search assumes has room
        /// for four thousand digits.
        /// </summary>
        private const int ScalingSteps = 8;
        private static readonly (double[] High, double[] Low) powersOfTen = BuildPowersOfTen();
        private static (double[] High, double[] Low) BuildPowersOfTen()
        {
            var high = new double[2 * PowersOfTenRange + 1];
            var low = new double[2 * PowersOfTenRange + 1];
            double h = 1, l = 0;
            for (var n = 0; n <= PowersOfTenRange; n++)
            {
                high[PowersOfTenRange + n] = h;
                low[PowersOfTenRange + n] = l;
                (high[PowersOfTenRange - n], low[PowersOfTenRange - n]) = n == 0 ? (1d, 0d) : Reciprocal(h, l);
                (h, l) = Multiply(h, l, 10, 0);
            }
            return (high, low);
        }

        /// <summary>
        /// <paramref name="num"/> as a double-double to within ten to the minus
        /// twenty-eight relative, from the leading hundred and six bits of its mantissa
        /// and its exponent, without rounding the whole mantissa; <see langword="false"/>
        /// where the value or its exponent is outside what the reading covers.
        /// </summary>
        private static bool TryReadAsDoubleDouble(EDecimal num, out double high, out double low)
        {
            high = low = 0;
            if (!num.IsFinite)
                return false;
            var exponent = num.Exponent;
            if (!exponent.CanFitInInt32())
                return false;
            var decimalExponent = exponent.ToInt32Unchecked();
            if (decimalExponent > ScalingSteps * PowersOfTenRange || decimalExponent < -ScalingSteps * PowersOfTenRange)
                return false;
            var mantissa = num.UnsignedMantissa;
            if (mantissa.IsZero)
                return true;
            var bits = mantissa.GetUnsignedBitLengthAsInt64();
            // The value's magnitude, within a double's range with room for the four
            // partial products below.
            var magnitude = (bits + 1) * 0.30102999566398120 + decimalExponent;
            if (magnitude > 290 || magnitude < -290)
                return false;
            var shift = bits > 106 ? (int)(bits - 106) : 0;
            var top = shift == 0 ? mantissa : mantissa.ShiftRight(shift);
            var upper = top.ShiftRight(53);
            var lower = top.Subtract(upper.ShiftLeft(53));
            // upper * 2^53 is exact in a double, and the sum with the lower fifty-three
            // bits is exact in the pair.
            (high, low) = TwoSum((double)upper.ToInt64Checked() * 9007199254740992.0, (double)lower.ToInt64Checked());
            // The binary shift and the decimal exponent applied in pieces, in turn,
            // since either alone can be far outside a double's range while the value is
            // not -- a five-hundred-digit mantissa is shifted by sixteen hundred bits.
            // A power of two with a whole exponent is exact, so scaling by it is; a
            // power of ten comes from the table. Two pieces reach five hundred digits;
            // past that, as many as keep each power of ten within the table and each
            // power of two within a double -- two halves of a thousand-digit value's
            // shift were 2^1608 each, an infinity, and every value at a thousand
            // digits went to the exact search.
            // https://github.com/asc-community/AngouriMath/issues/1338
            var pieces = Math.Max(2, Math.Max(
                (Math.Abs(decimalExponent) + PowersOfTenRange - 1) / PowersOfTenRange,
                (shift + 1000 - 1) / 1000));
            var exponentLeft = decimalExponent;
            var shiftLeft = shift;
            for (var piece = pieces; piece > 0; piece--)
            {
                var exponentPiece = exponentLeft / piece;
                var shiftPiece = shiftLeft / piece;
                (high, low) = Multiply(high, low, powersOfTen.High[PowersOfTenRange + exponentPiece], powersOfTen.Low[PowersOfTenRange + exponentPiece]);
                var scale = Math.Pow(2, shiftPiece);
                (high, low) = (high * scale, low * scale);
                exponentLeft -= exponentPiece;
                shiftLeft -= shiftPiece;
            }
            if (num.IsNegative)
                (high, low) = (-high, -low);
            return !double.IsInfinity(high) && !double.IsNaN(high);
        }
    }
}
