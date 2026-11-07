//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A limit read log(f^c), for a constant real c, as nothing but a logarithm of a power, never as
    /// c log(f). <c>log(x, arcsin(x)^tan(1/2))</c> at 0, which is tan(1/2) from each side, was left
    /// unevaluated after three seconds from the right and four two-sided, where <c>log(x, arcsin(x))</c>
    /// was 1 at once. It is c log(f) where f is positive near the point, and for c in (-1, 1] wherever
    /// f is real; where f is negative, it is c log(-f) + log((-1)^c). Found by limitcheck at <c>DEPTH=3</c>,
    /// <c>SEED=65</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ALogarithmOfAPowerTest
    {
        [Theory]
        // arcsin(x) is negative on the left of 0, and tan(1/2) is in (-1, 1].
        [InlineData("log(x, arcsin(x)^tan(1/2))", ApproachFrom.Right, "tan(1/2)")]
        [InlineData("log(x, arcsin(x)^tan(1/2))", ApproachFrom.Left, "tan(1/2)")]
        [InlineData("log(x, arcsin(x)^tan(1/2))", ApproachFrom.BothSides, "tan(1/2)")]
        [InlineData("ln(sin(x)^(1/3)) / ln(x)", ApproachFrom.BothSides, "1/3")]
        // Positive on the right, so any exponent; on the left, ln(f^pi) is pi ln(-f) + ln((-1)^pi).
        [InlineData("ln(arcsin(x)^pi) / ln(x)", ApproachFrom.Right, "pi")]
        [InlineData("ln(arcsin(x)^pi) / ln(x)", ApproachFrom.Left, "pi")]
        [InlineData("ln(arcsin(x)^pi) / ln(x)", ApproachFrom.BothSides, "pi")]
        public void IsTheExponentTimesTheLimit(string input, ApproachFrom side, string expected)
        {
            var limit = MathS.FromString(input).Limit("x", 0, side);
            // Simplifying a limit left unevaluated simplifies what is under it and asks again, which
            // answers some of these, so that it was answered is asserted first.
            Assert.DoesNotContain(limit.Nodes, node => node is Entity.Limitf);
            Assert.Equal(Entity.Number.Integer.Zero, (limit - MathS.FromString(expected)).Simplify());
        }
    }
}
