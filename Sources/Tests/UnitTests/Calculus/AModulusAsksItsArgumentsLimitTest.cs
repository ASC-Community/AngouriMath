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
    /// <c>abs(x * ln(x))</c> at 0 was NaN -- the claim that the limit does not exist -- and unevaluated from each side, where
    /// it is 0 from both: real on both sides, and from the left |x| |ln|x| + i pi| tends to 0 too. The modulus took its
    /// argument's limit as the descent reads it, and the descent reads x * ln(x) as 0 * -oo, which is NaN. The modulus now
    /// asks its argument's whole limit where that reading has no value, and takes the modulus of a value.
    /// <c>x * ln(x)</c> itself is still NaN two-sided, having no real values to the left of 0. The same on 2.6.0. Found
    /// while checking the modulus of a quotient; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AModulusAsksItsArgumentsLimitTest
    {
        [Theory]
        [InlineData("abs(x * ln(x))")]
        [InlineData("abs(sin(x) * ln(x))")]
        [InlineData("abs(x^2 * ln(x))")]
        public void IsZeroFromEachSideAndBoth(string input)
        {
            foreach (var side in new[] { ApproachFrom.Left, ApproachFrom.Right, ApproachFrom.BothSides })
                Assert.Equal(MathS.FromString("0"), MathS.FromString(input).Limit("x", 0, side));
        }

        // Real on both sides, since |x^x| is |x|^x to the left of 0; x^x itself is not, and is NaN below.
        [Fact]
        public void AModulusOfXToTheXIsOne()
            => Assert.Equal(MathS.FromString("1"), MathS.FromString("abs(x^x)").Limit("x", 0));

        [Theory]
        [InlineData("x * ln(x)")]
        [InlineData("x^x")]
        [InlineData("abs(sin(1/x))")]
        public void HasNoTwoSidedLimit(string input)
            => Assert.Equal(MathS.NaN, MathS.FromString(input).Limit("x", 0));
    }
}
