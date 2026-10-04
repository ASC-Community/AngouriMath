//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

// canoncheck -- does the library have a canonical form, and is InnerSimplified it?
//
// Three properties, none of which needs an oracle and all of which a canonical form has to
// have. A form that fails any of them is a normalisation, not a canonicalisation, and the
// difference decides what a rule may assume about the shape it is handed.
//
//   idempotence         canonicalising twice is canonicalising once
//   order independence  a commutative operator's operands may be written either way round
//   agreement           two writings of the same expression reach the same form
//
// Written for https://github.com/asc-community/AngouriMath/issues/746 tier 1, which asks for
// a written specification of what canonical means per node class and a stated distinction
// between canonical and "simplest".
//
// InnerSimplified is a normalisation and fails order independence by design, so the findings
// are not defects as such. The run fails when their list changes: canoncheck-baseline.tsv
// beside this file is the list, and Harness.Baseline says how it is kept.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;

static class CanonCheck
{
    static readonly TimeSpan PerCase = TimeSpan.FromSeconds(5);

    sealed record Finding(string Stage, string Property, string Left, string Right, string LeftForm, string RightForm)
    {
        /// <summary>The finding's line in the baseline: the case, written out in full.</summary>
        public string Key { get; init; }
    }

    static readonly List<Finding> Findings = new();

    /// <summary>The baseline lines of the cases a timeout kept from being measured.</summary>
    static readonly HashSet<string> Unmeasured = new(StringComparer.Ordinal);

    static int timedOut, unparsed;

    static string Key(string stage, string property, string left, string right)
        => string.Join("\t", stage, property, Harness.Baseline.Field(left), Harness.Baseline.Field(right));

    /// <summary>
    /// The two candidates for "the form an expression is in". <c>InnerSimplified</c> is the
    /// normalisation every node runs on construction; <c>Simplify</c> is the search over
    /// rewrites that returns the best candidate by the complexity criteria. Whether either is
    /// canonical is the question, and they have to be measured separately to answer it.
    /// </summary>
    sealed record Stage(string Name, Func<Entity, Entity> Apply, int Stride)
    {
        public int Idempotence, Order, Agreement;
    }

    static readonly Stage Canonicalising =
        new("InnerSimplified", e => e.InnerSimplified, 23);

    static readonly Stage Simplifying =
        new("Simplify", e => e.Simplify(), 149);

    /// <summary>
    /// The sort the library already has, applied on its own. <c>CanonicalOrderExact</c> groups
    /// by the whole subtree, which is the granularity a canonical form wants -- the other two
    /// levels deliberately treat <c>x</c> and <c>2 * x</c> as the same operand for collecting
    /// like terms, which is a different job. Measured separately because whether the order is
    /// total is a question about the sort, not about the pipeline that happens to run it.
    /// </summary>
    static readonly Stage Ordering =
        new("CanonicalOrderExact",
            e => RewriteRules.CanonicalOrderExact.ApplyOnce(e).InnerSimplified, 23);

    /// <summary>
    /// The same sort, run on an already-normalised tree. The order key depends on a node's
    /// class, and the normalisation changes classes — <c>1 * 2 ^ (-1)</c> is a product when the
    /// sort reads it and the number <c>1/2</c> immediately afterwards — so sorting first sorts
    /// a shape that is about to stop existing, and the next pass orders it differently.
    /// </summary>
    static readonly Stage NormaliseThenOrder =
        new("Normalise+Order",
            e => RewriteRules.CanonicalOrderExact.ApplyOnce(e.InnerSimplified).InnerSimplified, 23);

    /// <summary>
    /// Settings are [ThreadStatic] and a stack overflow in a rewrite would take the run with
    /// it, so every case runs on a pool thread with a cap.
    /// </summary>
    static T WithTimeout<T>(Func<T> f) where T : class
    {
        try
        {
            var task = Task.Run(f);
            if (task.Wait(PerCase)) return task.Result;
            timedOut++;
            return null;
        }
        catch { return null; }
    }

    static Entity Form(Stage stage, Entity e) => WithTimeout(() => stage.Apply(e));

    static string Show(Entity e) => e is null ? "<none>" : Truncate(e.Stringize());

    static string Truncate(string s, int at = 58) => s.Length <= at ? s : s.Substring(0, at - 3) + "...";

    // -------------------------------------------------------------------------------------
    // The properties.
    // -------------------------------------------------------------------------------------

    /// <summary>Canonicalising a canonical form leaves it alone.</summary>
    static void Idempotence(Stage stage, Entity expr)
    {
        var key = Key(stage.Name, "idempotence", expr.Stringize(), "(applied twice)");
        var before = timedOut;
        var once = Form(stage, expr);
        var twice = once is null ? null : Form(stage, once);
        if (twice is null)
        {
            if (timedOut != before) Unmeasured.Add(key);
            return;
        }
        stage.Idempotence++;
        if (!once.Equals(twice))
            Findings.Add(new Finding(stage.Name, "idempotence", Show(expr), "(applied twice)", Show(once), Show(twice)) { Key = key });
    }

    /// <summary>
    /// A commutative operator's operands may be written either way round, so the two writings
    /// have to reach the same form. Sums and products only -- the library's other commutative
    /// nodes are the connectives and the set operations, checked in the listed pairs below.
    /// </summary>
    static void OrderIndependence(Stage stage, Entity left, Entity right)
    {
        foreach (var (a, b) in new[]
                 {
                     ((Entity)(left + right), (Entity)(right + left)),
                     ((Entity)(left * right), (Entity)(right * left))
                 })
        {
            var key = Key(stage.Name, "order", a.Stringize(), b.Stringize());
            var before = timedOut;
            var formA = Form(stage, a);
            var formB = Form(stage, b);
            if (formA is null || formB is null)
            {
                if (timedOut != before) Unmeasured.Add(key);
                continue;
            }
            stage.Order++;
            if (!formA.Equals(formB))
                Findings.Add(new Finding(stage.Name, "order", Show(a), Show(b), Show(formA), Show(formB)) { Key = key });
        }
    }

    /// <summary>Two writings of the same expression reach the same form.</summary>
    static void Agreement(Stage stage, string left, string right)
    {
        Entity a, b;
        try { a = left.ToEntity(); b = right.ToEntity(); }
        catch { unparsed++; return; }
        var key = Key(stage.Name, "agreement", left, right);
        var before = timedOut;
        var formA = Form(stage, a);
        var formB = Form(stage, b);
        if (formA is null || formB is null)
        {
            if (timedOut != before) Unmeasured.Add(key);
            return;
        }
        stage.Agreement++;
        if (!formA.Equals(formB))
            Findings.Add(new Finding(stage.Name, "agreement", left, right, Show(formA), Show(formB)) { Key = key });
    }

    // -------------------------------------------------------------------------------------
    // Material.
    // -------------------------------------------------------------------------------------

    static readonly string[] Leaves = { "x", "y", "2", "-1", "1/2", "0", "1", "a" };

    static readonly string[] Unary =
    {
        "-({0})", "sqrt({0})", "abs({0})", "ln({0})", "e ^ ({0})",
        "sin({0})", "cos({0})", "sgn({0})", "1 / ({0})", "({0}) ^ 2", "({0}) ^ (-1)",
    };

    static readonly string[] Binary =
    {
        "({0}) + ({1})", "({0}) - ({1})", "({0}) * ({1})", "({0}) / ({1})", "({0}) ^ ({1})",
    };

    static List<string> Grow(List<string> below, bool binary)
    {
        var grown = new List<string>();
        foreach (var shape in Unary)
            foreach (var inner in below)
                grown.Add(string.Format(shape, inner));
        if (binary)
            foreach (var shape in Binary)
                foreach (var l in below)
                    foreach (var r in below)
                        grown.Add(string.Format(shape, l, r));
        return grown;
    }

    /// <summary>
    /// Pairs that denote the same expression however they are written. Each one is a claim
    /// the specification will have to make or disclaim, so a disagreement here is a decision
    /// to take rather than necessarily a defect.
    /// </summary>
    static readonly (string Left, string Right, string What)[] Pairs =
    {
        ("x - y",           "x + (-1) * y",     "a difference is a sum of a negation"),
        ("x - y",           "x + (-y)",         "a difference is a sum of a negation"),
        ("x / y",           "x * y ^ (-1)",     "a quotient is a product of a reciprocal"),
        ("sqrt(x)",         "x ^ (1/2)",        "a square root is a half power"),
        ("x ^ 2",           "x * x",            "a square is a product"),
        ("x * x * x",       "x ^ 3",            "repeated factors gather"),
        ("x + x",           "2 * x",            "repeated terms gather"),
        ("(x ^ 2) ^ 3",     "x ^ 6",            "a power of a power multiplies"),
        ("2 * 3 * x",       "6 * x",            "numeric factors fold"),
        ("x + 2 + 3",       "x + 5",            "numeric terms fold"),
        ("4 / 2",           "2",                "a rational folds to lowest terms"),
        ("x * 1",           "x",                "one is dropped from a product"),
        ("x + 0",           "x",                "zero is dropped from a sum"),
        ("x ^ 1",           "x",                "a first power is the base"),
        ("x * 0",           "0",                "a product with zero vanishes"),
        ("(x + y) + a",     "x + (y + a)",      "a sum reassociates"),
        ("(x * y) * a",     "x * (y * a)",      "a product reassociates"),
        ("x + y",           "y + x",            "a sum commutes"),
        ("x * y",           "y * x",            "a product commutes"),
        ("-(x + y)",        "-x - y",           "negation distributes over a sum"),
        ("1 / (1 / x)",     "x",                "a reciprocal is an involution"),
        ("e ^ ln(x)",       "x",                "exp and ln cancel on the principal branch"),
        ("x and y",         "y and x",          "conjunction commutes"),
        ("x or y",          "y or x",           "disjunction commutes"),
        ("{ 1, 2 }",        "{ 2, 1 }",         "a finite set is unordered"),
        ("{ 1, 2, 1 }",     "{ 1, 2 }",         "a finite set has no repeats"),
        ("abs(-x)",         "abs(x)",           "absolute value is even"),
        ("sin(-x)",         "-sin(x)",          "sine is odd"),
        ("cos(-x)",         "cos(x)",           "cosine is even"),
        ("x ^ 0",           "1",                "a zeroth power is one"),
    };

    static int Main()
    {
        var level1 = new List<string>(Leaves);
        var level2 = Grow(level1, binary: true);
        var level3 = Grow(level2.Where((_, i) => i % 11 == 0).ToList(), binary: false);
        var all = level1.Concat(level2).Concat(level3).ToList();
        Console.WriteLine($"{all.Count} expressions");

        var parsed = new List<Entity>();
        foreach (var source in all)
        {
            try { parsed.Add(source.ToEntity()); }
            catch { unparsed++; }
        }

        foreach (var stage in new[] { Canonicalising, Ordering, NormaliseThenOrder, Simplifying })
        {
            // Simplify is a search and costs orders of magnitude more than the normalisation,
            // so it runs over a sample. The sample is taken by stride rather than at random,
            // so a finding can be found again.
            var subjects = stage.Stride == 23
                ? parsed
                : parsed.Where((_, i) => i % 7 == 0).ToList();
            Console.WriteLine($"{stage.Name}: {subjects.Count} for idempotence");
            var done = 0;
            foreach (var expr in subjects)
            {
                Idempotence(stage, expr);
                if (++done % 500 == 0) Console.WriteLine($"  {stage.Name} idempotence {done}/{subjects.Count}");
            }

            var operands = parsed.Where((_, i) => i % stage.Stride == 0).ToList();
            Console.WriteLine($"{stage.Name}: {operands.Count} operands -> {operands.Count * operands.Count} ordered pairs");
            done = 0;
            foreach (var left in operands)
            {
                foreach (var right in operands)
                    OrderIndependence(stage, left, right);
                if (++done % 10 == 0) Console.WriteLine($"  {stage.Name} order {done}/{operands.Count}");
            }

            foreach (var (left, right, _) in Pairs)
                Agreement(stage, left, right);
        }

        Report();
        return Harness.Baseline.Check(
            "canoncheck-baseline.tsv",
            "canoncheck: stage, property, and the case that fails it -- the expression, or the two writings.",
            Findings.Select(f => f.Key),
            Unmeasured.Contains);
    }

    static void Report()
    {
        int Count(string stage, string property)
            => Findings.Count(f => f.Stage == stage && f.Property == property);

        var text = new StringBuilder();
        text.AppendLine("# canoncheck");
        text.AppendLine();
        text.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        text.AppendLine();
        text.AppendLine("Generated by `Sources/Tests/Harnesses/CanonCheck`. Three properties a canonical form has to have,");
        text.AppendLine("measured against both of the library's candidates for \"the form an expression is");
        text.AppendLine("in\". None of them needs an oracle: idempotence and order independence are");
        text.AppendLine("properties of the form itself, and the listed pairs are writings of one expression.");
        text.AppendLine();
        text.AppendLine("Forms are compared as **entities**, not as printed strings. Two trees that print");
        text.AppendLine("the same can differ, and one of the findings below is exactly that.");
        text.AppendLine();
        text.AppendLine("Every finding is listed in `canoncheck-baseline.tsv` beside the harness, and the run");
        text.AppendLine("fails when that list changes.");
        text.AppendLine();
        text.AppendLine("| property | InnerSimplified | + CanonicalOrderExact | normalise, then order | Simplify |");
        text.AppendLine("|---|---|---|---|---|");
        foreach (var (label, key, canonical, simplest) in new[]
                 {
                     ("idempotence", "idempotence", Canonicalising.Idempotence, Simplifying.Idempotence),
                     ("order independence", "order", Canonicalising.Order, Simplifying.Order),
                     ("listed agreements", "agreement", Canonicalising.Agreement, Simplifying.Agreement)
                 })
            text.AppendLine($"| {label} | {Count("InnerSimplified", key)} failed of {canonical} "
                + $"| {Count("CanonicalOrderExact", key)} failed "
                + $"| {Count("Normalise+Order", key)} failed "
                + $"| {Count("Simplify", key)} failed of {simplest} |");
        text.AppendLine();
        text.AppendLine($"Timed out at {PerCase.TotalSeconds:0}s: {timedOut}; did not parse: {unparsed}.");
        text.AppendLine();

        foreach (var group in Findings.GroupBy(f => (f.Stage, f.Property)))
        {
            text.AppendLine($"## {group.Key.Stage} -- {group.Key.Property}");
            text.AppendLine();
            text.AppendLine("| left | right | left form | right form |");
            text.AppendLine("|---|---|---|---|");
            foreach (var finding in group.Take(40))
                text.AppendLine($"| `{finding.Left}` | `{finding.Right}` | `{finding.LeftForm}` | `{finding.RightForm}` |");
            if (group.Count() > 40)
                text.AppendLine($"| ... | {group.Count() - 40} more | | |");
            text.AppendLine();
        }

        var path = Harness.Reports.PathFor("canoncheck.md");
        File.WriteAllText(path, text.ToString());
        Console.WriteLine();
        foreach (var stage in new[] { Canonicalising, Ordering, NormaliseThenOrder, Simplifying })
            Console.WriteLine($"canoncheck {stage.Name}: idempotence {Count(stage.Name, "idempotence")}/{stage.Idempotence} failed; "
                + $"order {Count(stage.Name, "order")}/{stage.Order} failed; "
                + $"agreement {Count(stage.Name, "agreement")}/{stage.Agreement} disagreed");
        Console.WriteLine($"Wrote {path}");
    }
}
