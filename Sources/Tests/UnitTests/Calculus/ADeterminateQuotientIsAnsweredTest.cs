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
    /// A quotient whose parts, each asked whole, tend to a form that is not indeterminate has the quotient of their
    /// limits for its limit. cot(x) - 1/x at 0 is +oo - +oo read part by part and 0 asked whole, so
    /// (cot(x) - 1/x) / ln(x) at 0, 0 over -oo, is 0; l'Hopital's rule was asked of it, had nothing to
    /// differentiate, and it was left unevaluated. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ADeterminateQuotientIsAnsweredTest
    {
        [Theory]
        [InlineData("(cot(x) - 1/x) / ln(x)", "0")]
        [InlineData("(cot(x) - 1/x) / ln(x)^2", "0")]
        [InlineData("(1/sin(x) - 1/x) / ln(x)", "0")]
        [InlineData("(cot(x) - 1/x) / cot(x)", "0")]
        [InlineData("ln(x) / (cot(x) - 1/x + 2)", "-oo")]
        [InlineData("(cot(x) - 1/x + 3) / (x + 2)", "3/2")]
        public void TheLimitFromTheRightIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);

        [Theory]
        [InlineData("(cot(x) - 1/x) / ln(x)", "0")]
        [InlineData("ln(x) / (cot(x) - 1/x + 2)", "-oo")]
        public void TheLimitFromTheLeftIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0, ApproachFrom.Left).Evaled);

        [Theory]
        [InlineData("(cot(x) - 1/x + 3) / (x + 2)", "3/2")]
        [InlineData("(1/sin(x) - 1/x) / cot(x)", "0")]
        public void TheTwoSidedLimitIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", 0).Evaled);

        /// <summary>
        /// Two-sided, only where the quotient is real on both sides: a side with no real values makes no two-sided
        /// limit, the convention x^x at 0 is held to. Each side of sinh(-1) (x^x - 1) is 0, and read as a quotient it
        /// came to 0 two-sided.
        /// </summary>
        [Fact]
        public void AQuotientNotRealOnASideHasNoTwoSidedLimit()
            => Assert.Equal(MathS.NaN, "sinh(-1) * (x^x - 1)".ToEntity().Limit("x", 0, ApproachFrom.BothSides).Evaled);
    }
}
