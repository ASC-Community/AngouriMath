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
    /// The limit of a floor or a ceil read its argument as real along the path. <c>4^((-1)^x)</c> at 1 from the right
    /// tends to 1/4 from below the real line, so that every floor near it is <c>-i</c>, and the limit was the floor of
    /// 1/4, 0. An argument that is complex just off the destination now has its imaginary part's side asked, and where
    /// that is not decided the limit is declined. An argument real there is read as before. Found by limitcheck at
    /// <c>DEPTH=3</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AStepOfAComplexArgumentNearARealLimitTest
    {
        [Theory]
        [InlineData("floor(4^((-1)^x))", "1", ApproachFrom.Right)]
        [InlineData("floor(4^((-1)^x))", "-1", ApproachFrom.Right)]
        [InlineData("ceil(4^((-1)^x))", "1", ApproachFrom.Left)]
        public void IsNotTheStepOfTheLimit(string input, string destination, ApproachFrom side)
            => Assert.IsType<Limitf>(MathS.FromString(input).Limit("x", destination, side));

        [Theory]
        [InlineData("floor(sqrt(x))", "4", ApproachFrom.Left, "1")]
        [InlineData("floor(sqrt(x))", "4", ApproachFrom.Right, "2")]
        [InlineData("floor(x^x)", "2", ApproachFrom.Left, "3")]
        [InlineData("floor(x^x)", "1", ApproachFrom.Left, "0")]
        [InlineData("floor(ln(x))", "1", ApproachFrom.Left, "-1")]
        [InlineData("ceil(x^(1/3))", "1", ApproachFrom.Right, "2")]
        public void ARealArgumentIsReadAsBefore(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));
    }
}
