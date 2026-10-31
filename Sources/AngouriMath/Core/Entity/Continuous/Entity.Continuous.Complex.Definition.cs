//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using PeterO.Numbers;
using HonkSharp.Laziness;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
#pragma warning disable SealedOrAbstract // The only few exceptions: Complex, Real, Rational
            public partial record Complex : Number
#pragma warning restore SealedOrAbstract // AMAnalyzer
            {
                /// <summary>
                /// Constructor does not downcast automatically. Use <see cref="Create(Real, Real)"/> for automatic downcasting
                /// </summary>
                private protected Complex(Real? real, Real? imaginary) =>
                    (this.real, this.imaginary) = (real, imaginary);
                private readonly Real? real;
                private readonly Real? imaginary;

                /// <summary>
                /// Real part of a complex number
                /// </summary>
                public virtual Real RealPart => real ?? Integer.Zero;

                /// <summary>
                /// Imaginary part of a complex number
                /// </summary>
                public Real ImaginaryPart => imaginary ?? Integer.Zero;
                
                /// <summary>
                /// Conjugate of a complex number. Given this = a + ib, Conjugate = a - ib
                /// </summary>
                /// <remarks>
                /// Made under the downcasting setting in force, as any other number is, and so not kept
                /// on the instance: the first to ask decided it for everyone. A division by <c>i</c> with
                /// the downcasting off left <c>0 - 1i</c>, in decimals, on <see cref="ImaginaryOne"/>, and
                /// <c>conj(i)</c> was that from then on, in every thread.
                /// </remarks>
                public Complex Conjugate => Create(RealPart, -ImaginaryPart);

                internal override Priority Priority =>
                    (RealPart, ImaginaryPart) switch
                    {
                        ({ IsZero: false }, { IsZero: false }) or ({ IsZero: true }, { IsNegative: true }) => Priority.Sum,
                        ({ IsZero: true }, Integer(1)) => Priority.Leaf,
                        _ => Priority.Mul
                    };

                /// <summary>
                /// An imaginary one. You can use it to avoid allocations
                /// </summary>
                [ConstantField] public static readonly Complex ImaginaryOne = new Complex(0, 1);

                /// <summary>
                /// An imaginary minus one. You can use it to avoid allocations
                /// </summary>
                [ConstantField] public static readonly Complex MinusImaginaryOne = new Complex(0, -1);

                /// <inheritdoc/>
                protected override bool ThisIsFinite => RealPart.EDecimal.IsFinite && ImaginaryPart.EDecimal.IsFinite;
                /// <inheritdoc/>
                public override bool IsExact => RealPart.IsExact && ImaginaryPart.IsExact;

                /// <summary>
                /// Checks if both parts equal 0
                /// </summary>
                /// <remarks>
                /// A rational part is zero when it is, and not when its decimal is: written out in the
                /// precision context, every rational under 1e-199 in size is 0, and 0 / 10^(-250) was NaN.
                /// <a href="https://github.com/asc-community/AngouriMath/issues/2277">#2277</a>
                /// </remarks>
                public new bool IsZero => PartIsZero(RealPart) && PartIsZero(ImaginaryPart);

                private static bool PartIsZero(Real part) =>
                    part is Rational rational ? rational.ERational.IsZero : part.EDecimal.IsZero;

                /// <summary>
                /// Creates an instance of Complex
                /// </summary>
                public static Complex Create(Real real, Real imaginary) =>
                    Create(real.EDecimal, imaginary.EDecimal);

                /// <summary>
                /// Creates an instance of Complex
                /// </summary>
                public static Complex Create(EDecimal real, EDecimal imaginary) => Create(real, imaginary, EDecimal.One, EDecimal.One, fromMachineNumbers: false);

                /// <summary>
                /// A number an arithmetic operation produced, each part zero only as a residual of the cancellation that
                /// produced it, as <see cref="Real.CreateAtScale(EDecimal, EDecimal)"/> reads one.
                /// https://github.com/asc-community/AngouriMath/issues/1944
                /// </summary>
                internal static Complex CreateAtScales(EDecimal real, EDecimal imaginary, EDecimal realScale, EDecimal imaginaryScale)
                    => Create(real, imaginary, realScale, imaginaryScale, fromMachineNumbers: false);

                /// <summary>
                /// A number from a pair of <see cref="double"/>s, each part rounded as
                /// <see cref="Real.FromMachineNumber(EDecimal)"/> rounds it.
                /// </summary>
                internal static Complex FromMachineNumbers(EDecimal real, EDecimal imaginary) => Create(real, imaginary, EDecimal.One, EDecimal.One, fromMachineNumbers: true);

                private static Complex Create(EDecimal real, EDecimal imaginary, EDecimal realScale, EDecimal imaginaryScale, bool fromMachineNumbers)
                {
                    if (!MathS.Settings.DowncastingEnabled)
                        return new Complex(real, imaginary);
                    Real Part(EDecimal part, EDecimal scale) => fromMachineNumbers ? Real.FromMachineNumber(part) : Real.CreateAtScale(part, scale);
                    if (imaginary.IsZero)
                        return Part(real, realScale);
                    if (real.IsNaN() || imaginary.IsNaN())
                        return Real.NaN;
                    if (imaginary.IsFinite && !imaginaryScale.IsZero && imaginary.Abs().LessThan(
                        ReferenceEquals(imaginaryScale, EDecimal.One) ? MathS.Settings.DowncastingTolerance : MathS.Settings.DowncastingTolerance.Multiply(imaginaryScale.Abs())))
                        return Part(real, realScale);
                    else
                        return new Complex(Part(real, realScale), Part(imaginary, imaginaryScale));
                }

                /// <summary>
                /// Deconstructs as record
                /// </summary>
                public void Deconstruct(out Real realPart, out Real imaginaryPart) =>
                    (realPart, imaginaryPart) = (RealPart, ImaginaryPart);

                /// <summary>The magnitude of this <see cref="Complex"/>.</summary>
                /// <returns>
                /// Returns the absolute value of this complex number, to be precise,
                /// if this = a + ib, this.Abs() -> sqrt(a^2 + b^2)
                /// </returns>
                /// <remarks>
                /// Made under the downcasting setting in force, as any other number is. It turned the
                /// downcasting on to answer a <see cref="Real"/>, so with it off a modulus within its
                /// tolerance of a small rational came back as that rational: the modulus of
                /// <c>75 + 316.22776601683796i</c>, <c>325 + 2.6e-17</c>, was 325.
                /// https://github.com/asc-community/AngouriMath/issues/1615
                /// </remarks>
                public new virtual Real Abs()
                    // A modulus is never a residual of cancellation: the sum of two squares cancels nothing.
                    // https://github.com/asc-community/AngouriMath/issues/1944
                    => Real.CreateAtScale((RealPart.EDecimal * RealPart.EDecimal + ImaginaryPart.EDecimal * ImaginaryPart.EDecimal)
                        .SqrtByIntegerRoot(MathS.Settings.DecimalPrecisionContext), EDecimal.Zero);

                /// <summary>
                /// The phase of a complex number (aka angle)
                /// </summary>
                public Real Phase() => ImaginaryPart.EDecimal.Arctan2(RealPart.EDecimal, MathS.Settings.DecimalPrecisionContext);

                /// <summary>
                /// Creates a normal complex from its polar representation
                /// </summary>
                public static Complex CreatePolar(EDecimal magnitude, EDecimal phase)
                {
                    var context = MathS.Settings.DecimalPrecisionContext;
                    // The residual of a cosine or a sine is the magnitude's: cos(pi/2) leaves 1e-100 of it.
                    var scale = magnitude.Abs();
                    return CreateAtScales(magnitude.Multiply(phase.Cos(context), context), magnitude.Multiply(phase.Sin(context), context), scale, scale);
                }

                /// <summary>-oo + -ooi</summary>
                [ConstantField] public static readonly Complex NegNegInfinity =
                    new Complex(Real.NegativeInfinity, Real.NegativeInfinity);

                /// <summary>-oo + +ooi</summary>
                [ConstantField] public static readonly Complex NegPosInfinity =
                    new Complex(Real.NegativeInfinity, Real.PositiveInfinity);

                /// <summary>+oo + -ooi</summary>
                [ConstantField] public static readonly Complex PosNegInfinity =
                    new Complex(Real.PositiveInfinity, Real.NegativeInfinity);

                /// <summary>+oo + +ooi</summary>
                [ConstantField] public static readonly Complex PosPosInfinity =
                    new Complex(Real.PositiveInfinity, Real.PositiveInfinity);

                /// <summary>Parses a <see cref="string"/> into <see cref="Complex"/></summary>
                /// <returns><see cref="Complex"/></returns>
                /// <exception cref="ParseException">Thrown when <paramref name="source"/> cannot be parsed.</exception>
                public static Complex Parse(string source) =>
                    TryParse(source, out var res) ? res : throw new CannotParseInstanceException(typeof(Complex), source);

                /// <summary>Tries to parse a <see cref="Complex"/> from <see cref="string"/></summary>
                /// <param name="source"></param>
                /// <param name="dst">
                /// The result will be written to this variable only if parsing was successful,
                /// if it was not, do not access this variable
                /// </param>
                /// <returns>If parsing was successful - <see langword="true"/>, <see langword="false"/> otherwise</returns>
                public static bool TryParse(string source,
                    [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Complex? dst)
                {
                    dst = null;
                    if (string.IsNullOrEmpty(source))
                        return false;
                    if (Real.TryParse(source, out var real))
                    {
                        dst = real;
                        return true;
                    }
                    if (source.Last() == 'i')
                    {
                        if (source == "i")
                        {
                            dst = ImaginaryOne;
                            return true;
                        }
                        else if (source == "-i")
                        {
                            dst = MinusImaginaryOne;
                            return true;
                        }
                        else if (Real.TryParse(source.Substring(0, source.Length - 1), out var realPart))
                        {
                            dst = new Complex(0, realPart);
                            return true;
                        }
                        else
                            return false;
                    }
                    return false;
                }

                /// <summary>
                /// Converts the Complex to its of the system module Numerics
                /// </summary>
                public System.Numerics.Complex ToNumerics() =>
                    new System.Numerics.Complex(RealPart.EDecimal.ToDouble(), ImaginaryPart.EDecimal.ToDouble());

                /// <summary>
                /// Narrows to <see cref="System.Numerics.Complex"/>, which is
                /// <see cref="ToNumerics"/> and loses precision the same way.
                /// </summary>
                public static explicit operator System.Numerics.Complex(Complex it)
                    => it.ToNumerics();

                /// <summary>Their sum.</summary>
                public static Complex operator +(Complex a, Complex b) => OpSum(a, b);

                /// <summary>Their difference.</summary>
                public static Complex operator -(Complex a, Complex b) => OpSub(a, b);

                /// <summary>Their product.</summary>
                public static Complex operator *(Complex a, Complex b) => OpMul(a, b);

                /// <summary>Their quotient.</summary>
                public static Complex operator /(Complex a, Complex b) => OpDiv(a, b);

                /// <summary>The operand itself; unary plus changes nothing.</summary>
                public static Complex operator +(Complex a) => a;

                /// <summary>Its negation.</summary>
                public static Complex operator -(Complex a) => OpMul(Integer.MinusOne, a);

                // The conversions below share one surprise, stated once here and referred to
                // rather than repeated: where MathS.Settings.DowncastingEnabled is on, which is
                // the default, an exact whole value arrives as an Integer and an exact ratio as
                // a Rational. The declared type is Complex either way, so the difference shows
                // up in the runtime type and in what the number prints as, not in the signature.

                /// <summary>The number as a <see cref="Complex"/>.</summary>
                public static implicit operator Complex(sbyte value) => (long)value;

                /// <summary>The number as a <see cref="Complex"/>.</summary>
                public static implicit operator Complex(byte value) => (ulong)value;

                /// <summary>The number as a <see cref="Complex"/>.</summary>
                public static implicit operator Complex(short value) => (long)value;

                /// <summary>The number as a <see cref="Complex"/>.</summary>
                public static implicit operator Complex(ushort value) => (ulong)value;

                /// <summary>The number as a <see cref="Complex"/>.</summary>
                public static implicit operator Complex(int value) => (long)value;

                /// <summary>The number as a <see cref="Complex"/>.</summary>
                public static implicit operator Complex(uint value) => (ulong)value;

                /// <summary>
                /// The number as a <see cref="Complex"/> — an <see cref="Integer"/> at runtime
                /// while downcasting is enabled.
                /// </summary>
                public static implicit operator Complex(long value)
                    => MathS.Settings.DowncastingEnabled
                        ? Integer.Create(value)
                        : Create(value, 0);
                /// <summary>
                /// The number as a <see cref="Complex"/> — an <see cref="Integer"/> at runtime
                /// while downcasting is enabled.
                /// </summary>
                public static implicit operator Complex(ulong value)
                    => MathS.Settings.DowncastingEnabled
                        ? Integer.Create(value)
                        : Create(value, 0);

                /// <summary>
                /// The integer as a <see cref="Complex"/> — an <see cref="Integer"/> at runtime
                /// while downcasting is enabled.
                /// </summary>
                public static implicit operator Complex(EInteger value)
                    => MathS.Settings.DowncastingEnabled
                        ? Integer.Create(value)
                        : Create(value, 0);

                /// <summary>
                /// The ratio as a <see cref="Complex"/> — a <see cref="Rational"/> at runtime
                /// while downcasting is enabled, and so still exact.
                /// </summary>
                public static implicit operator Complex(ERational value)
                    => MathS.Settings.DowncastingEnabled
                        ? Rational.Create(value)
                        : Create(value, 0);

                /// <summary>The decimal as a <see cref="Complex"/> with no imaginary part.</summary>
                public static implicit operator Complex(EDecimal value)
                    => Create(value, 0);

                /// <summary>
                /// The value as a <see cref="Complex"/> with no imaginary part. It is read as the
                /// binary <see langword="float"/> it is, so a literal such as <c>0.1f</c> arrives
                /// as the value that literal actually holds and not as one tenth.
                /// </summary>
                public static implicit operator Complex(float value)
                    => FromMachineNumbers(EDecimal.FromSingle(value), EDecimal.Zero);

                /// <summary>
                /// The value as a <see cref="Complex"/> with no imaginary part, read as the
                /// binary <see langword="double"/> it is rather than as the decimal it was
                /// written as.
                /// </summary>
                public static implicit operator Complex(double value)
                    => FromMachineNumbers(EDecimal.FromDouble(value), EDecimal.Zero);

                /// <summary>
                /// The value as a <see cref="Complex"/> with no imaginary part. A
                /// <see langword="decimal"/> is decimal already, so this one keeps the digits
                /// that were written.
                /// </summary>
                public static implicit operator Complex(decimal value)
                    => FromMachineNumbers(EDecimal.FromDecimal(value), EDecimal.Zero);

                /// <summary>
                /// The .NET complex number as this one, through its two
                /// <see langword="double"/> parts and their precision.
                /// </summary>
                public static implicit operator Complex(System.Numerics.Complex value) =>
                    FromMachineNumbers(EDecimal.FromDouble(value.Real), EDecimal.FromDouble(value.Imaginary));

                /// <summary>The pair read as real and imaginary parts.</summary>
                public static implicit operator Complex((int re, int im) v) => Create(v.re, v.im);

                /// <summary>The pair read as real and imaginary parts.</summary>
                public static implicit operator Complex((float re, float im) v) => Create(v.re, v.im);

                /// <summary>The pair read as real and imaginary parts.</summary>
                public static implicit operator Complex((decimal re, decimal im) v) => Create(v.re, v.im);

                /// <summary>The pair read as real and imaginary parts.</summary>
                public static implicit operator Complex((double re, double im) v) => Create(v.re, v.im);
            }
        }
    }
}
