//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Core.Exceptions;
using System;
using System.Linq;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;
using static AngouriMath.MathS;
using AngouriMath.Extensions;
using FluentAssertions;

namespace AngouriMath.Tests.Convenience
{
    [Trait("Area", "Convenience")]
    public sealed class FromStringTest
    {
        public static readonly Entity.Variable x = Var(nameof(x));
        public static readonly Entity.Variable y = Var(nameof(y));
        public static readonly Entity.Variable z = Var(nameof(z));

        [Theory]
        [InlineData("", "line 1:0")]
        [InlineData(" ", "line 1:1")]
        [InlineData("\t", "line 1:1")]
        [InlineData("  ", "line 1:2")]
        // Skipped like the whitespace and block-comment rows above it: the error is at the end
        // of the input, not at a stray `/`. It was `line 1:0` until #1039, when a `//` comment at
        // the end of the input did not match and its first `/` reached the parser.
        [InlineData("//", "line 1:2")]
        [InlineData("//\n", "line 2:0")]
        [InlineData("/**/", "line 1:4")]
        [InlineData("a+", "line 1:2")]
        [InlineData("*a", "line 1:0")]
        [InlineData("a*a_", "line 1:3")]
        [InlineData("+!", "line 1:1")]
        [InlineData("_", "line 1:0")]
        [InlineData("()", "line 1:1")]
        // A `provided` in a parenthesised comma list. ANTLR reports the syntax error and
        // then recovers, and the grammar's action for `provided` runs anyway on a rule
        // context whose value was never assigned -- so these used to bring the parse down
        // with a raw NullReferenceException, which a caller catching AngouriMathBaseException
        // around user input does not catch.
        // https://github.com/asc-community/AngouriMath/issues/813
        [InlineData("(1 provided x > 0, 2)", "line 1:17")]
        [InlineData("(a provided b, c)", "line 1:13")]
        [InlineData("(1 provided x > 0, 2, 3)", "line 1:17")]
        [InlineData("(1, 2 provided x > 0)", "line 1:2")]
        [InlineData("(1 provided x > 0, 2 provided x < 0)", "line 1:17")]
        public void Error(string input, string errorPrefix) =>
            Assert.StartsWith(errorPrefix,
                Assert.Throws<UnhandledParseException>(() => (Entity)input).Message);

        // The construct itself is untouched: what is refused is the invalid input, not
        // `provided`. https://github.com/asc-community/AngouriMath/issues/813
        [Theory]
        [InlineData("1 provided x > 0")]
        [InlineData("(1 provided x > 0)")]
        [InlineData("(1 provided x > 0) + 2")]
        [InlineData("a provided b provided c")]
        public void AProvidedStillParses(string input)
            => Assert.Contains(((Entity)input).Nodes, node => node is Providedf);

        // A comma list of `provided` is the piecewise syntax, and it parses -- so the fix
        // must not refuse it. The list without a piecewise in front of it is what has no
        // rule, which is what the crash was hiding.
        // https://github.com/asc-community/AngouriMath/issues/813
        [Theory]
        [InlineData("piecewise(1 provided x > 0, 2)")]
        [InlineData("piecewise(1 provided x > 0, 2 provided x < 0)")]
        [InlineData("piecewise(1 provided x > 0, 2 provided x < 0, 3)")]
        public void APiecewiseOverAProvidedListStillParses(string input)
            => Assert.IsType<Piecewise>((Entity)input);

        // Every way an input can be refused must arrive as this library's own exception --
        // Docs/Usage/Exceptions.md says so, and a caller wrapping user input in one catch
        // is relying on it. Nothing here may come back as a raw framework exception.
        // https://github.com/asc-community/AngouriMath/issues/813
        [Theory]
        [InlineData("(1 provided x > 0, 2)")]
        [InlineData("(provided, )")]
        [InlineData("(1 provided, 2)")]
        [InlineData("(1 provided x > 0,)")]
        [InlineData("(, 1 provided x)")]
        [InlineData("{1 provided x > 0, 2")]
        [InlineData("[1 provided x > 0, 2)")]
        [InlineData("piecewise(1 provided x > 0")]
        public void AnInvalidInputIsRefusedByThisLibrarysOwnException(string input)
        {
            var thrown = Record.Exception(() => (Entity)input);
            Assert.NotNull(thrown);
            Assert.True(thrown is AngouriMathBaseException,
                $"{input} was refused with {thrown!.GetType().FullName} rather than an AngouriMathBaseException");
        }
        [Theory]
        [InlineData("limitleft()", "limitleft should have exactly 3 arguments but 0 arguments are provided")]
        [InlineData("derivative(3)", "derivative should have exactly 3 arguments or 2 arguments but 1 argument is provided")]
        [InlineData("integral(3)", "integral should have exactly 4 arguments or 2 arguments but 1 argument is provided")]
        [InlineData("ln(3, 5)", "ln should have exactly 1 argument but 2 arguments are provided")]
        [InlineData("sin(3, 5, 8)", "sin should have exactly 1 argument but 3 arguments are provided")]
        [InlineData("log()", "log should have exactly 1 argument or 2 arguments but 0 arguments are provided")]
        [InlineData("log(1, 1, 1)", "log should have exactly 1 argument or 2 arguments but 3 arguments are provided")]
        public void WrongNumbersOfArgs(string input, string message) =>
            Assert.Equal(message, Assert.Throws<FunctionArgumentCountException>(() => (Entity)input).Message);
        [Theory]
        [InlineData("x+1")]
        [InlineData("sin(x)")]
        [InlineData("2s")]
        public void NotAVariable(string input) =>
            Assert.Throws<CannotParseInstanceException>(() => (Entity.Variable)input);
        [Fact] public void TestFormula1() => Assert.Equal(2, FromString("1 + 1").EvalNumerical());
        [Fact] public void TestFormula2() => Assert.Equal(0, FromString("sin(0)").EvalNumerical());
        [Fact] public void TestFormula3() => Assert.Equal(2, FromString("log(2, 4)").EvalNumerical());
        [Fact] public void TestFormula4() => Assert.Equal(2, FromString("log(100)").EvalNumerical());
        [Fact] public void TestFormula5() => Assert.Equal(Cos(x), FromString("sin(x)").Differentiate(x).Simplify());
        [Fact] public void TestFormula6() => Assert.Equal(7625597484987L, FromString("3 ^ 3 ^ 3").EvalNumerical());
        [Fact] public void TestFormula7() => Assert.Equal(6, FromString("2 + 2 * 2").EvalNumerical());
        [Fact] 
        public void TestFormula8()
        {
            // Only needed for Mac
            using var _ = Settings.PrecisionErrorZeroRange.Set(2e-16m);
            var roots = FromString("x^2+1").SolveNt(x);
            // Both roots, rather than whichever of them a set happens to hand back first.
            // That order was never the solver's to decide and it changed once the search
            // began collapsing its duplicates in a fixed order.
            Assert.Equal(2, roots.Count);
            Assert.Contains(Entity.Number.Complex.Create(0, 1), roots);
            Assert.Contains(Entity.Number.Complex.Create(0, -1), roots);
        }
        [Fact] public void TestFormula9() => Assert.Equal(1, FromString("cos(sin(0))").EvalNumerical());
        [Fact] public void TestFormula10() => Assert.Equal(Entity.Number.Complex.Create(4, 1), FromString("2i + 2 * 2 - 1i").EvalNumerical());
        [Fact] public void TestFormula11() => Assert.Equal(-1, FromString("i^2").EvalNumerical());
        [Fact] public void TestFormula12() => Assert.Equal(3, FromString("x^2-1").Substitute(x, 2).EvalNumerical());
        [Fact] public void TestFormula13() => Assert.Equal(DecimalConst.pi / 2, FromString("arcsin(x)").Substitute(x, 1).EvalNumerical());
        [Fact] public void TestFormula14() => Assert.Equal(Sqr(x), FromString("x2"));
        [Fact] public void TestFormula15() =>  Assert.Equal(2 * x, FromString("2x"));
        [Fact] public void TestFormula16() => Assert.Equal(9, FromString("3 2").EvalNumerical());
        [Fact] public void TestFormula17() => Assert.Equal(5 * x, FromString("x(2 + 3)").Simplify());
        [Fact] public void TestFormula18() => Assert.Equal(x * y, FromString("x y"));
        [Fact] public void TestFormula19() => Assert.Equal(x * MathS.Sqrt(3), FromString("x sqrt(3)"));
        [Fact] public void TestFormula20() => Assert.Equal(Factorial(x), FromString("x!"));
        // The double factorial, which was a parse error: https://github.com/asc-community/AngouriMath/issues/2182
        [Fact] public void TestFormula21() => Assert.Equal(MathS.DoubleFactorial(x), FromString("x!!"));
        [Fact] public void TestFormula22() => Assert.Equal(Factorial(Sin(x)), FromString("sin(x)!"));
        [Fact] public void TestFormula23() => Assert.Equal(Pow(2, MathS.Factorial(3)), FromString("2^3!"));
        [Fact] public void TestFormula24() => Assert.Equal(Pow(MathS.Factorial(2), MathS.Factorial(3)), FromString("2!^3!"));
        [Fact] public void TestFormula25() => Assert.Equal(Pow(MathS.Factorial(2), Factorial(x + 2)), FromString("2!^(x+2)!"));
        [Fact] public void TestFormula26() => Assert.Equal(-MathS.Factorial(1), FromString("-1!"));
        [Fact] public void TestFormula27() => Assert.Equal(MathS.Factorial("-1"), FromString("(-1)!"));
        [Fact] public void TestFormula28() => Assert.Equal(2, FromString("1 \r\n + \n 1").EvalNumerical());
        [Fact] public void TestFormulaSys() => Assert.Equal(Sqr(x), FromString("x2"));
        [Fact] public void TestNode28() => Assert.Equal(Derivative("x + 1", x), FromString("derivative(x + 1, x, 1)"));
        [Fact] public void TestNode29() => Assert.Equal(Derivative("x + 1", x, 5), FromString("derivative(x + 1, x, 5)"));
        [Fact] public void TestNode30() => Assert.Equal(Integral("x + 1", x), FromString("integral(x + 1, x)"));
        [Fact] public void TestNode31() => Assert.Equal(Integral("x + y", x, 3, 4), FromString("integral(x + y, x, 3, 4)"));
        [Fact] public void TestNode32() => Assert.Equal(Limit("x + y", x, 3), FromString("limit(x + y, x, 3)"));
        [Fact] public void TestNode33() => Assert.Equal(Limit("x + y", x, 3, ApproachFrom.Left), FromString("limitleft(x + y, x, 3)"));
        [Fact] public void TestNode34() => Assert.Equal(Signum("x"), FromString("signum(x)"));
        [Fact] public void TestNode35() => Assert.Equal(Abs("x"), FromString("abs(x)"));
        [Fact] public void TestMultipleUnary36() => Assert.Equal(-1 * (-1 * (-1 * x)), FromString("---x"));
        [Fact] public void TestMultipleUnary37() => Assert.Equal(-1 * (-1 * (-1 * x)), FromString("-++-+-+x"));
        [Fact] public void TestMultipleUnary38() => Assert.Equal(!!!x, FromString("not not not x"));
        [Fact] public void TestBool39() => Assert.Equal(x & x | x.Implies(x), FromString("x and x or (x -> x)"));
        [Fact] public void TestBool40() => Assert.Equal(x & x & x & x, FromString("x and x and x and x"));
        [Fact] public void TestBool41() => Assert.Equal(x | x | x | x, FromString("x or x or x or x"));
        [Fact] public void TestBoolConstants1() => Assert.Equal(Entity.Boolean.True, FromString("true"));
        [Fact] public void TestBoolConstants2() => Assert.Equal(Entity.Boolean.True, FromString("True"));
        [Fact] public void TestBoolConstants3() => Assert.Equal(Entity.Boolean.False, FromString("false"));
        [Fact] public void TestBoolConstants4() => Assert.Equal(Entity.Boolean.False, FromString("False"));
        [Fact] public void TestAbs42() => Assert.Equal(Abs(x), FromString("(|x|)"));
        [Fact] public void TestAbs43() => Assert.Equal(Abs(Abs(x) + 2).Pow(2), FromString("(|(|x|) + 2|) ^ 2"));
        [Fact] public void TestCoDomain44() => Assert.Equal(Sqrt(x).WithCodomain(Domain.Real), FromString("domain(sqrt(x), RR)"));
        [Fact] public void TestCoDomain45() => Assert.Equal(Sqrt(x).WithCodomain(Domain.Real), FromString("domain(domain(sqrt(x), RR), RR)"));
        [Fact] public void TestCoDomain46() => Assert.Equal(Sqrt(x).WithCodomain(Domain.Complex), FromString("domain(domain(sqrt(x), RR), CC)"));
        [Fact] public void TestInequality47() => Assert.Equal(x > y, FromString("x > y"));
        [Fact] public void TestInequality48() => Assert.Equal(x < y, FromString("x < y"));
        [Fact] public void TestInequality49() => Assert.Equal(x >= y, FromString("x >= y"));
        [Fact] public void TestInequality50() => Assert.Equal(x <= y, FromString("x <= y"));
        [Fact] public void TestInequality51() => Assert.Equal(x.EqualTo(y), FromString("x = y"));
        [Fact] public void TestInequality52() => Assert.Equal((x > y).EqualTo(x < y), FromString("(x > y) = (x < y)"));
        [Fact] public void TestInequality53() => Assert.Equal((x > y).Equalizes(x < y), FromString("x > y = (x < y)"));
        [Fact] public void TestInequality54() => Assert.Equal((x > y).Equalizes(x) < y, FromString("x > y = x < y"));
        [Fact] public void TestInterval1() => Assert.Equal(new Interval(x, true, y, true), FromString("[x; y]"));
        [Fact] public void TestInterval2() => Assert.Equal(new Interval(x, false, y, true), FromString("(x; y]"));
        [Fact] public void TestInterval3() => Assert.Equal(new Interval(x, true, y, false), FromString("[x; y)"));
        [Fact] public void TestInterval4() => Assert.Equal(new Interval(x, false, y, false), FromString("(x; y)"));
        [Fact] public void TestFiniteSet1() => Assert.Equal(new FiniteSet(x, y, 3), FromString("{ x, y, 3 }"));
        [Fact] public void TestFiniteSet2() => Assert.Equal(new FiniteSet(), FromString("{}"));
        [Fact] public void TestCSet1() => Assert.Equal(new ConditionalSet(x, "x + 3 > 0"), FromString("{ x : x + 3 > 0 }"));
        [Fact] public void TestCSet2() => Assert.Equal(new ConditionalSet(x, "x + 3 > 0 and x / 2 < y2"), FromString("{ x : x + 3 > 0 and x / 2 < y2 }"));
        [Fact] public void TestUnion1() => Assert.Equal(x.Unite(y), FromString(@"x \/ y"));
        [Fact] public void TestUnion2() => Assert.Equal(x.Unite(y), FromString(@"x unite y"));
        [Fact] public void TestIntersection1() => Assert.Equal(x.Intersect(y), FromString(@"x /\ y"));
        [Fact] public void TestIntersection2() => Assert.Equal(x.Intersect(y), FromString(@"x intersect y"));
        [Fact] public void TestSetSubtraction1() => Assert.Equal(x.SetSubtract(y), FromString(@"x \ y"));
        [Fact] public void TestSetSubtraction2() => Assert.Equal(x.SetSubtract(y), FromString(@"x setsubtract y"));
        [Fact] public void TestPlusInfinity1() => Assert.Equal(Real.PositiveInfinity, FromString("+oo"));
        [Fact] public void TestPlusInfinity2() => Assert.Equal(Real.PositiveInfinity + (Entity)2, FromString("+oo + 2"));
        [Fact] public void TestMinusInfinity1() => Assert.Equal(Real.NegativeInfinity, FromString("-oo"));
        [Fact] public void TestMinusInfinity2() => Assert.Equal(Real.NegativeInfinity + (Entity)2, FromString("-oo + 2"));
        [Fact] public void TestEquality1() => Assert.Equal(x.EqualTo(y) & y.EqualTo(x), FromString("x = y = x"));
        [Fact] public void TestEquality2() => Assert.Equal(x.EqualTo(y).EqualTo(x), FromString("(x = y) = x"));
        [Fact] public void TestDerivative2Args1() => Assert.Equal(MathS.Derivative("x + 2", "x"), FromString("derivative(x + 2, x)"));
        [Fact] public void TestIntegral2Args1() => Assert.Equal(MathS.Integral("x + 2", "x"), FromString("integral(x + 2, x)"));
        [Fact] public void TestDerivative2Args2() => Assert.Equal(2 * MathS.Derivative("x + 2", "x"), FromString("2 derivative(x + 2, x)"));
        [Fact] public void TestIntegral2Args2() => Assert.Equal(2 * MathS.Integral("x + 2", "x"), FromString("2 integral(x + 2, x)"));
        [Fact] public void TestSec() => Assert.Equal(MathS.Sec("x"), FromString("sec(x)"));
        [Fact] public void TestCosec() => Assert.Equal(MathS.Cosec("x"), FromString("csc(x)"));
        [Fact] public void TestArcsec() => Assert.Equal(MathS.Arcsec("x"), FromString("arcsec(x)"));
        [Fact] public void TestArccosec() => Assert.Equal(MathS.Arccosec("x"), FromString("arccsc(x)"));
        [Fact] public void TestProvided1() => Assert.Equal(MathS.Provided("a", "b"), FromString("a provided b"));
        [Fact] public void TestProvided2() => Assert.Equal(MathS.Provided("a", MathS.Provided("b", "c")), FromString("a provided b provided c"));
        [Fact] public void TestProvided3() => Assert.Equal(MathS.Provided(MathS.Provided("a", "b"), "c"), FromString("(a provided b) provided c"));
        // These two are different expressions -- `provided` folds to the right, so only the first
        // is what the flat spelling means -- and this used to assert that they print alike. That
        // is the round trip failing, written down as an expectation: the printed form of the
        // second came back as the first. They must print differently, and each must read back as
        // itself.
        [Fact] public void TestProvided4()
        {
            var rightNested = FromString("a provided (b provided c)");
            var leftNested = FromString("(a provided b) provided c");
            Assert.NotEqual(rightNested, leftNested);
            Assert.NotEqual(rightNested.Stringize(), leftNested.Stringize());
            Assert.Equal(rightNested, FromString(rightNested.Stringize()));
            Assert.Equal(leftNested, FromString(leftNested.Stringize()));
        }

        [Theory]
        [InlineData("sh", "Sinh")]
        [InlineData("sinh", "Sinh")]

        [InlineData("ch", "Cosh")]
        [InlineData("cosh", "Cosh")]

        [InlineData("tanh", "Tanh")]
        [InlineData("th", "Tanh")]

        [InlineData("coth", "Cotanh")]
        [InlineData("cth", "Cotanh")]

        [InlineData("sech", "Sech")]
        [InlineData("sch", "Sech")]

        [InlineData("csch", "Cosech")]

        [InlineData("asinh", "Arsinh")]
        [InlineData("arsinh", "Arsinh")]
        [InlineData("arsh", "Arsinh")]

        [InlineData("acosh", "Arcosh")]
        [InlineData("arcosh", "Arcosh")]
        [InlineData("arch", "Arcosh")]

        [InlineData("atanh", "Artanh")]
        [InlineData("artanh", "Artanh")]
        [InlineData("arth", "Artanh")]

        [InlineData("acoth", "Arcotanh")]
        [InlineData("arcoth", "Arcotanh")]
        [InlineData("arcth", "Arcotanh")]
        [InlineData("acotanh", "Arcotanh")]
        [InlineData("arcotanh", "Arcotanh")]

        [InlineData("asech", "Arsech")]
        [InlineData("arsech", "Arsech")]
        [InlineData("arsch", "Arsech")]

        [InlineData("acsch", "Arcosech")]
        [InlineData("arcsch", "Arcosech")]
        [InlineData("acosech", "Arcosech")]
        [InlineData("arcosech", "Arcosech")]
        public void TestHyperbolic(string parsedName, string methodName)
        {
            var methods = typeof(MathS.Hyperbolic).GetMethods();
            var withMethods = methods.Where(c => c.Name == methodName);
            if (withMethods.Count() != 1)
                throw new InvalidOperationException($"Incorrect method's name: {methodName}");
            var method = withMethods.First();
            Entity x = "x";
            var runned = method.Invoke(null, new []{ x });
            if (runned is not Entity expected)
                throw new InvalidOperationException("Method returned something weird -___-");
            var actual = MathS.FromString(parsedName + "(x)");
            Assert.Equal(expected, actual);
        }

        [Fact] public void TestInvalidArg1() => Assert.Throws<FunctionArgumentCountException>(() => FromString("integral(x)"));
        [Fact] public void TestInvalidArg2() => Assert.Throws<FunctionArgumentCountException>(() => FromString("integral(24)"));
        [Fact] public void TestInvalidArg3() => Assert.Throws<FunctionArgumentCountException>(() => FromString("integral(x, x, 4)"));
        [Fact] public void TestInvalidArg4() => Assert.Throws<FunctionArgumentCountException>(() => FromString("integral(x, x, x, x, x)"));
        [Fact] public void TestInvalidArg6() => Assert.Throws<FunctionArgumentCountException>(() => FromString("derivative(x)"));
        [Fact] public void TestInvalidArg7() => Assert.Throws<FunctionArgumentCountException>(() => FromString("derivative(24)"));
        [Fact] public void TestInvalidArg8() => Assert.Throws<FunctionArgumentCountException>(() => FromString("derivative(x, x, 4, x)"));
        [Fact] public void TestInvalidArg9() => Assert.Throws<FunctionArgumentCountException>(() => FromString("derivative(x, x, x, x)"));
        [Fact] public void TestInvalidArg10() => Assert.Throws<InvalidArgumentParseException>(() => FromString("derivative(x, x, a)"));
        [Fact] public void TestPowerUnary1() => Assert.Equal(Pow(x, "-5"), FromString("x^-5"));
        [Fact] public void TestPowerUnary2() => Assert.Equal(Pow(x, -x), FromString("x^-x"));
        [Fact] public void TestPowerUnary3() => Assert.Equal(Pow(x, -Pow(x, x)), FromString("x^ -x^x"));
        [Fact] public void TestPiecewise1() => Assert.Equal(Piecewise((x, y), (y, x), (x + 2, y + 2)), FromString("piecewise(x provided y, y provided x, x + 2 provided y + 2)"));
        [Fact] public void TestPiecewise2() => Assert.Equal(Piecewise(new[] { new Providedf(x, y), new Providedf(y, x), new Providedf(x + 2, y + 2) }, 3), FromString("piecewise(x provided y, y provided x, x + 2 provided y + 2, 3)"));
        [Fact] public void TestNegativeNumberParsing1() => Assert.Equal(-1, FromString("-1"));
        [Fact] public void TestNegativeNumberParsing2() => Assert.Equal(-234.2m, FromString("-234.2"));
        [Fact] public void TestNegativeNumberParsing3() => Assert.Equal(234.2m, FromString("+234.2"));
        [Fact] public void TestNegativeNumberParsing4() => Assert.Equal((Entity)(-1).ToNumber() * (Entity)(-2).ToNumber(), FromString("--2"));
        [Fact] public void TestMatrix2x2() => Assert.Equal(Matrix(new Entity[,] { { 1, 2 }, { 3, 4 } }), FromString("[ [ 1, 2 ] , [ 3, 4 ] ]"));
        [Fact] public void TestMatrix3x3() => Assert.Equal(I_3, FromString("[ [ 1, 0, 0 ], [ 0, 1, 0 ], [ 0, 0, 1 ] ]"));
        [Fact] public void TestMatrix4Vector1() => Assert.Equal(Vector(1, 2, 3), FromString("[ 1 , 2 , 3 ]"));
        [Fact] public void TestMatrix4Vector2() => Assert.Equal(Vector(1, 2, 3).T, FromString("[ 1 , 2 , 3 ]T"));
        [Fact] public void TestLambda1() => Assert.Equal(x.LambdaOver(x), FromString("lambda(x, x)"));
        [Fact] public void TestLambda2() => Assert.Equal((x + y).LambdaOver(x), FromString("lambda(x, x + y)"));
        [Fact] public void TestLambda3() => Assert.Equal((x + y).LambdaOver(x).LambdaOver(y), FromString("lambda(y, x, x + y)"));
        [Fact] public void TestLambda4() => Assert.Equal((x + y).LambdaOver(x).LambdaOver(y).LambdaOver(z), FromString("lambda(z, y, x, x + y)"));
        [Fact] public void TestLambda5() => Assert.Throws<InvalidArgumentParseException>(() => FromString("lambda(x + y, x + y)"));
        [Fact] public void TestLambda6() => Assert.Throws<InvalidArgumentParseException>(() => FromString("lambda(x + y, x, x + y)"));
        [Fact] public void TestLambda7() => Assert.Throws<InvalidArgumentParseException>(() => FromString("lambda(x, 1, x + y)"));
        [Fact] public void TestLambda8() => Assert.Throws<FunctionArgumentCountException>(() => FromString("lambda(x + y)"));
        [Fact] public void TestLambda9() => Assert.Throws<FunctionArgumentCountException>(() => FromString("lambda(x)"));
        [Fact] public void TestApply1() => Assert.Equal(x.Apply(x), FromString("apply(x, x)"));
        [Fact] public void TestApply2() => Assert.Equal(x.Apply(y), FromString("apply(x, y)"));
        [Fact] public void TestApply3() => Assert.Equal(x.Apply(y, z), FromString("apply(x, y, z)"));
        [Fact] public void TestApply4() => Assert.Equal((x + 1).Apply(y, z), FromString("apply(x + 1, y, z)"));
        [Fact] public void TestApply5() => Assert.Equal((x + 1).Apply(y, z), FromString("apply(x + 1, y, z)"));
        [Fact] public void TestApply6() => Assert.Throws<FunctionArgumentCountException>(() => FromString("apply(x)"));


        private (Entity xy, Entity xyz, Entity yz, string str) Extract(string signLeft, string signRight)
        {
            var s = $"x {signLeft} y {signRight} z";
            var nodeXY = signLeft switch
            {
                "<" => x < y,
                ">" => x > y,
                "<=" => x <= y,
                ">=" => x >= y,
                _ => throw new Exception("What the hell???")
            };
            var nodeXYZ = signRight switch
            {
                "<" => nodeXY < z,
                ">" => nodeXY > z,
                "<=" => nodeXY <= z,
                ">=" => nodeXY >= z,
                _ => throw new Exception("What the hell???")
            };
            var nodeYZ = signRight switch
            {
                "<" => y < z,
                ">" => y > z,
                "<=" => y <= z,
                ">=" => y >= z,
                _ => throw new Exception("What the hell???")
            };
            return (nodeXY, nodeXYZ, nodeYZ, s);
        }

        [Theory, CombinatorialData]
        public void TestInequalityCompositionCircle(
        [CombinatorialValues("<", ">", "<=", ">=")] string signLeft,
        [CombinatorialValues("<", ">", "<=", ">=")] string signRight)
        {
            var (_, nodeXYZ, _, str) = Extract(signLeft, signRight);
            Assert.Equal(nodeXYZ, FromString(str));
        }

        [Theory, CombinatorialData]
        public void TestInequalityCompositionPrettySyntax(
        [CombinatorialValues("<", ">", "<=", ">=")] string signLeft,
        [CombinatorialValues("<", ">", "<=", ">=")] string signRight)
        {
            var (nodeXY, _, nodeYZ, str) = Extract(signLeft, signRight);
            Assert.Equal(nodeXY & nodeYZ, FromString(str));
        }

        [Theory]
        [InlineData("α", "α")]
        [InlineData("αb", "αb")]
        [InlineData("αb_3", "αb_3")]
        [InlineData("1 + α", "α")]
        [InlineData("1 + αb", "αb")]
        [InlineData("1 + αb_3", "αb_3")]
        [InlineData("αβγδεζηθικλμνξοπρσςτυφχψω", "αβγδεζηθικλμνξοπρσςτυφχψω")]
        [InlineData("sin(αβγδεζηθικλμ)", "αβγδεζηθικλμ")]
        [InlineData("ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣϹΤΥΦΧΨΩ", "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣϹΤΥΦΧΨΩ")]
        [InlineData("1 + ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣϹΤΥΦΧΨΩ + 1", "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣϹΤΥΦΧΨΩ")]
        [InlineData("йцукенгшщзхъфывапролджэячсмитьбю", "йцукенгшщзхъфывапролджэячсмитьбю")]
        [InlineData("ЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ", "ЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ")]
        [InlineData("cos(АнгуриMath)", "АнгуриMath")]
        [InlineData("ΑγγούριMath + 11", "ΑγγούριMath")]
        [InlineData("БЕZHΘГIM_64 ^ 2", "БЕZHΘГIM_64")]
        [InlineData("ВсЕмПрИвЕтВэТоМчАтИкЕ < 3", "ВсЕмПрИвЕтВэТоМчАтИкЕ")]
        [InlineData("йА кΨеВеТкΩ", "йА")]
        public void TestUnicodeVariablesParser(string exprRaw, string expectedName)
        {
            Entity actual = exprRaw;
            var any = actual.Vars[0];
            Assert.Equal(expectedName, any.Name);
        }
        
        [Theory]
        [InlineData("2x")]
        [InlineData("2 x")]
        [InlineData("x2")]
        [InlineData("x 2")]
        [InlineData("x + 1 2")]
        [InlineData("2(x + 2)")]
        [InlineData("(x + 1)2")]
        [InlineData("x(x + 1)")]
        [InlineData("(x + 1)x")]
        [InlineData("(x + 1)(1 + 2)")]
        [InlineData("2sin(x)")]
        [InlineData("sin(x)2")]
        [InlineData("a sin(x)")]
        [InlineData("sin(x) a")]
        public void ThrowsAsExplicitParsingEnabled(string exprRaw)
        {
            exprRaw.ToEntity(); // doesn't throw, good
            using var _ = Settings.ExplicitParsingOnly.Set(true);
            Assert.Throws<MissingOperatorParseException>(exprRaw.ToEntity);
        }
        
        [Theory]
        [InlineData("x",     false,  "Success")]
        [InlineData("x2",    false,  "Success")]
        [InlineData("x2",    true,   "Missing")]
        [InlineData("2x",    false,  "Success")]
        [InlineData("2x",    true,   "Missing")]
        [InlineData("x a",   false,  "Success")]
        [InlineData("x a",   true,   "Missing")]
        [InlineData("1 + x", false,  "Success")]
        [InlineData("x;",    false,  "Unknown")]
        [InlineData("x -",   false,  "Unknown")]
        [InlineData("(x",    false,  "Unknown")]
        public void ReturnsTheFollowing(string exprRaw, bool explicitOnly, string result)
            => Settings.ExplicitParsingOnly.As(explicitOnly,
                    () => Parse(exprRaw)
                        .Switch(
                            valid => "Success",
                            invalid => invalid.Reason.Switch(
                                unknown => "Unknown",
                                missing => "Missing",
                                internalError => "Internal"
                            )
                        )
                        .Should().Be(result)
                );

        /// <summary>
        /// The cache keeps a parse for each value of the downcasting setting, which changes the
        /// parse: with it off, a whole number is a decimal. A string parsed first with the setting
        /// on came back with it off as the first parse.
        /// https://github.com/asc-community/AngouriMath/issues/1513
        /// </summary>
        [Fact]
        public void TheCacheKeepsAParseForEachDowncastingSetting()
        {
            // No other test parses this string, so no other test's cache entry answers it.
            const string text = "x ^ 7 + 1513";
            var on = FromString(text);
            Entity off, parsedOff;
            using (Settings.DowncastingEnabled.Set(false))
            {
                off = FromString(text);
                parsedOff = FromString(text, useCache: false);
            }
            Assert.Equal(parsedOff, off);
            Assert.Contains(off.Nodes, node => node is Real and not Rational);
            Assert.DoesNotContain(on.Nodes, node => node is Real and not Rational);
        }
    }
}

