//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// <c>floor</c>, <c>ceil</c>, <c>round</c> and <c>sgn</c> take only whole values, so one that has a limit from a side is
    /// equal to it on an interval on that side. <c>floor(x) ^ x</c> at 0+ is 0 to a positive power on <c>(0, 1)</c>, and was
    /// read as the form <c>0^0</c> and answered 1. No issue was filed; found by work/limitcheck at seed 11.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AStepFunctionNearItsLimitTest
    {
        [Theory]
        [InlineData("floor(x)^x", "0")]
        [InlineData("floor(x)^tanh(x)", "0")]
        [InlineData("floor(x)^sin(x)", "0")]
        [InlineData("ceil(x)^x", "1")]
        [InlineData("x^sgn(x)", "0")]
        public void FromTheRight(string expression, string limit)
            => Assert.Equal(limit.ToEntity(), expression.ToEntity().Limit("x", 0, ApproachFrom.Right).Simplify());

        // floor(x) is -1 on (-1, 0), and (-1)^x tends to 1.
        [Fact]
        public void FromTheLeft()
            => Assert.Equal(1, "floor(x)^x".ToEntity().Limit("x", 0, ApproachFrom.Left).Simplify());

        // 1 from the left and 0 from the right: no limit.
        [Fact]
        public void BothSidesDiffer()
            => Assert.Equal(MathS.NaN, "floor(x)^x".ToEntity().Limit("x", 0).Simplify());
    }
}
