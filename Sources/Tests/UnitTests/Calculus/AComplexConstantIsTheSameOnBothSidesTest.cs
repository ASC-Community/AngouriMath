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
    /// A two-sided limit whose sides the descent could not compare asks each side through the whole of the machinery,
    /// but only of an expression real on both sides, and a constant was required to be real. <c>pi^2 / log(x, -2)</c> is
    /// <c>pi^2 ln(x) / ln(-2)</c>, 0 from each side of 1, and its two-sided limit was NaN. A constant is the same number
    /// on both sides, so a complex one does not make them differ. Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=69</c>;
    /// no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AComplexConstantIsTheSameOnBothSidesTest
    {
        [Theory]
        [InlineData("pi^2 / log(x, -2)", "1", "0")]
        [InlineData("(log(1/2, 1) + pi^2) / log(x, -2)", "1", "0")]
        [InlineData("log(x ^ (-1), -1) ^ 2", "1", "-oo")]
        public void IsWhereBothSidesAgree(string input, string destination, string expected)
            => Assert.Equal(MathS.FromString(expected).Simplify(),
                MathS.FromString(input).Limit("x", destination).Simplify());

        // Where a part that depends on x is not real on one side, or the sides differ, there is still no limit.
        [Theory]
        [InlineData("i * sgn(x)", "0")]
        [InlineData("(2 + i) / x", "0")]
        [InlineData("x ^ x", "0")]
        [InlineData("x ^ i", "0")]
        [InlineData("x * ln(x)", "0")]
        [InlineData("ln(-2) / ln(x)", "1")]
        public void IsStillNaNWhereTheSidesDiffer(string input, string destination)
            => Assert.Equal(MathS.NaN, MathS.FromString(input).Limit("x", destination));
    }
}
