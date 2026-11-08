//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A real base tending to 0 with a sign, to a negative whole power, tends to +oo, or for an odd power
    /// to +oo or -oo by the base's sign. sin(x)^(-2) at 0 was left unevaluated from each side and was NaN
    /// two-sided, where 1/sin(x)^2 was +oo. Found by limitcheck at SEED=80; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AVanishingBaseToANegativeWholePowerTest
    {
        [Theory]
        [InlineData("sin(x)^(-2)", ApproachFrom.BothSides, "+oo")]
        [InlineData("sin(x)^(-2)", ApproachFrom.Left, "+oo")]
        [InlineData("sin(x)^(-2)", ApproachFrom.Right, "+oo")]
        [InlineData("(arcsin(x)^(-2))^2", ApproachFrom.BothSides, "+oo")]
        [InlineData("tan(x/2)^((x - 1) - (x + 1))", ApproachFrom.BothSides, "+oo")]
        [InlineData("sin(x)^(-1)", ApproachFrom.Left, "-oo")]
        [InlineData("sin(x)^(-3)", ApproachFrom.Right, "+oo")]
        public void TendsToAnInfinity(string input, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", 0, side));

        // The two sides of an odd power disagree, so there is no two-sided limit.
        [Fact]
        public void AnOddPowerHasNoTwoSidedLimit()
            => Assert.Equal(MathS.NaN, MathS.FromString("sin(x)^(-1)").Limit("x", 0, ApproachFrom.BothSides));

        // An exponent that only tends to a whole number is not one near the point. Under a negative base
        // the power is complex there, so it is left alone rather than given the odd power's sign.
        [Theory]
        [InlineData("sin(x)^(x - 2)")]
        [InlineData("sin(x)^(x - 1)")]
        public void AnExponentThatOnlyTendsToAWholeNumberIsLeftAloneUnderANegativeBase(string input)
            => Assert.IsType<Entity.Limitf>(MathS.FromString(input).Limit("x", 0, ApproachFrom.Left));

        // Under a base positive on the way in, any exponent tending to a negative number gives +oo.
        [Theory]
        [InlineData("sin(x)^(x - 2)")]
        [InlineData("sin(x)^(x - 1)")]
        public void AnExponentThatOnlyTendsToAWholeNumberTendsToPositiveInfinityUnderAPositiveBase(string input)
            => Assert.Equal(Entity.Number.Real.PositiveInfinity, MathS.FromString(input).Limit("x", 0, ApproachFrom.Right));
    }
}
