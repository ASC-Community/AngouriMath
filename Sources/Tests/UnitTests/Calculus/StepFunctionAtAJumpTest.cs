//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A one-sided limit of floor, ceil or sgn whose argument arrives on a jump is the value on the side the argument arrives
    /// from, where it was left unevaluated. That side is the sign the argument less the jump keeps near the destination, and
    /// is not the side x comes from: floor(2 - x^2) comes to 2 from below as x comes to 0 from the right. A two-sided limit at
    /// a jump has two sides that differ, and is NaN.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2266">#2266</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class StepFunctionAtAJumpTest
    {
        [Theory]
        [InlineData("limitleft(floor(x), x, 0)", "-1")]
        [InlineData("limitright(floor(x), x, 0)", "0")]
        [InlineData("limitright(floor(2 - x^2), x, 0)", "1")]
        [InlineData("limitright(floor(2 + x^2), x, 0)", "2")]
        [InlineData("limitright(floor(-x), x, 0)", "-1")]
        [InlineData("limitleft(floor(cos(x)), x, 0)", "0")]
        [InlineData("limitleft(ceil(x), x, 0)", "0")]
        [InlineData("limitright(ceil(x), x, 0)", "1")]
        [InlineData("limitright(sgn(x), x, 0)", "1")]
        [InlineData("limitleft(sgn(x), x, 0)", "-1")]
        [InlineData("limitright(sgn(x^3 - x), x, 0)", "-1")]
        [InlineData("limit(sgn(x^2), x, 0)", "1")]
        [InlineData("limit(floor(x^2), x, 0)", "0")]
        [InlineData("limit(sgn(x), x, 0)", "NaN")]
        [InlineData("limit(floor(x), x, 1)", "NaN")]
        [InlineData("limit(floor(x), x, 1/2)", "0")]
        public void ItIsTheValueOnTheSideTheArgumentArrivesFrom(string limit, string value)
            => Assert.Equal(value.ToEntity().Evaled, limit.ToEntity().Simplify().Evaled);
    }
}
