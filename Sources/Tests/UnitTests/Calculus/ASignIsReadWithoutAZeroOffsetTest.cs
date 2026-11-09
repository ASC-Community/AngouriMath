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
    /// A step function at a jump of 0, and a comparison with 0, ask for the sign of g - 0, and every reading of that sign
    /// carried the 0 along: floor(sinh(2)^log(2, x)) at 0 from the right asked for the limit of (g - 0) / (x - 0), which
    /// l'Hopital's rule took seven seconds to decline, where g / x is declined at once. Found by limitcheck; no issue was
    /// filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASignIsReadWithoutAZeroOffsetTest
    {
        // A minute: the answer is the same either way, and only a return to seven seconds a run, or worse, is caught.
        [Fact]
        public void AFloorAtAJumpOfZero()
        {
            var task = Task.Run(() => "floor(sinh(2)^log(2, x))".ToEntity().Limit("x", 0, ApproachFrom.Right));
            Assert.True(task.Wait(TimeSpan.FromMinutes(1)));
            Assert.Equal("0".ToEntity(), task.Result);
        }

        [Theory]
        [InlineData("floor(x^(3/2))", ApproachFrom.Right, "0")]
        [InlineData("floor(sin(x))", ApproachFrom.Left, "-1")]
        [InlineData("floor(sin(x))", ApproachFrom.Right, "0")]
        [InlineData("floor(-x^2)", ApproachFrom.BothSides, "-1")]
        [InlineData("ceil(x^2)", ApproachFrom.BothSides, "1")]
        public void AStepAtZeroIsReadAsBefore(string input, ApproachFrom side, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", 0, side));
    }
}
