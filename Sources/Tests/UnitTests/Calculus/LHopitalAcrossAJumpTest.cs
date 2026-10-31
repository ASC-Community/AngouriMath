//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// l'Hopital's rule is not applied across the jumps of a step function. It needs both parts differentiable on a punctured
    /// neighbourhood of the destination, the derivative of floor(x) is 0 provided not x in ZZ, and every neighbourhood of +oo
    /// holds integers: floor(x) / x at +oo read 0 / 1 and answered 0, where it is 1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2281">#2281</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LHopitalAcrossAJumpTest
    {
        [Theory]
        [InlineData("limit(floor(x) / x, x, +oo)", "1")]
        [InlineData("limit(ceil(x) / x, x, +oo)", "1")]
        [InlineData("limit(floor(2x) / x, x, +oo)", "2")]
        [InlineData("limit(floor(x)^2 / x^2, x, +oo)", "1")]
        [InlineData("limit(ln(floor(x)) / ln(x), x, +oo)", "1")]
        [InlineData("limit(ln(x) / ln(floor(x)), x, +oo)", "1")]
        [InlineData("limit((x - floor(x)) / x, x, +oo)", "0")]
        [InlineData("limitright(x * floor(1/x), x, 0)", "1")]
        public void ItIsTheLimitOrNothing(string limit, string value)
        {
            var answer = limit.ToEntity().Simplify();
            Assert.True(answer.Nodes.Any(node => node is Entity.Limitf) || answer.Evaled == value.ToEntity().Evaled,
                $"{limit} is {answer}, where it is {value}");
        }

        /// <summary>Where a step's argument settles, the step is constant on either side, and the rule holds.</summary>
        [Theory]
        [InlineData("limit(floor(x + 1/2) * sin(x) / x, x, 0)", "0")]
        [InlineData("limit((ceil(x + 1/2) + 1) * sin(x) / x, x, 0)", "2")]
        [InlineData("limit(sgn(x + 1) * (1 - cos(x)) / x^2, x, 0)", "1/2")]
        [InlineData("limit(sin(x) / x, x, 0)", "1")]
        [InlineData("limit(ln(x) / x, x, +oo)", "0")]
        public void WhereTheStepSettlesItIsAnswered(string limit, string value)
            => Assert.Equal(value.ToEntity().Evaled, limit.ToEntity().Simplify().Evaled);
    }
}
