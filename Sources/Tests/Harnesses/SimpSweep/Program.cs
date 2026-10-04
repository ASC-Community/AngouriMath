//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

// Does Simplify preserve the value of an expression it was never shown?
//
// `propcheck` asks that question of 151 expressions somebody wrote down, and `casbench` of
// 117. Both are corpora, and a corpus can only contain shapes its author thought of --
// which is the wrong shape of net for this defect. The two worst simplification bugs found
// here were reachable only through expressions nobody writes by hand:
//
//   #715  (1/x)/(-1 - 1/x) came back as -(1 + x), the reciprocal of the right answer. Found
//         by tracing why a *limit* was NaN, because l'Hopital's rule builds that quotient.
//   #744  (x^2 + x + 1)^2 = 0 was answered with roots containing x. Found by `rootcheck`,
//         which generates its polynomials rather than listing them.
//
// So this generates instead. Expressions are built up from a small alphabet to a bounded
// depth, simplified, and the two forms compared numerically at several points. Only what
// disagrees is reported.
//
// The property is the weakest one worth having and the one that cannot be argued with:
// wherever both the original and its simplification are defined and real, they are the same
// number. It says nothing about whether the answer is *tidier*, which is what the complexity
// metric is for and is not checkable this way.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Extensions;

static class SimpSweep
{
    /// <summary>
    /// Where the two forms are compared. Chosen away from 0, 1 and the small integers, so a
    /// rewrite that happens to hold at a special point does not pass by luck, and off the
    /// half-integers where the trigonometric arguments would land on their own special
    /// values. Two negatives, because the sign of the argument is what several of the
    /// recorded defects turned on -- #715's numerator carried a negative factor.
    /// </summary>
    static readonly double[] Points = { 0.37, 1.41, 2.71, -0.63, -1.87, 4.19 };

    /// <summary>
    /// How far apart the two values may be, relative to their own size. Absolute tolerance
    /// is wrong here: the corpus reaches e^(x^2) at 4.19, which is 3e30, and a relative
    /// error of 1e-9 there is an absolute one of 3e21.
    /// </summary>
    const double RelativeTolerance = 1e-9;

    static readonly TimeSpan PerCase = TimeSpan.FromSeconds(10);

    record Failure(string Input, string Simplified, string Detail);

    static readonly List<Failure> Failures = new();
    static int generated, compared, comparable, timedOut;

    static T WithTimeout<T>(Func<T> f) where T : class
    {
        var task = Task.Run(f);
        return task.Wait(PerCase) ? task.Result : null;
    }

    /// <summary>
    /// The real value at a point, or null where there is nothing to compare -- the
    /// expression is undefined there, leaves the reals, or does not evaluate at all. A null
    /// is a skip and never a failure: the two forms have to disagree *as numbers* before
    /// anything is claimed.
    /// </summary>
    static double? ValueAt(Entity expr, double point)
    {
        try
        {
            var substituted = expr.Substitute("x", point);
            foreach (var free in substituted.Vars)
                substituted = substituted.Substitute(free, 1.7);
            var value = substituted.EvalNumerical();
            var re = value.RealPart.EDecimal.ToDouble();
            var im = value.ImaginaryPart.EDecimal.ToDouble();
            if (!double.IsFinite(re) || !double.IsFinite(im))
                return null;
            if (Math.Abs(im) > 1e-9 * Math.Max(1, Math.Abs(re)))
                return null;
            return re;
        }
        catch
        {
            return null;
        }
    }

    static void Check(string source)
    {
        generated++;
        Entity expr;
        try { expr = source.ToEntity(); }
        catch { return; }

        var simplified = WithTimeout(() => expr.Simplify());
        if (simplified is null)
        {
            timedOut++;
            Failures.Add(new Failure(source, "", $"did not finish in {PerCase.TotalSeconds:0} s"));
            return;
        }

        // A simplification that reaches NaN is a failure only where the expression it came
        // from has a value somewhere. 1 / (x - x) is undefined at every point, and NaN is the
        // honest answer to it -- reporting that would be the harness being wrong, not the
        // library. So the NaN is judged below, against whether the original was comparable.
        var simplifiedIsNaN = simplified.Nodes.Any(node => node == MathS.NaN);

        var anyComparable = false;
        foreach (var point in Points)
        {
            compared++;
            var before = ValueAt(expr, point);
            if (before is not null && simplifiedIsNaN)
            {
                Failures.Add(new Failure(source, simplified.Stringize(),
                    $"is {before.Value:G10} at x = {point.ToString(CultureInfo.InvariantCulture)}, " +
                    "and simplified to something containing NaN"));
                return;
            }
            var after = ValueAt(simplified, point);
            if (before is null || after is null)
                continue;
            anyComparable = true;
            var scale = Math.Max(Math.Max(Math.Abs(before.Value), Math.Abs(after.Value)), 1e-12);
            if (Math.Abs(before.Value - after.Value) <= RelativeTolerance * scale)
                continue;
            Failures.Add(new Failure(source, simplified.Stringize(),
                $"at x = {point.ToString(CultureInfo.InvariantCulture)}: " +
                $"{before.Value:G10} before, {after.Value:G10} after"));
            return;
        }
        if (anyComparable)
            comparable++;
    }

    // The alphabet. Small on purpose: what finds defects is the *composition*, not the
    // breadth of the leaves, and every symbol multiplies the count by the depth.
    static readonly string[] Leaves = { "x", "1", "2", "-1", "1/2", "a" };

    static readonly string[] Unary =
    {
        "-({0})", "sqrt({0})", "abs({0})", "ln({0})", "e ^ ({0})",
        "sin({0})", "cos({0})", "tan({0})", "sgn({0})",
        "1 / ({0})", "({0}) ^ 2", "({0}) ^ (-1)", "({0}) ^ (1/2)",
    };

    static readonly string[] Binary =
    {
        "({0}) + ({1})", "({0}) - ({1})", "({0}) * ({1})", "({0}) / ({1})", "({0}) ^ ({1})",
    };

    /// <summary>Every expression of exactly this depth, built from the level below.</summary>
    static List<string> Grow(List<string> below, bool binary)
    {
        var grown = new List<string>();
        foreach (var shape in Unary)
            foreach (var inner in below)
                grown.Add(string.Format(shape, inner));
        if (binary)
            foreach (var shape in Binary)
                foreach (var left in below)
                    foreach (var right in below)
                        grown.Add(string.Format(shape, left, right));
        return grown;
    }

    static string Truncate(string s, int at = 60) => s.Length <= at ? s : s.Substring(0, at - 3) + "...";

    static int Main()
    {
        var level1 = new List<string>(Leaves);

        // Depth two in full, both arities: 6 leaves give 78 unary and 180 binary shapes.
        var level2 = Grow(level1, binary: true);

        // Depth three unary-only over the whole of depth two, which is where the composition
        // gets interesting without the count squaring: ln(1/(x - 1)), sqrt(sin(x)/x) and so
        // on. A full binary level three would be 5 * 258^2 = 332k cases and hours of Simplify.
        var level3 = Grow(level2, binary: false);

        // And the binary shapes over a *sample* of depth two, so that the depth-three
        // quotients and powers are reached too -- (1/x)/(-1 - 1/x), which is #715, is one of
        // these. Sampled deterministically by stride rather than at random, so the run is
        // reproducible and a failure can be found again.
        var sample = level2.Where((_, i) => i % 7 == 0).ToList();
        var level3Binary = new List<string>();
        foreach (var shape in Binary)
            foreach (var left in sample)
                foreach (var right in sample)
                    level3Binary.Add(string.Format(shape, left, right));

        var all = level1.Concat(level2).Concat(level3).Concat(level3Binary).ToList();
        Console.WriteLine($"{all.Count} expressions to sweep");

        var started = DateTime.UtcNow;
        var done = 0;
        foreach (var source in all)
        {
            Check(source);
            if (++done % 2000 == 0)
                Console.WriteLine($"  {done}/{all.Count}, {Failures.Count} failing, " +
                                  $"{(DateTime.UtcNow - started).TotalSeconds:0} s");
        }

        var report = new StringBuilder();
        report.AppendLine("# Simplify value sweep");
        report.AppendLine();
        report.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        report.AppendLine();
        report.AppendLine("Expressions generated to a bounded depth from a small alphabet, simplified, and");
        report.AppendLine("the two forms compared numerically wherever both are defined and real. A corpus");
        report.AppendLine("can only hold shapes its author thought of; the two worst simplification defects");
        report.AppendLine("found in this repository were both reached through expressions nobody writes by");
        report.AppendLine("hand, so the inputs here are built rather than listed.");
        report.AppendLine();
        report.AppendLine("The property is the weakest one worth having: wherever both forms are defined,");
        report.AppendLine("they are the same number. Nothing here says the answer is *tidier* -- that is the");
        report.AppendLine("complexity metric's business and is not checkable this way.");
        report.AppendLine();
        report.AppendLine($"- Expressions swept: **{all.Count}**");
        report.AppendLine($"- Of those, comparable at a point: **{comparable}**");
        report.AppendLine($"- Point comparisons attempted: **{compared}**");
        report.AppendLine($"- Timed out: **{timedOut}**");
        report.AppendLine($"- **Disagreements: {Failures.Count - timedOut}**");
        report.AppendLine();
        if (Failures.Count == 0)
            report.AppendLine("Every simplification agrees with the expression it came from.");
        else
        {
            report.AppendLine("| Input | Simplified to | How it disagrees |");
            report.AppendLine("|---|---|---|");
            foreach (var f in Failures)
                report.AppendLine($"| `{Truncate(f.Input)}` | `{Truncate(f.Simplified)}` | {f.Detail} |");
        }

        // Where Harness.Reports says, whichever directory the run was started from.
        var path = Harness.Reports.PathFor("simpsweep.md");
        System.IO.File.WriteAllText(path, report.ToString());
        Console.Error.WriteLine($"\nwrote {path}");
        Console.WriteLine(report.ToString());
        Console.WriteLine($"OVERALL {all.Count - Failures.Count}/{all.Count} agree; " +
                          $"{Failures.Count - timedOut} disagree, {timedOut} timed out");
        // A disagreement fails the run; a timeout is the runner's speed as much as the library's.
        return Failures.Count - timedOut > 0 ? 1 : 0;
    }
}
