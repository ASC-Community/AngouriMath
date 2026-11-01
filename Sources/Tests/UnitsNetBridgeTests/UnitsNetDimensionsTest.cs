//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.UnitsNet;
using UnitsNet;
using UnitsNet.Units;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.UnitsNet.Tests;

public sealed class UnitsNetDimensionsTest
{
    private static readonly Dimension L = Dimension.Length, M = Dimension.Mass, T = Dimension.Time;

    /// <summary>
    /// The quantities UnitsNet marks dimensionless that are: ratios, angles, levels in decibels, a
    /// count of bits. With <see cref="UnitsNetDimensions.NoTrueDimension"/>, this is every quantity
    /// UnitsNet marks dimensionless, so a UnitsNet that adds one, or gives one of the wrong ones its
    /// dimension, fails <see cref="TheDimensionlessQuantitiesAreEachAccountedFor"/>.
    /// </summary>
    private static readonly HashSet<string> TrulyDimensionless =
    [
        "AmplitudeRatio", "Angle", "Information", "Level", "MassFraction", "PowerRatio", "Ratio",
        "RelativeHumidity", "Scalar", "SolidAngle", "Turbidity", "VolumeConcentration",
    ];

    private static IEnumerable<QuantityInfo> WithTrueDimension
        => Quantity.Infos.Where(quantity => !UnitsNetDimensions.NoTrueDimension.ContainsKey(quantity.Name));

    [Fact]
    public void EveryQuantitysDimensionComesBackAsItWent()
    {
        foreach (var quantity in WithTrueDimension)
            Assert.Equal(quantity.BaseDimensions, UnitsNetDimensions.Of(quantity).ToBaseDimensions());
    }

    [Fact]
    public void TheSevenBaseDimensionsAreTheSameSeven()
    {
        Assert.Equal(L, UnitsNetDimensions.Of(LengthUnit.Meter));
        Assert.Equal(M, UnitsNetDimensions.Of(MassUnit.Kilogram));
        Assert.Equal(T, UnitsNetDimensions.Of(DurationUnit.Second));
        Assert.Equal(Dimension.Current, UnitsNetDimensions.Of(ElectricCurrentUnit.Ampere));
        Assert.Equal(Dimension.Temperature, UnitsNetDimensions.Of(TemperatureUnit.Kelvin));
        Assert.Equal(Dimension.Amount, UnitsNetDimensions.Of(AmountOfSubstanceUnit.Mole));
        Assert.Equal(Dimension.LuminousIntensity, UnitsNetDimensions.Of(LuminousIntensityUnit.Candela));
    }

    [Fact]
    public void AUnitAValueAndAQuantityHaveOneDimension()
    {
        var speed = L / T;
        Assert.Equal(speed, UnitsNetDimensions.Of(SpeedUnit.KilometerPerHour));
        Assert.Equal(speed, UnitsNetDimensions.Of(Speed.FromKilometersPerHour(90)));
        Assert.Equal(speed, UnitsNetDimensions.Of(Speed.Info));
        Assert.Equal(L * L * M / (T * T), UnitsNetDimensions.Of(EnergyUnit.KilowattHour));
    }

    /// <summary>A dimension does not decide a quantity, so every quantity of it is named.</summary>
    [Fact]
    public void ADimensionNamesEveryQuantityOfIt()
    {
        Assert.Equal(["Speed", "VolumeFlowPerArea"], UnitsNetDimensions.QuantitiesOf(L / T).Select(q => q.Name).Order());
        var energy = UnitsNetDimensions.QuantitiesOf(UnitsNetDimensions.Of(EnergyUnit.Joule)).Select(q => q.Name).ToList();
        Assert.Contains("Energy", energy);
        Assert.Contains("Torque", energy);
        Assert.Empty(UnitsNetDimensions.QuantitiesOf(Dimension.Create(0, 0, 0, 0, 0, 0, 5)));
    }

    /// <summary>The square root of a length is a dimension AngouriMath has and UnitsNet cannot hold.</summary>
    [Fact]
    public void AFractionalExponentHasNoUnitsNetForm()
    {
        var rootOfLength = L.Pow(Number.Rational.Create(1, 2));
        Assert.Null(rootOfLength.ToBaseDimensions());
        Assert.Empty(UnitsNetDimensions.QuantitiesOf(rootOfLength));
        Assert.Equal(L, (rootOfLength * rootOfLength).ToBaseDimensions()!.ToDimension());
    }

    [Fact]
    public void AnEnumThatIsNotAUnitIsRefused()
        => Assert.Throws<ArgumentException>(() => UnitsNetDimensions.Of(DayOfWeek.Monday));

    /// <summary>
    /// UnitsNet marks fuel efficiency dimensionless, and its units are volume per distance and
    /// distance per volume: no one dimension, and certainly not none.
    /// </summary>
    [Fact]
    public void AQuantityUnitsNetGivesTheWrongDimensionIsRefused()
    {
        var refusal = Assert.Throws<ArgumentException>(() => UnitsNetDimensions.Of(FuelEfficiencyUnit.LiterPer100Kilometers));
        Assert.Contains("FuelEfficiency", refusal.Message);
        Assert.Throws<ArgumentException>(() => UnitsNetDimensions.Of(SpecificFuelConsumption.Info));
        var dimensionless = UnitsNetDimensions.QuantitiesOf(Dimension.Dimensionless).Select(q => q.Name).ToList();
        Assert.Contains("Ratio", dimensionless);
        Assert.DoesNotContain("FuelEfficiency", dimensionless);
    }

    [Fact]
    public void TheDimensionlessQuantitiesAreEachAccountedFor()
    {
        var marked = Quantity.Infos.Where(q => q.BaseDimensions.IsDimensionless()).Select(q => q.Name).ToHashSet();
        Assert.Equal(TrulyDimensionless.Union(UnitsNetDimensions.NoTrueDimension.Keys).Order(), marked.Order());
        Assert.Empty(TrulyDimensionless.Intersect(UnitsNetDimensions.NoTrueDimension.Keys));
    }

    [Fact]
    public void AFormulaIsCheckedAsWithDimensions()
    {
        var units = new Dictionary<Variable, Enum>
        {
            ["g"] = AccelerationUnit.StandardGravity, ["h"] = LengthUnit.Foot, ["t"] = DurationUnit.Second,
        };
        var dimensions = new Dictionary<Variable, Dimension> { ["g"] = L / (T * T), ["h"] = L, ["t"] = T };
        foreach (var formula in new Entity[] { "sqrt(2 g h)", "h + g t", "h + g t^2 / 2", "h + k" })
            Assert.Equal(MathS.Dimensions.Of(formula, dimensions), UnitsNetDimensions.Of(formula, units));

        var fall = UnitsNetDimensions.Of("sqrt(2 g h)", units);
        Assert.Equal(DimensionStatus.Consistent, fall.Status);
        Assert.Contains(UnitsNetDimensions.QuantitiesOf(fall.Dimension!), q => q.Name == "Speed");
        Assert.Equal(DimensionStatus.Inconsistent, UnitsNetDimensions.Of("h + g t", units).Status);
        Assert.Equal(DimensionStatus.Undetermined, UnitsNetDimensions.Of("h + k", units).Status);
    }

    /// <summary>Density, speed, length and viscosity have one dimensionless group: Reynolds' number.</summary>
    [Fact]
    public void TheDimensionlessGroupsAreThoseOfTheDimensions()
    {
        Variable rho = "rho", v = "v", d = "d", mu = "mu";
        var units = new Dictionary<Variable, Enum>
        {
            [rho] = DensityUnit.KilogramPerCubicMeter, [v] = SpeedUnit.MeterPerSecond,
            [d] = LengthUnit.Millimeter, [mu] = DynamicViscosityUnit.Centipoise,
        };
        var dimensions = units.ToDictionary(pair => pair.Key, pair => UnitsNetDimensions.Of(pair.Value));
        var groups = UnitsNetDimensions.DimensionlessGroups([rho, v, d, mu], units);
        Assert.Equal(MathS.Dimensions.DimensionlessGroups([rho, v, d, mu], dimensions), groups);
        Assert.Single(groups);
    }
}
