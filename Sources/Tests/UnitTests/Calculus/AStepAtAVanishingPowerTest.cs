//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A step function at a jump of 0 asks the sign its argument less 0 keeps on the side approached.
    /// That difference was not read as the power it subtracts 0 from, so <c>floor(x^(3/2))</c> at 0 from
    /// the right was declined: its second derivative diverges there, and its slope, <c>sqrt(x)</c>, tends
    /// to 0. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AStepAtAVanishingPowerTest
    {
        [Theory]
        [InlineData("floor(x^(3/2))", "0")]
        [InlineData("floor(x^pi)", "0")]
        [InlineData("floor(x^2 * sqrt(x))", "0")]
        [InlineData("ceil(-x * sqrt(x))", "0")]
        [InlineData("ceil(x^(3/2))", "1")]
        public void IsAnsweredFromTheRight(string input, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", 0, ApproachFrom.Right));

        // On the left x^(3/2) is not real, and nothing here reads the side its imaginary part arrives from.
        [Fact]
        public void IsLeftAloneFromTheLeft()
            => Assert.IsType<Limitf>(MathS.FromString("floor(x^(3/2))").Limit("x", 0, ApproachFrom.Left));
    }
}
