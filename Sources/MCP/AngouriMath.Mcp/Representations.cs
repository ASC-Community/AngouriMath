//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Mcp;

/// <summary>
/// How a number is *encoded*, as opposed to what it equals.
///
/// This is squarely adapter work rather than algebra, which is why it lives here: a Q15
/// coefficient, the bits of a double, and a phasor are all the same number wearing different
/// clothes, and the question is always "what does the machine actually hold, and what did
/// that cost me". AngouriMath answers none of these because none of them are computer
/// algebra.
/// </summary>
internal static class Representations
{
    // ------------------------------------------------------------ fixed point

    public sealed record FixedPoint(
        EInteger Raw, Entity Represented, EDecimal Error, EDecimal RelativeError,
        bool Saturated, EInteger Min, EInteger Max, EDecimal Resolution);

    /// <summary>
    /// Quantise to Q-format. `totalBits` is the machine word (16 for a Q15 coefficient in an
    /// int16), `fractionBits` how many of them sit after the binary point.
    ///
    /// Saturation is reported rather than silently wrapped: on most processors a coefficient
    /// that does not fit is a bug you want to hear about at design time, not a value that
    /// quietly becomes its own negative at run time.
    /// </summary>
    public static FixedPoint ToFixed(EDecimal value, int totalBits, int fractionBits, bool signed)
    {
        var scale = EInteger.FromInt32(2).Pow(fractionBits);
        var scaled = value.Multiply(EDecimal.FromEInteger(scale));

        // Round to nearest, ties to even — what a decent toolchain does, and it avoids the
        // upward bias that ties-away introduces across a table of coefficients.
        var raw = scaled.RoundToIntegerNoRoundedFlag(
            EContext.ForRounding(ERounding.HalfEven)).ToEInteger();

        var min = signed ? -EInteger.FromInt32(2).Pow(totalBits - 1) : EInteger.Zero;
        var max = signed
            ? EInteger.FromInt32(2).Pow(totalBits - 1) - EInteger.One
            : EInteger.FromInt32(2).Pow(totalBits) - EInteger.One;

        var saturated = raw < min || raw > max;
        if (raw < min) raw = min;
        if (raw > max) raw = max;

        // The represented value is exact: an integer over a power of two is a rational, so
        // there is no second rounding here.
        Entity represented = MathS.Numbers.CreateRational(raw, scale);

        var back = EDecimal.FromEInteger(raw).Divide(
            EDecimal.FromEInteger(scale), EContext.Decimal128);
        var error = value.Subtract(back);
        var relative = value.IsZero
            ? EDecimal.Zero
            : error.Divide(value, EContext.Decimal128).Abs();

        var resolution = EDecimal.One.Divide(
            EDecimal.FromEInteger(scale), EContext.Decimal128);

        return new FixedPoint(raw, represented, error.Abs(), relative,
            saturated, min, max, resolution);
    }

    public static Entity FromFixed(EInteger raw, int fractionBits) =>
        MathS.Numbers.CreateRational(raw, EInteger.FromInt32(2).Pow(fractionBits));

    // ------------------------------------------------------------ IEEE 754

    public sealed record Ieee754(
        string Bits, string Hex, int Sign, int RawExponent, int UnbiasedExponent,
        string MantissaHex, string ExactValue, EDecimal Error, string Classification);

    /// <summary>
    /// What a float or double actually holds. The `ExactValue` is the point: 0.1 is not 0.1,
    /// and printing the exact decimal a double stores settles arguments that explanations do
    /// not.
    /// </summary>
    public static Ieee754 Decompose(EDecimal value, bool single)
    {
        if (single)
        {
            var f = (float)value.ToDouble();
            var bits = BitConverter.SingleToInt32Bits(f);
            var sign = (bits >> 31) & 1;
            var exponent = (bits >> 23) & 0xFF;
            var mantissa = bits & 0x7FFFFF;

            return Build(
                Convert.ToString((uint)bits, 2).PadLeft(32, '0'),
                $"0x{(uint)bits:X8}", sign, exponent, exponent == 0 ? -126 : exponent - 127,
                $"0x{mantissa:X6}", EDecimal.FromSingle(f), exponent, 0xFF, mantissa, value);
        }
        else
        {
            var d = value.ToDouble();
            var bits = BitConverter.DoubleToInt64Bits(d);
            var sign = (int)((bits >> 63) & 1);
            var exponent = (int)((bits >> 52) & 0x7FF);
            var mantissa = bits & 0xFFFFFFFFFFFFFL;

            return Build(
                Convert.ToString((long)bits, 2).PadLeft(64, '0'),
                $"0x{(ulong)bits:X16}", sign, exponent, exponent == 0 ? -1022 : exponent - 1023,
                $"0x{mantissa:X13}", EDecimal.FromDouble(d), exponent, 0x7FF, mantissa, value);
        }

        static Ieee754 Build(
            string binary, string hex, int sign, int rawExponent, int unbiased,
            string mantissaHex, EDecimal exact, int exponentField, int exponentMax,
            long mantissa, EDecimal original)
        {
            var classification =
                exponentField == exponentMax
                    ? (mantissa == 0 ? "infinity" : "NaN")
                    : exponentField == 0
                        ? (mantissa == 0 ? "zero" : "subnormal — reduced precision")
                        : "normal";

            var error = classification is "normal" or "subnormal — reduced precision"
                ? exact.Subtract(original).Abs()
                : EDecimal.Zero;

            return new Ieee754(binary, hex, sign, rawExponent, unbiased, mantissaHex,
                exact.ToString(), error, classification);
        }
    }

    // ------------------------------------------------------------ polar form

    public sealed record Polar(Entity Magnitude, Entity Phase, Entity PhaseDegrees);

    /// <summary>
    /// Rectangular to polar, kept symbolic where it can be: 1+i gives sqrt(2) and pi/4, not
    /// 1.414 and 0.785.
    /// </summary>
    public static Polar ToPolar(Entity real, Entity imaginary)
    {
        var magnitude = MathS.Sqrt(real * real + imaginary * imaginary).Simplify();

        // arctan alone loses the quadrant, so the sign of the real part is folded back in.
        // Without this, -1-i and 1+i report the same phase.
        var phase = MathS.Arctan(imaginary / real);
        var adjusted = Numeric.AsNumber(real) is { } r && r < 0
            ? (Numeric.AsNumber(imaginary) is { } im && im < 0 ? phase - MathS.pi : phase + MathS.pi)
            : phase;

        var simplified = adjusted.Simplify();
        return new Polar(magnitude, simplified, (simplified * 180 / MathS.pi).Simplify());
    }

    public static Entity FromPolar(Entity magnitude, Entity phase) =>
        (magnitude * MathS.Cos(phase) + magnitude * MathS.Sin(phase) * MathS.i).Simplify();
}
