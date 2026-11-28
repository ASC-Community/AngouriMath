//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// An odd root of a negative number is the real one, <c>(-8)^(1/3) = -2</c>. With downcasting off the parser keeps
    /// <c>1/3</c> as a quotient, which evaluates to the decimal <c>0.333…</c>, and the power took the principal root:
    /// <c>1 + 1.732i</c>. The value no longer depends on the setting.
    /// https://github.com/asc-community/AngouriMath/issues/2622
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AnOddRootIsRealWhateverTheDowncastingTest
    {
        [Theory]
        [InlineData("(-8)^(1/3)", -2.0, 0.0)]
        [InlineData("(-8)^(2/3)", 4.0, 0.0)]
        [InlineData("(-8)^(-1/3)", -0.5, 0.0)]
        [InlineData("(-32)^(1/5)", -2.0, 0.0)]
        [InlineData("(-32)^(3/5)", -8.0, 0.0)]
        [InlineData("(-30.5)^(1/3)", -3.1243998847631218, 0.0)]
        [InlineData("(-2)^(1/7)", -1.1040895136738123, 0.0)]
        // Halves are principal either way, a control.
        [InlineData("(-30.5)^(1/2)", 0.0, 5.522680508593630)]
        [InlineData("(-4)^(3/2)", 0.0, -8.0)]
        // A positive base, and an even denominator, are unchanged.
        [InlineData("8^(1/3)", 2.0, 0.0)]
        [InlineData("(-16)^(1/4)", 1.4142135623730951, 1.4142135623730951)]
        public void IsTheSameWithTheDowncastingOnAndOff(string power, double re, double im)
        {
            foreach (var downcasting in new[] { true, false })
            {
                using var _ = MathS.Settings.DowncastingEnabled.Set(downcasting);
                var entity = power.ToEntity();
                foreach (var value in new[] { entity.Evaled, entity.EvalNumerical() })
                {
                    var number = Assert.IsAssignableFrom<Number.Complex>(value);
                    Assert.True(System.Math.Abs(number.RealPart.EDecimal.ToDouble() - re) < 1e-12
                        && System.Math.Abs(number.ImaginaryPart.EDecimal.ToDouble() - im) < 1e-12,
                        $"{power} with downcasting {(downcasting ? "on" : "off")} is {value}");
                }
            }
        }
    }
}
