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
            /// <summary>
            /// Represents a real number, such complex
            /// that its imaginary part equals 0
            /// </summary>
#pragma warning disable SealedOrAbstract // The only few exceptions: Complex, Real, Rational
            public partial record Real : Complex, System.IComparable<Real>
#pragma warning restore SealedOrAbstract // AMAnalyzer
            {
                /// <summary>
                /// Constructor does not downcast automatically. Use <see cref="Create(EDecimal)"/> for automatic downcasting.
                /// </summary>
                private protected Real(EDecimal @decimal) : base(null, null) => storedDecimal = @decimal;

                /// <summary>A rational holds no decimal of its own; it writes one out when asked.</summary>
                private protected Real() : base(null, null) { }

                /// <summary>
                /// The PeterO number representation in <see cref="PeterO.Numbers.EDecimal"/>
                /// </summary>
                public virtual EDecimal EDecimal => storedDecimal!;
                private readonly EDecimal? storedDecimal;

                /// <summary>
                /// Deconstructs as record
                /// </summary>
                public void Deconstruct(out EDecimal @decimal) => @decimal = EDecimal;

                /// <inheritdoc/>
                public override Real RealPart => this;
                internal override Priority Priority => EDecimal.IsNegative ? Priority.Sum : Priority.Leaf;

                /// <inheritdoc/>
                public override bool IsExact => !EDecimal.IsFinite;

                /// <summary>Strictly less than 0</summary>
                /// <remarks>
                /// A rational's sign is its own, and not its decimal's: the decimal is written out in the
                /// precision context, whose exponent range writes every rational under 1e-199 in size as
                /// zero, so 0^(10^(-250)) was NaN.
                /// <a href="https://github.com/asc-community/AngouriMath/issues/2277">#2277</a>
                /// </remarks>
                public bool IsNegative => this is Rational rational ? rational.ERational.Sign < 0 : EDecimal.IsNegative;

                /// <summary>Strictly greater than 0</summary>
                /// <remarks>A rational's sign is its own, as for <see cref="IsNegative"/>.</remarks>
                public bool IsPositive => this is Rational rational ? rational.ERational.Sign > 0 : !EDecimal.IsNegative && !EDecimal.IsZero;

                /// <summary>
                /// Creates an instance of Real
                /// (one can do it by implicit conversation)
                /// </summary>
                /// <remarks>
                /// A value is the small rational it is near within half the working digits, as it is the integer it
                /// is near. Within a fixed 1e-16, <c>ln(e^a + sqrt(2) e^(a/2) + 1)</c> was exactly <c>a</c> at two
                /// thousand digits, where it is <c>a + 1.2e-67</c>, and a derivative built on the difference of two
                /// such logarithms lost its imaginary part.
                /// https://github.com/asc-community/AngouriMath/issues/1944
                /// </remarks>
                public static Real Create(EDecimal value) => Create(value, fromMachineNumber: false, EDecimal.One);

                /// <summary>
                /// A value an arithmetic operation produced, which is a residual of cancellation, and zero, only below
                /// half the working digits of <paramref name="cancelledScale"/>, the size of the terms that cancelled;
                /// zero for a value no cancellation produced -- a product, a quotient or a power of nonzero values --
                /// which is never rounded onto zero. With the floor at half the working digits outright, every value
                /// below 1e-50 was zero at a hundred digits, e^(-155) among them, and a modulus near 1e-136 was zero, so
                /// that a quotient by it was NaN. https://github.com/asc-community/AngouriMath/issues/1944
                /// </summary>
                internal static Real CreateAtScale(EDecimal value, EDecimal cancelledScale)
                    => Create(value, fromMachineNumber: false, cancelledScale);

                /// <summary>
                /// A number from a <see cref="float"/>, a <see cref="double"/> or a <see cref="decimal"/>, which carries
                /// that type's digits and not the working precision's: it is the small rational it is near within
                /// <see cref="MathS.Settings.PrecisionErrorZeroRange"/>, ten to the minus sixteen, so that <c>1m/3</c>
                /// is <c>1/3</c>. https://github.com/asc-community/AngouriMath/issues/1944
                /// </summary>
                internal static Real FromMachineNumber(EDecimal value) => Create(value, fromMachineNumber: true, EDecimal.One);

                // The tolerance is made only past the cheap check below, which almost every value stops at: made at
                // the call, it was 580 more bytes on each evaluation of a cubic at a hundred digits.
                private static Real Create(EDecimal value, bool fromMachineNumber, EDecimal cancelledScale)
                {
                    if (!MathS.Settings.DowncastingEnabled)
                        return new Real(value);

                    if (!value.IsFinite)
                        return new Real(value);
                    // Decided cheaply before the hundred-digit splitting and the search below:
                    // a value that is not near any small rational -- which is what an
                    // evaluation produces at almost every node -- is a Real and nothing else.
                    // https://github.com/asc-community/AngouriMath/issues/1338
                    if (!Rational.MayBeASmallRational(value, MathS.Settings.FloatToRationalIterCount, MathS.Settings.DowncastingToleranceAsDouble))
                        return new Real(value);
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

                    var attempt = Rational.FindRationalWithin(value, fromMachineNumber ? MathS.Settings.PrecisionErrorZeroRange : tolerance);
                    if (attempt is null ||
                        attempt.ERational.Numerator.Abs() > MathS.Settings.MaxAbsNumeratorOrDenominatorValue ||
                        attempt.ERational.Denominator.Abs() > MathS.Settings.MaxAbsNumeratorOrDenominatorValue)
                        return new Real(value);
                    else
                        return attempt;
                }

                /// <inheritdoc/>
                public override Real Abs() => Create(EDecimal.Abs());

                internal static bool TryParse(string s,
                    [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Real? dst)
                {
                    // PeterO.Numbers has no TryParse; FromString throws FormatException for a string
                    // that is not a number, and nothing else is caught.
                    try
                    {
                        dst = EDecimal.FromString(s);
                        return true;
                    }
                    catch (System.FormatException)
                    {
                        dst = null;
                        return false;
                    }
                }

                /// <summary>Negative Infinity (-oo)</summary>
                [ConstantField] public static readonly Real NegativeInfinity = new Real(EDecimal.NegativeInfinity);

                /// <summary>Positive Infinity (+oo)</summary>
                [ConstantField] public static readonly Real PositiveInfinity = new Real(EDecimal.PositiveInfinity);

                /// <summary>Not A Number (NaN)</summary>
                [ConstantField] public static readonly Real NaN = new Real(EDecimal.NaN);

                /// <summary>
                /// Converts the given number to a double (not recommended in general unless you need a built-in type)
                /// </summary>
                public double AsDouble() => EDecimal.ToDouble();

                // Comparison here is on the value and answers a bool, unlike the operators on
                // Entity, which build an inequality node to be solved or evaluated later.
                //
                // The four operators refuse NaN: every one of them is false when either side is
                // NaN, as with double. CompareTo below deliberately does not, because sorting
                // needs a total order and an operator does not. See
                // https://github.com/asc-community/AngouriMath/issues/947 -- these used to order
                // NaN above every number, so `NaN > 1` was true and a guard reading
                // `if (value > threshold)` treated an undefined value as exceeding the threshold,
                // which is the least safe of the two available defaults.

                /// <summary>
                /// Whether the first is strictly greater, and <see langword="false"/> where
                /// either is <see cref="NaN"/>.
                /// </summary>
                /// <remarks>
                /// A comparison against <see cref="NaN"/> is false in both directions, as with
                /// <see langword="double"/>: <c>NaN &gt; x</c> and <c>x &gt; NaN</c> are both
                /// false, and so are the other three operators. "Not a number" is not a position
                /// in the order, so asking where it sits has no true answer to give.
                /// <see cref="CompareTo(Real)"/> answers a different question and still orders it.
                /// </remarks>
                public static bool operator >(Real a, Real b)
                    => !a.IsNaN && !b.IsNaN && a.EDecimal.GreaterThan(b.EDecimal);

                /// <summary>
                /// Whether the first is greater or they are equal, and <see langword="false"/>
                /// where either is <see cref="NaN"/> -- including <c>NaN &gt;= NaN</c>.
                /// </summary>
                public static bool operator >=(Real a, Real b)
                    => !a.IsNaN && !b.IsNaN && a.EDecimal.GreaterThanOrEquals(b.EDecimal);

                /// <summary>
                /// Whether the first is strictly less, and <see langword="false"/> where either
                /// is <see cref="NaN"/>. See <see cref="op_GreaterThan(Real, Real)"/>.
                /// </summary>
                public static bool operator <(Real a, Real b)
                    => !a.IsNaN && !b.IsNaN && a.EDecimal.LessThan(b.EDecimal);

                /// <summary>
                /// Whether the first is less or they are equal, and <see langword="false"/> where
                /// either is <see cref="NaN"/> -- including <c>NaN &lt;= NaN</c>.
                /// </summary>
                public static bool operator <=(Real a, Real b)
                    => !a.IsNaN && !b.IsNaN && a.EDecimal.LessThanOrEquals(b.EDecimal);

                /// <summary>
                /// Negative, zero or positive as this is less than, equal to or greater than
                /// <paramref name="other"/>, which is what sorting wants.
                /// </summary>
                /// <remarks>
                /// <b>This orders <see cref="NaN"/> and the operators do not</b>, which is
                /// deliberate rather than an inconsistency left behind. Sorting requires a total
                /// order -- <see cref="System.Array.Sort(System.Array)"/> may loop or throw
                /// without one -- so <see cref="NaN"/> has to sort somewhere, and it sorts above
                /// every number. "Where does this sort" and "is this greater" are different
                /// questions, and only the second has no answer for a value that is not a number.
                /// See https://github.com/asc-community/AngouriMath/issues/947.
                /// </remarks>
                /// <exception cref="System.ArgumentNullException">
                /// Thrown where <paramref name="other"/> is <see langword="null"/>. Unlike
                /// <see cref="System.IComparable{T}"/>'s usual contract, null does not sort first
                /// here; it is refused.
                /// </exception>
                public int CompareTo(Real? other) => other is null ? throw new System.ArgumentNullException() : EDecimal.CompareTo(other.EDecimal);

                /// <summary>Their sum, computed.</summary>
                public static Real operator +(Real a, Real b) => OpSum(a, b);

                /// <summary>Their difference, computed.</summary>
                public static Real operator -(Real a, Real b) => OpSub(a, b);

                /// <summary>Their product, computed.</summary>
                public static Real operator *(Real a, Real b) => OpMul(a, b);

                /// <summary>
                /// Their quotient, computed. Division by zero gives <see cref="NaN"/> rather
                /// than throwing, so a quotient always has an answer of some kind.
                /// </summary>
                public static Real operator /(Real a, Real b) => OpDiv(a, b).Downcast<Real>();

                /// <summary>The operand itself; unary plus changes nothing.</summary>
                public static Real operator +(Real a) => a;

                /// <summary>Its negation, computed.</summary>
                public static Real operator -(Real a) => OpMul(Integer.MinusOne, a);
                /// <summary>
                /// The floored remainder, which takes the sign of the divisor: -7 % 3 is 2 and
                /// 7 % (-3) is -2. See https://github.com/asc-community/AngouriMath/issues/708.
                /// </summary>
                /// <remarks>
                /// This one used to truncate and so took the sign of the dividend, disagreeing
                /// with the same operator on <see cref="Integer"/> and on
                /// <see cref="Rational"/> -- which one applied depended on the static type at
                /// the call site rather than on the values.
                /// </remarks>
                public static Real operator %(Real a, Real b)
                    => a.EDecimal.Remainder(b.EDecimal, MathS.Settings.DecimalPrecisionContext)
                        .Alias(out var truncated)
                        .IsZero || truncated.IsNegative == b.EDecimal.IsNegative
                        ? truncated
                        : truncated.Add(b.EDecimal, MathS.Settings.DecimalPrecisionContext);
                // With MathS.Settings.DowncastingEnabled, which is the default, a value that is
                // exactly whole arrives as an Integer and one that is exactly a ratio as a
                // Rational. The declared type is Real either way, so the difference is in the
                // runtime type and in how the number prints, not in the signature.

                /// <summary>The number as a <see cref="Real"/>.</summary>
                public static implicit operator Real(sbyte value) => (long)value;

                /// <summary>The number as a <see cref="Real"/>.</summary>
                public static implicit operator Real(byte value) => (ulong)value;

                /// <summary>The number as a <see cref="Real"/>.</summary>
                public static implicit operator Real(short value) => (long)value;

                /// <summary>The number as a <see cref="Real"/>.</summary>
                public static implicit operator Real(ushort value) => (ulong)value;

                /// <summary>The number as a <see cref="Real"/>.</summary>
                public static implicit operator Real(int value) => (long)value;

                /// <summary>The number as a <see cref="Real"/>.</summary>
                public static implicit operator Real(uint value) => (ulong)value;

                /// <summary>
                /// The number as a <see cref="Real"/> — an <see cref="Integer"/> at runtime
                /// while downcasting is enabled.
                /// </summary>
                public static implicit operator Real(long value) => MathS.Settings.DowncastingEnabled ? Integer.Create(value) : new Real(value);

                /// <summary>
                /// The number as a <see cref="Real"/> — an <see cref="Integer"/> at runtime
                /// while downcasting is enabled.
                /// </summary>
                public static implicit operator Real(ulong value) => MathS.Settings.DowncastingEnabled ? Integer.Create(value) : new Real(value);

                /// <summary>
                /// The integer as a <see cref="Real"/> — an <see cref="Integer"/> at runtime
                /// while downcasting is enabled.
                /// </summary>
                public static implicit operator Real(EInteger value) => MathS.Settings.DowncastingEnabled ? Integer.Create(value) : new Real(value);

                /// <summary>
                /// The ratio as a <see cref="Real"/>. While downcasting is enabled it stays a
                /// <see cref="Rational"/> and so stays exact; with it off the ratio is evaluated
                /// to the working precision and a third becomes a finite decimal.
                /// </summary>
                public static implicit operator Real(ERational value) => MathS.Settings.DowncastingEnabled ? Rational.Create(value) : Create(value.ToEDecimal(MathS.Settings.DecimalPrecisionContext));

                /// <summary>The decimal as a <see cref="Real"/>.</summary>
                public static implicit operator Real(EDecimal value) => Create(value);

                /// <summary>
                /// The value as a <see cref="Real"/>, read as the binary <see langword="float"/>
                /// it is — so <c>0.1f</c> becomes the value that literal holds, not one tenth.
                /// </summary>
                public static implicit operator Real(float value) => FromMachineNumber(EDecimal.FromSingle(value));

                /// <summary>
                /// The value as a <see cref="Real"/>, read as the binary
                /// <see langword="double"/> it is rather than as the decimal it was written as.
                /// </summary>
                public static implicit operator Real(double value) => FromMachineNumber(EDecimal.FromDouble(value));

                /// <summary>
                /// The value as a <see cref="Real"/>. A <see langword="decimal"/> is decimal
                /// already, so this one keeps the digits that were written.
                /// </summary>
                public static implicit operator Real(decimal value) => FromMachineNumber(EDecimal.FromDecimal(value));
            }
        }
    }
}
