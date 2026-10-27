//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>Simplify</c> gathered fractions a pair or a sort level at a time, and left some sums of
    /// fractions apart that are a constant over one denominator. The rational canonical form
    /// decides them, and is offered as a candidate since
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2137">#2137</a>; the first
    /// row of <a href="https://github.com/asc-community/AngouriMath/issues/2133">#2133</a> is the
    /// same shape.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class SumOfFractionsOverOneDenominatorTest
    {
        [Theory]
        [InlineData("x/(x + y) + y/(x + y)", 1)]
        [InlineData("1/(1 - (b/a)^2) - a^2/(a^2 - b^2)", 0)]
        [InlineData("x/c + (y*c - x*d)/(c^2*(z + d/c)) - (x*z + y)/(c*z + d)", 0)]
        [InlineData("R^2 - R^2*(a + b)*(1/a + 1/b)/4 - R^2*(a - b)*(1/a - 1/b)/4", 0)]
        public void ItIsTheConstantItIs(string input, int constant)
        {
            var simplified = input.Simplify();
            while (simplified is Providedf(var inner, _))
                simplified = inner;
            Assert.Equal(Integer.Create(constant), simplified);
        }

        /// <summary>
        /// The condition the answer carries is the input's: defined where the input is.
        /// </summary>
        [Theory]
        [InlineData("x/(x + y) + y/(x + y)", "x", "1", "y", "2")]
        [InlineData("1/(1 - (b/a)^2) - a^2/(a^2 - b^2)", "a", "3", "b", "1")]
        [InlineData("R^2 - R^2*(a + b)*(1/a + 1/b)/4 - R^2*(a - b)*(1/a - 1/b)/4", "a", "2", "b", "5")]
        public void ItIsDefinedWhereTheInputIs(string input, string first, string firstValue, string second, string secondValue)
        {
            var at = input.Simplify().Substitute(first, firstValue.ToEntity()).Substitute(second, secondValue.ToEntity()).Substitute("R", 7);
            Assert.IsAssignableFrom<Complex>(at.Evaled);
            Assert.NotEqual(MathS.NaN, at.Evaled);
        }
    }
}
