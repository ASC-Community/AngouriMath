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
    /// A product tending to 0 * oo with a power u^v for a factor, v tending to a constant c other than 0, is read as
    /// (other * u^c) * u^(v - c). (tan(x) + 1)^(x^(x - 1)) at 0 from the right has the exponent tan(x) * x^(x - 1), which is
    /// tan(x) / x times x^x, and l'Hopital's rule differentiated x^(x - 1) for longer than any caller waits.
    /// Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerIsSplitAtItsExponentsLimitTest
    {
        [Theory]
        [InlineData("(tan(x) + 1)^(x^(x - 1))", "e")]
        [InlineData("tan(x) * x^(x - 1)", "1")]
        [InlineData("sin(x) * x^(x - 1)", "1")]
        [InlineData("x^2 * x^(x - 3)", "+oo")]
        [InlineData("ln(x) * x^(x + 1)", "0")]
        public void TheLimitFromTheRightIsAnswered(string input, string expected)
        {
            var limit = Task.Run(() => input.ToEntity().Limit("x", 0, ApproachFrom.Right));
            Assert.True(limit.Wait(TimeSpan.FromSeconds(30)), $"the limit of {input} did not finish");
            Assert.Equal(expected.ToEntity().Evaled, limit.Result.Evaled);
        }
    }
}
