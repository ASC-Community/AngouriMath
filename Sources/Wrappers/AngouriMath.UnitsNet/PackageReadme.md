This package connects [UnitsNet](https://github.com/angularsen/UnitsNet) to AngouriMath's dimensional analysis: give each variable of a formula a UnitsNet unit, and AngouriMath checks that the formula is physically meaningful and says what dimension it has.

```csharp
using AngouriMath;
using AngouriMath.UnitsNet;
using UnitsNet.Units;

var units = new Dictionary<Entity.Variable, Enum> { ["g"] = AccelerationUnit.StandardGravity, ["h"] = LengthUnit.Foot, ["t"] = DurationUnit.Second };
UnitsNetDimensions.Of("sqrt(2 g h)", units);   // Consistent: L T^-1
UnitsNetDimensions.Of("h + g t", units);       // Inconsistent at h + g * t
UnitsNetDimensions.QuantitiesOf(UnitsNetDimensions.Of("sqrt(2 g h)", units).Dimension!);   // Speed, VolumeFlowPerArea
```

References:
[Website](https://am.angouri.org) · [Github](https://github.com/asc-community/AngouriMath) · [What's new](https://am.angouri.org/whatsnew/) · [Wiki](https://am.angouri.org/wiki/) · [Contacts](https://am.angouri.org/#contacts)
