//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// Under the complex codomain two agreeing one-sided limits were not taken for the two-sided one, since <c>x^x</c> has a
    /// limit from the left only through its complex values. <c>(x^2)^x</c> is real on both sides of 0 and tends to 1 from
    /// each, and its two-sided limit was NaN, the claim that there is none. No issue was filed; found by work/limitcheck at
    /// seed 11.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ALimitRealOnBothSidesTest
    {
        [Theory]
        [InlineData("(x^2)^x", "1")]
        [InlineData("(x*x)^(x - 1)", "+oo")]
        [InlineData("(x^4)^(x - 1)", "+oo")]
        public void IsWhereItsSidesAgree(string expression, string limit)
            => Assert.Equal(limit.ToEntity().Evaled, expression.ToEntity().Limit("x", 0).Evaled);

        // Negative on the left, where the values are complex: still no two-sided limit.
        [Theory]
        [InlineData("x^x")]
        [InlineData("(x/pi)^x")]
        public void IsNotWhereOneSideIsComplex(string expression)
            => Assert.Equal(MathS.NaN, expression.ToEntity().Limit("x", 0).Evaled);
    }
}
