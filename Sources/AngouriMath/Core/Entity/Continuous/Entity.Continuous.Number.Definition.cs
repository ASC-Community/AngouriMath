//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using System;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>Number node.
        /// This class represents all possible numerical values as a hierarchy,
        /// <list>
        ///   <see cref="Number"/>
        ///   <list type="bullet">
        ///     <see cref="Complex"/>
        ///       <list type="bullet">
        ///         <see cref="Real"/>
        ///         <list type="bullet">
        ///           <see cref="Rational"/>
        ///           <list type="bullet">
        ///             <see cref="Integer"/>
        ///           </list>
        ///         </list>
        ///       </list>
        ///     </list>
        ///   </list>
        /// </summary>
        public abstract partial record Number : ContinuousNode
        {
            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func) => func(this);
            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => Array.Empty<Entity>();

            /// <summary>
            /// Checks whether the number is not exposed to implicit rounding
            /// For example, integers and rationals are such
            /// </summary>
            public abstract bool IsExact { get; }
            // Each of these lands on the narrowest kind that holds the value exactly: the
            // integral types on Integer, a ratio on Rational, the rest on Real. Number is
            // abstract, so what comes back is always one of its concrete kinds.

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(sbyte value) => Integer.Create(value);

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(byte value) => Integer.Create(value);

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(short value) => Integer.Create(value);

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(ushort value) => Integer.Create(value);

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(int value) => Integer.Create(value);

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(uint value) => Integer.Create(value);

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(long value) => Integer.Create(value);

            /// <summary>The number as an <see cref="Integer"/>.</summary>
            public static implicit operator Number(ulong value) => Integer.Create(value);

            /// <summary>
            /// The integer as an <see cref="Integer"/>, of any size — this is the conversion to
            /// reach for where a value will not fit in a <see langword="long"/>.
            /// </summary>
            public static implicit operator Number(EInteger value) => Integer.Create(value);

            /// <summary>
            /// The ratio as a <see cref="Rational"/>, in lowest terms and with a positive
            /// denominator — or an <see cref="Integer"/> where the denominator reduces to one.
            /// </summary>
            public static implicit operator Number(ERational value) => Rational.Create(value);

            /// <summary>The decimal as a <see cref="Real"/>.</summary>
            public static implicit operator Number(EDecimal value) => Real.Create(value);

            /// <summary>
            /// The value as a <see cref="Real"/>, read as the binary <see langword="float"/> it
            /// is — so <c>0.1f</c> becomes the value that literal holds, not one tenth.
            /// </summary>
            public static implicit operator Number(float value) => Real.FromMachineNumber(EDecimal.FromSingle(value), value);

            /// <summary>
            /// The value as a <see cref="Real"/>, read as the binary <see langword="double"/> it
            /// is rather than as the decimal it was written as.
            /// </summary>
            public static implicit operator Number(double value) => Real.FromMachineNumber(EDecimal.FromDouble(value), value);

            /// <summary>
            /// The value as a <see cref="Real"/>. A <see langword="decimal"/> is decimal
            /// already, so this one keeps the digits that were written.
            /// </summary>
            public static implicit operator Number(decimal value) => Real.FromMachineNumber(EDecimal.FromDecimal(value));

            /// <summary>
            /// The .NET complex number as a <see cref="Complex"/>, through its two
            /// <see langword="double"/> parts and their precision.
            /// </summary>
            public static implicit operator Number(System.Numerics.Complex value)
                => Complex.FromMachineNumbers(value.Real, value.Imaginary);
            /// <summary>The big integer as an <see cref="Integer"/>, exactly and at any size.</summary>
            /// <remarks>
            /// Read as the two's-complement bytes it is, which is what
            /// <c>BigInteger.ToByteArray</c> returns and what the same conversion on
            /// <see cref="Entity"/> has always done. The overload reached before was
            /// <c>EInteger.FromString(byte[])</c>, which reads a byte array as ASCII digits, so
            /// every value whose bytes are not digit characters threw a <c>FormatException</c> —
            /// which is nearly all of them, 1 included.
            /// </remarks>
            public static implicit operator Number(System.Numerics.BigInteger bigInt)
                => Integer.Create(EInteger.FromBytes(bigInt.ToByteArray(), littleEndian: true));
        }
    }
}
