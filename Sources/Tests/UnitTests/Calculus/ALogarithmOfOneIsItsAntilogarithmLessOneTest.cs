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
    /// A factor ln(u) whose antilogarithm tends to 1 is equivalent to u - 1, as sin(f) is to f where f tends to 0:
    /// ln(1 + ln(sin(x) / x) / ln(x)) / x at 0 from the right is ln(sin(x) / x) / (x ln(x)), which is 0, and was left
    /// unevaluated. A term of a sum is not rewritten, and neither is the logarithm of a power, which is read as a product.
    /// Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ALogarithmOfOneIsItsAntilogarithmLessOneTest
    {
        [Theory]
        [InlineData("ln(1 + ln(sin(x) / x) / ln(x)) / x", "0")]
        [InlineData("ln(1 + x / ln(x)) * ln(x) / x", "1")]
        [InlineData("ln(cos(x) ^ ln(x)) / (x ^ 2 * ln(x))", "-1/2")]
        public void TheLimitFromTheRightIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);

        [Theory]
        [InlineData("(ln(1 + x) - x) / x ^ 2", "-1/2")]
        [InlineData("ln(1 + x) * cot(x)", "1")]
        [InlineData("ln(cos(x)) / sin(x) ^ 2", "-1/2")]
        public void TheLimitFromBothSidesIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.BothSides).Evaled);
    }
}
