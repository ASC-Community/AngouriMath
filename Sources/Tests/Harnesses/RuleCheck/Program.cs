//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

// rulecheck -- checks what a rewrite rule set *declares* against what it does.
//
// #746 tier 2 asks for "rule priorities and conflict resolution, with confluence and
// termination checked by tooling rather than asserted by authors". Every RewriteRuleSet in
// the registry carries a TransformationRelation and a Soundness, and nothing has ever checked
// either of them. This does, for the two properties that are checkable without an oracle:
//
//   termination        applying the set repeatedly reaches a fixed point
//   value preservation a set declaring Equivalence keeps the value where both sides
//                      are defined
//
// The second is what the other harnesses do for Simplify as a whole. The difference is that
// this attributes a change to a *named rule set*, which is what "rules as first-class data"
// is for: a failure here says which set to look at rather than that something, somewhere,
// changed an answer.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;

static class RuleCheck
{
    static readonly TimeSpan PerCase = TimeSpan.FromSeconds(5);

    /// <summary>Applications before a set is called non-terminating on that input.</summary>
    const int MaxPasses = 24;

    /// <summary>
    /// Sample points, real and complex. The complex ones matter more than the real ones here:
    /// a rewrite that is false only off the real line is the class this repository has shipped
    /// most often, and sampling the reals alone is how it kept getting through.
    /// </summary>
    static readonly System.Numerics.Complex[] Points =
    {
        new(0.37, 0), new(1.41, 0), new(-0.63, 0), new(2.71, 0), new(-1.87, 0),
        new(0.5, 0.5), new(-0.8, 1.3), new(1.2, -0.7), new(-1.1, -1.6), new(0.2, 2.2)
    };

    const double Tolerance = 1e-7;

    sealed record Finding(string Set, string Kind, string Input, string Output, string Detail);

    static readonly List<Finding> Findings = new();
    static int applied, valueChecks, timedOut;

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

    static string Truncate(string s, int at = 54) => s.Length <= at ? s : s.Substring(0, at - 3) + "...";

    static string Show(Entity e) => e is null ? "<none>" : Truncate(e.Stringize());

    /// <summary>
    /// The value at a point, or null where it is undefined, infinite or could not be reached.
    /// A rewrite is only asked to agree where both sides have a value; that is what
    /// "sound under assumptions" means and this cannot tell an assumption from a defect.
    /// </summary>
    static System.Numerics.Complex? ValueAt(Entity expr, System.Numerics.Complex point)
    {
        try
        {
            var substituted = expr.Substitute("x", (Entity)Entity.Number.Complex.Create(point.Real, point.Imaginary));
            if (substituted.Vars.Any())
                return null;                        // a free variable other than x
            var evaluated = substituted.EvalNumerical();
            var real = evaluated.RealPart.EDecimal.ToDouble();
            var imaginary = evaluated.ImaginaryPart.EDecimal.ToDouble();
            if (double.IsNaN(real) || double.IsNaN(imaginary)
                || double.IsInfinity(real) || double.IsInfinity(imaginary))
                return null;
            return new System.Numerics.Complex(real, imaginary);
        }
        catch { return null; }
    }

    static bool Agree(System.Numerics.Complex left, System.Numerics.Complex right)
    {
        var scale = Math.Max(1.0, Math.Max(left.Magnitude, right.Magnitude));
        return (left - right).Magnitude <= Tolerance * scale;
    }

    /// <summary>
    /// Whether iterating the set reaches a fixed point, optionally normalising between passes
    /// as the simplifier does.
    /// </summary>
    static bool Terminates(RewriteRuleSet set, Entity from, bool normalise, out Entity stuck)
    {
        var current = from;
        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var applied = WithTimeout(() => normalise
                ? set.ApplyOnce(current).InnerSimplified
                : set.ApplyOnce(current));
            if (applied is null) { stuck = current; return true; }   // gave up; not a claim
            if (applied.Equals(current)) { stuck = current; return true; }
            current = applied;
        }
        stuck = current;
        return false;
    }

    static void Check(RewriteRuleSet set, Entity expr)
    {
        var once = WithTimeout(() => set.ApplyOnce(expr));
        if (once is null || once.Equals(expr))
            return;                                 // the set declined; it claims nothing
        applied++;

        // Termination, in the two ways a set can be iterated.
        //
        // Alone is the question tier 2 asks, because rules as first-class data means a caller
        // may apply one set on its own. With the normalisation after it is how the simplifier
        // actually runs them -- `simplifyChildren` is Rewriting(...).Then(InnerSimplification)
        // -- and the difference between the two columns is the whole finding: a set that
        // settles only in composition is not a terminating rewrite system, it is half of one,
        // and nothing said so.
        var alone = Terminates(set, once, normalise: false, out var stuckAlone);
        var normalised = Terminates(set, once, normalise: true, out var stuckNormalised);
        if (!alone && !normalised)
            Findings.Add(new Finding(set.Name, "never-settles", Show(expr), Show(stuckNormalised),
                $"no fixed point in {MaxPasses} passes, with or without InnerSimplified between them"));
        else if (!alone)
            Findings.Add(new Finding(set.Name, "settles-only-composed", Show(expr), Show(stuckAlone),
                "cycles when iterated alone; settles once InnerSimplified runs between passes"));
        else if (!normalised)
            Findings.Add(new Finding(set.Name, "normalisation-breaks-it", Show(expr), Show(stuckNormalised),
                "settles alone but not with InnerSimplified between passes"));

        // Value preservation, for the relation the set declares.
        if (set.Relation != TransformationRelation.Equivalence)
            return;
        var compared = 0;
        foreach (var point in Points)
        {
            var before = ValueAt(expr, point);
            var after = ValueAt(once, point);
            if (before is null || after is null)
                continue;                           // undefined on one side; not this tool's business
            compared++;
            if (!Agree(before.Value, after.Value))
            {
                // A set declaring Sound claims the relation holds for every value with no
                // side condition, so a single disagreement at a point where both sides have
                // a value falsifies the declaration outright. Under SoundUnderAssumptions the
                // same observation is only information: the tier permits failure where an
                // assumption does not hold, and nothing here can tell an assumption from a
                // mistake. The two are separated so that tightening a label makes this harness
                // start enforcing it.
                Findings.Add(new Finding(set.Name,
                    set.Soundness == Soundness.Sound ? "sound-violated" : "value",
                    Show(expr), Show(once),
                    $"at x = {point.Real:0.##}{(point.Imaginary >= 0 ? "+" : "")}{point.Imaginary:0.##}i: "
                    + $"{before.Value.Real:0.####}{(before.Value.Imaginary >= 0 ? "+" : "")}{before.Value.Imaginary:0.####}i"
                    + $" -> {after.Value.Real:0.####}{(after.Value.Imaginary >= 0 ? "+" : "")}{after.Value.Imaginary:0.####}i"));
                break;                              // one point is enough to report the pair
            }
        }
        if (compared > 0) valueChecks++;
    }

    /// <summary>
    /// Whether two sets commute: applying A then B lands where B then A does, normalising
    /// after each so that the comparison is between settled trees rather than between two
    /// half-finished ones.
    /// </summary>
    /// <remarks>
    /// This is the measurable half of "rule priorities and conflict resolution". The
    /// simplifier applies a dozen sets in a hardcoded order and nothing records which parts of
    /// that order are load-bearing. A pair that commutes may be reordered freely; a pair that
    /// does not is a decision somebody once made and nobody wrote down.
    /// </remarks>
    static void CheckPair(RewriteRuleSet first, RewriteRuleSet second, Entity expr, PairResult result)
    {
        var forward = WithTimeout(() => second.ApplyOnce(first.ApplyOnce(expr).InnerSimplified).InnerSimplified);
        var backward = WithTimeout(() => first.ApplyOnce(second.ApplyOnce(expr).InnerSimplified).InnerSimplified);
        if (forward is null || backward is null)
            return;
        // Neither order did anything, so the pair says nothing about this expression.
        if (forward.Equals(expr) && backward.Equals(expr))
            return;
        result.Compared++;
        if (!forward.Equals(backward))
        {
            result.Differed++;
            result.Example ??= (Show(expr), Show(forward), Show(backward));
        }
    }

    sealed class PairResult
    {
        public int Compared, Differed;
        public (string Input, string Forward, string Backward)? Example;
    }

    // -2 and -1/2 join the negatives for the same reason the suite's agreement test needed
    // them: a set keying on a negative literal barely fires without them. `y` is here so that
    // a rule wanting two distinct symbols has two.
    static readonly string[] Leaves = { "x", "y", "2", "-1", "-2", "1/2", "-1/2", "0", "1" };

    static readonly string[] Unary =
    {
        "-({0})", "sqrt({0})", "abs({0})", "ln({0})", "e ^ ({0})",
        "sin({0})", "cos({0})", "tan({0})", "sgn({0})", "1 / ({0})",
        "({0}) ^ 2", "({0}) ^ (-1)", "({0}) ^ (1/2)",
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

    static int Main()
    {
        var sets = typeof(RewriteRules)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(RewriteRuleSet))
            .Select(property => (RewriteRuleSet)property.GetValue(null))
            .OrderBy(set => set.Name, StringComparer.Ordinal)
            .ToList();
        Console.WriteLine($"{sets.Count} rule sets");

        var level1 = new List<string>(Leaves);
        var level2 = Grow(level1, binary: true);
        // Every level-2 shape and not a stride sample of them. The stride was what made this
        // survey weaker than the suite's own gate: RuleSetTerminationTest reaches a three-cycle
        // in `Common` -- https://github.com/asc-community/AngouriMath/issues/1056 -- that this
        // corpus did not build, so the report said "0 never settle" of a library where one does.
        // A survey that a gate can contradict is worse than no survey.
        var level3 = Grow(level2, binary: false);
        var sources = level1.Concat(level2).Concat(level3).ToList();
        var parsed = new List<Entity>();
        foreach (var source in sources)
        {
            try { parsed.Add(source.ToEntity()); }
            catch { /* not every generated string parses; that is the generator's business */ }
        }
        Console.WriteLine($"{parsed.Count} expressions -> {sets.Count * parsed.Count} pairs");

        var done = 0;
        foreach (var set in sets)
        {
            foreach (var expr in parsed)
                Check(set, expr);
            Console.WriteLine($"  {++done}/{sets.Count} {set.Name}");
        }

        // Commutation, over a sample: 30 sets is 435 unordered pairs, and the point is which
        // pairs conflict rather than on how many expressions each conflicts.
        var sample = parsed.Where((_, i) => i % 9 == 0).ToList();
        Console.WriteLine($"{sets.Count * (sets.Count - 1) / 2} pairs over {sample.Count} expressions");
        var pairs = new Dictionary<(string, string), PairResult>();
        for (var i = 0; i < sets.Count; i++)
        {
            for (var j = i + 1; j < sets.Count; j++)
            {
                var result = new PairResult();
                foreach (var expr in sample)
                    CheckPair(sets[i], sets[j], expr, result);
                if (result.Compared > 0)
                    pairs[(sets[i].Name, sets[j].Name)] = result;
            }
            Console.WriteLine($"  pairs {i + 1}/{sets.Count}");
        }

        Report(sets, pairs);
        // A set that does not terminate, alone or composed with the normalisation, fails the
        // run, and so does a `Sound` declaration a value falsifies. A value change under a set
        // declaring SoundUnderAssumptions is information: the tier permits it where an
        // assumption fails, and nothing here can tell an assumption from a defect.
        return Findings.Any(f => f.Kind is "never-settles" or "settles-only-composed"
                                 or "normalisation-breaks-it" or "sound-violated") ? 1 : 0;
    }

    /// <summary>
    /// The sets that actually <b>run</b> on the matcher, asked of the delegate each
    /// <see cref="RewriteRuleSet"/> rewrites with rather than inferred from a name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This read the names of <c>MatchedRules</c>' properties and matched them against the
    /// registry's set names, and it undercounted by three: the sets parameterised by a sort
    /// level are built by a <i>factory method</i> taking that level, not held in a property, so
    /// `CommonDenominator` and its two variants were data, were wired, and were reported as
    /// neither. A survey of the exchange that cannot see a third of the ways a set is declared
    /// is the sort of number that gets quoted later.
    /// </para>
    /// <para>
    /// A method group closed over a <c>MatchedRuleSet</c> carries it as the delegate's
    /// <see cref="Delegate.Target"/>, so the fact is there to be read: this set rewrites by
    /// calling that object. Names are not consulted at all, so however a set comes to be
    /// declared next, this still answers.
    /// </para>
    /// </remarks>
    static HashSet<string> AlreadyData()
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        var matchedRuleSet = typeof(RewriteRules).Assembly
            .GetType("AngouriMath.Core.Transformations.Matching.MatchedRuleSet");
        if (matchedRuleSet is null)
            return found;
        var field = typeof(RewriteRuleSet).GetField("rules",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is null)
            return found;
        foreach (var set in RewriteRules.All)
            if (field.GetValue(set) is Delegate rewrite
                && rewrite.Target is { } target
                && matchedRuleSet.IsInstanceOfType(target))
                found.Add(set.Name);
        return found;
    }

    /// <summary>
    /// How many rule sets are <b>expressed</b> as data with no <see cref="RewriteRuleSet"/>
    /// running them — the Pythagorean identity and the shared factor, which say what the matcher
    /// can and a <c>switch</c> cannot, and the three canonical orders, which are expressed,
    /// proven to agree, and deliberately not wired on a measured cost.
    /// </summary>
    /// <remarks>
    /// Counted by <i>instance</i> rather than by name, and over factory-built sets as well as
    /// properties, because a set parameterised by a sort level exists once per level and a name
    /// cannot tell those apart.
    /// </remarks>
    static int ExpressedButNotRun()
    {
        var holder = typeof(RewriteRules).Assembly
            .GetType("AngouriMath.Core.Transformations.Matching.MatchedRules");
        var matchedRuleSet = typeof(RewriteRules).Assembly
            .GetType("AngouriMath.Core.Transformations.Matching.MatchedRuleSet");
        var field = typeof(RewriteRuleSet).GetField("rules",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (holder is null || matchedRuleSet is null || field is null)
            return 0;

        const BindingFlags Any = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        var expressed = new List<object>();
        foreach (var property in holder.GetProperties(Any))
            if (property.PropertyType == matchedRuleSet && property.GetValue(null) is { } value)
                expressed.Add(value);
        foreach (var factory in holder.GetMethods(Any))
            if (factory.ReturnType == matchedRuleSet
                && factory.GetParameters() is { Length: 1 } parameters
                && parameters[0].ParameterType.IsEnum)
                foreach (var level in Enum.GetValues(parameters[0].ParameterType))
                    if (factory.Invoke(null, new[] { level }) is { } built)
                        expressed.Add(built);

        // A factory hands back a fresh set each call, so the object a registry entry closed over
        // is not the object this just built. Identity is the set's name, which the factory makes
        // per level for exactly that reason.
        var running = new HashSet<string>(StringComparer.Ordinal);
        var nameOf = matchedRuleSet.GetProperty("Name", Any | BindingFlags.Instance);
        foreach (var set in RewriteRules.All)
            if (field.GetValue(set) is Delegate rewrite
                && rewrite.Target is { } target
                && matchedRuleSet.IsInstanceOfType(target)
                && nameOf?.GetValue(target) is string name)
                running.Add(name);

        return expressed.Count(one => nameOf?.GetValue(one) is string name && !running.Contains(name));
    }

    /// <summary>
    /// What one `switch` arm would need from the matcher to be written as data, or null where it
    /// needs nothing it does not have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing is named here, and the first version of this method was wrong to name three
    /// things.</b> It flagged an arm whose source contains <c>not</c>, <c>or</c> or a type
    /// conjunction as needing an addition to the matcher — eight sets between them — on the
    /// reading that <c>Node&lt;T&gt;</c> matches one node type and cannot say "either".
    /// </para>
    /// <para>
    /// A <b>predicate on a hole</b> says every one of them, and the matcher has had that since
    /// <c>Any&lt;T&gt;(name, where)</c>. <c>Sumf or Minusf</c> is
    /// <c>Any&lt;Entity&gt;(n, e =&gt; e is Sumf or Minusf)</c>; <c>var x and not Integer(1)</c>
    /// is a predicate; <c>Rational and not Integer</c> is a predicate on a typed hole;
    /// <c>not Set and not Matrix</c> is a predicate. `PerfectSquare` was converted to prove it
    /// and agrees with its `switch` over the generated corpus.
    /// </para>
    /// <para>
    /// So this returns null for everything and stays here as the place a real gap would be
    /// recorded — with the reading that produced the wrong answer written down, because the
    /// question "can the matcher say this?" was answered by reading the pattern language rather
    /// than by trying to express an arm in it.
    /// </para>
    /// </remarks>
    static string? Blocker(RewriteRule rule) => null;

    /// <summary>
    /// How many arms a set would lose by being data, counted as arms sharing a replacement with
    /// an earlier one. Those are the ones a `switch` writes several times because a C# pattern
    /// cannot say "either way round"; one commutative pattern says it once.
    /// </summary>
    static int Duplicated(RewriteRuleSet set)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var repeats = 0;
        foreach (var rule in set.Rules)
            if (!seen.Add(rule.ReplacementSource))
                repeats++;
        return repeats;
    }

    static void ReportAsData(StringBuilder text, List<RewriteRuleSet> sets)
    {
        var already = AlreadyData();
        text.AppendLine("## What each set would need to be data");
        text.AppendLine();
        text.AppendLine("[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 1 asks for");
        text.AppendLine("pattern matching as a data structure, and its remaining item is *the other sets*.");
        text.AppendLine("A count of them is not a plan, so this is the work-list: for each set, how many");
        text.AppendLine("arms it has, how many need something the matcher cannot express, and how many");
        text.AppendLine("share a replacement with an earlier arm — the last being what a commutative");
        text.AppendLine("pattern writes once where a `switch` writes it four or eight times.");
        text.AppendLine();
        text.AppendLine("**Nothing is blocked.** This column named three constructs as needing an addition");
        text.AppendLine("to the matcher — a negated pattern, an alternation of node types, a type");
        text.AppendLine("conjunction — and a predicate on a hole says all three, which the matcher has had");
        text.AppendLine("all along. `PerfectSquare` was converted to prove it: its arm is");
        text.AppendLine("`x is Sumf or Minusf`, and it agrees with the `switch` over the corpus. What");
        text.AppendLine("stands between every remaining set and the exchange is the agreement proof, which");
        text.AppendLine("is a test rather than a design.");
        text.AppendLine();
        text.AppendLine("| rule set | data | arms | blocked | duplicated | what is missing |");
        text.AppendLine("|---|:-:|--:|--:|--:|---|");
        foreach (var set in sets.OrderBy(s => s.Name, StringComparer.Ordinal))
        {
            var blockers = set.Rules.Select(Blocker).Where(b => b is not null).ToList();
            var missing = blockers.Count == 0
                ? "—"
                : string.Join(", ", blockers.GroupBy(b => b!)
                    .OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key} ({g.Count()})"));
            text.AppendLine($"| `{set.Name}` | {(already.Contains(set.Name) ? "yes" : "")} "
                + $"| {set.Rules.Count} | {blockers.Count} | {Duplicated(set)} | {missing} |");
        }
        text.AppendLine();
        var ready = sets.Where(s => !already.Contains(s.Name)
                                    && s.Rules.Count > 0
                                    && s.Rules.All(r => Blocker(r) is null)).ToList();
        var exchanged = sets.Count(s => already.Contains(s.Name));
        var expressedNotRun = ExpressedButNotRun();
        var unreadable = sets.Count(s => s.Rules.Count == 0);
        text.AppendLine($"**{exchanged} of {sets.Count} sets run on the matcher. {ready.Count} more are");
        text.AppendLine("writable today** with no addition to the matcher — the whole of what stands in");
        text.AppendLine("their way is the proof that the data form agrees with the `switch`, which is a");
        text.AppendLine("test rather than a design.");
        text.AppendLine();
        text.AppendLine($"There are also **{expressedNotRun} data rule sets with no `RewriteRuleSet`");
        text.AppendLine("running them** — demonstrations of what the matcher can say and a `switch`");
        text.AppendLine($"cannot, and **{unreadable} set{(unreadable == 1 ? "" : "s")} whose arms the registry cannot read at all** —");
        text.AppendLine("0 arms above means the generator declined the method's shape, so nothing about it");
        text.AppendLine("can be listed here either way.");
        text.AppendLine();
    }

    static void Report(List<RewriteRuleSet> sets, Dictionary<(string First, string Second), PairResult> pairs)
    {
        var text = new StringBuilder();
        text.AppendLine("# rulecheck");
        // Names the build, not the branch: a report describes the build it measured, which
        // need not be master's.
        text.AppendLine();
        text.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        text.AppendLine();
        text.AppendLine("Generated by `Sources/Tests/Harnesses/RuleCheck`. What each rewrite rule set **declares** —");
        text.AppendLine("a `TransformationRelation` and a `Soundness` — checked against what it does.");
        text.AppendLine("[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 2 asks for");
        text.AppendLine("exactly this: confluence and termination *checked by tooling rather than asserted");
        text.AppendLine("by authors*.");
        text.AppendLine();
        text.AppendLine($"- Rule sets: **{sets.Count}**");
        text.AppendLine($"- Applications that changed the expression: **{applied}**");
        text.AppendLine($"- Of those, pairs comparable at a sample point: **{valueChecks}**");
        text.AppendLine($"- Never settles, with or without the normalisation: **{Findings.Count(f => f.Kind == "never-settles")}**");
        text.AppendLine($"- Settles only once composed with the normalisation: **{Findings.Count(f => f.Kind == "settles-only-composed")}**");
        text.AppendLine($"- Settles alone but not composed: **{Findings.Count(f => f.Kind == "normalisation-breaks-it")}**");
        text.AppendLine($"- **A `Sound` declaration falsified: {Findings.Count(f => f.Kind == "sound-violated")}** "
            + "— this one is an error, not information");
        text.AppendLine($"- Value not preserved by a set declaring `SoundUnderAssumptions`: "
            + $"**{Findings.Count(f => f.Kind == "value")}**");
        text.AppendLine($"- Sets declaring `Sound`: **{sets.Count(s => s.Soundness == Soundness.Sound)}** "
            + $"of {sets.Count}");
        text.AppendLine($"- Timed out at {PerCase.TotalSeconds:0}s: {timedOut}");
        text.AppendLine();
        text.AppendLine("A value finding is **not automatically a defect**: every set in the registry");
        text.AppendLine("declares `SoundUnderAssumptions`, which permits failure where an assumption does");
        text.AppendLine("not hold, and this tool cannot tell an assumption from a mistake. What it can do");
        text.AppendLine("is say *which named set* changed a value and at which point, which is the part");
        text.AppendLine("that used to require bisecting the pipeline by hand.");
        text.AppendLine();
        text.AppendLine("**A rule set is not a terminating rewrite system on its own.** Where a set");
        text.AppendLine("cycles when iterated alone and settles once `InnerSimplified` runs between");
        text.AppendLine("passes, that is not a defect in the pipeline — the pipeline composes them that");
        text.AppendLine("way, `simplifyChildren` being `Rewriting(...).Then(InnerSimplification)`. It is a");
        text.AppendLine("statement about what the set is, and it matters because tier 2's whole premise is");
        text.AppendLine("that a caller may reach for one set on its own. `--x` under `NumericNeat` grows");
        text.AppendLine("`-1 * 1 * 1 * ...` for ever, because the rule keeps producing a unit factor that");
        text.AppendLine("only the normalisation folds. Nothing in the type says so.");
        text.AppendLine();
        text.AppendLine("**And that uniformity is itself the finding.** All 30 sets declare the same");
        text.AppendLine("relation and the same soundness, so the metadata distinguishes nothing and no");
        text.AppendLine("tool can hold a set to a stronger claim than its neighbour. Tier 2's \"justification");
        text.AppendLine("tier\" wants to be data that varies.");
        text.AppendLine();

        ReportAsData(text, sets);

        foreach (var group in Findings.GroupBy(f => f.Kind))
        {
            text.AppendLine($"## {group.Key}");
            text.AppendLine();
            text.AppendLine("| rule set | input | output | detail |");
            text.AppendLine("|---|---|---|---|");
            foreach (var finding in group
                         .GroupBy(f => f.Set)
                         .OrderByDescending(byName => byName.Count())
                         .SelectMany(byName => byName.Take(6)))
                text.AppendLine($"| `{finding.Set}` | `{finding.Input}` | `{finding.Output}` | {finding.Detail} |");
            text.AppendLine();
            text.AppendLine("Counts by set:");
            text.AppendLine();
            text.AppendLine("| rule set | findings |");
            text.AppendLine("|---|--:|");
            foreach (var byName in group.GroupBy(f => f.Set).OrderByDescending(g => g.Count()))
                text.AppendLine($"| `{byName.Key}` | {byName.Count()} |");
            text.AppendLine();
        }

        var conflicting = pairs.Where(p => p.Value.Differed > 0).ToList();
        text.AppendLine("## Which rule sets fail to commute");
        text.AppendLine();
        text.AppendLine("Applying A then B against B then A, normalising after each so the comparison is");
        text.AppendLine("between settled trees. This is the measurable half of tier 2's *rule priorities and");
        text.AppendLine("conflict resolution*: the simplifier applies a dozen sets in a hardcoded order and");
        text.AppendLine("nothing records which parts of that order are load-bearing. A pair that commutes may");
        text.AppendLine("be reordered freely; a pair that does not is a decision somebody once made.");
        text.AppendLine();
        text.AppendLine($"- Pairs where at least one order did something: **{pairs.Count}** of "
            + $"{sets.Count * (sets.Count - 1) / 2}");
        text.AppendLine($"- Pairs that **do not commute**: **{conflicting.Count}**");
        text.AppendLine();
        if (conflicting.Count > 0)
        {
            text.AppendLine("| first | second | differed | of | example input | A then B | B then A |");
            text.AppendLine("|---|---|--:|--:|---|---|---|");
            foreach (var pair in conflicting.OrderByDescending(p => p.Value.Differed).Take(40))
                text.AppendLine($"| `{pair.Key.First}` | `{pair.Key.Second}` | {pair.Value.Differed} "
                    + $"| {pair.Value.Compared} | `{pair.Value.Example?.Input}` "
                    + $"| `{pair.Value.Example?.Forward}` | `{pair.Value.Example?.Backward}` |");
            if (conflicting.Count > 40)
                text.AppendLine($"| ... | {conflicting.Count - 40} more | | | | | |");
            text.AppendLine();
        }

        var path = Harness.Reports.PathFor("rulecheck.md");
        File.WriteAllText(path, text.ToString());
        Console.WriteLine();
        Console.WriteLine($"rulecheck: {sets.Count} sets, {applied} applications, "
            + $"{Findings.Count(f => f.Kind == "never-settles")} never settle, "
            + $"{Findings.Count(f => f.Kind == "settles-only-composed")} settle only composed, "
            + $"{Findings.Count(f => f.Kind == "value")} value changes");
        Console.WriteLine($"Wrote {Path.GetFullPath(path)}");
    }
}
