//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;

namespace BoundCheck;

/// <summary>
/// Does <see cref="Entity.Simplify"/> keep the value <em>at the boundary</em> — off the real
/// line, across a branch cut, outside a principal interval, at a pole?
/// </summary>
/// <remarks>
/// The other harnesses here cannot answer that, and the reason is structural rather than a
/// gap in their corpora. `simpsweep` samples real points, so a rewrite that is wrong only off
/// the real line never disagrees with anything; and it builds its expressions from a grammar
/// that never nests a function inside its own inverse, so that shape is outside its space
/// entirely. Four rules were wrong from 2020 to
/// https://github.com/asc-community/AngouriMath/issues/884 with every harness green.
///
/// So this one is built the other way round: the shapes come from the *nodes* — every unary
/// function the library has, composed with every other, found by reflection so that a node
/// added later is covered without anyone remembering — and the points are chosen to sit
/// exactly where an assumption fails.
///
/// See Docs/Contributing/SimplificationContract.md for what a rule is allowed to assume; this
/// is the mechanical half of it.
/// </remarks>
static class Program
{
    /// How far apart two values may be, relative to their own size.
    const double RelativeTolerance = 1e-9;

    /// A single Simplify may not hold the run up. Hitting this is reported, not swallowed.
    static readonly TimeSpan PerCase = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Where an assumption fails. Not a sweep: every point is here because some rule's
    /// justification breaks at it.
    /// </summary>
    static readonly (string Name, Complex Value)[] Points =
    {
        // Outside every principal interval, one period out in each direction. This is the
        // #884 class: arcsin(sin(3)) is pi - 3, not 3.
        ("3", new Complex(3, 0)),
        ("-3", new Complex(-3, 0)),
        ("4", new Complex(4, 0)),
        ("2", new Complex(2, 0)),
        ("-2", new Complex(-2, 0)),
        ("pi", new Complex(Math.PI, 0)),
        ("2*pi", new Complex(2 * Math.PI, 0)),

        // Inside them, so that a rule which stops firing altogether is not mistaken for a
        // rule which fires correctly.
        ("1/2", new Complex(0.5, 0)),
        ("-1/3", new Complex(-1.0 / 3, 0)),

        // The two values that make a rule's own arithmetic degenerate rather than its branch:
        // a logarithm to base 1 divides by ln(1) = 0, and 0 is where a reciprocal, a
        // logarithm's argument and 0^0 all give out. Without these, log(x, 1) -> 0 -- which is
        // right everywhere except at x = 1 -- looks sound at every point tried.
        ("1", Complex.One),
        ("0", Complex.Zero),

        // The negative reals: the branch cut of ln, of sqrt, and of every fractional power.
        ("-1", new Complex(-1, 0)),
        ("-1/2", new Complex(-0.5, 0)),
        ("-8", new Complex(-8, 0)),

        // Off the real line, which is where a rule that is fine over RR and false over CC
        // shows itself. ln(e^(3*pi*i)) is pi*i and not 3*pi*i.
        ("i", Complex.ImaginaryOne),
        ("-i", -Complex.ImaginaryOne),
        ("1+i", new Complex(1, 1)),
        ("-1+i", new Complex(-1, 1)),
        ("2+3i", new Complex(2, 3)),
        ("3*pi*i", new Complex(0, 3 * Math.PI)),
        ("i/2", new Complex(0, 0.5)),

        // Either side of the poles of tan and cotan, and near zero, where a cancelled
        // singularity would show.
        ("pi/2 + 1/100", new Complex(Math.PI / 2 + 0.01, 0)),
        ("pi/2 - 1/100", new Complex(Math.PI / 2 - 0.01, 0)),
        ("1/1000", new Complex(0.001, 0)),
        ("-1/1000", new Complex(-0.001, 0)),
    };

    /// <summary>
    /// Shapes whose soundness turns on something other than a single unary composition, and
    /// which the pairing below cannot build. Each is a rewrite the contract discusses.
    /// </summary>
    static readonly string[] BinaryShapes =
    {
        "ln(e^x)", "e^ln(x)", "log(2, 2^x)", "2^log(2, x)",
        "ln(x^2)", "ln(x^3)", "log(x, x)",
        // The degenerate logarithm bases and arguments, where log_b(z) = ln z / ln b has a
        // zero or an infinity in it. https://github.com/asc-community/AngouriMath/issues/890
        "log(x, 1)", "log(x, 0)", "log(1, x)", "log(0, x)", "log(1, 1)", "log(1/2, 0)",
        "sqrt(x^2)", "sqrt(x)^2", "(x^2)^(1/2)", "(x^(1/2))^2",
        "(x^2)^(1/3)", "(x^3)^(1/3)", "(x^(1/3))^3", "(x^6)^(1/6)",
        "x^2/x", "x/x", "x^3/x^2", "(x^2-1)/(x-1)",
        "abs(x)^2", "abs(x^2)", "abs(-x)", "abs(x)/x", "x/abs(x)",
        "sgn(x)*abs(x)", "abs(x)*sgn(x)",
        "sqrt(x)*sqrt(x)", "sqrt(-x)", "sqrt(1/x)", "1/sqrt(x)",
        // A power of a reciprocal beside a power of its denominator, which splitting the power
        // of the quotient got wrong for every negative x.
        // https://github.com/asc-community/AngouriMath/issues/1734
        "sqrt(x)*sqrt(1/x)", "sqrt(1/x)*x", "(1/x)^(3/2)*x^(3/2)", "sec(x)^(3/2)*cos(x)^(3/2)",
        "(-x)^(1/3)", "(-x)^(1/2)", "(2*x)^(1/2)",
        "tan(x)*cotan(x)", "sin(x)/cos(x)", "sin(x)^2+cos(x)^2",
        "arcsin(x)+arccos(x)", "arctan(x)+arccotan(x)",
        "ln(x)+ln(x+1)", "ln(x)-ln(x+1)", "2*ln(x)", "ln(x*x)",
    };

    static readonly List<Finding> Findings = new();
    static int shapes, rewritten, compared, timedOut, skipped;
    // The shapes Simplify left alone, by name. A count cannot tell you *which*, and the
    // count is the thing that moves silently: this harness went from 41 rewritten to 35
    // across a fortnight of merges with 0 disagreements throughout, and nothing recorded
    // said which six stopped being rewritten or whether that was a guard or a loss.
    static readonly List<string> Untouched = new();

    record Finding(string Source, string Simplified, string Detail);

    static int Main()
    {
        var expressions = new List<string>(BinaryShapes);
        expressions.AddRange(UnaryCompositions());

        Console.WriteLine($"{expressions.Count} shapes, {Points.Length} boundary points each");

        foreach (var source in expressions.Distinct())
            Check(source);

        Report();
        return Findings.Count == 0 ? 0 : 1;
    }

    /// <summary>
    /// Every unary function the library has, composed with every unary function including
    /// itself, both orders. Found by reflection, so a node added later is covered without
    /// anyone having to remember this file.
    /// </summary>
    static IEnumerable<string> UnaryCompositions()
    {
        var names = UnaryFunctionNames().ToList();
        Console.WriteLine($"{names.Count} unary function nodes found by reflection: "
                          + string.Join(", ", names));
        foreach (var outer in names)
            foreach (var inner in names)
                yield return $"{outer}({inner}(x))";
    }

    /// <summary>
    /// The parser name of every <see cref="IUnaryNode"/> under <see cref="Entity"/>, taken by
    /// building one and reading what it prints rather than by keeping a list in step by hand.
    /// </summary>
    static IEnumerable<string> UnaryFunctionNames()
    {
        Entity x = "x";
        foreach (var type in typeof(Entity).GetNestedTypes(BindingFlags.Public)
                     .Where(t => !t.IsAbstract && typeof(IUnaryNode).IsAssignableFrom(t))
                     .OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            var constructor = type.GetConstructor(new[] { typeof(Entity) });
            if (constructor is null) continue;
            string printed;
            try { printed = ((Entity)constructor.Invoke(new object[] { x })).Stringize(); }
            catch { continue; }
            // A function node prints as `name(x)`; anything else -- an operator spelling, a
            // node that prints its argument in brackets of its own -- is left out rather than
            // guessed at, since this has to produce something the parser reads back.
            var open = printed.IndexOf('(');
            if (open <= 0 || !printed.EndsWith(")")) continue;
            var name = printed.Substring(0, open);
            if (!name.All(c => char.IsLetterOrDigit(c))) continue;
            if (printed != $"{name}(x)") continue;
            yield return name;
        }
    }

    static void Check(string source)
    {
        shapes++;
        Entity expr;
        try { expr = source.ToEntity(); }
        catch { skipped++; return; }

        var simplified = WithTimeout(() => expr.Simplify());
        if (simplified is null)
        {
            timedOut++;
            Findings.Add(new Finding(source, "", $"did not finish in {PerCase.TotalSeconds:0} s"));
            return;
        }

        // Nothing was rewritten, so there is no claim to check. Reported as a count, because
        // "the rule did not fire" is not the same as "the rule is sound" -- a rewrite that
        // loses on complexity here may win on a neighbouring shape.
        if (simplified == expr) { Untouched.Add(source); return; }
        rewritten++;

        foreach (var (name, point) in Points)
        {
            var before = ValueAt(expr, point);
            var after = ValueAt(simplified, point);

            // Both undefined is agreement: 1/(x - x) has no value anywhere and NaN is the
            // honest answer to it. What is forbidden is one of them having a value.
            if (before is null && after is null) continue;
            compared++;

            if (before is null || after is null)
            {
                Findings.Add(new Finding(source, simplified.Stringize(),
                    $"at x = {name} the original is "
                    + (before is null ? "undefined" : Show(before.Value))
                    + " and the simplification is "
                    + (after is null ? "undefined" : Show(after.Value))));
                break;
            }

            var scale = Math.Max(Math.Max(before.Value.Magnitude, after.Value.Magnitude), 1e-12);
            if ((before.Value - after.Value).Magnitude <= RelativeTolerance * scale) continue;

            Findings.Add(new Finding(source, simplified.Stringize(),
                $"at x = {name}: {Show(before.Value)} before, {Show(after.Value)} after"));
            break;
        }
    }

    static string Show(Complex z) =>
        Math.Abs(z.Imaginary) < 1e-12
            ? z.Real.ToString("G10", CultureInfo.InvariantCulture)
            : z.Real.ToString("G10", CultureInfo.InvariantCulture) + " + "
              + z.Imaginary.ToString("G10", CultureInfo.InvariantCulture) + "i";

    /// <summary>The value at a complex point, or null where there is none to read.</summary>
    static Complex? ValueAt(Entity expr, Complex point)
    {
        try
        {
            var substituted = expr.Substitute("x",
                Entity.Number.Complex.Create(
                    Entity.Number.Real.Create(PeterO.Numbers.EDecimal.FromDouble(point.Real)),
                    Entity.Number.Real.Create(PeterO.Numbers.EDecimal.FromDouble(point.Imaginary))));
            var evaled = WithTimeout(() => substituted.EvalNumerical());
            if (evaled is not Entity.Number.Complex value) return null;
            var real = (double)value.RealPart;
            var imaginary = (double)value.ImaginaryPart;
            if (double.IsNaN(real) || double.IsNaN(imaginary)
                || double.IsInfinity(real) || double.IsInfinity(imaginary)) return null;
            return new Complex(real, imaginary);
        }
        catch
        {
            return null;
        }
    }

    static Entity WithTimeout(Func<Entity> work)
    {
        try
        {
            var task = Task.Run(work);
            return task.Wait(PerCase) ? task.Result : null;
        }
        catch
        {
            return null;
        }
    }

    static void Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Simplification at the boundary");
        // Names the build, not the branch: a report describes the build it measured, which
        // need not be master's.
        sb.AppendLine();
        sb.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        sb.AppendLine();
        sb.AppendLine("Generated by `Sources/Tests/Harnesses/BoundCheck`. Every shape is simplified and then compared "
                      + "with the expression it came from at points chosen to sit where a rewrite's "
                      + "assumption fails -- outside a principal interval, across a branch cut, off "
                      + "the real line, beside a pole. Two values that are both undefined agree; "
                      + "what is reported is one of them having a value the other does not, or the "
                      + "two differing.");
        sb.AppendLine();
        sb.AppendLine("See `Docs/Contributing/SimplificationContract.md` for what a rule may assume.");
        sb.AppendLine();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Shapes | {shapes} |");
        sb.AppendLine($"| Of those, actually rewritten by `Simplify` | {rewritten} |");
        sb.AppendLine($"| Point comparisons | {compared} |");
        sb.AppendLine($"| **Disagreements** | **{Findings.Count(f => !f.Detail.StartsWith("did not"))}** |");
        sb.AppendLine($"| Did not finish | {timedOut} |");
        sb.AppendLine($"| Did not parse | {skipped} |");
        sb.AppendLine();
        if (Findings.Count == 0)
            sb.AppendLine("Every simplification keeps the value of the expression it came from at "
                          + "every boundary point tried.");
        else
        {
            sb.AppendLine("## What disagrees");
            sb.AppendLine();
            foreach (var f in Findings)
            {
                sb.AppendLine($"### `{f.Source}`");
                sb.AppendLine();
                if (f.Simplified.Length > 0)
                    sb.AppendLine($"simplifies to `{f.Simplified}`, and {f.Detail}.");
                else
                    sb.AppendLine(f.Detail + ".");
                sb.AppendLine();
            }
        }

        // Listed rather than counted, so that a change in what Simplify touches shows up in
        // the diff of this file instead of being a number that moved. This harness went from
        // 41 rewritten to 35 across a fortnight of merges with 0 disagreements throughout,
        // and nothing recorded said which six stopped being rewritten, or whether that was a
        // guard being added or a simplification being lost.
        sb.AppendLine();
        sb.AppendLine($"## Shapes `Simplify` left alone ({Untouched.Count})");
        sb.AppendLine();
        sb.AppendLine("Not a defect in itself -- a rule that does not fire here may fire on a");
        sb.AppendLine("neighbouring shape, or may have been guarded deliberately. Listed so that the");
        sb.AppendLine("set is comparable between runs rather than only its size.");
        sb.AppendLine();
        foreach (var u in Untouched.OrderBy(x => x, StringComparer.Ordinal))
            sb.AppendLine($"- `{u}`");
        sb.AppendLine();

        var path = Harness.Reports.PathFor("boundcheck.md");
        System.IO.File.WriteAllText(path, sb.ToString());

        Console.WriteLine($"boundcheck: {shapes} shapes, {rewritten} rewritten, {compared} comparisons "
                          + $"-- {Findings.Count(f => !f.Detail.StartsWith("did not"))} disagreements, "
                          + $"{timedOut} did not finish");
        Console.WriteLine($"Wrote {path}");
    }
}
