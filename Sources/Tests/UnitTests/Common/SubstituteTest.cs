//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    [Trait("Area", "Common")]
    public sealed class SubstituteTest
    {
        [Theory]
        [InlineData("x + 2", "1 + 2")]
        [InlineData("x2", "1 ^ 2")]
        [InlineData("sqrt(x)", "sqrt(1)")]
        [InlineData("x", "1")]
        [InlineData("sin(x)", "sin(1)")]
        [InlineData("{ x : x > 3 }", "{ x : x > 3 }")]
        [InlineData("x + { x : x > 3 }", "1 + { x : x > 3 }")]
        // The variable of a derivative and of an indefinite integral is free in it, so the value is
        // where it is evaluated; it was dropped, and derivative(x^3, x) at x = 1 was 3 x^2.
        // https://github.com/asc-community/AngouriMath/issues/2189
        [InlineData("x * derivative(x + 2, x, 1)", "1 * apply(lambda(x, derivative(x + 2, x)), 1)")]
        [InlineData("x * integral(x + 2, x)", "1 * apply(lambda(x, integral(x + 2, x)), 1)")]
        [InlineData("x * integral(x + 2, x, x, x)", "1 * integral(x + 2, x, 1, 1)")]
        [InlineData("x * limit(x + 2, x, 1)", "1 * limit(x + 2, x, 1)")]
        [InlineData("sin(cos(sec(csc(x))))", "sin(cos(sec(csc(1))))")]
        [InlineData("arcsin(arccos(arcsec(arccsc(x))))", "arcsin(arccos(arcsec(arccsc(1))))")] 
        [InlineData("[ i , 3 , x ]", "[ i , 3 , 1 ]")]
        [InlineData("[ [ 1, 2, x ] , [ 4, x^2, 3 ] , [ x, x, x ] ]", "[ [ 1, 2, 1 ] , [ 4, 1^2, 3 ] , [ 1, 1, 1 ] ]")]
        public void Test(string unsubstituted, string expectedRaw)
        {
            var actual = unsubstituted.Substitute("x", 1);
            var expected = expectedRaw.ToEntity();
            Assert.Equal(expected, actual);
        }

        [Fact] public void TupleSub1() => Assert.Equal("3 + 2 * 6", "x + 2y".Substitute(("x", "y"), (3, 6)));
        [Fact] public void TupleSub2() => Assert.Equal("3 + 2 * 6 + 8", "x + 2y + z".Substitute(("x", "y", "z"), (3, 6, 8)));
        // Constructed rather than written as a string: substitution leaves an unevaluated
        // Divf(8, 11), and since #873 no text spells that -- "8 / 11" parses to the Rational.
        [Fact] public void TupleSub3() => Assert.Equal(
            (Entity)3 + 2 * (Entity)6 + new Entity.Divf(8, 11),
            "x + 2y + z / d".Substitute(("x", "y", "z", "d"), (3, 6, 8, 11)));
    }
}
