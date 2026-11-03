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
        partial record Number
        {
            /// <summary>Use <see cref="Create(EInteger)"/> instead of the constructor for consistency with
            /// <see cref="Rational"/>, <see cref="Real"/> and <see cref="Complex"/>.</summary>
            public sealed partial record Integer : Rational, System.IComparable<Integer>
            {
                private Integer(EInteger value) : base(value) => EInteger = value;

                internal override Priority Priority => IsNegative ? Priority.Sum : Priority.Leaf;

                /// <summary>
                /// Represents PeterO number in EInteger
                /// </summary>
                public EInteger EInteger { get; }

                /// <summary>
                /// A zero, you can use it to avoid allocations
                /// </summary>
                [ConstantField] public static readonly Integer Zero = new Integer(EInteger.Zero);

                /// <summary>
                /// A one, you can use it to avoid allocations
                /// </summary>
                [ConstantField] public static readonly Integer One = new Integer(EInteger.One);

                /// <summary>
                /// A minus one, you can use it to avoid allocations
                /// </summary>
                [ConstantField] public static readonly Integer MinusOne = new Integer(-EInteger.One);


                /// <summary>
                /// Creates an instance of Integer
                /// </summary>
                public static Integer Create(int value)
                {
                    if (value == 0)
                        return Zero;
                    if (value == 1)
                        return One;
                    if (value == -1)
                        return MinusOne;
                    return new Integer(value);
                }

                /// <summary>
                /// Creates an instance of Integer
                /// </summary>
                public static Integer Create(EInteger value)
                {
                    if (value.IsZero)
                        return Zero;
                    if (value.Equals(EInteger.One))
                        return One;
                    return new Integer(value);
                }

                /// <summary>
                /// Computes Euler phi function
                /// <a href="https://en.wikipedia.org/wiki/Euler%27s_totient_function"/>
                /// </summary>
                /// If integer x is non-positive, the result will be 0
                public Integer Phi() => EInteger.Phi();

                /// <summary>
                /// Factorization of integer
                /// </summary>
                public IEnumerable<(Integer prime, Integer power)> Factorize() =>
                    EInteger.Factorize().Select(x => ((Integer) x.prime, (Integer) x.power));

                /// <summary>
                /// Count of all divisors of an integer
                /// </summary>
                public Integer CountDivisors() => EInteger.CountDivisors();

                /// <summary>
                /// Detemine whether integer is prime or not.
                /// </summary>
                /// <remarks>
                /// By Miller–Rabin and Lucas–Lehmer, as membership in <c>PP</c> is. It was whether the number had two
                /// divisors, by trial division over <c>long</c>, so <c>2^89 - 1</c> threw <c>OverflowException</c> and the
                /// largest prime below 2^63 did not finish. Where primality is not decided -- past
                /// <c>3317044064679887385961981</c>, for a number that is not a Mersenne number and passes every test --
                /// it throws <see cref="Core.Exceptions.NotSufficientlySupportedException"/>.
                /// https://github.com/asc-community/AngouriMath/issues/2341
                /// </remarks>
                public bool IsPrime => Functions.Primes.IsPrime(EInteger)
                    ?? throw new Core.Exceptions.NotSufficientlySupportedException($"Whether {EInteger} is prime is not decided");

                /// <summary>
                /// Deconstructs as record
                /// </summary>
                public void Deconstruct(out int? value) =>
                    value = EInteger.CanFitInInt32() ? EInteger.ToInt32Unchecked() : new int?();

                /// <inheritdoc/>
                public override Real Abs() => Create(EInteger.Abs());

                /// <summary>
                /// Performs integer division of the
                /// number by the given number
                /// </summary>
                public Integer IntegerDiv(Integer a) => EInteger.Divide(a.EInteger);

                // Comparison here is on the value and answers a bool, where the same operators
                // on Entity build an inequality node instead.

                /// <summary>Whether the first is strictly greater.</summary>
                public static bool operator >(Integer a, Integer b) => a.EInteger.CompareTo(b.EInteger) > 0;

                /// <summary>Whether the first is greater or they are equal.</summary>
                public static bool operator >=(Integer a, Integer b) => a.EInteger.CompareTo(b.EInteger) >= 0;

                /// <summary>Whether the first is strictly less.</summary>
                public static bool operator <(Integer a, Integer b) => a.EInteger.CompareTo(b.EInteger) < 0;

                /// <summary>Whether the first is less or they are equal.</summary>
                public static bool operator <=(Integer a, Integer b) => a.EInteger.CompareTo(b.EInteger) <= 0;

                /// <summary>
                /// Negative, zero or positive as this is less than, equal to or greater than
                /// <paramref name="other"/>, which is what sorting wants.
                /// </summary>
                /// <exception cref="System.ArgumentNullException">
                /// Thrown where <paramref name="other"/> is <see langword="null"/>, rather than
                /// sorting it first as <see cref="System.IComparable{T}"/> usually would.
                /// </exception>
                public int CompareTo(Integer? other) => other is null ? throw new System.ArgumentNullException() : EInteger.CompareTo(other.EInteger);

                /// <summary>Their sum, exactly and at any size.</summary>
                public static Integer operator +(Integer a, Integer b) => OpSum(a, b);

                /// <summary>Their difference, exactly and at any size.</summary>
                public static Integer operator -(Integer a, Integer b) => OpSub(a, b);

                /// <summary>Their product, exactly and at any size.</summary>
                public static Integer operator *(Integer a, Integer b) => OpMul(a, b);

                /// <summary>
                /// Their quotient — <b>not</b> integer division. <c>1 / 2</c> is a half and not
                /// zero, which is why the result is a <see cref="Real"/>: it is a
                /// <see cref="Rational"/> wherever the division is not exact, and
                /// <see cref="Real.NaN"/> where the divisor is zero. For the truncating kind,
                /// see <see cref="IntegerDiv(Integer)"/>.
                /// </summary>
                public static Real operator /(Integer a, Integer b) => (Real)OpDiv(a, b);
                /// <summary>
                /// The floored remainder, which takes the sign of the divisor: -7 % 3 is 2 and
                /// 7 % (-3) is -2. See https://github.com/asc-community/AngouriMath/issues/708.
                /// </summary>
                /// <remarks>
                /// Not <c>EInteger.Mod</c>, which refuses a negative divisor outright and so
                /// made this operator throw on ordinary input.
                /// </remarks>
                public static Integer operator %(Integer a, Integer b)
                    => a.EInteger.Remainder(b.EInteger)
                        .Alias(out var truncated)
                        .IsZero || truncated.Sign == b.EInteger.Sign
                        ? truncated
                        : truncated.Add(b.EInteger);
                /// <summary>The operand itself; unary plus changes nothing.</summary>
                public static Integer operator +(Integer a) => a;

                /// <summary>Its negation.</summary>
                public static Integer operator -(Integer a) => OpMul(MinusOne, a);

                // Nothing to downcast to here, so unlike the conversions on Real and Rational
                // these give exactly what they say.

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(sbyte value) => Create(value);

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(byte value) => Create(value);

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(short value) => Create(value);

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(ushort value) => Create(value);

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(int value) => Create(value);

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(uint value) => Create(value);

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(long value) => Create(value);

                /// <summary>The number as an <see cref="Integer"/>.</summary>
                public static implicit operator Integer(ulong value) => Create(value);

                /// <summary>
                /// The integer as an <see cref="Integer"/>, of any size — this is the conversion
                /// to reach for where a value will not fit in a <see langword="long"/>.
                /// </summary>
                public static implicit operator Integer(EInteger value) => Create(value);

            }
        }
    }
}
