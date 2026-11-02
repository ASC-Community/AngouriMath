//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The arctangent's cut is the imaginary axis past <c>i</c> and <c>-i</c>, and its principal value there is the limit
    /// from one side only. <c>arctan(log(2, x))</c> at -1+ has its argument arriving at <c>i pi / ln(2)</c> from the left
    /// half-plane and tends to <c>-pi/2 + 0.224i</c>, and it answered the principal value, <c>pi/2 + 0.224i</c>. Such a
    /// limit is now left unevaluated. No issue was filed; found by work/limitcheck at seed 11.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LimitOnAnArctangentsCutTest
    {
        [Theory]
        [InlineData("arctan(log(2, x))", "-1", ApproachFrom.Right)]
        [InlineData("arctan(2i + x)", "0", ApproachFrom.Left)]
        public void IsNotThePrincipalValue(string expression, string destination, ApproachFrom side)
        {
            var limit = expression.ToEntity().Limit("x", destination, side);
            Assert.True(limit.Nodes.Any(node => node is Entity.Limitf), $"{expression} at {destination} from {side} was {limit}");
        }

        // Off the cut it is still read off.
        [Fact]
        public void OffTheCutIsTheValue()
            => Assert.Equal("arctan(1 + 2i)".ToEntity().Evaled, "arctan(x + 2i)".ToEntity().Limit("x", 1).Evaled);
    }
}
