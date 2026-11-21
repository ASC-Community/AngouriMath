//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Two constants were equal when their evaluated difference was below an absolute 1e-16, so
    /// <c>10^(-20) = 2 * 10^(-20)</c> and <c>pi/10^20 = 2 pi/10^20</c> were True. They are equal when the
    /// difference is zero, which the number factory makes of a residual of the cancellation, so two
    /// writings of one number rounded apart still compare equal. Found while looking for more absolute
    /// tolerances; no issue was filed. https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ConstantsEqualOnlyWhenTheirDifferenceIsZeroTest
    {
        [Theory]
        [InlineData("10^(-20) = 2 * 10^(-20)", false)]
        [InlineData("pi / 10^20 = 2 * pi / 10^20", false)]
        [InlineData("1/3 = 0.3333333333333333333333", false)]
        // Controls: one number written two ways, rounded apart, is still equal to itself.
        [InlineData("sqrt(i) = (1 + i) / sqrt(2)", true)]
        [InlineData("10^(-20) = 10^(-20)", true)]
        [InlineData("sin(pi/7)^2 + cos(pi/7)^2 = 1", true)]
        [InlineData("e^(i pi) = -1", true)]
        public void TheEqualityIsDecided(string expression, bool expected)
            => Assert.Equal(expected ? Boolean.True : Boolean.False, expression.ToEntity().Evaled);
    }
}
