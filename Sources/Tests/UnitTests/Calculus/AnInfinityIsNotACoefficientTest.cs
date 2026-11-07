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
    /// An infinity that does not depend on the variable was taken for a coefficient. <c>log(x, 0)</c>, which is
    /// <c>ln(0) / ln(x)</c>, is +oo at every point between 0 and 1, since <c>ln(0)</c> is -oo, and its limit at 0
    /// from the right was 0; so was <c>-oo / x</c> at +oo and <c>+oo * x</c> at 0 from the right, where each is an
    /// infinity all the way in. The polynomial readers read <c>-oo / x</c> as a constant over a line, and the
    /// descent put the other part's limit in the infinity's place, folding <c>+oo * 0</c> to 0. The same on 2.6.0.
    /// Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=55</c> and <c>SEED=57</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnInfinityIsNotACoefficientTest
    {
        [Theory]
        [InlineData("log(x, 0)", "0", ApproachFrom.Right, "+oo")]
        [InlineData("log(x, 0)", "+oo", ApproachFrom.BothSides, "-oo")]
        [InlineData("log(x, 0)", "1", ApproachFrom.Left, "+oo")]
        [InlineData("log(x, 0)", "1", ApproachFrom.Right, "-oo")]
        [InlineData("-oo / ln(x)", "0", ApproachFrom.Right, "+oo")]
        [InlineData("-oo / x", "+oo", ApproachFrom.BothSides, "-oo")]
        [InlineData("+oo / x", "-oo", ApproachFrom.BothSides, "-oo")]
        [InlineData("+oo * x", "0", ApproachFrom.Right, "+oo")]
        [InlineData("+oo * x", "0", ApproachFrom.Left, "-oo")]
        [InlineData("arctan(log(sinh(x), 0))", "0", ApproachFrom.Right, "pi/2")]
        public void IsTheInfinityWithTheOtherPartsSign(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected).Simplify(),
                MathS.FromString(input).Limit("x", destination, side).Simplify());

        // The two sides disagree, so there is no two-sided limit; and a finite part over or times an infinity is
        // read as before.
        [Fact]
        public void LeavesEverythingElseAsItWas()
        {
            Assert.Equal(MathS.NaN, MathS.FromString("+oo * x").Limit("x", 0));
            Assert.Equal(MathS.FromString("0"), MathS.FromString("5 / x").Limit("x", "+oo"));
            Assert.Equal(MathS.FromString("+oo"), MathS.FromString("+oo * x").Limit("x", "+oo"));
        }

        // The values are 1 near 1/2; a coefficient evaluated to NaN answered NaN. It is not answered yet, and a
        // declined limit keeps its node.
        [Theory]
        [InlineData("1 ^ log(1 - x, x - x)", "1/2", ApproachFrom.BothSides)]
        public void ANaNCoefficientIsNotAnAnswer(string input, string destination, ApproachFrom side)
            => Assert.IsType<Entity.Limitf>(MathS.FromString(input).Limit("x", destination, side));

        // The values are +oo near 0 from the right, where a coefficient evaluated to NaN answered NaN too. It is
        // answered since x ^ x, an indeterminate power in a sum, is read through its exponent.
        [Fact]
        public void ANaNCoefficientInASumWithAnIndeterminatePower()
            => Assert.Equal(MathS.FromString("+oo"), MathS.FromString("sqrt(x) / x ^ pi + x ^ x").Limit("x", 0, ApproachFrom.Right));
    }
}
