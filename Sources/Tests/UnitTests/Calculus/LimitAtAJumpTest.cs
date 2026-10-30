//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A limit of an expression built on a step function is not answered by substituting the destination where the
    /// step's argument arrives on a jump: <c>limitleft(floor(x) - 1, x, 0)</c> was <c>-1</c>, where it is <c>-2</c>, and the
    /// two-sided limit, which does not exist, was <c>-1</c> too. Declining is an honest answer there; a value is a wrong one
    /// unless it is the limit.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2266">#2266</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LimitAtAJumpTest
    {
        [Theory]
        [InlineData("limitleft(floor(x) - 1, x, 0)", "-2")]
        [InlineData("limitright(floor(x) - 1, x, 0)", "-1")]
        [InlineData("limitleft(floor(x)^2, x, 0)", "1")]
        [InlineData("limitleft(arcsin(floor(x)), x, 0)", "-pi / 2")]
        [InlineData("limitleft(-sgn(x), x, 0)", "1")]
        [InlineData("limitright(-sgn(x), x, 0)", "-1")]
        [InlineData("limitleft(ceil(x) + 1, x, 1)", "2")]
        public void AOneSidedLimitAtAJumpIsTheLimitOrNothing(string limit, string value)
        {
            var answer = limit.ToEntity().Simplify();
            Assert.True(answer is not Entity.Number || answer == value.ToEntity().Simplify(), $"{limit} is {answer}, where it is {value}");
        }

        [Theory]
        [InlineData("limit(floor(x) - 1, x, 0)")]
        [InlineData("limit(floor(x) + x, x, 1)")]
        [InlineData("limit(-sgn(x), x, 0)")]
        [InlineData("limit(round(x) + x, x, 1/2)")]
        public void ATwoSidedLimitAtAJumpHasNoValue(string limit)
        {
            var answer = limit.ToEntity().Simplify();
            Assert.True(answer is not Entity.Number || answer.IsNaN, $"{limit} is {answer}, where the two sides differ");
        }

        /// <summary>Away from a jump the step is constant, and the limit is its value.</summary>
        [Theory]
        [InlineData("limit(floor(x) - 1, x, 1/2)", "-1")]
        [InlineData("limit(sgn(x) + x, x, 2)", "3")]
        [InlineData("limitleft(ceil(x) + 1, x, 1/2)", "2")]
        [InlineData("limit(round(x) + x, x, 1/4)", "1/4")]
        public void AwayFromAJumpTheLimitIsTheValue(string limit, string value)
            => Assert.Equal(value.ToEntity().Simplify(), limit.ToEntity().Simplify());
    }
}
