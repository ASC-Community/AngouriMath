//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// l'Hopital's rule turns ln(sinh(x)^3) into a quotient with (e^x - e^(-x))^2 above and
    /// (e^x - e^(-x))^3 below, and the factor the two share was differentiated with everything else
    /// until the rule's budget ran out: (sinh(x)^3)^tan(x) at 0 was left unevaluated from each side,
    /// where it is 1. Found by limitcheck at <c>SEED=62</c>, <c>DEPTH=3</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LhopitalCancelsAFactorOnBothSidesTest
    {
        [Theory]
        [InlineData("(sinh(x)^3) ^ tan(x)", ApproachFrom.Left, "1")]
        [InlineData("(sinh(x)^3) ^ tan(x)", ApproachFrom.Right, "1")]
        [InlineData("x / cos(x) * ln(sinh(x)^3)", ApproachFrom.Right, "0")]
        [InlineData("sin(x) / cos(x) * ln((e^x - 1)^3)", ApproachFrom.Right, "0")]
        public void Finishes(string input, ApproachFrom side, string expected)
        {
            var limit = Task.Run(() => MathS.FromString(input).Limit("x", 0, side).Simplify());
            Assert.True(limit.Wait(TimeSpan.FromSeconds(60)), $"{input} at 0 from the {side} did not finish");
            Assert.Equal(MathS.FromString(expected).Simplify(), limit.Result);
        }

        // A factor that is 0 everywhere but evaluates to a rounding error is not cancelled.
        [Fact]
        public void DoesNotCancelAFactorThatIsZeroByAnIdentity()
            => Assert.IsType<Entity.Limitf>(MathS.FromString("(sin(x)^2 + cos(x)^2 - 1) * x / ((sin(x)^2 + cos(x)^2 - 1) * x^2)").Limit("x", 0, ApproachFrom.Right));
    }
}
