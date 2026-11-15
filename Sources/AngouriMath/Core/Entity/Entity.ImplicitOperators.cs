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
        // A number written where an expression is expected becomes a literal node of the
        // narrowest kind that holds it exactly: the integral types become Integer, a ratio
        // becomes Rational, and the rest become Real. This is what lets `x + 1` and
        // `MathS.Sin(2)` be written at all.

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(sbyte value) => Number.Integer.Create(value);

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(byte value) => Number.Integer.Create(value);

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(short value) => Number.Integer.Create(value);

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(ushort value) => Number.Integer.Create(value);

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(int value) => Number.Integer.Create(value);

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(uint value) => Number.Integer.Create(value);

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(long value) => Number.Integer.Create(value);

        /// <summary>The number as an <see cref="Number.Integer"/> literal.</summary>
        public static implicit operator Entity(ulong value) => Number.Integer.Create(value);

        /// <summary>
        /// The integer as an <see cref="Number.Integer"/> literal, of any size — this is the
        /// conversion to reach for where a value will not fit in a <see langword="long"/>.
        /// </summary>
        public static implicit operator Entity(EInteger value) => Number.Integer.Create(value);

        /// <summary>The ratio as a <see cref="Number.Rational"/> literal, exactly.</summary>
        public static implicit operator Entity(ERational value) => Number.Rational.Create(value);

        /// <summary>The decimal as a <see cref="Number.Real"/> literal.</summary>
        public static implicit operator Entity(EDecimal value) => Number.Real.Create(value);

        /// <summary>
        /// The value as a <see cref="Number.Real"/> literal, read as the binary
        /// <see langword="float"/> it is — so <c>0.1f</c> becomes the value that literal holds
        /// and not one tenth. Write <c>0.1m</c>, or a <see cref="ERational"/>, for the exact
        /// number.
        /// </summary>
        public static implicit operator Entity(float value) => Number.Real.FromMachineNumber(EDecimal.FromSingle(value), value);

        /// <summary>
        /// The value as a <see cref="Number.Real"/> literal, read as the binary
        /// <see langword="double"/> it is rather than as the decimal it was written as.
        /// </summary>
        public static implicit operator Entity(double value) => Number.Real.FromMachineNumber(EDecimal.FromDouble(value), value);

        /// <summary>
        /// The value as a <see cref="Number.Real"/> literal. A <see langword="decimal"/> is
        /// decimal already, so this one keeps the digits that were written.
        /// </summary>
        public static implicit operator Entity(decimal value) => Number.Real.FromMachineNumber(EDecimal.FromDecimal(value));

        /// <summary>
        /// The .NET complex number as a <see cref="Number.Complex"/> literal, through its two
        /// <see langword="double"/> parts and their precision.
        /// </summary>
        public static implicit operator Entity(System.Numerics.Complex value) =>
            Number.Complex.FromMachineNumbers(value.Real, value.Imaginary);

        /// <summary>The big integer as an <see cref="Number.Integer"/> literal, exactly.</summary>
        public static implicit operator Entity(System.Numerics.BigInteger bigInt)
            => Number.Integer.Create(EInteger.FromBytes(bigInt.ToByteArray(), littleEndian: true));
    }
}
