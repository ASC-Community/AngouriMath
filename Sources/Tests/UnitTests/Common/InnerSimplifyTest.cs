//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using System.Linq;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    [Trait("Area", "Common")]
    public class InnerSimplifyTest
    {
        [Theory]
        [InlineData("ln(abs(x))")]
        [InlineData("ln(abs(x)) + 1")]
        [InlineData("ln(abs(x)) - 1")]
        [InlineData("2 * ln(abs(x))")]
        [InlineData("ln(abs(x))^2")]
        // Both are 0 at x = 0, since sgn(0) is 0, so neither collapses to 1 any more.
        // https://github.com/asc-community/AngouriMath/issues/892
        [InlineData("signum(abs(x))")]
        [InlineData("abs(signum(x))")]
        [InlineData("signum(abs(x/x))")]
        [InlineData("abs(signum(x/x))")]
        public void ShouldNotChangeTest(string expr)
        {
            var expected = expr.ToEntity();
            var actual = expected.InnerSimplified;
            Assert.Same(expected, actual);
        }
        [Theory(Skip = "Moved to the 1.2.2 milestone, see issue here https://github.com/asc-community/AngouriMath/issues/263")]
        [InlineData("3 ^ 100")]
        [InlineData("(-3) ^ 100")]
        [InlineData("0.01 ^ 100")]
        [InlineData("integral((4x^2+5x-4)/((5x-2)(4x^2+2)), x)")]
        public void ShouldNotChangeTestTodo(string expr)
        {
            var expected = expr.ToEntity();
            var actual = expr.ToEntity().InnerSimplified;
            Assert.Same(expected, actual);
        }

        [Theory]
        [InlineData("a provided b", "a provided b")]
        [InlineData("a provided (b provided c)", "a provided c and b")]
        [InlineData("(a provided (b provided c)) + 1 + x + (y provided h)", "(a + 1 + x + y) provided (c and b and h)")]
        [InlineData("a provided 0 = 0", "a")]
        [InlineData("a provided 0 = 1", "NaN")]
        [InlineData("a provided b provided c provided d", "a provided d and c and b")]
        [InlineData(@"[ { sqrt(3), sqrt(5) }; sqrt(10) ]", @"{ [ sqrt(3); sqrt(10) ], [ sqrt(5); sqrt(10) ] }")]
        [InlineData(@"[ sqrt(3); { sqrt(5), sqrt(10) } ]", @"{ [ sqrt(3); sqrt(5) ], [ sqrt(3); sqrt(10) ] }")]
        [InlineData(@"[ { sqrt(2), sqrt(3) }; { sqrt(5), sqrt(10) } ]", @"{ [ sqrt(2); sqrt(5) ], [ sqrt(3); sqrt(5) ], [ sqrt(2); sqrt(10) ], [ sqrt(3); sqrt(10) ] }")]
        [InlineData(@"piecewise((a provided true),  (b provided false), (c provided false))",    "a")]
        [InlineData(@"piecewise((a provided true),  (b provided true),  (c provided false))",    "a")]
        [InlineData(@"piecewise((a provided true),  (b provided false), (c provided true))",     "a")]
        [InlineData(@"piecewise((a provided false), (b provided false), (c provided false))",    "NaN")]
        [InlineData(@"piecewise((a provided false), (b provided true),  (c provided false))",    "b")]
        [InlineData(@"piecewise((a provided false), (b provided false), (c provided false), d)", "d")]
        [InlineData(@"piecewise((a provided false), (c provided f),     d)",                     "piecewise((c provided f), d)")]
        [InlineData(@"piecewise((a provided false), (c provided f),     (g provided false), d)",         "piecewise((c provided f), d)")]
        [InlineData(@"piecewise((a provided false), (c provided true),  (g provided k),     d)", "c")]
        [InlineData(@"piecewise((a provided h),     (c provided true),  (g provided k),     d)", "piecewise((a provided h), c)")]

        [InlineData("a and true", "a")]
        [InlineData("a and false", "false")]
        [InlineData("true and a", "a")]
        [InlineData("false and a", "false")]

        [InlineData("a or true", "true")]
        [InlineData("a or false", "a")]
        [InlineData("true or a", "true")]
        [InlineData("false or a", "a")]

        [InlineData("a xor true", "not a")]
        [InlineData("a xor false", "a")]
        [InlineData("true xor a", "not a")]
        [InlineData("false xor a", "a")]

        [InlineData("a implies true", "true")]
        [InlineData("a implies false", "not a")]
        [InlineData("true implies a", "a")]
        [InlineData("false implies a", "true")]

        [InlineData("abs(abs(x))", "abs(x)")]
        [InlineData("signum(signum(x))", "signum(x)")]
        // signum(abs(x)) and abs(signum(x)) asserted 1 and are 0 at x = 0
        // (https://github.com/asc-community/AngouriMath/issues/892), so they are now left as
        // written and live in ShouldNotChange below.
        //
        // The x/x pair went with them, and that is a coverage loss rather than a correction:
        // x/x is nonzero everywhere it is defined, so 1 provided not x = 0 was right for it.
        // Telling the two apart needs "nonzero throughout its domain", and the only tests
        // available here are the argument's Evaled -- which leaves x/x as x/x -- and its
        // DomainCondition, which is what conflated them in the first place. Simplify would
        // answer it and cannot be called from inside InnerSimplify (#403).

        [InlineData("log(10, x) * log(10, x)", "log(10, x)^2")]
        // A quotient by minus one is the negation, so the root of `b - x = 0`, which the
        // linear solver writes as `-b / (-1)`, is `b`.
        [InlineData("-b / (-1)", "b")]
        [InlineData("(a + 1) / (-1)", "-(a + 1)")]
        public void ShouldChangeTo(string from, string to)
        {
            var expected = to.ToEntity().Replace(c => c == "NaN" ? MathS.NaN : c);
            var fromEntity = from.ToEntity();
            var actualInnerSimplified = fromEntity.InnerSimplified;
            var actualInnerEvaled = from.ToEntity().Evaled;
            Assert.Equal(expected, actualInnerSimplified);
            Assert.Equal(expected, actualInnerEvaled);
        }

        [Theory, CombinatorialData]
        public void PiecewiseInnerSimplifyOneArgumentFunction(
            [CombinatorialValues("arcsin", "arccos", "sin", "cos", 
            "tan", "cotan", "arctan", "arccotan", "sec", "cosec",
            "arcsec", "arccosec")]
            string func)
        {
            Entity initial = @$"{func}(piecewise(a provided b, c provided d, e provided f))";
            Entity expected = @$"piecewise(({func}(a) provided b), ({func}(c) provided d), ({func}(e) provided f))";
            var actual = initial.InnerSimplified;
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("", " + 3")]
        [InlineData("", " - 3")]
        [InlineData("", " * 3")]
        [InlineData("", " / 3")]
        [InlineData("log(", ", 3)")]
        [InlineData("log(", ", 4)")]
        [InlineData("log(3, ", ")")]
        [InlineData("log(4, ", ")")]
        public void PiecewiseInnerSimplifyTwoArgumentFunction(string before, string after)
        {
            Entity initial = @$"{before}piecewise((a provided b), (c provided d), (e provided f)){after}";
            Entity expected = @$"piecewise(({before}a{after} provided b), ({before}c{after} provided d), ({before}e{after} provided f))";
            var actual = initial.InnerSimplified;
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("", " + ")]
        [InlineData("", " - ")]
        [InlineData("", " * ")]
        [InlineData("", " / ")]
        [InlineData("", " ^ ")]
        [InlineData("log", ", ")]
        public void PiecewiseAndPiecewise(string before, string inBetween)
        {
            var rawInitial = @$"{before}(piecewise(a provided b, c provided d) {inBetween} piecewise(k provided f, g provided j))";
            var rawExpected = @$"piecewise({before}(a {inBetween} k) provided b and f, {before}(a {inBetween} g) provided b and j, {before}(c {inBetween} k) provided d and f, {before}(c {inBetween} g) provided d and j)";
            var initial = (Entity)rawInitial;
            var expected = (Entity)rawExpected;
            var actual = initial.InnerSimplified;
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("[ 1 , 3 , 5 ] * 3", "[ 3 , 9 , 15 ]")]
        [InlineData("3 * [ 1 , 3 , 5 ]", "[ 3 , 9 , 15 ]")]
        [InlineData("[ 1 , 3 , 5 ] + 3", "[ 4 , 6 , 8 ]")]
        [InlineData("3 + [ 1 , 3 , 5 ]", "[ 4 , 6 , 8 ]")]
        [InlineData("[ 1 , 3 , 5 ] - 3", "[ -2 , 0 , 2 ]")]
        [InlineData("3 - [ 1 , 3 , 5 ]", "[ 2 , 0 , -2 ]")]
        [InlineData("[ 6 , 3 , 18 ] / 3", "[ 2 , 1 , 6 ]")]
        [InlineData("[ [ 1, 0, 0 ] , [ 0, 1, 0 ] , [ 0, 0, 1 ] ] * [ 6 , 3 , 18 ]", "[ 6 , 3 , 18 ]")]
        [InlineData("[ 1 ]", "1")]
        [InlineData("[ [1] ]", "1")]
        [InlineData("[ 1, 2, 3 ]T * [ a , b , c ]", "a + 2b + 3c")]
        [InlineData("[[1, 0], [0, 1]] ^ 12", "[[1, 0], [0, 1]]")]
        [InlineData("(|[3, 4]|)", "5")]
        [InlineData("(|[2, 3, 6]|)", "7")]
        public void TestMatrices(string before, string after)
            => ShouldChangeTo(before, after);
        [Fact] public void DefiniteIntegralSimplify1() =>
            "integral(apply(f, x), x, a, b)".ToEntity().InnerSimplified.ShouldBe("integral(apply(f, x), x, a, b)");
        [Fact] public void DefiniteIntegralSimplify2() =>
            "integral(sin(x), x, a, b)".ToEntity().InnerSimplified.ShouldBe("-cos(b)--cos(a)");
        [Fact] public void DefiniteIntegralEval1() =>
            "integral(apply(f, x), x, a, b)".ToEntity().Evaled.ShouldBe("integral(apply(f, x), x, a, b)");
        [Fact] public void DefiniteIntegralEval2() =>
            "integral(sin(x), x, a, b)".ToEntity().Evaled.ShouldBe("-cos(b)--cos(a)");
        [Fact] public void DerivativeToIntegralSimplify() =>
            "derivative(apply(f, x), x, -1)".ToEntity().InnerSimplified.ShouldBe("integral(apply(f, x), x)");
        [Fact] public void DerivativeToIntegralSimplify2() =>
            "derivative(apply(f, x), x, -2)".ToEntity().InnerSimplified.ShouldBe("integral(integral(apply(f, x), x), x)");
        [Fact] public void DerivativeToIntegralEval() =>
            "derivative(apply(f, x) + (sin(3)^2 + cos(3)^2), x, -1)".ToEntity().Evaled.ShouldBe("integral(apply(f, x) + 1, x)");
        // The multiplication is distributed into the branches now that Integrate
        // inner-simplifies its answer (https://github.com/asc-community/AngouriMath/issues/772).
        // Same value, and it is the form the library already produced for the same
        // question asked through the node -- see PiecewiseIntegrate1NodeInnerSimplified
        // below, which has always expected `piecewise(2 * x + C provided a, ...)`. The two
        // paths disagreed; the old expectation here was pinning that disagreement.
        [Fact] public void PiecewiseIntegrate1() =>
            "piecewise(2 provided a, 3)".Integrate("x")
            .ShouldBe("piecewise(2 * x provided a, 3 * x) + C");

        [Fact] public void PiecewiseIntegrate2() =>
            "piecewise(2 provided a, 3)".Integrate("d")
            .ShouldBe("piecewise(2 * d provided a, 3 * d) + C");

        [Fact] public void PiecewiseIntegrate3() =>
            "piecewise(x provided a, 1/x)".Integrate("x")
            .ShouldBe("integral(piecewise(x provided a, 1/x), x)"); // evaluation requires inner simplification!

        [Fact] public void PiecewiseDerivative1() =>
            "piecewise(x provided a, 1/x)".Differentiate("x")
            .ShouldBe("piecewise(1 provided a, ((-1)) / ((x) ^ (2)))");

        // The two cases carried one expression and merged into a single case guarded
        // by either predicate when #327 landed -- which is what the note that used to
        // sit here predicted. The predicate was `a or not x^2 = 0`, the second half the
        // quotient rule's denominator for 1/x, which does not vary in y: its derivative is
        // 0 with no condition, so the merged case always holds.
        [Fact] public void PiecewiseDerivative2() {
            var result = "piecewise(x provided a, 1/x)".Differentiate("y");
            result.ShouldBe("piecewise(0 provided True)");
            result.Simplify().ShouldBe("0");
        }

        [Fact] public void PiecewiseLimit1() =>
            "piecewise(sin(a x) / (b x) provided a, sin(b x) / sin(a x))".Limit("x", "0")
            .ShouldBe("piecewise(a / b provided a, b / a)");




        [Fact] public void PiecewiseIntegrate1NodeEvaled() =>
            "integral(piecewise(2 provided a, 3), x)".ToEntity().Evaled
            .ShouldBe("piecewise(2x + C provided a, 3x + C)");

        [Fact] public void PiecewiseIntegrate2NodeEvaled() =>
            "integral(piecewise(2 provided a, 3), d)".ToEntity().Evaled
            .ShouldBe("piecewise(2d + C provided a, 3d + C)");

        [Fact] public void PiecewiseIntegrate3NodeEvaled() =>
            "integral(piecewise(x provided a, 1/x), x)".ToEntity().Evaled
            .ShouldBe("piecewise(x ^ 2 / 2 + C provided a, ln(x) + C)".ToEntity().Evaled);

        [Fact] public void PiecewiseDerivative1NodeEvaled() =>
            "derivative(piecewise(x provided a, 1/x), x)".ToEntity().Evaled
            .ShouldBe("piecewise(1 provided a, ((-1)) / ((x) ^ (2)))");

        // The two cases carried one expression and merged into a single case guarded
        // by either predicate when #327 landed -- which is what the note that used to
        // sit here predicted. The predicate was `a or not x^2 = 0`, the second half the
        // quotient rule's denominator for 1/x, which does not vary in y: its derivative is
        // 0 with no condition, so the merged case always holds.
        [Fact] public void PiecewiseDerivative2NodeEvaled()
        {
            var result = "derivative(piecewise(x provided a, 1/x), y)".ToEntity().Evaled;
            result.ShouldBe("piecewise(0 provided True)");
            result.Simplify().ShouldBe("0");
        }

        [Fact] public void PiecewiseLimit1NodeEvaled() =>
            "limit(piecewise(sin(a x) / (b x) provided a, sin(b x) / sin(a x)), x, 0)".ToEntity().Evaled
            .ShouldBe("piecewise(a / b provided a, b / a)");




        [Fact] public void PiecewiseIntegrate1NodeInnerSimplified() =>
            "integral(piecewise(2 provided a, 3), x)".ToEntity().InnerSimplified
            .ShouldBe("piecewise(2 * x + C provided a, 3 * x + C)");

        [Fact] public void PiecewiseIntegrate2NodeInnerSimplified() =>
            "integral(piecewise(2 provided a, 3), d)".ToEntity().InnerSimplified
            .ShouldBe("piecewise(2 * d + C provided a, 3 * d + C)");

        [Fact] public void PiecewiseIntegrate3NodeInnerSimplified() =>
            "integral(piecewise(x provided a, 1/x), x)".ToEntity().InnerSimplified
            .ShouldBe("piecewise(x ^ 2 / 2 + C provided a, ln(x) + C)");

        [Fact] public void PiecewiseDerivative1NodeInnerSimplified() =>
            "derivative(piecewise(x provided a, 1/x), x)".ToEntity().InnerSimplified
            .ShouldBe("piecewise(1 provided a, ((-1)) / ((x) ^ (2)))");

        // The two cases carried one expression and merged into a single case guarded
        // by either predicate when #327 landed -- which is what the note that used to
        // sit here predicted. The predicate was `a or not x^2 = 0`, the second half the
        // quotient rule's denominator for 1/x, which does not vary in y: its derivative is
        // 0 with no condition, so the merged case always holds.
        [Fact] public void PiecewiseDerivative2NodeInnerSimplified()
        {
            var result = "derivative(piecewise(x provided a, 1/x), y)".ToEntity().InnerSimplified;
            result.ShouldBe("piecewise(0 provided True)");
            result.Simplify().ShouldBe("0");
        }

        [Fact] public void PiecewiseLimit1NodeInnerSimplified() =>
            "limit(piecewise(sin(a x) / (b x) provided a, sin(b x) / sin(a x)), x, 0)".ToEntity().InnerSimplified
            .ShouldBe("piecewise(a / b provided a, b / a)");

        [Theory]
        [InlineData("pi", "1")]
        [InlineData("2pi", "2 * 1")]
        [InlineData("pi * 2", "1 * 2")]
        [InlineData("1 / (2 / pi)", "1 / (2 / 1)")]
        [InlineData("1 / (2 / (3 / (4 / pi)))", "1 / (2 / (3 / (4 / 1)))")]
        [InlineData("pi * pi", "1 * pi")]
        public void TestDivideByEntityStrict(string inputRaw, string expectedRaw)
        {
            Entity input = inputRaw;
            var actual = MathS.UnsafeAndInternal.DivideByEntityStrict(input, "pi");
            actual.ShouldBeNotNull().ShouldBe(expectedRaw);
        }

        /// <summary>
        /// The rows whose expectation is an <b>unevaluated</b> quotient of two integers, which
        /// since https://github.com/asc-community/AngouriMath/issues/873 can no longer be written
        /// as text: <c>"1 / 2"</c> now parses to the <c>Rational</c> it denotes, so the string no
        /// longer names the tree this operation produces.
        /// </summary>
        /// <remarks>
        /// Constructed rather than parsed, and kept as its own test rather than folded into the
        /// theory above, because the reason they are different is the point. Comparing
        /// <c>InnerSimplified</c> on both sides would make them pass and would stop the test being
        /// about strictness, which is what it is named for.
        /// </remarks>
        [Fact]
        public void TestDivideByEntityStrictOnUnevaluatedQuotients()
        {
            static Entity Half() => new Entity.Divf(1, 2);
            static void Check(string input, Entity expected) =>
                MathS.UnsafeAndInternal.DivideByEntityStrict(input, "pi").ShouldBeNotNull().ShouldBe(expected);

            Check("pi / 2", Half());
            Check("pi + pi / 2", 1 + Half());
            Check("pi / 2 + pi / 3", Half() + new Entity.Divf(1, 3));
            Check("pi / 2 - 2 pi", Half() - 2 * (Entity)1);
            Check("pi / 2 - pi * 2", Half() - (Entity)1 * 2);
            Check("1 / (2 / pi) + pi / 2", new Entity.Divf(1, new Entity.Divf(2, 1)) + Half());
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1 + pi")]
        [InlineData("1 / pi")]
        [InlineData("2 - pi")]
        [InlineData("pi ^ 2")]
        [InlineData("sin(pi)")]
        public void TestDivideByEntityStrictShouldBeNull(string inputRaw)
        {
            Entity input = inputRaw;
            MathS.UnsafeAndInternal.DivideByEntityStrict(input, "pi").ShouldBeNull();
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("1/3", 1)]
        [InlineData("1/6", 1)]
        [InlineData("2/3", 1)]
        [InlineData("1/2 + 1/3", 2)]
        [InlineData("19", 1)]
        [InlineData("3/2 + 1/3", 2)]
        [InlineData("3/2 + 1/3 + 4/17", -1 /* current algo cannot take this */)]
        [InlineData("3/2 + 1/3 + 1/19", -1)]
        [InlineData("37/17", 2)]
        public void TestRepresentRational(string ratRaw, int countOfForms)
        {
            var rat = (Rational)ratRaw.ToEntity().InnerSimplified;

            var forms = new[] { "1/2", "1/3", "1/6", "1/17" }.Select(c => (Rational)c.ToEntity().InnerSimplified);

            var repr = MathS.UnsafeAndInternal.RepresentRational(rat, forms);

            if (countOfForms is -1)
                repr.ShouldBeNull();
            else
            {
                Rational res = 0;
                foreach (var (coef, form) in repr.ShouldBeNotNull().ShouldCountTo(countOfForms))
                    res += coef * form;
                res.ShouldBe(rat);
            }
        }
        
        [Theory]
        [InlineData("pi / 2")]
        [InlineData("-pi / 2")]
        [InlineData("-5pi / 2")]
        [InlineData("-9pi / 2")]
        [InlineData("9pi / 2")]
        [InlineData("5pi / 2")]
        public void TanShouldBeNaN(string angle)
        {
            MathS.Tan(angle).Evaled.ShouldBe(MathS.NaN);
            MathS.Tan(angle).InnerSimplified.ShouldBe(MathS.NaN);
        }

        [Theory]
        [InlineData("pi / 2")]
        [InlineData("-pi / 2")]
        [InlineData("-5pi / 2")]
        [InlineData("-9pi / 2")]
        [InlineData("9pi / 2")]
        [InlineData("5pi / 2")]
        public void SecShouldBeNaN(string angle)
        {
            MathS.Sec(angle).Evaled.ShouldBe(MathS.NaN);
            MathS.Sec(angle).InnerSimplified.ShouldBe(MathS.NaN);
        }

        [Theory]
        [InlineData("pi")]
        [InlineData("-pi")]
        [InlineData("-5pi")]
        [InlineData("-9pi")]
        [InlineData("9pi")]
        [InlineData("5pi")]
        public void CotanShouldBeNaN(string angle)
        {
            MathS.Cotan(angle).Evaled.ShouldBe(MathS.NaN);
            MathS.Cotan(angle).InnerSimplified.ShouldBe(MathS.NaN);
        }

        [Theory]
        [InlineData("pi")]
        [InlineData("-pi")]
        [InlineData("-5pi")]
        [InlineData("-9pi")]
        [InlineData("9pi")]
        [InlineData("5pi")]
        public void CosecShouldBeNaN(string angle)
        {
            MathS.Cosec(angle).Evaled.ShouldBe(MathS.NaN);
            MathS.Cosec(angle).InnerSimplified.ShouldBe(MathS.NaN);
        }

        [Theory]
        [InlineData("1 / 0 > 0")]
        [InlineData("1 / 0 < 0")]
        [InlineData("1 / 0 >= 0")]
        [InlineData("1 / 0 <= 0")]
        [InlineData("0 < 1 / 0")]
        [InlineData("0 > 1 / 0")]
        [InlineData("0 <= 1 / 0")]
        [InlineData("0 >= 1 / 0")]
        [InlineData("i > i")]
        [InlineData("i < i")]
        [InlineData("i >= (i + 1)")]
        [InlineData("i <= (i + 1)")]
        [InlineData("i >= -i")]
        [InlineData("i <= -i")]
        public void InequalityShouldBeNaN(string expr) {
            expr.ToEntity().Evaled.ShouldBe(MathS.NaN);
            expr.ToEntity().InnerSimplified.ShouldBe(MathS.NaN);
        }

        /// <summary>
        /// Normalising a normalised expression leaves it alone. `cos(0 ^ y)` did not: the
        /// exact value of a cosine reached through a half turn was built as a negation over
        /// the value it turned to and handed back unfolded, so the answer arrived as
        /// `-(-1) provided ...` and became `1 provided ...` only if something normalised it
        /// again. https://github.com/asc-community/AngouriMath/issues/930
        /// </summary>
        /// <remarks>
        /// Compared as entities rather than as strings, deliberately. The general failure here
        /// includes two trees that print alike and differ — `(x + y) + a` and `x + (y + a)` do
        /// — so a comparison of printed forms would wave those through. See
        /// `Docs/Contributing/CanonicalForm.md`.
        /// </remarks>
        [Theory]
        [InlineData("cos(0 ^ y)")]
        [InlineData("sin(0 ^ y)")]
        [InlineData("cos(0)")]
        [InlineData("cos(pi)")]
        [InlineData("sin(pi / 6)")]
        [InlineData("cos(pi / 3)")]
        [InlineData("tan(pi / 4)")]
        [InlineData("x + sin(0 ^ y)")]
        public void InnerSimplifiedIsIdempotent(string expr)
        {
            var once = expr.ToEntity().InnerSimplified;
            Assert.Equal(once, once.InnerSimplified);
        }

        /// <summary>
        /// The README's own solve example carried `-b / (-1)` among its roots, from the factor
        /// `b - x`; the root is `b`.
        /// </summary>
        [Fact]
        public void TheRootOfALinearWithANegativeSlopeIsNotAQuotientByMinusOne()
        {
            var roots = "(sin(x)^2 - sin(x) + a)(b - x)((-3) * x + 2 + 3 * x ^ 2 + (x + (-3)) * x ^ 3)".SolveEquation("x");
            Assert.DoesNotContain("-1)", roots.Stringize());
            Assert.Contains(MathS.Var("b"), roots.Members(0));
        }
    }
}


