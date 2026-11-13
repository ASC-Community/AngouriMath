//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using AngouriMath.Core;
using static AngouriMath.Entity.Number;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    [Trait("Area", "Calculus")]
    public sealed class Limits
    {
        void TestLimit(Entity expr, Entity where, ApproachFrom appr, Entity desiredOutput)
            => Assert.Equal(desiredOutput.Simplify(), expr.Limit("x", where, appr).Simplify());

        void TestLimit(Entity expr, Entity where, Entity desiredOutput)
            => Assert.Equal(desiredOutput.Simplify(), expr.Limit("x", where).Simplify());

        [Theory]
        [InlineData("x", "+oo", "+oo", ApproachFrom.Left)]
        [InlineData("-x", "+oo", "-oo", ApproachFrom.Left)]
        [InlineData("x / (2x + 1)", "+oo", "1/2", ApproachFrom.Left)]
        [InlineData("x / (a * x + 1)", "+oo", "1/a", ApproachFrom.Left)]
        [InlineData("(x2 + x) / x", "+oo", "+oo", ApproachFrom.Left)]
        [InlineData("(x2 + x) / (a * x)", "+oo", "+oo / a", ApproachFrom.Left)]
        [InlineData("(x + x) / (a * x2)", "+oo", "0", ApproachFrom.Left)]
        [InlineData("(x^3 + a*x^2)/(b*x^2 + 1)", "a", "2 a3 / (1 + a2 * b)", ApproachFrom.Left)]
        [InlineData("x / a", "1", "1 / a", ApproachFrom.Left)]
        // [InlineData("sin(x) / x", "0", "1", ApproachFrom.Left)]
        // [InlineData("a * sin(x) / x", "0", "1", ApproachFrom.Left)]
        [InlineData("a x", "+oo", "+oo * a", ApproachFrom.Left)]
        [InlineData("a x2 + b / x", "+oo", "+oo * a", ApproachFrom.Left)]
        [InlineData("a x ^ (-2) + b / x", "+oo", "0", ApproachFrom.Left)]
        [InlineData("1 / (x^2 + x)", "-1", "+oo", ApproachFrom.Left)]
        [InlineData("1 / x", "0", "-oo", ApproachFrom.Left)]
        [InlineData("1 / x", "0", "+oo", ApproachFrom.Right)]
        [InlineData("ln(x)", "0", "-oo", ApproachFrom.Right)]
        [InlineData("ln(x)", "0", "-oo", ApproachFrom.Left)]
        [InlineData("ln(x)", "+oo", "+oo", ApproachFrom.Left)]
        [InlineData("ln(x)", "-oo", "+oo", ApproachFrom.Right)]
        [InlineData("ln(1 / (x - a))", "a", "+oo", ApproachFrom.Right)]
        [InlineData("log(b, x)", "0", "-oo / ln(b)", ApproachFrom.Right)]
        [InlineData("ln(x^4 - x^2)", "+oo", "+oo", ApproachFrom.Left)]
        [InlineData("ln((x - 1) / (2x + 1))", "+oo", "ln(0.5)", ApproachFrom.Right)]
        [InlineData("log(x, x^2)", "0", "2", ApproachFrom.Right)]
        [InlineData("log(1/x, x^2)", "0", "-2", ApproachFrom.Right)]
        [InlineData("log(x, x^(-2))", "0", "-2", ApproachFrom.Right)]
        [InlineData("log(1/x, x^(-2))", "0", "2", ApproachFrom.Right)]
        [InlineData("log(x, x)", "0", "1", ApproachFrom.Right)]
        [InlineData("ln(ln((e^2*x + t) / (x + 1)))", "+oo", "ln(2)", ApproachFrom.Left)]
        [InlineData("log((2x - 1)/(x + 1), (x - 1)/(2x - 1))", "+oo", "-1", ApproachFrom.Left)]
        [InlineData("(x + 3) / (x + 4)", "+oo", "1", ApproachFrom.BothSides)]
        [InlineData("log(x3, x)", "+oo", "1/3")]
        [InlineData("log(x3, x7)", "+oo", "7/3")]
        [InlineData("log(x3, x ^ a)", "+oo", "a / 3")]
        [InlineData("log(x ^ b, x ^ a)", "+oo", "a / b")]
        public void TestGeneral(string exprRaw, string destRaw, string expectedRaw, ApproachFrom appr = ApproachFrom.BothSides)
            => TestLimit(exprRaw, destRaw, appr, expectedRaw);

        [Theory]
        [InlineData("sin(x) / x", "1")]
        [InlineData("sin(2x) / x", "2")]
        [InlineData("sin(a x) / x", "a")]
        [InlineData("sin(a x) / (a x)", "1")]
        [InlineData("tan(x) / x", "1")]
        [InlineData("tan(2x) / x", "2")]
        [InlineData("tan(a x) / x", "a")]
        [InlineData("tan(a x) / (a x)", "1")]
        [InlineData("arcsin(x) / x", "1")]
        [InlineData("arcsin(2x) / x", "2")]
        [InlineData("arcsin(a x) / x", "a")]
        [InlineData("arcsin(a x) / (a x)", "1")]
        [InlineData("arctan(x) / x", "1")]
        [InlineData("arctan(2x) / x", "2")]
        [InlineData("arctan(a x) / x", "a")]
        [InlineData("arctan(a x) / (a x)", "1")]
        [InlineData("(tan(a x) - sin(b x)) / (a x)", "(a - b) / a")]
        [InlineData("sin(a^x - 1) / tan(b^x - 1)", "ln(a) / ln(b) provided not (a = 0 or b = 0)")]
        [InlineData("(1 - cos(x)) / x2", "1/2")]
        [InlineData("((1 + x)^4 - 1) / x", "4")]
        [InlineData("((1 + x)^a - 1) / x", "a")]
        [InlineData("((1 + c x2 + b x)^a - 1)", "0")]
        [InlineData("sin(x - a * x) / tan(b * x - x)", "(a - 1) / (1 - b)")]
        [InlineData("sin(x2) / sin(x)", "0")]
        // [InlineData("(sin(x) - tan(a x)) / (sin(b x) - tan(x))", "(a - 1) / (1 - b)", Skip = "Equivalence table needs improvement")]
        [InlineData("sec(x)", "1")]
        // [InlineData("a x * csc(b x)", "a / b", Skip = "Equivalence table needs improvement")]
        [InlineData("(arccos(x) - pi / 2) / x", "-1")]
        [InlineData("arccsc(1 / x)", "0")]
        [InlineData("arcsec(1 / x) - pi / 2", "0")]
        public void TestEquivalenceTableTo0(string input, string expected)
        {
            var limit = input.ToEntity();
            var exp = expected.Simplify();
            var actual = limit.Limit("x", 0).Simplify();
            Assert.Equal(exp, actual);
        }

        [Theory]
        [InlineData("(1 + 1/x)^x", "e")]
        [InlineData("(1 + 1/(2x))^x", "sqrt(e)")]
        [InlineData("(1 + 1/x)^(2x)", "e2")]
        [InlineData("(1 + 2/x)^x", "e2")]
        [InlineData("(1 + a/x)^x", "e^a")]
        [InlineData("(1 + a/x)^(b x)", "e^(a * b)")]
        [InlineData("(1 + a/(2x))^(b x + c x)", "e^(a * (b + c) / 2)")]
        // [InlineData("e^x / x", "+oo", Skip = "how do we do it")]
        public void TestEquivalenceTableToInfinity(string input, string expected)
        {
            var limit = input.ToEntity();
            var exp = expected.Simplify();
            var actual = limit.Limit("x", "+oo").Simplify();
            Assert.Equal(exp, actual);
        }

        [Fact]
        public void TestComplicated()
        {
            Entity subExpr = "(a * x2 + b x) / (c x2 - 3)";
            Entity expr = MathS.Sqrt(subExpr * 3 / MathS.Sin(subExpr) + MathS.Sin("d"));
            Entity.Variable x = nameof(x);
            Entity dest = Real.PositiveInfinity;
            var limit = expr.Limit(x, dest, ApproachFrom.Left);
            Assert.NotNull(limit);
            Assert.Equal("sqrt(a / c * 3 / sin(a / c) + sin(d))", limit?.Stringize());
        }
        [Theory]
        [InlineData("limit(1/x,x,0)")]
        [InlineData("limit(1/x^3,x,0)")]
        [InlineData("limit(x^x,x,0)")]
        public void TestNoLimit(string input) // a two-sided limit does not exist
        {
            var limit = input.ToEntity();
            Assert.Equal(MathS.NaN, limit.InnerSimplified);
            Assert.Equal(MathS.NaN, limit.Evaled);
        }
        [Theory]
        [InlineData("limit(apply(f, x),x,-1)")]
        [InlineData("limit(x!,x,-1)")]
        public void TestCantSolve(string input) // this is different from no limit
        {
            var limit = input.ToEntity();
            Assert.Equal(input, limit.InnerSimplified);
            Assert.Equal(input, limit.Evaled);
        }
    }
}