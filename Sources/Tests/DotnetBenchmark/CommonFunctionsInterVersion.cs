//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Csv;
using PeterO.Numbers;

namespace DotnetBenchmark
{
    // DO NOT FORGET TO SWITCH TO RELEASE!
    [ArtifactsPath(@"./benchmark_results.csv")]
    [CsvExporter(CsvSeparator.Semicolon)]
    [MemoryDiagnoser]
    public class CommonFunctionsInterVersion
    {
        // BenchmarkDotNet times one call before it decides how to measure, and where that call
        // takes more than two thirds of its 500 ms iteration it times one call per iteration from
        // then on. For a call of tens of microseconds that is another measurement: on CI, with the
        // first call at 349-362 ms, SimplifyEasy read 12x its time and 3% more allocation and failed
        // the gate, while at 300 ms it was piloted and matched the baseline. The first call paid for
        // this class's static fields, parsed when the class is first touched, and for the library's
        // own set-up, which is why EvalEasy's took 133 ms. Done here, before the timed call,
        // SimplifyEasy's first call is 8 ms where it was 172 ms, on one machine, and what each
        // benchmark allocates is unchanged.
        [GlobalSetup]
        public void SetUpBeforeTheFirstTimedCall()
        {
            GC.KeepAlive(simplifyEasy);
            "x + 1".ToEntity().Simplify();
            "x2 + 1".ToEntity().SolveEquation("x");
            MathS.Sin(2).EvalNumerical();
        }

        // Testing parsing
        [Benchmark] public void ParseEasy() => MathS.FromString("1 + 2 / x + 2 / (y + x)", useCache: false);
        [Benchmark] public void ParseHard() => MathS.FromString("x ^ (x + y ^ 2 ^ sin(3 / z + i)) - log(-i, 3) ^ (x + x * y) - sqrt(y) / (i + sqrt(-1))", useCache: false);

        // Testing simplification
        [Benchmark] public void SimplifyEasy() => simplifyEasy.Simplify(4);
        private static readonly Entity simplifyEasy = "x + 3 / 3 + x ^ 0 - log(e, e2)";
        [Benchmark] public void SimplifyHard() => simplifyHard.Simplify(4);
        internal static Entity simplifyHardPart = "y / u + 4 * a * 8";
        internal static Entity simplifyHardPart2 = MathS.Sin(simplifyHardPart) + MathS.Pow(simplifyHardPart, "y + u");
        internal static Entity simplifyHardPart3 = MathS.Log(3, simplifyHardPart2) + MathS.Arccos(simplifyHardPart2);
        internal static Entity simplifyHard = ("(x + 1) + 2 + y + z + 3 + log(3, e + 3 / 6) - " +
                                               "(x2 - 1) / (x - 1) + sin(4 * x) / cos(4 * x) + " + 
                                               "1 / x / a + (x + x * y) + log(3, 9) + a ^ 1 + (cos(a)2 + sin(a))").ToEntity().Substitute("x", simplifyHardPart3);

        // Testing derivation
        [Benchmark] public void Derivate() => toDerive.Differentiate("x");
        private static readonly Entity toDerive = "x + 3 + arccos(x + 2) / sqrt(x2 + 1)";

        // Testing solver
        [Benchmark] public void SolveEasy() => toSolveEasy.SolveEquation("x");
        private static readonly Entity toSolveEasy = "x2 + x + 1";
        [Benchmark] public void SolveEasyMedium() => toSolveEasyMedium.SolveEquation("x");
        private static readonly Entity toSolveEasyMedium = "x2 + x + a";
        [Benchmark] public void SolveMedium() => toSolveMedium.SolveEquation("x");
        private static readonly Entity toSolveMedium = "(x + sqr(x) + a)2 + (x + sqr(x) + a) + b";
        [Benchmark] public void SolveMediumHard() => toSolveMediumHard.SolveEquation("x");
        private static readonly Entity toSolveMediumHard = "(sin(x) + sqr(sin(x)) + a)4 + (sin(x) + sqr(sin(x)) + a) + b";
        [Benchmark] public void SolveHard() => toSolveHard.SolveEquation("x");
        private static readonly Entity toSolveHard = "(sin(cos(x) + sin(x) + c) + sqr(sin(cos(x) + sin(x) + c)) + a)4 + (sin(cos(x) + sin(x) + c) + sqr(sin(cos(x) + sin(x) + c)) + a) + b";

        // Testing evaluation
        // NB: evalEasy is one instance and an Entity caches its own Evaled, so from the
        // moment that cache was added this stopped measuring evaluation and started
        // measuring a dictionary lookup -- which is why the row reads single-digit
        // nanoseconds. Kept as it is so the column history stays comparable; the two
        // benchmarks below build their expression afresh each call for that reason.
        [Benchmark] public void EvalEasy() => evalEasy.EvalNumerical();
        private static readonly Entity evalEasy = "1 + 2 + log(2, 3) + sqrt(4) - 4 ^ 7 + e * pi";

        // Testing trigonometry against precision, which is what #167 asked for. Building
        // the nodes is included in the measurement and costs microseconds against
        // milliseconds of arithmetic, so it does not move the number; evaluating a cached
        // instance instead would move it to zero.
        [Benchmark] public void EvalTrig()
            => (MathS.Sin(1) + MathS.Cos(1) + MathS.Tan(1)).EvalNumerical();

        [Benchmark] public void EvalTrigPrecise()
        {
            using var _ = MathS.Settings.DecimalPrecisionContext.Set(fiveHundredDigits);
            (MathS.Sin(1) + MathS.Cos(1) + MathS.Tan(1)).EvalNumerical();
        }
        private static readonly EContext fiveHundredDigits =
            new(500, ERounding.HalfUp, -500, 5000, false);

        // Numerical evaluation of an expression at a point, which is what a plot or a
        // root-finder asks for thousands of times: the expression is built once and the
        // point substituted afresh on every call, so the per-node cost of the evaluation is
        // what is measured and not the cache of a previous answer. A polynomial, for the
        // arithmetic alone, and a logarithm times an arctangent, for the series behind the
        // transcendental functions; the polynomial once more at fifteen digits, since how
        // much of the cost is the width of the numbers and how much is the evaluator's own
        // overhead is the question https://github.com/asc-community/AngouriMath/issues/1338
        // asks. The point is a decimal that is not within the downcasting tolerance of a
        // small rational -- an exact `37/100` would be evaluated in rational arithmetic and
        // never touch a decimal, and a binary double's fifty-three-digit expansion of 0.37
        // is not what anybody means by it.
        [Benchmark] public void EvalPolynomialFresh() => evalPolynomial.Substitute(evalX, evalAt).EvalNumerical();
        [Benchmark] public void EvalTranscendentalFresh() => evalTranscendental.Substitute(evalX, evalAt).EvalNumerical();
        [Benchmark] public void EvalPolynomialFresh15Digits()
        {
            using var _ = MathS.Settings.DecimalPrecisionContext.Set(fifteenDigits);
            evalPolynomial.Substitute(evalX, evalAt).EvalNumerical();
        }
        private static readonly Entity.Variable evalX = MathS.Var("x");
        private static readonly Entity evalAt = Entity.Number.Real.Create(EDecimal.FromString("0.37000000000001234"));
        private static readonly Entity evalPolynomial = "x^3 + 2*x^2 - x + 1";
        private static readonly Entity evalTranscendental = "ln(1 + x^2)*arctan(x)";
        private static readonly EContext fifteenDigits =
            new(15, ERounding.HalfUp, -100, 1000, false);

        // Testing compilation
        [Benchmark] public void CompileEasy() => toCompileEasy.Compile<Complex, Complex>("x");
        private static readonly Entity toCompileEasy = "x + x / 2 + sin(x)";
        [Benchmark] public void CompileHard() => toCompileHard.Compile<Complex, Complex>("x");
        private static readonly Entity toCompileHard = "(x + log(2, x) + sin(x + cos(x))3) / sqrt(x + tan(x))";

        // Testing compiled functions
        private readonly Complex toSub = new Complex(3, -1);
        [Benchmark] public void RunEasy() => toRunEasy(toSub);
        private readonly Func<Complex, Complex> toRunEasy = "x + sin(x) + 3".ToEntity().Compile<Complex, Complex>("x");
        [Benchmark] public void RunMedium() => toRunMedium(toSub);
        private readonly Func<Complex, Complex> toRunMedium = "x + sin(x) + 3 / sin(x + cos(x)) + sqrt(x3) / e ^ x + x * log(e, 4)".ToEntity().Compile<Complex, Complex>("x");
        [Benchmark] public void RunHard() => toRunHard(toSub);
        private static readonly Entity toRunHardEntity = "x + sin(x) + 3 / sin(x + cos(x)) + sqrt(x3) / e ^ x + x * log(e, 4)";
        private static readonly Func<Complex, Complex> toRunHard = (MathS.Sqr(toRunHardEntity) + MathS.Sqrt(toRunHardEntity) + MathS.Sin(toRunHardEntity) / (MathS.Sqrt(toRunHardEntity) + MathS.Sin(toRunHardEntity) + MathS.Tan(toRunHardEntity))).Compile<Complex, Complex>("x");
    }
}