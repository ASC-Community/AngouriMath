//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

// Completeness checker for AngouriMath's equation solver.
//
// casbench asks whether an answer is *right*: it substitutes each root back and checks
// the equation holds. Nothing asked whether the answer was *all there*. Those are
// different properties, and only the first was being checked -- so
// `x^5 - 2x^3 - x^2 + 2 = 0` could come back as { 1, sqrt(2), -sqrt(2) }, every one of
// them a genuine root, with the pair (-1 +- i*sqrt(3))/2 missing and nothing to say so.
// A root set two roots short is indistinguishable from a complete one by substitution.
//
// So: build polynomials by multiplying factors whose roots are known before anything is
// solved, hand the solver both the product and its expansion, and check both directions.
//
//   sound     -- every root that comes back satisfies the equation
//   complete  -- every root that went in comes back
//
// Multiplying known factors is what makes this possible. Taking an arbitrary polynomial
// and asking whether the answer is complete needs a second solver to compare against;
// constructing it from its roots means the answer is known in advance and costs nothing.
//
// Roots are compared numerically. An exact comparison would fail on the difference
// between sqrt(3) and the same number written as -1/4 + (1/2 + 2*sqrt(2))/2, which is a
// question about presentation and not about whether the root is there.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Extensions;

static class RootCheck
{
    /// <summary>How close a returned root has to be to a known one to count as it.</summary>
    const double RootTolerance = 1e-5;

    /// <summary>
    /// How large a residual has to be, next to the size of the terms it is the sum of,
    /// before the root is called unsound. Relative rather than absolute: a root of a
    /// polynomial with coefficients in the thousands leaves a larger residual than one
    /// with coefficients of 1 without being any less of a root.
    /// </summary>
    const double ResidualTolerance = 1e-6;

    /// <summary>No single equation may hold the run up for longer than this.</summary>
    static readonly TimeSpan PerCase = TimeSpan.FromSeconds(20);

    record Factor(string Text, Complex[] Roots);

    record Case(string Form, string Product, int Degree, string Verdict,
                int Returned, int Missing, int Unsound, string Detail);

    static readonly List<Case> Cases = new();

    static Factor Linear(double root, string text) => new(text, new[] { new Complex(root, 0) });

    // Every factor's roots are written down rather than solved for. Linear factors with
    // whole and with fractional roots; quadratics that are irreducible over the rationals
    // in the two ways that matters here -- real irrational roots, and complex ones --
    // because those reach the solver by different routes.
    static readonly Factor[] Pool =
    {
        Linear(1, "(x - 1)"),
        Linear(-1, "(x + 1)"),
        Linear(2, "(x - 2)"),
        Linear(-2, "(x + 2)"),
        Linear(3, "(x - 3)"),
        Linear(0.5, "(2 * x - 1)"),
        new("(x ^ 2 - 2)", new[] { new Complex(Math.Sqrt(2), 0), new Complex(-Math.Sqrt(2), 0) }),
        new("(x ^ 2 - 3)", new[] { new Complex(Math.Sqrt(3), 0), new Complex(-Math.Sqrt(3), 0) }),
        new("(x ^ 2 - 5)", new[] { new Complex(Math.Sqrt(5), 0), new Complex(-Math.Sqrt(5), 0) }),
        new("(x ^ 2 + 1)", new[] { new Complex(0, 1), new Complex(0, -1) }),
        new("(x ^ 2 + 2)", new[] { new Complex(0, Math.Sqrt(2)), new Complex(0, -Math.Sqrt(2)) }),
        new("(x ^ 2 + x + 1)", new[]
        {
            new Complex(-0.5, Math.Sqrt(3) / 2), new Complex(-0.5, -Math.Sqrt(3) / 2)
        }),
    };

    static Complex? Numeric(Entity entity)
    {
        try
        {
            if (entity.Vars.Any())
                return null;                    // a root standing for a family, not a number
            var value = entity.EvalNumerical();
            var re = value.RealPart.EDecimal.ToDouble();
            var im = value.ImaginaryPart.EDecimal.ToDouble();
            return double.IsFinite(re) && double.IsFinite(im) ? new Complex(re, im) : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// The residual the root leaves, and the scale to read it against.
    /// </summary>
    /// <remarks>
    /// The scale is the size of the other factors where this one vanishes, which is what
    /// the residual is actually sensitive to: near a root of factor f, the product moves
    /// like the distance from the root times everything f is multiplied by. So a root of
    /// (x - 1) inside a product whose other factors come to 500 there is allowed a
    /// residual 500 times larger than one standing alone, and neither is judged by an
    /// absolute number that happens to suit the small cases.
    /// </remarks>
    static (double Residual, double Scale)? Substituted(Entity polynomial, Factor[] factors, Entity root)
    {
        if (Numeric(polynomial.Substitute("x", root).Expand()) is not { } residual)
            return null;
        var scale = 1.0;
        foreach (var factor in factors)
            if (Numeric(factor.Text.ToEntity().Substitute("x", root).Expand()) is { } value)
                scale *= Math.Max(1.0, value.Magnitude);
        return (residual.Magnitude, scale);
    }

    static void Check(string form, string product, Entity polynomial, Factor[] factors, Entity asked, Complex[] known)
    {
        string verdict = "ok";
        int returned = 0, missing = 0, unsound = 0;
        var detail = new List<string>();

        try
        {
            var task = Task.Run(() => (asked.Stringize() + " = 0").ToEntity().Solve("x"));
            if (!task.Wait(PerCase))
            {
                Cases.Add(new Case(form, product, known.Length, "timeout", 0, known.Length, 0,
                    $"no answer in {PerCase.TotalSeconds:0} s"));
                return;
            }

            if (task.Result is not Entity.Set.FiniteSet roots)
            {
                Cases.Add(new Case(form, product, known.Length, "not-finite", 0, known.Length, 0,
                    $"answered `{Truncate(task.Result.Stringize())}`"));
                return;
            }

            returned = roots.Count;
            var numeric = roots.Select(Numeric).ToArray();

            foreach (var root in roots)
                if (Substituted(polynomial, factors, root) is { } measured
                    && measured.Residual > ResidualTolerance * measured.Scale)
                {
                    unsound++;
                    detail.Add($"`{Truncate(root.Stringize(), 40)}` leaves {measured.Residual:g3}");
                }

            foreach (var root in known)
                if (!numeric.Any(got => got is { } value && (value - root).Magnitude < RootTolerance))
                {
                    missing++;
                    detail.Add($"missing {Show(root)}");
                }

            verdict = unsound > 0 && missing > 0 ? "unsound+incomplete"
                    : unsound > 0 ? "unsound"
                    : missing > 0 ? "incomplete"
                    : "ok";
        }
        catch (Exception e)
        {
            verdict = "error";
            detail.Add(e.GetType().Name);
        }

        Cases.Add(new Case(form, product, known.Length, verdict, returned, missing, unsound,
            string.Join("; ", detail.Take(4))));
    }

    static string Show(Complex z) =>
        Math.Abs(z.Imaginary) < 1e-12
            ? z.Real.ToString("0.####", CultureInfo.InvariantCulture)
            : $"{z.Real.ToString("0.####", CultureInfo.InvariantCulture)}" +
              $"{(z.Imaginary < 0 ? "-" : "+")}{Math.Abs(z.Imaginary).ToString("0.####", CultureInfo.InvariantCulture)}i";

    static string Truncate(string s, int at = 70) => s.Length <= at ? s : s.Substring(0, at - 3) + "...";

    static int Main()
    {
        var combinations = new List<int[]>();
        for (var i = 0; i < Pool.Length; i++)
            for (var j = i + 1; j < Pool.Length; j++)
            {
                combinations.Add(new[] { i, j });
                for (var k = j + 1; k < Pool.Length; k++)
                    combinations.Add(new[] { i, j, k });
            }
        // A repeated factor is one root, not two, and is worth asking about separately:
        // it is the case where the count of roots and the degree legitimately disagree.
        for (var i = 0; i < Pool.Length; i++)
            combinations.Add(new[] { i, i });

        foreach (var combination in combinations)
        {
            var factors = combination.Select(index => Pool[index]).ToArray();
            var product = string.Join(" * ", factors.Select(factor => factor.Text));
            // Distinct, because a repeated factor contributes its root once.
            var known = factors.SelectMany(factor => factor.Roots)
                .GroupBy(root => (Math.Round(root.Real, 9), Math.Round(root.Imaginary, 9)))
                .Select(group => group.First()).ToArray();
            var polynomial = product.ToEntity();

            // Both forms, because they reach the solver differently: the product still
            // carries its factorization, the expansion has to have one found for it.
            Check("product", product, polynomial, factors, polynomial, known);
            Check("expanded", product, polynomial, factors, polynomial.Expand(), known);
        }

        var bad = Cases.Where(c => c.Verdict != "ok").ToList();
        var report = new StringBuilder();
        report.AppendLine("# Root completeness check");
        // Names the build, not the branch: a report describes the build it measured, which
        // need not be master's.
        report.AppendLine();
        report.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        report.AppendLine();
        report.AppendLine("Polynomials built by multiplying factors whose roots are known before anything");
        report.AppendLine("is solved, then handed to the solver both as the product and as its expansion.");
        report.AppendLine("Two properties, not one:");
        report.AppendLine();
        report.AppendLine("- **sound** -- every root that comes back satisfies the equation;");
        report.AppendLine("- **complete** -- every root that went in comes back.");
        report.AppendLine();
        report.AppendLine("Substituting roots back, which is what `casbench` does, can only see the first.");
        report.AppendLine("A root set that is short of two roots looks exactly like one that is not.");
        report.AppendLine();
        report.AppendLine($"- Cases: **{Cases.Count}** ({combinations.Count} polynomials, product and expanded)");
        report.AppendLine($"- Incomplete: **{Cases.Count(c => c.Missing > 0)}**, missing **{Cases.Sum(c => c.Missing)}** roots in total");
        report.AppendLine($"- Unsound: **{Cases.Count(c => c.Unsound > 0)}**, **{Cases.Sum(c => c.Unsound)}** roots that are not roots");
        report.AppendLine($"- Timed out or not answered with a finite set: **{Cases.Count(c => c.Verdict is "timeout" or "not-finite" or "error")}**");
        report.AppendLine();
        if (bad.Count > 0)
        {
            report.AppendLine("| Form | Polynomial | Verdict | Roots | Of | What is wrong |");
            report.AppendLine("|---|---|---|---|--:|---|");
            foreach (var c in bad)
                report.AppendLine($"| `{c.Form}` | `{Truncate(c.Product, 48)}` | {c.Verdict} | {c.Returned} | {c.Degree} | {c.Detail} |");
        }
        else
            report.AppendLine("No case is short of a root, and no root that comes back fails its equation.");

        // Where Harness.Reports says, whichever directory the run was started from.
        var path = Harness.Reports.PathFor("rootcheck.md");
        System.IO.File.WriteAllText(path, report.ToString());
        Console.Error.WriteLine($"\nwrote {path}");
        Console.WriteLine(report.ToString());
        Console.WriteLine($"OVERALL {Cases.Count - bad.Count}/{Cases.Count} clean; " +
                          $"{Cases.Count(c => c.Missing > 0)} incomplete, " +
                          $"{Cases.Count(c => c.Unsound > 0)} unsound, " +
                          $"{Cases.Count(c => c.Verdict is "timeout" or "not-finite" or "error")} unanswered");
        // A missing root, a returned value that is not a root and an exception fail the run. A
        // timeout does not: a shared runner is slower than the machine the budget was set on,
        // and that is a fact about the runner rather than about the solver.
        return Cases.Any(c => c.Missing > 0 || c.Unsound > 0 || c.Verdict == "error") ? 1 : 0;
    }
}
