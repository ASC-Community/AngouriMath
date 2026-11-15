//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// <see cref="Real.CreateWithoutDowncasting"/> is <see cref="Real.Create(EDecimal)"/> under a scope that turns the
    /// downcasting off -- the same type, decimal, text and hash -- under any settings, and leaves the settings alone.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class ARealMadeWithoutDowncastingTest
    {
        private static IEnumerable<EDecimal> Values()
        {
            var random = new Random(17);
            foreach (var s in new[] { "0", "-0", "1", "-1", "0.5", "1E+3", "1000", "0.1", "0.3333333333333333333333", "1E-20", "1E-200",
                "123456789012345678901234567890", "NaN", "Infinity", "-Infinity", "2.5000", "7E-1" })
                yield return EDecimal.FromString(s);
            for (var i = 0; i < 20_000; i++)
            {
                var x = (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-30, 31));
                yield return EDecimal.FromDouble(x);
                yield return EDecimal.FromDouble(Math.Round(x));
                yield return ERational.Create(random.Next(-1000, 1000), random.Next(1, 1000)).ToEDecimal(EContext.ForPrecision(100));
            }
        }

        private static Real Reference(EDecimal value)
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            return Real.Create(value);
        }

        private static void Check()
        {
            foreach (var value in Values())
            {
                var expected = Reference(value);
                var actual = Real.CreateWithoutDowncasting(value);
                Assert.Equal(typeof(Real), actual.GetType());
                Assert.Equal(expected.GetType(), actual.GetType());
                Assert.Equal(expected, actual);
                Assert.Equal(expected.GetHashCode(), actual.GetHashCode());
                Assert.Equal(expected.ToString(), actual.ToString());
                Assert.True(expected.EDecimal.Equals(actual.EDecimal), value.ToString());
                Assert.True(ReferenceEquals(value, actual.EDecimal), value.ToString());
            }
        }

        [Fact] public void AtTheDefaults() => Check();

        [Fact] public void WithTheDowncastingOff()
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            Check();
        }

        [Fact] public void AtThirtyAndThreeHundredDigits()
        {
            foreach (var digits in new[] { 30, 300 })
            {
                using var _ = MathS.Settings.DecimalPrecisionContext.Set(new EContext(digits, ERounding.HalfUp, -digits, 1000, false));
                Check();
            }
        }

        [Fact] public void LeavesTheDowncastingOn()
        {
            Real.CreateWithoutDowncasting(EDecimal.FromString("0.5"));
            Assert.True(MathS.Settings.DowncastingEnabled);
            Assert.IsType<Rational>(Real.Create(EDecimal.FromString("0.5")));
        }
    }
}
