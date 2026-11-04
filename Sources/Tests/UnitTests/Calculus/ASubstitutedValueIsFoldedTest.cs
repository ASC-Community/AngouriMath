//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A one-sided limit at a finite point is taken by moving the variable out to infinity, and the value read off there kept
    /// the infinity in it: <c>x^2 + x - 1</c> from the left at 0 came back to the descent as <c>-1 - 1/+oo + 1/+oo^2</c>.
    /// The sine above it asked whether that was finite, was told no, and declined -- so <c>sin(x^2 + x - 1)</c> from
    /// the left at 0 was left unevaluated, as on 2.6.0. Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=26</c>; no issue
    /// was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASubstitutedValueIsFoldedTest
    {
        [Theory]
        [InlineData("sin(x^2 + x - 1)", ApproachFrom.Left, "sin(-1)")]
        [InlineData("cos(x^2 + x + 2)", ApproachFrom.Left, "cos(2)")]
        [InlineData("sin(x^2 + x - 1)", ApproachFrom.BothSides, "sin(-1)")]
        [InlineData("sin(x^2 + x - 1)", ApproachFrom.Right, "sin(-1)")]
        public void IsTheValueAtTheDestination(string input, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", 0, side));

        // Answered as a quotient of the two, which is the tangent's value.
        [Theory]
        [InlineData("tan(x^2 + x + 2)", ApproachFrom.Left)]
        [InlineData("tan(x^2 - x - 1)", ApproachFrom.Left)]
        [InlineData("tan(x^2 + x - 1)", ApproachFrom.BothSides)]
        public void IsTheTangentsValue(string input, ApproachFrom side)
        {
            var limit = MathS.FromString(input).Limit("x", 0, side);
            var expected = MathS.FromString(input).Substitute("x", 0);
            Assert.False(limit is Limitf, $"lim {side} x->0 {input} was left unevaluated");
            var difference = (limit - expected).EvalNumerical();
            Assert.True(difference is Number.Complex value && Number.Complex.Abs(value) is Number.Real abs && abs < 1e-10,
                $"lim {side} x->0 {input} = {limit}, which is not {expected}");
        }
    }
}
