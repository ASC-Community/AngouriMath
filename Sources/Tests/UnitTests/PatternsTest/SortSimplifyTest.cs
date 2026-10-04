//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.PatternsTest
{
    [Trait("Area", "PatternsTest")]
    public sealed class SortSimplifyTest
    {
        [Theory]
        [InlineData("a + x + e + d + sin(x) + c + 1 + 2 + 2a", "3 + sin(x) + 3 * a + c + d + e + x")]
        [InlineData("x + a + b + c + arcsin(x2) + d + e + 1/2 - 23 * sqrt(3) + arccos(x * x)", "1/2 + (-23) * sqrt(3) + pi / 2 + a + b + c + d + e + x")]
        // The sorting in this case was a side effect of the collapse, not independent of it:
        // arctan(x^2) + arccotan(x^2) used to become pi/2, which shortened the sum and made the
        // sorted-and-collected candidate the winner. That collapse is only valid where the
        // argument's sign is known, and x^2 is not a known non-negative for a symbolic x -- at
        // x = i it is -1 -- so the sum now stays and nothing shortens. The arcsin/arccos case
        // above still collapses, because that identity needs no assumption, and it still sorts.
        // https://github.com/asc-community/AngouriMath/issues/887
        [InlineData("x + a + b + c + arctan(x2) + d + e + 1/2 - 23 * sqrt(3) + arccot(x * x)", "x + a + b + c + arctan(x ^ 2) + d + e + 1/2 - 23 * sqrt(3) + arccotan(x ^ 2)")]
        [InlineData("a / b + c + d + e + f + sin(x) + arcsin(x) + 1 + 0 - a * (b ^ -1)", "1 + arcsin(x) + sin(x) + c + d + e + f provided not b = 0")]
        // Skipped
        // [InlineData("sin(arcsin(c x) + arccos(x c) + c)2 + a + b + sin(x) + 0 + cos(c - -arcsin(c x) - -arccos(-c x * (-1)))2", "1")]
        // [InlineData("sin(arcsin(c x) + arccos(x c) + c)2 + a + b + sin(x) + 0 + cos(c - -arcsin(c x) - -arccos(-c x * (-1)))2", ") ^ 2", false)]
        [InlineData("sec(x) + a + sin(x) + c + 1 + 0 + 3 + sec(x)", "4 + 2 * sec(x) + sin(x) + a + c")]
        [InlineData("tan(x) * a * b / c / sin(h + 0.1) * cotan(x)", "a * b / (c * sin(h + 1/10)) provided not sin(x) = 0 and not cos(x) = 0")]
        [InlineData("sin(x) * a * b / c / sin(h + 0.1) * cosec(x)", "a * b / (c * sin(h + 1/10)) provided not sin(x) = 0")]
        [InlineData("cos(x) * a * b / c / sin(h + 0.1) * sec(x)", "a * b / (c * sin(h + 1/10)) provided not cos(x) = 0")]
        [InlineData("sec(x) * a * b / c / sin(h + 0.1) * cos(x)", "a * b / (c * sin(h + 1/10)) provided not cos(x) = 0")]
        [InlineData("cosec(x) * a * b / c / sin(h + 0.1) * sin(x)", "a * b / (c * sin(h + 1/10)) provided not sin(x) = 0")]
        public void Test(string exprRaw, string result)
        {
            var entity = exprRaw.ToEntity();
            var actual = entity.Simplify(5);
            Assert.Equal(result, actual.Stringize());
        }
    }
}
