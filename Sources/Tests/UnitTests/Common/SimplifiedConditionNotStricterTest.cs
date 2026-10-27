//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>Simplify</c> answered <c>-(t x + y)/(1 - y x) + (t x + y)/(1 - y x)</c> with
    /// <c>0 provided not 1 - x y = 0 and not x = 0</c>. At <c>x = 0</c> the input is
    /// <c>-y + y</c>, which is defined, and the answer was not: a polynomial division in
    /// <c>y</c> had divided by the divisor's leading coefficient <c>-x</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2134">#2134</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class SimplifiedConditionNotStricterTest
    {
        [Theory]
        [InlineData("-(t*x + y)/(1 - y*x) + (t*x + y)/(1 - y*x)")]
        [InlineData("(t*x + y)/(1 - y*x) - (t*x + y)/(1 - y*x)")]
        public void TheAnswerIsDefinedWhereTheInputIs(string input)
        {
            var simplified = input.Simplify()
                .Substitute("x", 0).Substitute("y", 2).Substitute("t", 3);
            Assert.Equal(Integer.Zero, simplified.Evaled);
        }
    }
}
