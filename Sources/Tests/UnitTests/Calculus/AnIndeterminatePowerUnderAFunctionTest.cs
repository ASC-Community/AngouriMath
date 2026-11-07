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
    /// An indeterminate power under a function is read through its exponent, as one standing alone is.
    /// x^x at 0+ is 1, and sin(x^x) there was unevaluated, because the rule that reads a power through
    /// its exponent was asked only of a whole expression. Found by limitcheck at SEED=84; no issue was
    /// filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnIndeterminatePowerUnderAFunctionTest
    {
        [Theory]
        [InlineData("sin(x^x)", "0", ApproachFrom.Right, "sin(1)")]
        [InlineData("cos(x^x)", "0", ApproachFrom.Right, "cos(1)")]
        [InlineData("sin(x^x + 1)", "0", ApproachFrom.Right, "sin(2)")]
        [InlineData("sin(sin(x)^x)", "0", ApproachFrom.Right, "sin(1)")]
        [InlineData("sin((x - 1)^ln(x))", "1", ApproachFrom.Right, "sin(1)")]
        [InlineData("sin(x^(1/x))", "+oo", ApproachFrom.BothSides, "sin(1)")]
        public void IsReadThroughItsExponent(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));

        // The base of x^x is negative on the left of 0, where the power is complex, and the two-sided
        // limit there is left as it was.
        [Fact]
        public void AComplexSideIsNotRead()
        {
            Assert.Equal(MathS.NaN, MathS.FromString("sin(x^x)").Limit("x", 0, ApproachFrom.BothSides));
            Assert.IsType<Entity.Limitf>(MathS.FromString("sin(x^x)").Limit("x", 0, ApproachFrom.Left));
            // Its mirror as well: two sides each read through the exponent would settle (-x)^x at 0
            // and leave x^x NaN.
            Assert.Equal(MathS.NaN, MathS.FromString("(-x)^x").Limit("x", 0, ApproachFrom.BothSides));
            Assert.Equal(MathS.NaN, MathS.FromString("arctan((-x)^(2x))").Limit("x", 0, ApproachFrom.BothSides));
        }
    }
}
