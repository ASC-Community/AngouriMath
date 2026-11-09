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
    /// The first remarkable limit asked whether a quotient's parts vanish from both sides of the point, also inside a
    /// one-sided limit, and asked the dividend first. sinh((x - 1)^ln(x)) at 1 from the right is a quotient over 2, and
    /// its dividend was taken through the left side, where (x - 1)^ln(x) is not real, for eight seconds. Found by
    /// limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ARemarkableLimitIsAskedFromItsSideTest
    {
        [Theory]
        [InlineData("sinh((x - 1)^ln(x))", 1, ApproachFrom.Right, "sinh(1)")]
        [InlineData("cosh((x - 1)^ln(x))", 1, ApproachFrom.Right, "cosh(1)")]
        public void AFunctionOfAPowerFinishes(string input, int at, ApproachFrom side, string expected)
        {
            Entity? limit = null;
            var task = Task.Factory.StartNew(() => limit = input.ToEntity().Limit("x", at, side), TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), "the limit did not finish in 30 seconds");
            Assert.NotNull(limit);
            Assert.Equal(expected.ToEntity().EvalNumerical(), limit!.EvalNumerical());
        }

        // The remarkable limits themselves, from either side and both.
        [Theory]
        [InlineData("sin(x) / x", ApproachFrom.BothSides, "1")]
        [InlineData("sin(x) / x", ApproachFrom.Left, "1")]
        [InlineData("tan(2 * x) / sin(x)", ApproachFrom.Right, "2")]
        [InlineData("tan(x) * ln(x)", ApproachFrom.Right, "0")]
        [InlineData("(sin(x) / x - 1) / x^2", ApproachFrom.BothSides, "-1/6")]
        public void TheRemarkableLimitStillHolds(string input, ApproachFrom side, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", 0, side));
    }
}
