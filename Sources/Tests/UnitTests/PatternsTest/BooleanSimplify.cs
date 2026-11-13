//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

/* Copyright (c) 2019-2020 Angourisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.PatternsTest
{
    [Trait("Area", "PatternsTest")]
    public sealed class BooleanSimplify
    {
        [Theory]
        [InlineData("true and true", "true")]
        [InlineData("false implies anything", "true")]
        [InlineData("false or anything", "anything")]
        [InlineData("true or anything", "true")]
        [InlineData("anything or false", "anything")]
        [InlineData("anything or true", "true")]
        [InlineData("anything or anything", "anything")]
        [InlineData("anything and anything", "anything")]
        [InlineData("anything implies anything", "true")]
        [InlineData("not a or not b", "not (a and b)")]
        [InlineData("not a and not b", "not (a or b)")]
        [InlineData("not a or b", "a implies b")]
        [InlineData("not not a", "a")]
        [InlineData("not not not a", "not a")]
        [InlineData("a and b and a", "a and b")]
        [InlineData("a and b and b", "a and b")]
        [InlineData("a and b and c and f and a", "a and b and c and f")]
        [InlineData("a and not b or b and not a", "a xor b")]
        [InlineData("not a and b or a and not b", "a xor b")]
        [InlineData("not a and b or not b and a", "a xor b")]
        [InlineData("a and not b or not a and b", "a xor b")]
        [InlineData("b xor a xor c xor d", "a xor b xor c xor d")]
        public void Test(string expr, string expected)
        {
            var exp = expected.ToEntity();
            var act = expr.ToEntity();
            Assert.Equal(exp, act.Simplify());
            
        }

        [Theory]
        [InlineData("0 < x",  "x > 0")]
        [InlineData("0 <= x", "x >= 0")]
        [InlineData("0 > x",  "x < 0")]
        [InlineData("0 >= x", "x <= 0")]
        [InlineData("0 = x", "x = 0")]

        [InlineData("2 * x > 0", "x > 0")]
        [InlineData("2 * x < 0", "x < 0")]
        [InlineData("2 * x >= 0", "x >= 0")]
        [InlineData("2 * x <= 0", "x <= 0")]

        [InlineData("x * 2 > 0", "x > 0")]
        [InlineData("x * 2 < 0", "x < 0")]
        [InlineData("x * 2 >= 0", "x >= 0")]
        [InlineData("x * 2 <= 0", "x <= 0")]

        [InlineData("-2 * x > 0", "x < 0")]
        [InlineData("-2 * x < 0", "x > 0")]
        [InlineData("-2 * x >= 0", "x <= 0")]
        [InlineData("-2 * x <= 0", "x >= 0")]

        [InlineData("x * (-2) > 0", "x < 0")]
        [InlineData("x * (-2) < 0", "x > 0")]
        [InlineData("x * (-2) >= 0", "x <= 0")]
        [InlineData("x * (-2) <= 0", "x >= 0")]

        [InlineData("x / 2 > 0", "x > 0")]
        [InlineData("x / 2 < 0", "x < 0")]
        [InlineData("x / 2 >= 0", "x >= 0")]
        [InlineData("x / 2 <= 0", "x <= 0")]

        [InlineData("x / (-2) > 0", "x < 0")]
        [InlineData("x / (-2) < 0", "x > 0")]
        [InlineData("x / (-2) >= 0", "x <= 0")]
        [InlineData("x / (-2) <= 0", "x >= 0")]

        [InlineData("0 > 2 * x", "x < 0")]
        [InlineData("0 < 2 * x", "x > 0")]
        [InlineData("0 >= 2 * x", "x <= 0")]
        [InlineData("0 <= 2 * x", "x >= 0")]
        [InlineData("x ^ 2 = 0", "x = 0")]
        [InlineData("x ^ 0 = 0", "false provided not x = 0")] // The 'provided' describes the domain where the expression is valid (it is 'undefined' outside). It is incorrect to drop the predicate.
        [InlineData("x ^ -1 = 0", "false provided not x = 0")]
        [InlineData("x ^ -2 = 0", "false provided not x = 0")]
        public void TestInequality(string expr, string expected)
        {
            var exp = expected.ToEntity();
            var act = expr.ToEntity();
            Assert.Equal(exp, act.Simplify());
        }

        // Transitivity is decided where the operands are ordered, which a symbol is over the
        // reals. Over the complex plane x > y > z -> x > z need not have a truth value, and is
        // left as written: see AChainOfComparisonsIsTransitiveWhereOrderedTest.
        [Theory]
        [InlineData("x > y > z -> x > z", "true")]
        [InlineData("x < y < z -> x < z", "true")]
        public void TestTransitivityOverTheReals(string expr, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            Assert.Equal(expected.ToEntity(), expr.ToEntity().Simplify());
        }
        [Theory]
        // InnerSimplified patterns
        [InlineData("false provided false", null)]
        [InlineData("true provided true", "true")]
        [InlineData("true provided false", null)]
        [InlineData("false provided true", "false")]
        [InlineData("(x provided y) provided (true provided z)", "x provided y and z")]
        // Patterns.LiftProvided
        [InlineData("x provided y^2 = 0", "x provided y = 0")]
        [InlineData("x provided (y provided z) + 1 = 0", "x provided 1 + y = 0 and z")]
        [InlineData("{ x provided (y provided z) + 1 = 0, y provided k } provided (g provided w)", "{ x provided 1 + y = 0 and z, y provided k } provided g and w")]
        public void TestProvided(string expr, string? expected)
        {
            var exp = expected is { } ? expected.ToEntity() : MathS.NaN;
            var act = expr.ToEntity();
            Assert.Equal(exp, act.Simplify());
        }
        [Theory]
        [InlineData("not (not x)", "x")]
        [InlineData("not (a = b and b = c)", "not a = b or not b = c")]
        [InlineData("not (a <= b and b < c)", "a > b or b >= c")]
        [InlineData("not (a > b and b >= c)", "a <= b or b < c")]
        [InlineData("not (a <= b and b <= c and 1)", "not (1 and a <= b and b <= c)")]
        [InlineData("not (a = b or b = c)", "not a = b and not b = c")]
        [InlineData("not (a <= b or b < c)", "a > b and b >= c")]
        [InlineData("not (a > b or b >= c)", "a <= b and b < c")]
        [InlineData("not (a <= b or b <= c or 1)", "not (1 or a <= b or b <= c)")]
        [InlineData("not (a < b or c > d and e = f)", "a >= b and (c <= d or not e = f)")]
        [InlineData("not (not (a > b and b >= c) and y)", "a > b and b >= c or not y")]
        [InlineData("not (not (a = b and b = c) and y)", "a = b and b = c or not y")]
        [InlineData("not (not (not a = b and not b = c) and y)", "not ((a = b or b = c) and y)")]
        [InlineData("not (a < b and b < c and not c = d and not d and not (e and not (f or g = h)))", "a >= b or b >= c or c = d or d or e and not f and not g = h")]
        public void NestedNot(string expr, string expected)
        {
            var exp = expected.ToEntity();
            var act = expr.ToEntity();
            Assert.Equal(exp, act.Simplify());
        }
    }
}
