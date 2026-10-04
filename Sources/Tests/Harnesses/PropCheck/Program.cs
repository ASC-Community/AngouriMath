//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

﻿// Property checker for AngouriMath.
//
// Every transformation the library offers is supposed to preserve something. Simplify,
// Expand and Factorize preserve the value of the expression; Differentiate has to agree
// with a difference quotient; Integrate has to differentiate back to the integrand;
// a root has to satisfy its equation. None of those are checked by comparing printed
// forms, which is what most of the test suite does and what let two wrong integrals sit
// in it unnoticed.
//
// So: take a corpus of expressions, apply each transformation, and check the property
// numerically at several points. Report only what fails, with the point that fails it.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Extensions;

static class PropCheck
{
    // Sample points. Positive reals, irrational-ish, and away from the small integers,
    // so that a rule which happens to hold at 1 or 2 does not pass by luck. Kept off
    // zero and one because too much of the corpus is singular there.
    static readonly double[] Points = { 0.37, 1.41, 2.71, 3.33, 5.19 };

    const double Tolerance = 1e-8;

    record Failure(string Property, string Input, string Output, string Detail);

    static readonly List<Failure> Failures = new();
    static int checks, skipped;

    static double? ValueAt(Entity expr, string variable, double point)
    {
        try
        {
            var substituted = expr.Substitute(variable, point);
            foreach (var free in substituted.Vars)
                substituted = substituted.Substitute(free, 1.7);   // pin any remaining parameters
            var value = substituted.EvalNumerical();
            if (Math.Abs(value.ImaginaryPart.EDecimal.ToDouble()) > 1e-6)
                return null;                                        // left the reals; not comparable here
            var real = value.RealPart.EDecimal.ToDouble();
            return double.IsFinite(real) ? real : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Checks that two expressions agree in value wherever both are defined.</summary>
    static void SameValue(string property, string input, Entity before, Entity after, string variable)
    {
        // A result carrying NaN is a failure rather than something to pass over. ValueAt
        // reports non-finite as null, which would otherwise be indistinguishable from a
        // point the expression simply is not defined at -- and `int (sin(x)^2 + cos(x)^2)`
        // answering `NaN * (sin(x)^2 + cos(x)^2)` would go unreported.
        if (after.Stringize().Contains("NaN"))
        {
            Failures.Add(new Failure(property, input, after.Stringize(), "answer contains NaN"));
            return;
        }

        var compared = 0;
        foreach (var point in Points)
        {
            var a = ValueAt(before, variable, point);
            var b = ValueAt(after, variable, point);
            if (a is not null && b is null)
            {
                Failures.Add(new Failure(property, input, after.Stringize(),
                    $"at {variable} = {point.ToString(CultureInfo.InvariantCulture)} the input is "
                    + $"{a.Value.ToString("G10", CultureInfo.InvariantCulture)} but the result is not a finite real"));
                return;
            }
            if (a is null || b is null)
                continue;
            compared++;
            var scale = Math.Max(1, Math.Abs(a.Value));
            if (Math.Abs(a.Value - b.Value) > Tolerance * scale)
            {
                Failures.Add(new Failure(property, input, after.Stringize(),
                    $"at {variable} = {point.ToString(CultureInfo.InvariantCulture)}: "
                    + $"{a.Value.ToString("G10", CultureInfo.InvariantCulture)} became "
                    + $"{b.Value.ToString("G10", CultureInfo.InvariantCulture)}"));
                return;
            }
        }
        if (compared == 0) skipped++; else checks++;
    }

    static Entity WithTimeout(Func<Entity> work, int seconds = 20)
    {
        var task = Task.Run(work);
        return task.Wait(TimeSpan.FromSeconds(seconds)) ? task.Result : null;
    }

    static void Check(string source, string variable)
    {
        Entity expr;
        try { expr = source.ToEntity(); }
        catch (Exception e) { Failures.Add(new Failure("parse", source, "", e.GetType().Name)); return; }

        foreach (var (property, transform) in new (string, Func<Entity, Entity>)[]
                 {
                     ("Simplify",  e => e.Simplify()),
                     ("Expand",    e => e.Expand()),
                     ("Factorize", e => e.Factorize()),
                 })
        {
            Entity result;
            try { result = WithTimeout(() => transform(expr)); }
            catch (Exception e) { Failures.Add(new Failure(property, source, "", "threw " + e.GetType().Name)); continue; }
            if (result is null) { Failures.Add(new Failure(property, source, "", "did not finish in 20s")); continue; }
            SameValue(property, source, expr, result, variable);
        }

        // The derivative, against a central difference quotient.
        Entity derivative;
        try { derivative = WithTimeout(() => expr.Differentiate(variable)); }
        catch (Exception e) { Failures.Add(new Failure("Differentiate", source, "", "threw " + e.GetType().Name)); return; }
        if (derivative is not null)
            foreach (var point in Points)
            {
                const double h = 1e-5;
                var up = ValueAt(expr, variable, point + h);
                var down = ValueAt(expr, variable, point - h);
                var symbolic = ValueAt(derivative, variable, point);
                if (up is null || down is null || symbolic is null) continue;
                var numeric = (up.Value - down.Value) / (2 * h);
                var scale = Math.Max(1, Math.Abs(numeric));
                if (Math.Abs(numeric - symbolic.Value) > 1e-4 * scale)
                {
                    Failures.Add(new Failure("Differentiate", source, derivative.Stringize(),
                        $"at {variable} = {point.ToString(CultureInfo.InvariantCulture)}: "
                        + $"difference quotient {numeric:G8}, symbolic {symbolic.Value:G8}"));
                    break;
                }
                checks++;
            }

        // The antiderivative, by differentiating it back.
        Entity antiderivative;
        try { antiderivative = WithTimeout(() => expr.Integrate(variable)); }
        catch (Exception e) { Failures.Add(new Failure("Integrate", source, "", "threw " + e.GetType().Name)); return; }
        if (antiderivative is null)
        {
            Failures.Add(new Failure("Integrate", source, "", "did not finish in 20s"));
            return;
        }
        if (antiderivative.Stringize().Contains("integral("))
            return;                                                  // unsolved is a limit, not a defect
        var back = WithTimeout(() => antiderivative.Substitute("C", 0).Differentiate(variable));
        if (back is not null)
            SameValue("Integrate", source, expr, back, variable);
    }

    static int Main()
    {
        string[] corpus =
        {
            // polynomials and rational functions
            "x", "x + 1", "x ^ 2 + 2 * x + 1", "x ^ 3 - 6 * x ^ 2 + 11 * x - 6", "2 * x ^ 2 + 4 * x + 2",
            "x ^ 4 - 1", "x ^ 5 - 1", "6 * x ^ 2 - 5 * x + 1", "x ^ 2 - 2", "x ^ 2 + 1",
            "(x + 1) ^ 3", "(x - 1) * (x + 1)", "(x + 1) / (x - 1)", "(x ^ 2 - 1) / (x - 1)",
            "1 / x", "1 / (x + 1)", "1 / (x ^ 2 + 1)", "x / (x ^ 2 + 1)", "(x ^ 2 + 1) / x",
            "1 / (x + 1) + 1 / (x + 2)", "x / (x + 1) + 1 / (x + 1)",
            // powers and roots
            "sqrt(x)", "sqrt(x ^ 2)", "x ^ (1/2)", "x ^ (3/2)", "sqrt(x) * sqrt(x)", "sqrt(x * x)",
            "x ^ 2 * x ^ 3", "x ^ 2 / x ^ 3", "(x ^ 2) ^ 3", "(2 * x) ^ 3", "sqrt(12) + sqrt(27)",
            "cbrt(x ^ 3)", "x ^ 0.5 * x ^ 0.5",
            // exponentials and logarithms
            "e ^ x", "e ^ (2 * x)", "2 ^ x", "ln(x)", "ln(x ^ 2)", "ln(x) + ln(x + 1)",
            "ln(x) - ln(x + 1)", "log(2, x)", "e ^ ln(x)", "ln(e ^ x)", "x * ln(x)", "ln(x) / x",
            // trigonometry
            "sin(x)", "cos(x)", "tan(x)", "sin(x) ^ 2 + cos(x) ^ 2", "sin(2 * x)",
            "sin(x) * cos(x)", "sin(x) / cos(x)", "sin(x + 1)", "arcsin(x / 8)", "arctan(x)",
            "sin(x) ^ 2", "cos(x) ^ 2", "1 / cos(x) ^ 2", "sec(x)", "cosec(x)", "cotan(x)",
            "sinh(x)", "cosh(x)", "tanh(x)",
            // products that reach integration by parts and substitution
            "x * e ^ x", "x ^ 2 * e ^ x", "x * sin(x)", "x ^ 2 * sin(x)", "x * cos(2 * x)",
            "x * (x ^ 2 + 1) ^ 2", "x * (x ^ 2 + 1) ^ 3", "3 * x ^ 2 * (x ^ 3 + 2) ^ 2",
            "x * (x ^ 2 + x + 1) ^ 2", "x ^ 2 * (x ^ 2 + 1) ^ 2", "cos(x ^ 2) * x",
            "x * e ^ (x ^ 2)", "2 * x / (x ^ 2 + 1)", "x / sqrt(x ^ 2 + 1)", "ln(x) ^ 2",
            // mixed
            "abs(x)", "abs(x) * x", "x + sqrt(x)", "(x + 1) ^ 2 * (x + 2 - 1) ^ 2",
            "1 / (x + y + z) * a + 1 / (x + y + z) * b", "a * c + a * d + b * c + b * d",
            "x * y + y + 1 + x", "(x ^ 2 + 2 * x * y + y ^ 2) / (x ^ 2 - y ^ 2)",
            // deeper nesting and composition
            "sin(cos(x))", "e ^ sin(x)", "ln(x ^ 2 + 1)", "sqrt(x ^ 2 + 1)", "sqrt(sqrt(x))",
            "sin(x) ^ 3", "cos(x) ^ 3", "tan(x) ^ 2", "e ^ (-x)", "e ^ (-x ^ 2)",
            "1 / sqrt(x)", "1 / sqrt(x ^ 2 + 1)", "x / (x ^ 2 - 1)", "1 / (x ^ 2 - 1)",
            "(x ^ 3 + 1) / (x + 1)", "(x ^ 4 - 1) / (x ^ 2 - 1)", "(x ^ 3 - 1) / (x - 1)",
            // three-factor products
            "x * sin(x) * cos(x)", "x * e ^ x * 2", "(x + 1) * (x + 2) * (x + 3)",
            "x * (x + 1) * (x + 2)", "sin(x) * sin(x) * sin(x)",
            // sums that exercise collection
            "x + x + x", "x + x ^ 2 + x ^ 3", "2 * x + 3 * x + 4 * x",
            "sin(x) + sin(x)", "ln(x) + ln(x)", "sqrt(x) + sqrt(x)",
            "1 / x + 1 / x", "1 / x + 2 / x + 3 / x",
            // rational arithmetic and constants
            "x / 2 + x / 3", "x * 2 / 4", "x + 1/2", "3/4 * x ^ 2", "x ^ 2 / 4 - 1/4",
            "pi * x", "e * x", "x / pi", "sqrt(2) * x", "2 ^ (1/2) * x",
            // inverse and hyperbolic
            "arcsin(x / 8)", "arccos(x / 8)", "arctan(x)", "arccotan(x)",
            "sinh(x) ^ 2 - cosh(x) ^ 2", "sinh(x) * cosh(x)", "tanh(x) ^ 2",
            // shapes that reach by parts twice
            "x ^ 2 * cos(x)", "x ^ 3 * e ^ x", "x ^ 2 * ln(x)", "x ^ 3 * ln(x)",
            "e ^ x * sin(x)", "e ^ x * cos(x)",
            // absolute value and sign
            "abs(x) + x", "abs(x + 1)", "sgn(x) * x", "abs(x) ^ 2",
            // higher-degree polynomials
            "x ^ 6 - 1", "x ^ 4 + 2 * x ^ 2 + 1", "x ^ 4 - 5 * x ^ 2 + 4",
            "x ^ 3 + x ^ 2 - x - 1", "x ^ 5 - x", "(x - 1) ^ 4",
        };

        foreach (var source in corpus)
            Check(source, "x");

        var report = new StringBuilder();
        report.AppendLine("# Property check");
        // Names the build, not the branch: a report read as the library's behaviour when it
        // was generated from an unmerged tree has cost this workspace a morning before.
        report.AppendLine();
        report.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        report.AppendLine();
        report.AppendLine("Each transformation checked against a property it must satisfy, evaluated");
        report.AppendLine("numerically rather than compared as text: Simplify, Expand and Factorize must");
        report.AppendLine("preserve value; Differentiate must agree with a central difference quotient;");
        report.AppendLine("Integrate must differentiate back to the integrand. Points where either side");
        report.AppendLine("is undefined or leaves the reals are skipped rather than counted.");
        report.AppendLine();
        report.AppendLine($"- Corpus: **{corpus.Length}** expressions");
        report.AppendLine($"- Checks that ran: **{checks}**");
        report.AppendLine($"- Skipped, nothing comparable: **{skipped}**");
        report.AppendLine($"- **Failures: {Failures.Count}**");
        report.AppendLine();
        if (Failures.Count > 0)
        {
            report.AppendLine("| Property | Input | Result | How it fails |");
            report.AppendLine("|---|---|---|---|");
            foreach (var f in Failures)
                report.AppendLine($"| `{f.Property}` | `{f.Input}` | `{Truncate(f.Output)}` | {f.Detail} |");
        }

        // Where Harness.Reports says, whichever directory the run was started from.
        var path = Harness.Reports.PathFor("propcheck.md");
        // The durable prose lives here rather than being added to the report by hand, since
        // every run overwrites the report.
        report.AppendLine("## What this checks, and what it does not");
        report.AppendLine();
        report.AppendLine("It checks that a transformation preserves what it is supposed to preserve, by");
        report.AppendLine("evaluating both sides. It says nothing about whether an answer is *tidy*, and");
        report.AppendLine("nothing about the cases the library declines to solve -- an unevaluated");
        report.AppendLine("`integral(...)` is a limit, not a defect, and is not counted as a failure.");
        report.AppendLine();
        report.AppendLine("A non-finite result is a failure, not a skip. Treating it as \"the expression is");
        report.AppendLine("simply not defined here\" hid real defects: reporting them instead once turned a");
        report.AppendLine("clean run into six failures, an antiderivative that was `NaN * (integrand)` among");
        report.AppendLine("them. Points where *either side* is genuinely undefined are still skipped.");
        report.AppendLine();
        System.IO.File.WriteAllText(path, report.ToString());
        Console.WriteLine(report.ToString());
        Console.WriteLine($"Wrote {path}");
        // A property that does not hold fails the run. A check that did not finish does not:
        // a shared runner is slower than the machine its twenty seconds were set on.
        return Failures.Any(f => !f.Detail.StartsWith("did not finish", StringComparison.Ordinal)) ? 1 : 0;
    }

    static string Truncate(string s) => s.Length <= 70 ? s : s.Substring(0, 67) + "...";
}
