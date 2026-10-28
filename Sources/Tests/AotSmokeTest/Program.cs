//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using AngouriMath;
using AngouriMath.Core.Compilation.IntoLinq;
using AngouriMath.Extensions;
using GenericTensor.Core;

namespace AngouriMath.AotSmokeTest
{
    /// <summary>
    /// Publishes trimmed and NativeAOT, then runs. Anything the kernel reaches for at run time
    /// that the trimmer removed shows up here as an exception rather than as a warning, which is
    /// why this program is run and not merely built.
    /// </summary>
    internal static class Program
    {
        private static int failed;
        private static int passed;

        private static int Main()
        {
            Console.WriteLine($"AngouriMath AOT/trimming smoke test, runtime {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Console.WriteLine();

            Parsing();
            Simplification();
            Solving();
            Differentiation();
            Integration();
            Limits();
            Compilation();

            Console.WriteLine();
            Console.WriteLine($"passed: {passed}, failed: {failed}");
            if (failed > 0)
                Console.WriteLine("FAILED");
            else
                Console.WriteLine("OK");
            return failed > 0 ? 1 : 0;
        }

        private static void Section(string name)
        {
            Console.WriteLine($"--- {name}");
        }

        private static void Check(string what, bool ok, string detail)
        {
            if (ok)
            {
                passed++;
                Console.WriteLine($"  pass  {what}: {detail}");
            }
            else
            {
                failed++;
                Console.WriteLine($"  FAIL  {what}: {detail}");
            }
        }

        private static void CheckClose(string what, Complex actual, Complex expected, double tolerance = 1e-9)
        {
            var ok = Complex.Abs(actual - expected) <= tolerance * Math.Max(1, Complex.Abs(expected));
            Check(what, ok, $"{Show(actual)} (expected {Show(expected)})");
        }

        private static string Show(Complex c)
            => c.Imaginary == 0
                ? c.Real.ToString("G17", CultureInfo.InvariantCulture)
                : c.Real.ToString("G17", CultureInfo.InvariantCulture) + " + " + c.Imaginary.ToString("G17", CultureInfo.InvariantCulture) + "i";

        // Value at a point, not printed form: a smoke test that asserted strings would fail on a
        // reordering that the trimmer had nothing to do with.
        private static Complex At(Entity expr, string variable, Complex value)
            => expr.Substitute(variable, (Entity.Number.Complex)value).EvalNumerical().ToNumerics();

        private static void Parsing()
        {
            Section("parse");

            var parsed = MathS.FromString("x ^ 2 + 2 * x + 1");
            Check("parses to a sum", parsed is Entity.Sumf, parsed.GetType().Name);
            CheckClose("value at x = 3", At(parsed, "x", 3), 16);

            // The parser is ANTLR-generated and the lexer tables are static data the trimmer has
            // to keep; a trimmed build that lost them throws here rather than at load.
            var set = MathS.FromString("{ 1, 2, 3 } \\/ { 3, 5 }").InnerSimplified;
            Check("set union parses and evaluates", set is Entity.Set.FiniteSet { Count: 4 }, set.Stringize());

            var piecewise = MathS.FromString("piecewise(1 provided x > 0, 0 provided x <= 0)");
            CheckClose("piecewise at x = 2", At(piecewise, "x", 2), 1);

            // det is computed while parsing, so it is reached from every native build. A sine is not
            // a polynomial entry, so this takes the Laplace expansion rather than the elimination.
            // https://github.com/asc-community/AngouriMath/issues/2147
            var determinant = MathS.FromString("det([[1, 2], [3, sin(x)]])");
            CheckClose("det at x = 0", At(determinant, "x", 0), -6);
            var dot = MathS.FromString("dot(cross([1, 0, 0], [0, 1, 0]), [0, 0, x])");
            CheckClose("dot of a cross at x = 5", At(dot, "x", 5), 5);
        }

        private static void Simplification()
        {
            Section("Simplify");

            var simplified = "(x ^ 2 - 1) / (x - 1)".Simplify();
            CheckClose("(x2 - 1)/(x - 1) at x = 5", At(simplified, "x", 5), 6);
            Check("(x2 - 1)/(x - 1) has no division left", !simplified.Nodes.Any(n => n is Entity.Divf), simplified.Stringize());

            var trig = "sin(x) ^ 2 + cos(x) ^ 2".Simplify();
            Check("sin2 + cos2 collapses to 1", trig == 1, trig.Stringize());

            var expanded = "(a + b) ^ 2".Expand().Simplify();
            CheckClose("(a+b)^2 at a = 2, b = 3", At(At2(expanded, "a", 2), "b", 3), 25);
        }

        private static Entity At2(Entity expr, string variable, Complex value)
            => expr.Substitute(variable, (Entity.Number.Complex)value);

        private static void Solving()
        {
            Section("Solve");

            var roots = "x ^ 2 - 4 = 0".Solve("x");
            Check("x2 - 4 = 0 has two roots", roots is Entity.Set.FiniteSet { Count: 2 }, roots.Stringize());
            if (roots is Entity.Set.FiniteSet finite)
                foreach (var root in finite)
                    CheckClose($"root {root.Stringize()} substituted back", At(MathS.FromString("x ^ 2 - 4"), "x", root.EvalNumerical().ToNumerics()), 0);

            var linear = "3 * x + 6 = 0".Solve("x");
            Check("3x + 6 = 0 solves to -2", linear is Entity.Set.FiniteSet { Count: 1 } one && one.First() == -2, linear.Stringize());

            var system = MathS.Equations("x + y - 3", "x - y - 1").Solve("x", "y");
            Check("the 2x2 system has one solution", system is { RowCount: 1, ColumnCount: 2 }, system?.Stringize() ?? "null");
        }

        private static void Differentiation()
        {
            Section("Differentiate");

            var d = "sin(x) * x".Differentiate("x");
            CheckClose("(x sin x)' at x = 1", At(d, "x", 1), Math.Cos(1) + Math.Sin(1));

            var second = "x ^ 4".Differentiate("x").Differentiate("x").InnerSimplified;
            CheckClose("(x^4)'' at x = 2", At(second, "x", 2), 48);

            var chain = "e ^ (x ^ 2)".Differentiate("x");
            CheckClose("(e^(x2))' at x = 1", At(chain, "x", 1), 2 * Math.E);
        }

        private static void Integration()
        {
            Section("Integrate");

            // Differentiated back rather than compared to a form: the antiderivative is a
            // Derivation, so only the derivative is an equality anything may assert.
            var antiderivative = "x ^ 2 + 3 * x".Integrate("x");
            var back = antiderivative.Differentiate("x").InnerSimplified;
            CheckClose("d/dx of the antiderivative of x2 + 3x, at x = 2", At(back, "x", 2), 4 + 6);
        }

        private static void Limits()
        {
            Section("Limit");

            var limit = "sin(x) / x".Limit("x", 0).InnerSimplified;
            CheckClose("sin(x)/x as x -> 0", limit.EvalNumerical().ToNumerics(), 1);
        }

        // The compilation cases are also the before/after comparison for the de-reflection of
        // CompilationProtocol: every function GetDef used to look up by string is here.
        private static readonly (string Expr, Complex Arg)[] complexCases =
        {
            ("sin(x)", 0.7),
            ("cos(x)", 0.7),
            ("tan(x)", 0.7),
            ("cotan(x)", 0.7),
            ("sec(x)", 0.7),
            ("cosec(x)", 0.7),
            ("arcsin(x)", 0.3),
            ("arccos(x)", 0.3),
            ("arctan(x)", 0.3),
            ("arccotan(x)", 0.3),
            ("arcsec(x)", 2.5),
            ("arccosec(x)", 2.5),
            ("abs(x)", -1.25),
            ("sgn(x)", -1.25),
            ("x ^ 2.5", 1.75),
            ("log(2, x)", 9.0),
            ("ln(x)", 9.0),
            ("x / (x + 1)", 4.0),
            ("x + 2 * x - 3 * x ^ 2", 1.5),
            ("sin(x) ^ 2 + cos(x) ^ 2", 0.9),
        };

        private static void Compilation()
        {
            Section("Compile");

            var x = MathS.Var("x");
            var y = MathS.Var("y");

            foreach (var (text, arg) in complexCases)
            {
                Complex got;
                try
                {
                    got = text.Compile<Complex, Complex>(x)(arg);
                }
                catch (Exception e)
                {
                    failed++;
                    Console.WriteLine($"  FAIL  compile {text}: threw {e.GetType().Name}: {e.Message}");
                    continue;
                }

                var expected = At(MathS.FromString(text), "x", arg);
                CheckClose($"compile {text} at {Show(arg)}", got, expected, 1e-8);
            }

            // double, not Complex: a different set of MathAllMethods overloads.
            var d = "sin(x) + cos(x)".Compile<double, double>(x);
            CheckClose("compiled double sin(x) + cos(x) at 0.4", d(0.4), Math.Sin(0.4) + Math.Cos(0.4), 1e-12);

            // Two arguments, and the floored remainder that takes the sign of the divisor.
            var mod = "x mod y".Compile<long, long, long>(x, y);
            Check("compiled -7 mod 3", mod(-7, 3) == 2, mod(-7, 3).ToString(CultureInfo.InvariantCulture));

            // The one path that reached Type.GetMethod("IsNaN"): a double-valued tree converted
            // to a nullable integral return type, where NaN has to become null.
            var toNullable = "x + 1".Compile<Func<double, long?>>(new(), typeof(long?), new[] { (typeof(double), x) });
            Check("compiled double -> long? at 4.5", toNullable(4.5) == 5, toNullable(4.5)?.ToString(CultureInfo.InvariantCulture) ?? "null");
            Check("compiled double -> long? at NaN", toNullable(double.NaN) is null, toNullable(double.NaN)?.ToString(CultureInfo.InvariantCulture) ?? "null");

            // Booleans and a piecewise, which is the ConvertOtherNode path.
            var boolean = "x > 0 and x < 10".Compile<double, bool>(x);
            Check("compiled predicate at 5", boolean(5) && !boolean(50), $"{boolean(5)}, {boolean(50)}");

            var piecewise = "piecewise(x provided x > 0, 0 provided x <= 0)".Compile<double, double>(x);
            Check("compiled piecewise at -3 and 3", piecewise(-3) == 0 && piecewise(3) == 3, $"{piecewise(-3)}, {piecewise(3)}");

            // A matrix, which compiles to a tensor: the arithmetic on it must stay ahead-of-time
            // compilable, which GenericTensor's own expression-compiled loops are not.
            // https://github.com/asc-community/AngouriMath/issues/526
            var matrix = "[[x, 2 x], [x + 1, x^2]] * [[1, 0], [0, 1]] + 1".Compile<double, GenTensor<double, DoubleOperations>>(x);
            var at3 = matrix(3);
            Check("compiled matrix at 3", at3[0, 0] == 4 && at3[0, 1] == 7 && at3[1, 0] == 5 && at3[1, 1] == 10,
                $"[[{at3[0, 0]}, {at3[0, 1]}], [{at3[1, 0]}, {at3[1, 1]}]]");
        }
    }
}
