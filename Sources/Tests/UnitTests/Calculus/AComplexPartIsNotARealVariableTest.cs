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
    /// A sign read through a change of variable puts a real t for a part u, so u has to be real on the side approached.
    /// ln(x) on the left of 0 is ln|x| + i pi, and e^ln(x), which is x and negative there, was read as e^t at -oo, which is
    /// positive: floor(e^ln(x)) at 0 from the left was 0 and sgn(e^ln(x)) was 1, where both are -1. They are declined now,
    /// as they were in 2.6.0. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AComplexPartIsNotARealVariableTest
    {
        [Theory]
        [InlineData("floor(exp(ln(x)))", ApproachFrom.Left)]
        [InlineData("floor(exp(ln(x)))", ApproachFrom.BothSides)]
        [InlineData("sgn(exp(ln(x)))", ApproachFrom.Left)]
        [InlineData("floor(exp(ln(x)) + 1)", ApproachFrom.Left)]
        public void ThePositiveReadingIsNotGiven(string input, ApproachFrom side)
            => Assert.IsType<Entity.Limitf>(input.ToEntity().Limit("x", 0, side));

        [Fact]
        public void TheRightSideIsStillAnswered()
            => Assert.Equal(MathS.FromString("0"), "floor(exp(ln(x)))".ToEntity().Limit("x", 0, ApproachFrom.Right).Evaled);
    }
}
