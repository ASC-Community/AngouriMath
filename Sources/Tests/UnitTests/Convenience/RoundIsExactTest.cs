//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// round of a rational is the nearest integer, exactly, and the even one at a tie, and an infinity is its own: through
    /// EDecimal a rational of more digits than the precision was rounded onto the integer beside it first, and
    /// round(10^120 + 2/3) came out 10^120, while round(+oo) threw OverflowException. floor and ceil had both defects and
    /// were fixed in #830 and #1807.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2282">#2282</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class RoundIsExactTest
    {
        [Theory]
        [InlineData("round(10^120 + 2/3)", "10^120 + 1")]
        [InlineData("round(10^120 - 2/3)", "10^120 - 1")]
        [InlineData("round(10^120 + 3/2)", "10^120 + 2")]
        [InlineData("round(10^120 + 1/2)", "10^120")]
        [InlineData("round(-10^120 - 2/3)", "-10^120 - 1")]
        [InlineData("round(-10^400 + 1/3)", "-10^400")]
        [InlineData("round(2/3)", "1")]
        [InlineData("round(5/2)", "2")]
        [InlineData("round(7/2)", "4")]
        [InlineData("round(-5/2)", "-2")]
        [InlineData("round(-7/2)", "-4")]
        [InlineData("round(+oo)", "+oo")]
        [InlineData("round(-oo)", "-oo")]
        public void ItIsTheNearestInteger(string input, string expected)
        {
            Assert.Equal(expected.ToEntity().InnerSimplified, input.ToEntity().InnerSimplified);
            Assert.Equal(expected.ToEntity().InnerSimplified, input.ToEntity().Evaled);
        }
    }
}
