//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The limit machinery remembers the limits it has found within one question, and does not take them again. In
    /// tanh(log(arcsin(x), x)) at 0 the limit of the exponent ln(x) / ln(arcsin(x)) was taken four times over, at a
    /// second or more each: ten seconds from the left, against under half a second with the answers kept. Found by
    /// limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ALimitAlreadyFoundIsNotTakenAgainTest
    {
        [Theory]
        [InlineData(ApproachFrom.Left)]
        [InlineData(ApproachFrom.Right)]
        [InlineData(ApproachFrom.BothSides)]
        public void TheLimitIsAnswered(ApproachFrom side)
            => Assert.Equal("tanh(1)".ToEntity().Evaled, "tanh(log(arcsin(x), x))".ToEntity().Limit("x", 0, side).Evaled);
    }
}
