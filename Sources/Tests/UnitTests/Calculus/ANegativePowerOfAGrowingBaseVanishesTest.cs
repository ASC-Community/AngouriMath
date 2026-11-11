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
    /// A power to a constant negative whole exponent whose base grows without bound in modulus tends to 0, read from the
    /// reciprocal of the base: ((x + 1) - log(x, -1))^(-16) at 1, where log(x, -1) = i pi / ln(x) takes the base to a
    /// complex infinity, did not finish in 30 s from any side, because the whole power was simplified and Simplify
    /// expanded it. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ANegativePowerOfAGrowingBaseVanishesTest
    {
        [Theory]
        [InlineData(ApproachFrom.Left)]
        [InlineData(ApproachFrom.Right)]
        [InlineData(ApproachFrom.BothSides)]
        public void TheLimitAtOneIsZero(ApproachFrom side)
        {
            var limit = Task.Run(() => "((x + 1) - log(x, -1)) ^ (-16)".ToEntity().Limit("x", 1, side));
            Assert.True(limit.Wait(TimeSpan.FromSeconds(30)), $"the limit from {side} did not finish in 30 seconds");
            Assert.Equal(Entity.Number.Integer.Zero, limit.Result.Evaled);
        }

        // A base with no limit at all keeps its reciprocal from tending to 0, and is not answered.
        [Fact]
        public void ABaseWithZerosArbitrarilyFarOutIsNotAnswered()
            => Assert.IsType<Entity.Limitf>("(x * sin(x)) ^ (-2)".ToEntity().Limit("x", "+oo", ApproachFrom.BothSides));
    }
}
