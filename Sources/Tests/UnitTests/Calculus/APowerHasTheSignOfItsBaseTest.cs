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
    /// The sign a power keeps near a point is read from its base before its slope: a positive base to a real power is
    /// positive. sgn((x * x)^arctan(1/2)) at 0 from the left asked for the slope of the power first, which l'Hopital's
    /// rule took 6.6 s over, for 1. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerHasTheSignOfItsBaseTest
    {
        [Theory]
        [InlineData("sgn((x * x) ^ arctan(1/2))", ApproachFrom.Left, "1")]
        [InlineData("sgn((x * x) ^ arctan(1/2))", ApproachFrom.BothSides, "1")]
        [InlineData("floor((x * x) ^ pi)", ApproachFrom.Left, "0")]
        public void TheLimitAtZeroIsAnswered(string input, ApproachFrom side, string expected)
        {
            var limit = Task.Run(() => input.ToEntity().Limit("x", 0, side));
            Assert.True(limit.Wait(TimeSpan.FromSeconds(30)), $"{input} at 0 from {side} did not finish in 30 seconds");
            Assert.Equal(expected.ToEntity().Evaled, limit.Result.Evaled);
        }

        // A negative base to a power that is not whole is complex on that side, and has no sign.
        [Fact]
        public void AComplexPowerHasNoSign()
            => Assert.IsType<Entity.Limitf>("sgn((x * x * x) ^ pi)".ToEntity().Limit("x", 0, ApproachFrom.Left));
    }
}
