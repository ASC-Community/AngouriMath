//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The tables of exact trigonometric values matched an argument within an absolute 1e-16 of a table
    /// entry, so <c>sin(pi/6 + 10^(-20))</c> simplified to exactly <c>1/2</c>, <c>tan(pi/4 + 10^(-18))</c> to
    /// 1 and <c>arcsin(1/2 + 10^(-20))</c> to <c>pi/6</c>. They match to all but ten of the working digits,
    /// relative to the argument, which is what evaluating an exact one leaves. Found while looking for more
    /// absolute tolerances; no issue was filed. https://github.com/asc-community/AngouriMath/issues/1944
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ATrigonometricTableValueIsOnlyForItsArgumentTest
    {
        [Theory]
        [InlineData("sin(pi/6 + 10^(-20))", "1/2")]
        [InlineData("tan(pi/4 + 10^(-18))", "1")]
        [InlineData("cos(pi/3 - 10^(-30))", "1/2")]
        [InlineData("sin(10^(-20))", "0")]
        [InlineData("arcsin(1/2 + 10^(-20))", "pi/6")]
        [InlineData("arctan(1 + 10^(-20))", "pi/4")]
        public void ANearbyArgumentIsNotTheTableOne(string expression, string tableValue)
            => Assert.NotEqual(tableValue.ToEntity().Simplify(), expression.ToEntity().Simplify());

        [Theory]
        // Controls: the table's own arguments, also written so that evaluating them rounds.
        [InlineData("sin(pi/6)", "1/2")]
        [InlineData("tan(pi/4)", "1")]
        [InlineData("cos(2 * pi/3)", "-1/2")]
        [InlineData("sin(pi/6 + 2 * pi)", "1/2")]
        [InlineData("arcsin(1/2)", "pi/6")]
        [InlineData("arctan(sqrt(3))", "pi/3")]
        [InlineData("arccos(-sqrt(2)/2)", "3/4 * pi")]
        public void ATableArgumentIsAnswered(string expression, string tableValue)
            => Assert.Equal(tableValue.ToEntity().Simplify(), expression.ToEntity().Simplify());
    }
}
