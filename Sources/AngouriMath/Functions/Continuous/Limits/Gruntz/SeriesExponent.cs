//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra
{
    /// <summary>
    /// An exponent of <c>w</c> in an <see cref="AsymptoticSeries"/>: a rational number plus
    /// rational multiples of irrational real constants.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Gruntz's algorithm writes every member of the mrv set as a power of one of them, and
    /// that power is whatever constant the ratio of their logarithms tends to. Usually it is
    /// rational. It is not for <c>2^x</c> and <c>3^x</c>: written in <c>w = 2^(-x)</c>,
    /// <c>3^x</c> is <c>w^(-ln(3)/ln(2))</c>. Gruntz's thesis allows any real constant there.
    /// With rational exponents only, the series could not be built, and limits such as
    /// <c>(3^x + 2^x)^(1/x)</c> went unanswered.
    /// https://github.com/asc-community/AngouriMath/issues/2597
    /// </para>
    /// <para>
    /// Exponents that come out of the same constants are added and compared term by term, so
    /// two terms at the same power merge exactly and can cancel. Two exponents written over
    /// different constants are compared by value. Constants can depend on each other
    /// rationally (<c>ln(6)/ln(2)</c> is <c>1 + ln(3)/ln(2)</c>). Then two exponents can be
    /// equal while written differently, and their terms would not merge. A comparison whose
    /// values cannot be told apart therefore sets <see cref="Undecided"/>, and the caller
    /// declines rather than read a leading term that might have cancelled.
    /// </para>
    /// </remarks>
    internal sealed class SeriesExponent : IComparable<SeriesExponent>, IEquatable<SeriesExponent>
    {
        /// <summary>
        /// Set when two exponents with different forms could not be told apart by value. The
        /// series built while it was set is not to be read.
        /// </summary>
        [ThreadStatic] internal static bool Undecided;

        [ConstantField] private static readonly ERational MinusOne = ERational.FromInt32(-1);
        [ConstantField] private static readonly EContext Context = EContext.ForPrecision(60);
        [ConstantField] private static readonly EDecimal Resolution = EDecimal.FromString("1E-40");

        internal ERational Rational { get; }

        /// <summary>Each irrational constant with its rational multiple, never zero.</summary>
        private readonly Dictionary<Entity, (ERational Multiple, EDecimal Value)> irrational;

        private readonly EDecimal value;

        private SeriesExponent(ERational rational, Dictionary<Entity, (ERational Multiple, EDecimal Value)> irrational)
        {
            Rational = rational;
            this.irrational = irrational;
            var sum = rational.ToEDecimal(Context);
            foreach (var (multiple, constant) in irrational.Values)
                sum = sum.Add(multiple.ToEDecimal(Context).Multiply(constant, Context), Context);
            value = sum;
        }

        [ConstantField] private static readonly Dictionary<Entity, (ERational, EDecimal)> None = new();

        internal static SeriesExponent Of(ERational rational) => new(rational, None);

        internal static SeriesExponent Of(int rational) => Of(ERational.FromInt32(rational));

        [ConstantField] internal static readonly SeriesExponent Zero = Of(0);

        [ConstantField] internal static readonly SeriesExponent One = Of(1);

        /// <summary>
        /// The exponent <paramref name="constant"/>, a real constant: rational, or an
        /// irrational one, optionally negated. Anything else is <see langword="null"/>.
        /// </summary>
        internal static SeriesExponent? Of(Entity constant)
        {
            switch (constant.Evaled)
            {
                case Number.Rational exact:
                    return Of(exact.ERational);
                case Real { IsFinite: true, IsNaN: false }:
                    var multiple = ERational.One;
                    if (constant is Mulf(Integer { IsNegative: true } minusOne, var rest) && minusOne == Integer.MinusOne)
                        (multiple, constant) = (MinusOne, rest);
                    if (constant.Evaled is not Real { IsFinite: true, IsNaN: false } basis)
                        return null;
                    return new(ERational.Zero, new() { [constant] = (multiple, basis.EDecimal) });
                default:
                    return null;
            }
        }

        internal bool IsRational => irrational.Count == 0;

        internal Entity ToEntity()
            => irrational.Aggregate((Entity)Number.Rational.Create(Rational),
                static (sum, term) => sum + Number.Rational.Create(term.Value.Multiple) * term.Key);

        internal SeriesExponent Add(SeriesExponent other)
        {
            if (other.IsRational && IsRational)
                return Of(Rational.Add(other.Rational));
            var terms = new Dictionary<Entity, (ERational Multiple, EDecimal Value)>(irrational);
            foreach (var term in other.irrational)
            {
                var (constant, (multiple, basis)) = (term.Key, term.Value);
                var total = terms.TryGetValue(constant, out var already) ? already.Multiple.Add(multiple) : multiple;
                if (total.IsZero) terms.Remove(constant);
                else terms[constant] = (total, basis);
            }
            return new(Rational.Add(other.Rational), terms);
        }

        internal SeriesExponent Scale(ERational factor)
        {
            if (factor.IsZero)
                return Zero;
            if (IsRational)
                return Of(Rational.Multiply(factor));
            var terms = new Dictionary<Entity, (ERational Multiple, EDecimal Value)>();
            foreach (var term in irrational)
                terms[term.Key] = (term.Value.Multiple.Multiply(factor), term.Value.Value);
            return new(Rational.Multiply(factor), terms);
        }

        internal SeriesExponent Negate() => Scale(MinusOne);

        internal SeriesExponent Subtract(SeriesExponent other) => Add(other.Negate());

        /// <summary>
        /// The sign of the exponent, or <see langword="null"/> where its value cannot be told
        /// from zero although its form is not zero.
        /// </summary>
        internal int? Sign => CompareTo(Zero) switch
        {
            _ when Undecided => null,
            var order => order,
        };

        public bool Equals(SeriesExponent? other)
            => other is not null
               && Rational.CompareTo(other.Rational) == 0
               && irrational.Count == other.irrational.Count
               && irrational.All(term => other.irrational.TryGetValue(term.Key, out var theirs)
                                         && theirs.Multiple.CompareTo(term.Value.Multiple) == 0);

        public override bool Equals(object? obj) => obj is SeriesExponent other && Equals(other);

        public override int GetHashCode()
            // Not from the rational part: ERational does not promise one hash for 1/2 and 2/4.
            => irrational.Keys.Aggregate(irrational.Count, static (hash, constant) => hash ^ constant.GetHashCode());

        public int CompareTo(SeriesExponent? other)
        {
            if (other is null)
                return 1;
            if (IsRational && other.IsRational)
                return Rational.CompareTo(other.Rational);
            if (Equals(other))
                return 0;
            var difference = value.Subtract(other.value, Context);
            if (difference.Abs().CompareTo(Resolution) >= 0)
                return difference.Sign;
            Undecided = true;
            // Any order that is the same every time will do: what was built is discarded.
            return string.CompareOrdinal(ToEntity().Stringize(), other.ToEntity().Stringize()) switch
            {
                0 => Rational.CompareTo(other.Rational),
                var order => order,
            };
        }

        public override string ToString() => ToEntity().Stringize();
    }
}
