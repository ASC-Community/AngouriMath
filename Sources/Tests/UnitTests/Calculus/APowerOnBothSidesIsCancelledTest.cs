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
    /// l'Hopital's rule divides a factor written on both sides out of the quotient it differentiates, and now also out of
    /// the quotient it gets, where simplification leaves a power of the variable on both sides beside a root:
    /// arccsc(-x) / (1 / x^3) at +oo differentiated into -1/3 * x^6 / (x^4 * sqrt(1 - 1 / x^2)), which counted as growth.
    /// And a quotient whose denominator cancels to a constant is asked as the product it is. Found by limitcheck; no issue
    /// was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerOnBothSidesIsCancelledTest
    {
        [Theory]
        [InlineData("x^3 * arccsc(-x)", "-oo")]
        [InlineData("x^3 * arccsc(x)", "+oo")]
        [InlineData("x^3 * arccsc(-x) * sqrt(1 - 1 / x^2) / x^2", "-1")]
        [InlineData("x * arccsc(-x)", "-1")]
        public void TheLimitIsAnswered(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", "+oo", ApproachFrom.BothSides).Evaled);

        [Theory]
        [InlineData("1 / x^2 - 1 / sin(x)^2", "0", "-1/3")]
        [InlineData("ln(sin(x)) / ln(sinh(x))", "0", "1")]
        [InlineData("(1 - cos(x)) / x^2", "0", "1/2")]
        [InlineData("x^2 * ln(1 - 1 / x)", "+oo", "-oo")]
        public void TheLimitStillHolds(string input, string at, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", at, ApproachFrom.Right).Evaled);
    }
}
