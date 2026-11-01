//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core;
using UnitsNet;
using static AngouriMath.Entity;

namespace AngouriMath.UnitsNet
{
    /// <summary>
    /// <see cref="MathS.Dimensions"/> in UnitsNet's terms: the dimension of a UnitsNet quantity or
    /// unit, the quantities a dimension can be, and a formula checked with a unit for each variable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two libraries name the same seven SI base dimensions in the same order, so a
    /// <see cref="BaseDimensions"/> and a <see cref="Dimension"/> carry the same exponents. One
    /// difference: UnitsNet's exponents are integers and AngouriMath's are rationals, because the
    /// square root of an area is a length and the square root of a length is still a dimension.
    /// Such a dimension has no UnitsNet form, and where one is needed the answer says so rather
    /// than rounding.
    /// </para>
    /// <para>
    /// Everything here reads UnitsNet's own metadata (<see cref="Quantity.Infos"/>), so a unit or
    /// a quantity added to UnitsNet is known here without a change to this package. Where that
    /// metadata is known to be wrong, the answer is a refusal, not the wrong dimension; see
    /// <see cref="NoTrueDimension"/>.
    /// </para>
    /// <para>
    /// Evaluating a formula on UnitsNet values is not offered, because UnitsNet 5 has no
    /// dependable way to read a value in coherent SI units: its <see cref="UnitSystem.SI"/> reads
    /// 49 of its 135 quantities (not mass, since its kilogram declares no base units), and the
    /// base units of several quantities are not the coherent SI unit (grams per second,
    /// volt-ampere hours, degrees of angle).
    /// </para>
    /// </remarks>
    public static class UnitsNetDimensions
    {
        /// <summary>
        /// The quantities UnitsNet marks dimensionless that are not, with what they are. Their
        /// dimension is refused rather than given as <see cref="Dimension.Dimensionless"/>, and
        /// <see cref="QuantitiesOf(Dimension)"/> leaves them out.
        /// </summary>
        public static IReadOnlyDictionary<string, string> NoTrueDimension { get; } = new Dictionary<string, string>
        {
            ["ElectricPotentialAc"] = "a voltage, L^2 M T^-3 I^-1",
            ["ElectricPotentialDc"] = "a voltage, L^2 M T^-3 I^-1",
            ["FuelEfficiency"] = "volume per distance in some of its units and distance per volume in others, so no one dimension",
            ["SpecificFuelConsumption"] = "mass per force and time, L^-1 T",
            ["VitaminA"] = "an amount in international units, a measure of biological activity with no SI dimension",
        };

        /// <summary>The dimension UnitsNet's <paramref name="dimensions"/> stand for.</summary>
        /// <example>
        /// <code>
        /// Speed.Info.BaseDimensions.ToDimension()   // L T^-1
        /// </code>
        /// </example>
        public static Dimension ToDimension(this BaseDimensions dimensions)
            => Dimension.Create(dimensions.Length, dimensions.Mass, dimensions.Time, dimensions.Current,
                dimensions.Temperature, dimensions.Amount, dimensions.LuminousIntensity);

        /// <summary>
        /// <paramref name="dimension"/> as UnitsNet's <see cref="BaseDimensions"/>, or
        /// <see langword="null"/> where an exponent is not an integer, which UnitsNet cannot hold.
        /// </summary>
        public static BaseDimensions? ToBaseDimensions(this Dimension dimension)
        {
            if (!dimension.IsIntegral)
                return null;
            var exponents = new int[7];
            for (var k = 0; k < exponents.Length; k++)
            {
                var exponent = dimension[(BaseDimension)k].ERational.Numerator;
                if (!exponent.CanFitInInt32())
                    return null;
                exponents[k] = exponent.ToInt32Checked();
            }
            return new BaseDimensions(exponents[0], exponents[1], exponents[2], exponents[3],
                exponents[4], exponents[5], exponents[6]);
        }

        /// <summary>The dimension of a UnitsNet quantity, such as <c>Speed.Info</c>.</summary>
        /// <exception cref="ArgumentException"><paramref name="quantity"/> is in <see cref="NoTrueDimension"/>.</exception>
        public static Dimension Of(QuantityInfo quantity)
            => NoTrueDimension.TryGetValue(quantity.Name, out var actually)
                ? throw new ArgumentException($"UnitsNet marks {quantity.Name} dimensionless, and it is {actually}.", nameof(quantity))
                : quantity.BaseDimensions.ToDimension();

        /// <summary>The dimension of a UnitsNet value, such as <c>Length.FromFeet(3)</c>.</summary>
        /// <exception cref="ArgumentException"><paramref name="quantity"/> is of a quantity in <see cref="NoTrueDimension"/>.</exception>
        public static Dimension Of(IQuantity quantity) => Of(quantity.QuantityInfo);

        /// <summary>The dimension of a UnitsNet unit, such as <c>SpeedUnit.KilometerPerHour</c>.</summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="unit"/> is not one of UnitsNet's units, or is a unit of a quantity in <see cref="NoTrueDimension"/>.
        /// </exception>
        public static Dimension Of(Enum unit) => Of(QuantityOf(unit));

        /// <summary>
        /// Every UnitsNet quantity of <paramref name="dimension"/>, in UnitsNet's order. Often more
        /// than one, because a dimension does not decide a quantity: torque and energy are both
        /// <c>L^2 M T^-2</c>. Empty where UnitsNet has none, which includes every dimension with an
        /// exponent that is not an integer. The quantities in <see cref="NoTrueDimension"/> are
        /// never among them.
        /// </summary>
        /// <example>
        /// <code>
        /// UnitsNetDimensions.QuantitiesOf(Dimension.Length / Dimension.Time)   // Speed, VolumeFlowPerArea
        /// </code>
        /// </example>
        public static IReadOnlyList<QuantityInfo> QuantitiesOf(Dimension dimension)
            => dimension.ToBaseDimensions() is { } dimensions
                ? Quantity.Infos.Where(quantity => quantity.BaseDimensions == dimensions && !NoTrueDimension.ContainsKey(quantity.Name)).ToList()
                : [];

        /// <summary>
        /// <see cref="MathS.Dimensions.Of(Entity, IReadOnlyDictionary{Variable, Dimension})"/>, with
        /// each variable's dimension given as a UnitsNet unit.
        /// </summary>
        /// <example>
        /// <code>
        /// UnitsNetDimensions.Of("sqrt(2 g h)", new Dictionary&lt;Entity.Variable, Enum&gt;
        ///     { ["g"] = AccelerationUnit.StandardGravity, ["h"] = LengthUnit.Foot })   // Consistent: L T^-1
        /// </code>
        /// </example>
        /// <exception cref="ArgumentException">One of <paramref name="units"/> is not one of UnitsNet's units, or is a unit of a quantity in <see cref="NoTrueDimension"/>.</exception>
        public static DimensionCheck Of(Entity expr, IReadOnlyDictionary<Variable, Enum> units)
            => MathS.Dimensions.Of(expr, DimensionsOf(units));

        /// <summary>
        /// <see cref="MathS.Dimensions.DimensionlessGroups(IReadOnlyList{Variable}, IReadOnlyDictionary{Variable, Dimension})"/>,
        /// with each variable's dimension given as a UnitsNet unit.
        /// </summary>
        /// <exception cref="ArgumentException">One of <paramref name="units"/> is not one of UnitsNet's units, or is a unit of a quantity in <see cref="NoTrueDimension"/>.</exception>
        public static IReadOnlyList<Entity> DimensionlessGroups(IReadOnlyList<Variable> variables, IReadOnlyDictionary<Variable, Enum> units)
            => MathS.Dimensions.DimensionlessGroups(variables, DimensionsOf(units));

        private static Dictionary<Variable, Dimension> DimensionsOf(IReadOnlyDictionary<Variable, Enum> units)
            => units.ToDictionary(pair => pair.Key, pair => Of(pair.Value));

        private static QuantityInfo QuantityOf(Enum unit)
            => Quantity.Infos.FirstOrDefault(quantity => quantity.UnitType == unit.GetType())
                ?? throw new ArgumentException($"{unit.GetType().Name}.{unit} is not one of UnitsNet's units.", nameof(unit));
    }
}
