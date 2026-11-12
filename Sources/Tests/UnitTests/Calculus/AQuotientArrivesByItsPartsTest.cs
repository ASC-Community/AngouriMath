//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The side a quotient a / b arrives at a constant c from is the sign of a - c b times that of b. floor(log(x / 2, tan(x)))
    /// at 0 from the right is the floor of ln(tan(x)) / ln(x / 2), which tends to 1 from below, since ln(tan(x)) - ln(x / 2)
    /// tends to ln(2) and ln(x / 2) to -oo. It was left unevaluated after 20 s in l'Hopital's rule on the slope.
    /// Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AQuotientArrivesByItsPartsTest
    {
        [Theory]
        [InlineData("floor(log(x / 2, tan(x)))", "0")]
        [InlineData("floor(log(pi / x, tan(x)))", "-1")]
        [InlineData("floor(ln(tan(x)) / ln(x / 2))", "0")]
        [InlineData("ceil(ln(tan(x)) / ln(x / 2))", "1")]
        [InlineData("floor(tanh(1 / x))", "0")]
        public void TheLimitFromTheRightIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);

        // tanh(1 / x) is (e^(2 / x) - 1) / (e^(2 / x) + 1), which arrives at 1 from below on the right and at -1 from above on
        // the left, so its floor is 0 on one side and -1 on the other, and the two-sided limit does not exist.
        [Fact]
        public void TheSidesOfAQuotientCanDisagree()
        {
            Assert.Equal(MathS.FromString("-1"), MathS.FromString("floor(tanh(1 / x))").Limit("x", 0, ApproachFrom.Left).Evaled);
            Assert.Equal(MathS.NaN, MathS.FromString("floor(tanh(1 / x))").Limit("x", 0));
        }
    }
}
