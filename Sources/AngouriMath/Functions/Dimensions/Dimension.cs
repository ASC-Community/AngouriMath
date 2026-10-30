//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;

namespace AngouriMath.Core
{
    /// <summary>
    /// One of the seven base dimensions of the SI, in the order the SI brochure lists them.
    /// </summary>
    public enum BaseDimension
    {
        /// <summary>L, measured in metres.</summary>
        Length,
        /// <summary>M, measured in kilograms.</summary>
        Mass,
        /// <summary>T, measured in seconds.</summary>
        Time,
        /// <summary>I, measured in amperes.</summary>
        Current,
        /// <summary>Θ, measured in kelvins.</summary>
        Temperature,
        /// <summary>N, measured in moles.</summary>
        Amount,
        /// <summary>J, measured in candelas.</summary>
        LuminousIntensity,
    }

    /// <summary>
    /// A physical dimension: a product of powers of the seven <see cref="BaseDimension"/>s,
    /// such as <c>L T^-2</c> for an acceleration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The exponents are rationals, not integers, because a square root of a dimensional
    /// quantity is a legitimate intermediate: <c>sqrt(L)</c> appears inside the period of a
    /// pendulum before it is divided by <c>sqrt(L T^-2)</c>.
    /// </para>
    /// <para>
    /// A dimension says what kind of quantity something is and nothing about its unit: a
    /// metre and a foot are both <see cref="Length"/>. Units and their conversions are a
    /// separate concern, which UnitsNet and its kind already handle.
    /// </para>
    /// </remarks>
    public sealed class Dimension : IEquatable<Dimension>
    {
        private const int Count = 7;
        [ConstantField] private static readonly string[] symbols = { "L", "M", "T", "I", "Θ", "N", "J" };
        private readonly ERational[] exponents;

        // PeterO keeps a rational as it was computed, 2/2 included; reducing here is what lets
        // ToString and IsIntegral read the numerator and denominator directly.
        private Dimension(ERational[] exponents)
            => this.exponents = exponents.Select(e =>
            {
                var g = e.Numerator.Gcd(e.Denominator);
                return g.IsZero || g.Equals(EInteger.One) ? e : ERational.Create(e.Numerator / g, e.Denominator / g);
            }).ToArray();

        private static Dimension Base(BaseDimension b)
        {
            var exponents = Enumerable.Repeat(ERational.Zero, Count).ToArray();
            exponents[(int)b] = ERational.One;
            return new Dimension(exponents);
        }

        /// <summary>A pure number: a ratio, an angle in radians, a count.</summary>
        [ConstantField] public static readonly Dimension Dimensionless = new(Enumerable.Repeat(ERational.Zero, Count).ToArray());
        /// <summary>L.</summary>
        [ConstantField] public static readonly Dimension Length = Base(BaseDimension.Length);
        /// <summary>M.</summary>
        [ConstantField] public static readonly Dimension Mass = Base(BaseDimension.Mass);
        /// <summary>T.</summary>
        [ConstantField] public static readonly Dimension Time = Base(BaseDimension.Time);
        /// <summary>I.</summary>
        [ConstantField] public static readonly Dimension Current = Base(BaseDimension.Current);
        /// <summary>Θ.</summary>
        [ConstantField] public static readonly Dimension Temperature = Base(BaseDimension.Temperature);
        /// <summary>N.</summary>
        [ConstantField] public static readonly Dimension Amount = Base(BaseDimension.Amount);
        /// <summary>J.</summary>
        [ConstantField] public static readonly Dimension LuminousIntensity = Base(BaseDimension.LuminousIntensity);

        /// <summary>
        /// The dimension with the given exponent on each base dimension, in the order of
        /// <see cref="BaseDimension"/>; unlisted trailing exponents are zero.
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath.Core;
        ///
        /// Console.WriteLine(Dimension.Create(1, 1, -2));
        /// </code>
        /// Prints
        /// <code>
        /// L M T^-2
        /// </code>
        /// </example>
        public static Dimension Create(params int[] exponents)
        {
            if (exponents.Length > Count)
                throw new ArgumentException($"There are {Count} base dimensions, and {exponents.Length} exponents were given.", nameof(exponents));
            var e = Enumerable.Repeat(ERational.Zero, Count).ToArray();
            for (var k = 0; k < exponents.Length; k++)
                e[k] = ERational.FromInt32(exponents[k]);
            return new Dimension(e);
        }

        /// <summary>The exponent of <paramref name="b"/>.</summary>
        public Entity.Number.Rational this[BaseDimension b] => Entity.Number.Rational.Create(exponents[(int)b]);

        /// <summary>Whether every exponent is zero.</summary>
        public bool IsDimensionless => exponents.All(e => e.IsZero);

        /// <summary>Whether every exponent is a whole number, as a unit system's base units need.</summary>
        public bool IsIntegral => exponents.All(e => e.Denominator.Equals(EInteger.One));

        /// <summary>The product of two dimensions: exponents add.</summary>
        public static Dimension operator *(Dimension a, Dimension b)
            => new(a.exponents.Zip(b.exponents, (x, y) => x + y).ToArray());

        /// <summary>The quotient of two dimensions: exponents subtract.</summary>
        public static Dimension operator /(Dimension a, Dimension b)
            => new(a.exponents.Zip(b.exponents, (x, y) => x - y).ToArray());

        /// <summary>This dimension to a rational power: every exponent is multiplied by it.</summary>
        public Dimension Pow(Entity.Number.Rational power) => Pow(power.ERational);

        internal Dimension Pow(ERational power) => new(exponents.Select(e => e * power).ToArray());

        internal ERational Exponent(int k) => exponents[k];

        /// <inheritdoc/>
        public bool Equals(Dimension? other)
            => other is not null && exponents.Zip(other.exponents, (x, y) => x.CompareTo(y) == 0).All(eq => eq);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as Dimension);

        /// <inheritdoc/>
        public override int GetHashCode()
            => exponents.Aggregate(17, (h, e) => unchecked(h * 31 + e.Numerator.GetHashCode() * 7 + e.Denominator.GetHashCode()));

        /// <summary>Equality of every exponent.</summary>
        public static bool operator ==(Dimension? a, Dimension? b) => a is null ? b is null : a.Equals(b);

        /// <summary>Inequality of some exponent.</summary>
        public static bool operator !=(Dimension? a, Dimension? b) => !(a == b);

        /// <summary>
        /// The base dimensions with nonzero exponents, in SI order, as <c>L M T^-2</c>;
        /// <c>1</c> for <see cref="Dimensionless"/>.
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();
            for (var k = 0; k < Count; k++)
            {
                var e = exponents[k];
                if (e.IsZero) continue;
                parts.Add(e.CompareTo(ERational.One) == 0 ? symbols[k]
                    : e.Denominator.Equals(EInteger.One) ? $"{symbols[k]}^{e.Numerator}"
                    : $"{symbols[k]}^({e.Numerator}/{e.Denominator})");
            }
            return parts.Count == 0 ? "1" : string.Join(" ", parts);
        }
    }
}
