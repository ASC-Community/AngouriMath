//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

// confluence -- where two arms of one rule set both fire at a node, do they agree?
//
// #746 tier 2 asks for "rule priorities and conflict resolution, with confluence and
// termination checked by tooling rather than asserted by authors". `rulecheck` answers the
// termination half and answers conflict at *set* grain: whether applying A then B lands where
// B then A does. This answers it at *rule* grain, which only became askable once the registry
// carried individual arms (#825).
//
// A switch takes the first arm that matches. Where a second arm would also have fired and
// would have produced something else, the order of the arms is load-bearing -- a decision
// somebody made, usually without saying so, and one that a future edit can silently reverse
// by inserting an arm above it. Where every overlapping pair agrees, the order is free and the
// set is locally confluent on what was sampled.
//
// What this cannot say: nothing here is a proof. It samples generated expressions, and a pair
// that never overlapped on the sample is reported as untested rather than as confluent.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;

static class Confluence
{
    static readonly TimeSpan PerCase = TimeSpan.FromSeconds(5);

    /// <summary>The sets in which a timeout kept a pair of arms from being compared.</summary>
    static readonly HashSet<string> TimedOutIn = new(StringComparer.Ordinal);

    static T WithTimeout<T>(Func<T> f, string set) where T : class
    {
        try
        {
            var task = Task.Run(f);
            if (task.Wait(PerCase)) return task.Result;
            TimedOutIn.Add(set);
            return null;
        }
        catch { return null; }
    }

    static string Truncate(string s, int at = 46) => s.Length <= at ? s : s.Substring(0, at - 3) + "...";

    static string Show(Entity e) => e is null ? "<none>" : Truncate(e.Stringize());

    sealed class Overlap
    {
        public int Nodes;                                   // nodes where both arms fired
        public int Disagreed;
        public (string Node, string First, string Second) Example;
    }

    sealed class SetResult
    {
        public int NodesSeen, NodesWithOneArm, NodesWithSeveral, DisagreeingNodes;
        public readonly Dictionary<(int, int), Overlap> Overlaps = new();
    }

    /// <summary>
    /// The arms that change this node, in the order the switch tries them. An arm that matches
    /// and hands the node back has not fired -- the factorial arms do that on purpose -- so a
    /// returned value equal to the node is not an overlap.
    /// </summary>
    static List<int> FiringAt(RewriteRuleSet set, Entity node)
    {
        var firing = new List<int>();
        for (var i = 0; i < set.Rules.Count; i++)
        {
            Entity applied;
            try { applied = set.Rules[i].TryApply(node); }
            catch { continue; }
            if (applied is not null && !applied.Equals(node))
                firing.Add(i);
        }
        return firing;
    }

    static void Check(RewriteRuleSet set, Entity expr, SetResult result)
    {
        foreach (var node in expr.Nodes)
        {
            result.NodesSeen++;
            var firing = FiringAt(set, node);
            if (firing.Count == 0)
                continue;
            if (firing.Count == 1) { result.NodesWithOneArm++; continue; }
            result.NodesWithSeveral++;

            // Compared after normalisation, so that two arms writing the same answer two ways
            // -- which the sorts do constantly -- are not called a conflict.
            var settled = new Dictionary<int, Entity>();
            foreach (var i in firing)
            {
                var applied = WithTimeout(() => set.Rules[i].TryApply(node).InnerSimplified, set.Name);
                if (applied is not null)
                    settled[i] = applied;
            }

            var disagreed = false;
            foreach (var i in settled.Keys)
                foreach (var j in settled.Keys)
                {
                    if (i >= j) continue;
                    var key = (i, j);
                    if (!result.Overlaps.TryGetValue(key, out var overlap))
                        result.Overlaps[key] = overlap = new Overlap();
                    overlap.Nodes++;
                    if (settled[i].Equals(settled[j]))
                        continue;
                    overlap.Disagreed++;
                    disagreed = true;
                    if (overlap.Example.Node is null)
                        overlap.Example = (Show(node), Show(settled[i]), Show(settled[j]));
                }
            if (disagreed)
                result.DisagreeingNodes++;
        }
    }

    static readonly string[] Leaves = { "x", "y", "2", "-1", "1/2", "0", "1", "3" };

    static readonly string[] Unary =
    {
        "-({0})", "sqrt({0})", "abs({0})", "ln({0})", "e ^ ({0})",
        "sin({0})", "cos({0})", "tan({0})", "sgn({0})", "1 / ({0})",
        "({0}) ^ 2", "({0}) ^ (-1)", "({0}) ^ (1/2)", "({0})!",
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
        var sets = RewriteRules.All.Where(set => set.Rules.Count > 0)
            .OrderBy(set => set.Name, StringComparer.Ordinal).ToList();
        Console.WriteLine($"{sets.Count} addressable sets, {sets.Sum(s => s.Rules.Count)} rules");

        var level1 = new List<string>(Leaves);
        var level2 = Grow(level1, binary: true);
        var level3 = Grow(level2.Where((_, i) => i % 11 == 0).ToList(), binary: false);
        var parsed = new List<Entity>();
        foreach (var source in level1.Concat(level2).Concat(level3))
        {
            try { parsed.Add(source.ToEntity()); }
            catch { }
        }
        Console.WriteLine($"{parsed.Count} expressions");

        var results = new Dictionary<string, SetResult>();
        var done = 0;
        foreach (var set in sets)
        {
            var result = new SetResult();
            foreach (var expr in parsed)
                Check(set, expr, result);
            results[set.Name] = result;
            Console.WriteLine($"  {++done}/{sets.Count} {set.Name}: "
                + $"{result.NodesWithSeveral} nodes with several arms, {result.DisagreeingNodes} disagreeing");
        }

        Report(sets, results);

        // An arm's name is its pattern, which survives an arm being inserted above it; its index
        // does not.
        var conflicts = sets.SelectMany(set => results[set.Name].Overlaps
            .Where(pair => pair.Value.Disagreed > 0)
            .Select(pair => string.Join("\t", set.Name,
                Harness.Baseline.Field(set.Rules[pair.Key.Item1].Name),
                Harness.Baseline.Field(set.Rules[pair.Key.Item2].Name))));
        return Harness.Baseline.Check(
            "confluence-baseline.tsv",
            "confluence: a rule set, and two of its arms that both fire at some node and disagree, the earlier first.",
            conflicts,
            line => TimedOutIn.Contains(line.Split('\t')[0]));
    }

    static void Report(List<RewriteRuleSet> sets, Dictionary<string, SetResult> results)
    {
        var text = new StringBuilder();
        text.AppendLine("# confluence");
        text.AppendLine();
        text.AppendLine("Generated by `Sources/Tests/Harnesses/Confluence`. Where two arms of one rewrite rule set both fire at");
        text.AppendLine("a node, do they agree? A `switch` takes the first arm that matches, so a disagreement");
        text.AppendLine("means the **order of the arms is load-bearing** — and nothing in the source says so.");
        text.AppendLine();
        text.AppendLine("[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 2 asks for");
        text.AppendLine("confluence *checked by tooling rather than asserted by authors*. `rulecheck` answers");
        text.AppendLine("that at set grain — whether A then B lands where B then A does. This answers it at");
        text.AppendLine("rule grain, which only became askable once the registry carried individual arms.");
        text.AppendLine();
        text.AppendLine("**This is a sample, not a proof.** A pair of arms that never overlapped on the");
        text.AppendLine("generated input says nothing either way, and is not counted as agreeing.");
        text.AppendLine();
        text.AppendLine("Every conflicting pair is listed in `confluence-baseline.tsv` beside the harness, and the");
        text.AppendLine("run fails when that list changes.");
        text.AppendLine();
        text.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        text.AppendLine();

        var totalSeveral = results.Values.Sum(r => r.NodesWithSeveral);
        var totalDisagree = results.Values.Sum(r => r.DisagreeingNodes);
        var overlappingPairs = results.Values.Sum(r => r.Overlaps.Count);
        var conflictingPairs = results.Values.Sum(r => r.Overlaps.Values.Count(o => o.Disagreed > 0));

        text.AppendLine("## Overall");
        text.AppendLine();
        text.AppendLine($"- **{sets.Count}** addressable sets, **{sets.Sum(s => s.Rules.Count)}** rules");
        text.AppendLine($"- **{totalSeveral}** nodes where more than one arm of a set fired");
        text.AppendLine($"- **{totalDisagree}** of those where the arms did not agree");
        text.AppendLine($"- **{overlappingPairs}** pairs of arms observed overlapping, **{conflictingPairs}** of them conflicting");
        text.AppendLine();

        text.AppendLine("## By set");
        text.AppendLine();
        text.AppendLine("| set | rules | nodes with >1 arm | disagreeing | conflicting pairs |");
        text.AppendLine("|---|--:|--:|--:|--:|");
        foreach (var set in sets)
        {
            var r = results[set.Name];
            text.AppendLine($"| `{set.Name}` | {set.Rules.Count} | {r.NodesWithSeveral} | {r.DisagreeingNodes} "
                + $"| {r.Overlaps.Values.Count(o => o.Disagreed > 0)} |");
        }
        text.AppendLine();

        text.AppendLine("## Where the order is load-bearing");
        text.AppendLine();
        var any = false;
        foreach (var set in sets)
        {
            var conflicts = results[set.Name].Overlaps
                .Where(pair => pair.Value.Disagreed > 0)
                .OrderByDescending(pair => pair.Value.Disagreed).ToList();
            if (conflicts.Count == 0)
                continue;
            any = true;
            text.AppendLine($"### `{set.Name}`");
            text.AppendLine();
            text.AppendLine("| earlier arm | later arm | nodes | disagreeing | example |");
            text.AppendLine("|---|---|--:|--:|---|");
            foreach (var (key, overlap) in conflicts.Take(12))
            {
                var (i, j) = key;
                text.AppendLine($"| `[{i}] {Truncate(set.Rules[i].PatternSource, 40)}` "
                    + $"| `[{j}] {Truncate(set.Rules[j].PatternSource, 40)}` "
                    + $"| {overlap.Nodes} | {overlap.Disagreed} "
                    + $"| `{overlap.Example.Node}` -> `{overlap.Example.First}` vs `{overlap.Example.Second}` |");
            }
            if (conflicts.Count > 12)
                text.AppendLine($"| … | {conflicts.Count - 12} more pairs | | | |");
            text.AppendLine();
        }
        if (!any)
            text.AppendLine("No pair of arms disagreed on the sampled input.");

        var path = Harness.Reports.PathFor("confluence.md");
        File.WriteAllText(path, text.ToString());
        Console.WriteLine($"wrote {path}");
        Console.WriteLine($"OVERALL {totalSeveral} nodes with several arms, {totalDisagree} disagreeing; "
            + $"{conflictingPairs} of {overlappingPairs} overlapping arm pairs conflict");
    }
}
