//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A base tending to a positive number or to +oo, to a constant real exponent, is continuous, and the power's
    /// limit is read from the base's. The whole power was simplified first, and Simplify expands it:
    /// (x + 1 + i / ln(x))^(-16) at +oo took 7.8 s, and to the power -24 more than 30 s, for 0.
    /// Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerOfAPositiveLimitIsContinuousTest
    {
        [Theory]
        [InlineData("(x + 1 + i / ln(x)) ^ (-32)", "0")]
        [InlineData("(x + 1 + i / ln(x)) ^ 32", "+oo")]
        [InlineData("((x + 1) - log(x, -1)) ^ (2 ^ 3 * (-1 + -1))", "0")]
        public void TheLimitAtInfinityIsAnswered(string input, string expected)
        {
            var limit = Task.Run(() => input.ToEntity().Limit("x", "+oo", ApproachFrom.BothSides));
            Assert.True(limit.Wait(TimeSpan.FromSeconds(30)), $"{input} at +oo did not finish in 30 seconds");
            Assert.Equal(expected.ToEntity().Evaled, limit.Result.Evaled);
        }

        [Theory]
        [InlineData("(2 + 1 / x) ^ (-3)", "1/8")]
        [InlineData("(4 + i / ln(x)) ^ (1/2)", "2")]
        [InlineData("(x + 1 + i / ln(x)) ^ (-1/2)", "0")]
        public void APositiveOrInfiniteBaseLimitIsRaisedToTheExponent(string input, string expected)
            => Assert.Equal(expected.ToEntity().Evaled, input.ToEntity().Limit("x", "+oo", ApproachFrom.BothSides).Evaled);
    }
}
