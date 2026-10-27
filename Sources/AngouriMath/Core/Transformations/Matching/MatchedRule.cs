//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AngouriMath.Core.Transformations.Matching
{
    /// <summary>
    /// One rewrite rule, addressable on its own: a name, a pattern to match, a side condition,
    /// what to build, and the tier its claim is justified at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what <a href="https://github.com/asc-community/AngouriMath/issues/825">#825</a>
    /// asks for and what a <c>switch</c> arm cannot be. A rule here can be listed, named in a
    /// bug report, tested by itself, and — the part that matters most —
    /// <b>carry its own <see cref="Soundness"/></b>. A *set's* tier is the minimum over its arms,
    /// so one conditional arm drags every unconditional one down with it and every set in the
    /// registry ends up declaring the same value — which is the honest label for a set and a
    /// useless one for a rule. A rule declares its own, and unlike the set's it distinguishes:
    /// both tiers are well populated. No count is repeated here, because a number in a comment
    /// drifts silently; <c>RuleAuthoringGuideTest</c> measures the live ones and fails when they
    /// move.
    /// </para>
    /// <para>
    /// <b>Where the right-hand side is a pattern too, the rule has two directions rather than
    /// one.</b> <see cref="Reversed"/> is the same rule read the other way, and
    /// <see cref="Reversal"/> says why a rule has no such reading when it has none. That was
    /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2's first
    /// missing piece; it is delivered and it has a production consumer, since
    /// <c>Saturation.RulesUpTo</c> uses a declared inverse pair to keep the collecting direction
    /// and withhold the expanding one. <c>Docs/Contributing/ReversibleRules.md</c> is the argument
    /// for when a reversal is licensed.
    /// </para>
    /// </remarks>
    internal sealed class MatchedRule
    {
        /// <summary>A rule whose right-hand side is a pattern, and which therefore has two directions.</summary>
        internal MatchedRule(
            string name,
            MatchPattern left,
            MatchPattern right,
            Soundness soundness,
            Func<Bindings, bool>? when = null,
            string? description = null,
            [CallerLineNumber] int line = 0)
            : this(name, left, null, right ?? throw new ArgumentNullException(nameof(right)), soundness, null, when, description, line)
        {
            // A name the replacement reads and the pattern never binds is a typo, and it is a
            // typo that would otherwise show up as a rule that silently never fires. Only a
            // right-hand side written as data can be checked for it at all.
            foreach (var wanted in right.BoundNames)
                if (!left.BoundNames.Contains(wanted))
                    throw new ArgumentException(
                        $"'{name}' builds '{wanted}', which its pattern does not bind", nameof(right));
            // And the same failure mode from the other direction. A node type is matchable
            // without being buildable -- see MatchPattern.Construct -- so a template naming one
            // is a rule that matches, builds nothing, and is indistinguishable at run time from a
            // rule that did not apply. It cost an afternoon and a twenty-eight-row agreement
            // failure before this check existed, which is the argument for the check.
            if (!right.IsBuildable)
                throw new ArgumentException(
                    $"'{name}' has a replacement this cannot build: some node type in it is "
                    + "matchable but not constructible. Add it to MatchPattern.Construct, or "
                    + "write the replacement as code.", nameof(right));
        }

        /// <summary>
        /// A rule whose right-hand side is code. One-way: see <see cref="RuleReversal.ReplacementIsCode"/>.
        /// </summary>
        internal MatchedRule(
            string name,
            MatchPattern left,
            Func<Bindings, Entity> right,
            Soundness soundness,
            RewriteRuleGrowth? growth = null,
            Func<Bindings, bool>? when = null,
            string? description = null,
            [CallerLineNumber] int line = 0)
            : this(name, left, right is null ? null : (_, bound) => right(bound),
                   right is null ? throw new ArgumentNullException(nameof(right)) : null,
                   soundness, growth, when, description, line)
        {
        }

        /// <summary>
        /// A rule whose right-hand side is code and wants <b>the node it matched</b> as well as
        /// the bindings.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not a convenience. A replacement that needs the whole expression — because it hands it
        /// to a helper that decides for itself whether to rewrite — otherwise has to rebuild it
        /// from the bindings, and <b>a rebuilt node is a different object with none of the
        /// original's cached work</b>: its <c>InnerSimplified</c>, <c>Evaled</c>, <c>Codomain</c>
        /// and rate are all computed again, on every attempt, for every node of the tree.
        /// </para>
        /// <para>
        /// Measured on the limit of <c>x * ln(x) / cos(x)</c>, which reaches
        /// <c>RationalizeDenominator</c> at every quotient it builds: rebuilding cost <b>4.0 s to
        /// 5.2 s</b>, and a test with a thirty-second cap failed on two of the three CI platforms
        /// while passing locally. Handing the node over costs nothing and the rule keeps the
        /// caches it was given.
        /// </para>
        /// </remarks>
        internal MatchedRule(
            string name,
            MatchPattern left,
            Func<Entity, Bindings, Entity> right,
            Soundness soundness,
            RewriteRuleGrowth? growth = null,
            Func<Bindings, bool>? when = null,
            string? description = null,
            [CallerLineNumber] int line = 0)
            : this(name, left, right ?? throw new ArgumentNullException(nameof(right)), null,
                   soundness, growth, when, description, line)
        {
        }

        private MatchedRule(
            string name,
            MatchPattern left,
            Func<Entity, Bindings, Entity>? rightCode,
            MatchPattern? rightPattern,
            Soundness soundness,
            RewriteRuleGrowth? declaredGrowth,
            Func<Bindings, bool>? when,
            string? description,
            int line)
        {
            SourceLine = line;
            Description = description;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Left = left ?? throw new ArgumentNullException(nameof(left));
            this.right = rightCode;
            Right = rightPattern;
            Soundness = soundness;
            this.when = when;
            // Settled here rather than on first ask. It depends on nothing that changes
            // afterwards, and a lazily cached enum is a field wider than a word: two threads
            // arriving together could read a half-written one, where a reference cannot tear.
            Reversal = Classify();
            Growth = ClassifyGrowth(declaredGrowth);
        }

        private readonly Func<Entity, Bindings, Entity>? right;
        private readonly Func<Bindings, bool>? when;

        /// <summary>What to call this rule in a report, a test or a bug.</summary>
        internal string Name { get; }

        /// <summary>
        /// The identity this rule is, in the notation a mathematician would write it in — or
        /// <see langword="null"/> where nobody has written one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Not the same thing as <see cref="Name"/> or as the rendered pattern.</b>
        /// <c>a / (b / c) = a * c / b</c> is what this says; <c>dividing-by-a-quotient-multiplies-by-its-reciprocal</c>
        /// is what to call it, and <c>Divf(var a, Divf(var b, var c))</c> is how the matcher spells
        /// it. #746 tier 2 wants metadata "rich enough that v5.0 can render a step as a sentence",
        /// and of those three only this one is the sentence.
        /// </para>
        /// <para>
        /// <b>It exists because the generator has one and this did not.</b>
        /// <c>RuleRegistryGenerator</c> reads the comment written above a <c>switch</c> arm into
        /// <see cref="RewriteRule.Description"/> — <b>95 of the registry's 407 arms</b> carry one
        /// that way. A rule written as data has its identity in a comment too, and a comment is
        /// not readable at run time, so until this parameter existed converting a set to data
        /// <i>lost</i> the description rather than keeping it. That is what stands between the
        /// registry and describing the rules it actually runs.
        /// </para>
        /// </remarks>
        internal string? Description { get; }

        /// <summary>
        /// The line this rule is declared on, taken from the compiler rather than written down.
        /// </summary>
        /// <remarks>
        /// A rule written as data has a real source location like a <c>switch</c> arm does, and
        /// <c>[CallerLineNumber]</c> is how it keeps one without anybody maintaining it. It is
        /// what lets a data rule appear in the registry beside the arms.
        /// </remarks>
        internal int SourceLine { get; }

        /// <summary>Whether this rule carries a side condition beyond its pattern.</summary>
        internal bool HasCondition => when is not null;

        /// <summary>The shape it fires on.</summary>
        internal MatchPattern Left { get; }

        /// <summary>
        /// What it puts there instead, where that is a pattern — or <see langword="null"/> where
        /// the replacement is code and there is nothing to match against.
        /// </summary>
        internal MatchPattern? Right { get; }

        /// <summary>How well justified this rule's claim is — per rule, which is the point.</summary>
        internal Soundness Soundness { get; }

        /// <summary>
        /// The rewritten expression, or <see langword="null"/> where the rule does not apply: a
        /// builder that cannot use the bindings it was handed returns <see langword="null"/>, a
        /// refusal rather than an error.
        /// </summary>
        internal Entity? TryApply(Entity expr)
        {
            // A pattern with no choice in it has at most one match, so asking for a sequence of
            // them allocates an iterator per pattern node to deliver either zero answers or one.
            // Most rules are of that kind, and a rewrite pass makes an attempt at every node of
            // the tree, so this path is worth having separately.
            if (Left.IsDeterministic)
            {
                if (!Left.TryMatchOnce(expr, Bindings.Empty, out var only)) return null;
                if (when is not null && !when(only)) return null;
                return Build(expr, only);
            }

            // Between one match and however many sits the case that is neither and is common:
            // a commutative node of deterministic children offers the written order and the
            // swapped one and nothing else. Walking those by index costs nothing, where
            // enumerating them allocates an iterator state machine per pattern node at every
            // node of the tree -- 6.3 MB on `SolveMediumHard` for two commutative rules in
            // `RewriteRules.Power`. https://github.com/asc-community/AngouriMath/issues/1079
            //
            // The index is an upper bound rather than a count, so a candidate that does not
            // exist answers false and is skipped, exactly as an enumeration would omit it.
            if (Left.ChoiceCount is var choices and not MatchPattern.Unbounded)
            {
                for (var choice = 0; choice < choices; choice++)
                {
                    if (!Left.TryMatchChoice(expr, Bindings.Empty, choice, out var bound)) continue;
                    if (when is not null && !when(bound)) continue;
                    if (Build(expr, bound) is { } rewritten) return rewritten;
                }
                return null;
            }

            // Every way the pattern matches, in order, and the first that also satisfies the
            // side condition wins. Taking only the first *match* would be wrong: commutativity
            // means `b*a + c*a` matches `k*p + k*q` several ways and only some of them bind
            // `k` to the factor the condition is about.
            foreach (var bindings in Left.Match(expr, Bindings.Empty))
            {
                if (when is not null && !when(bindings))
                    continue;
                // A match whose replacement cannot be built is a match that does not apply, and
                // another way of matching may still. Only the last of them decides that the rule
                // declines.
                if (Build(expr, bindings) is { } rewritten)
                    return rewritten;
            }
            return null;
        }

        /// <summary>
        /// The e-class <see cref="TryApply"/> would produce, found by matching against
        /// <paramref name="classId"/> directly rather than a materialised term. Caller must check
        /// <see cref="MatchPattern.CanEMatch"/> on <see cref="Left"/> first -- this throws rather
        /// than silently falling back, so a caller cannot forget the check and get the old,
        /// slower path without knowing it.
        /// </summary>
        internal bool TryEMatchApply(
            EGraph graph, int classId, Func<Entity, double> cost, out int resultClassId)
            => TryEMatchApply(graph, classId, cost, null, out resultClassId);

        /// <summary>
        /// <see cref="TryEMatchApply(EGraph, int, Func{Entity, double}, out int)"/>, charging
        /// <paramref name="spend"/> once for every binding tried after the first. A binding tried
        /// is a match attempt: the caller charged the first, and an attempt over a class with a
        /// thousand members is a thousand of them and not one -- which is what let a two-second
        /// budget run for sixty-nine. A <see langword="false"/> from <paramref name="spend"/>
        /// ends the attempt as not matched; the caller's ledger says why.
        /// https://github.com/asc-community/AngouriMath/issues/1199
        /// </summary>
        internal bool TryEMatchApply(
            EGraph graph, int classId, Func<Entity, double> cost, Func<bool>? spend, out int resultClassId)
        {
            if (!Left.CanEMatch)
                throw new InvalidOperationException(
                    $"'{Name}' cannot e-match; check {nameof(Left)}.{nameof(MatchPattern.CanEMatch)} first.");

            resultClassId = 0;
            var first = true;
            foreach (var ebindings in Left.EMatch(graph, classId, EBindings.Empty, cost))
            {
                if (!first && spend is not null && !spend()) return false;
                first = false;

                Bindings? entityBindings = null;
                bool TryEntityBindings(out Bindings result)
                {
                    if (entityBindings is { } already) { result = already; return true; }
                    var built = Bindings.Empty;
                    foreach (var boundName in Left.BoundNames)
                    {
                        if (!ebindings.TryGet(boundName, out var boundClass)) { result = built; return false; }
                        var witness = graph.Extract(boundClass, cost);
                        if (witness is null) { result = built; return false; }
                        built = built.With(boundName, witness);
                    }
                    entityBindings = built;
                    result = built;
                    return true;
                }

                if (when is not null)
                {
                    if (!TryEntityBindings(out var forWhen)) continue;
                    if (!when(forWhen)) continue;
                }

                if (Right is { } right && right.CanEMatch)
                {
                    if (right.ETryBuild(graph, ebindings, cost, out resultClassId)) return true;
                    continue;
                }

                if (!TryEntityBindings(out var forBuild)) continue;
                var matched = graph.Extract(classId, cost);
                if (matched is null) continue;
                if (Build(matched, forBuild) is { } rewritten)
                {
                    resultClassId = graph.AddEntity(rewritten);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The replacement under these bindings, or <see langword="null"/> where it cannot be
        /// built — which is the rule declining rather than an error, whichever form the
        /// right-hand side takes.
        /// </summary>
        private Entity? Build(Entity matched, Bindings bindings)
        {
            if (Right is not null)
                return Right.TryBuild(bindings, out var built) ? built : null;
            return right!(matched, bindings);
        }

        /// <summary>
        /// Whether this rule can be read backwards, and where it cannot, why not.
        /// </summary>
        /// <remarks>
        /// <b>Derived from the two sides, not declared.</b> Every clause below is a property of
        /// what the rule is written as, so the distinction between a one-way rewrite and a
        /// two-way one is something a test can fail on rather than something a comment asserts.
        /// </remarks>
        internal RuleReversal Reversal { get; }

        private RuleReversal Classify()
        {
            if (Right is null)
                return RuleReversal.ReplacementIsCode;
            // Set equality, and the direction that can fail is the one checked: a name the
            // replacement builds and the pattern does not bind is refused in the constructor, so
            // what is left is a name the pattern binds and the replacement throws away.
            foreach (var bound in Left.BoundNames)
                if (!Right.BoundNames.Contains(bound))
                    return RuleReversal.ReplacementDropsHoles;
            if (!Left.IsBuildable || !Right.IsBuildable)
                return RuleReversal.PatternCannotBeBuilt;
            return RuleReversal.Reversible;
        }

        /// <summary>
        /// Whether this rule's replacement is smaller, the same size, or larger than its pattern.
        /// Computed exactly from <see cref="MatchPattern.NodeCount"/> where the replacement is a
        /// pattern; <b>declared</b>, not derived, where it is code, the same way
        /// <see cref="Soundness"/> is declared rather than derived — a code-built replacement has no
        /// pattern tree to count nodes on, so the only source of truth is whoever wrote the rule and
        /// can justify the claim. Undeclared code-built rules stay <see cref="RewriteRuleGrowth.Unknown"/>,
        /// which is the honest default: not proven safe is not the same as safe.
        /// </summary>
        internal RewriteRuleGrowth Growth { get; }

        private RewriteRuleGrowth ClassifyGrowth(RewriteRuleGrowth? declared)
        {
            if (Right is null) return declared ?? RewriteRuleGrowth.Unknown;
            var leftSize = Left.NodeCount;
            var rightSize = Right.NodeCount;
            return rightSize < leftSize ? RewriteRuleGrowth.Collects
                 : rightSize > leftSize ? RewriteRuleGrowth.Expands
                 : RewriteRuleGrowth.Rearranges;
        }

        /// <summary>
        /// This rule read the other way, or <see langword="null"/> where it has no such reading.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The two sides swap and nothing else does. The side condition is carried over
        /// unchanged, because it is a predicate on the bindings and both directions produce the
        /// same bindings; and <see cref="Soundness"/> is carried over unchanged, because what a
        /// rewrite rule claims is an equality and an equality is symmetric.
        /// </para>
        /// <para>
        /// <b>What does not carry over is termination.</b> A rule that collects becomes one that
        /// expands, so a reversed rule composed with the rule it came from does not reach a fixed
        /// point — <c>k*p + k*q</c> and <c>k*(p + q)</c> rewrite to each other forever. A reversed
        /// set is a thing to ask questions of, not one to run to stability.
        /// </para>
        /// </remarks>
        internal MatchedRule? Reversed
            => Reversal is RuleReversal.Reversible
                ? reversed ??= new MatchedRule($"reversed {Name}", Right!, Left, Soundness, when,
                    Description is null ? null : $"read backwards: {Description}")
                : null;

        private MatchedRule? reversed;
    }

    /// <summary>
    /// Whether a <see cref="MatchedRule"/> can be read backwards, and where it cannot, what stops
    /// it.
    /// </summary>
    /// <remarks>
    /// <b>Not every rewrite has an inverse worth having, and this says which do.</b>
    /// <c>x - x -&gt; 0</c> is a rewrite nobody wants to read backwards and nobody could: from
    /// <c>0</c> there is no recovering which <c>x</c> was cancelled. That is
    /// <see cref="ReplacementDropsHoles"/>, and it is a fact about the rule rather than a
    /// judgement about it.
    /// </remarks>
    internal enum RuleReversal
    {
        /// <summary>Both sides are patterns over the same holes, and both can be built.</summary>
        Reversible,

        /// <summary>
        /// The replacement is a builder over the bindings rather than a pattern, so there is
        /// nothing to match an expression against.
        /// </summary>
        ReplacementIsCode,

        /// <summary>
        /// The replacement does not mention every hole the pattern binds, so reading it backwards
        /// would have to invent what the forward direction discarded. The Pythagorean identity is
        /// the standing example: <c>sin(x)^2 + cos(x)^2 -&gt; 1</c> forgets the angle.
        /// </summary>
        ReplacementDropsHoles,

        /// <summary>
        /// One of the two sides is over a node type this cannot construct, so it can be matched
        /// and not written. See <c>MatchPattern.Construct</c>.
        /// </summary>
        PatternCannotBeBuilt
    }

    /// <summary>
    /// An ordered list of <see cref="MatchedRule"/>, applied first-match-wins over every node —
    /// the same discipline the <c>switch</c>-based rule sets follow, so that one can be
    /// exchanged for the other and the two compared.
    /// </summary>
    internal sealed class MatchedRuleSet
    {
        internal MatchedRuleSet(string name, params MatchedRule[] rules)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            byPriority = ByPriority(this.rules);
        }

        /// <summary>
        /// Held as the array as well as exposed as a list, and walked as the array in
        /// <see cref="ApplyHere"/>. <c>foreach</c> over an <see cref="IReadOnlyList{T}"/> goes
        /// through the interface and boxes an enumerator; over the array it does not. That is
        /// 32 bytes per rule set per node, on a path a rewrite pass takes for every node in the
        /// tree, and it was the whole of what remained after the match itself stopped allocating.
        /// </summary>
        private readonly MatchedRule[] rules;

        /// <summary>
        /// The same rules in the order they are actually tried: <b>the more specific rule
        /// first</b>, and declaration order wherever specificity has no opinion. See
        /// <see cref="ByPriority"/>.
        /// </summary>
        private readonly MatchedRule[] byPriority;

        /// <summary>
        /// <paramref name="declared"/> ordered so that no rule is tried before a rule its pattern
        /// subsumes — the more specific one first — keeping declaration order everywhere else.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>What this replaces is the position somebody typed a rule at.</b> A set is
        /// first-match-wins, so where two rules both fire at a node and disagree, whichever comes
        /// first decides the answer. Where one pattern subsumes the other that decision is not a
        /// preference: the general rule would swallow the special one and the special one would
        /// never fire at all, so the specific rule has to be tried first. That was maintained by
        /// hand and written in a comment on one set;
        /// <see cref="MatchPattern.Subsumes"/> computes it, and this applies it.
        /// </para>
        /// <para>
        /// <b>It changes no answer today, and that is a measurement rather than an intention.</b>
        /// Over the 5,480 within-set rule pairs in <see cref="MatchedRules"/>, subsumption has an
        /// opinion about <b>28</b> of them — spread across eight sets, <c>Boolean</c> and
        /// <c>InequalityEquality</c> carrying half — and in <b>every one</b> the specific rule is
        /// already declared first. None is mutual. So this orders what was already ordered; what
        /// it adds is that inserting a general rule above a specific one no longer silently
        /// reverses an answer. <c>RulePriorityTest</c> holds the two orders equal, so the file
        /// stays readable as well as correct.
        /// </para>
        /// <para>
        /// A stable topological sort, taking the earliest-declared rule that has nothing left
        /// which must precede it. Subsumption is conservative, so most pairs constrain nothing and
        /// fall through to declaration order. It cannot cycle — a cycle would need each of two
        /// rules to be strictly more general than the other — but a cycle is emitted in
        /// declaration order rather than trusted not to happen, because a sort that silently drops
        /// a rule would be a set quietly missing a rewrite.
        /// </para>
        /// </remarks>
        private static MatchedRule[] ByPriority(MatchedRule[] declared)
        {
            var count = declared.Length;
            if (count < 2) return declared;

            // mustPrecede[g] is the set of rules that have to be tried before rule g, which is
            // every rule g's pattern strictly subsumes.
            var waitingOn = new int[count];
            var blocks = new List<int>[count];
            for (var g = 0; g < count; g++)
                for (var sp = 0; sp < count; sp++)
                {
                    if (g == sp) continue;
                    if (!declared[g].Left.Subsumes(declared[sp].Left)) continue;
                    if (declared[sp].Left.Subsumes(declared[g].Left)) continue;
                    (blocks[sp] ??= new List<int>()).Add(g);
                    waitingOn[g]++;
                }

            var ordered = new MatchedRule[count];
            var taken = new bool[count];
            for (var slot = 0; slot < count; slot++)
            {
                var next = -1;
                for (var i = 0; i < count; i++)
                    if (!taken[i] && waitingOn[i] == 0) { next = i; break; }
                // Only reachable if subsumption were not antisymmetric. Falling back to
                // declaration order keeps every rule in the set.
                if (next < 0)
                    for (var i = 0; i < count; i++)
                        if (!taken[i]) { next = i; break; }
                taken[next] = true;
                ordered[slot] = declared[next];
                if (blocks[next] is { } blocked)
                    foreach (var g in blocked) waitingOn[g]--;
            }
            return ordered;
        }

        // Indexing the rules by the node type each one requires -- the thing a `switch` cannot
        // do and a set of values can -- was tried here and is deliberately absent. Measured, a
        // per-runtime-type cache of the applicable rules cost 24 bytes per node and 912 bytes on
        // a pass, deterministically and reproducibly, and bought a time improvement that sat
        // inside this machine's run-to-run drift. Where the allocation came from was never
        // accounted for, and an optimisation that makes the deterministic column worse for an
        // unexplained reason is not one to keep. The cost being fought is in the match attempt
        // rather than in the dispatch, so an index is aimed at the wrong half.

        internal string Name { get; }

        /// <summary>
        /// The rules, <b>in the order they are written</b>. <b>Enumerable</b>, which is the whole
        /// point.
        /// </summary>
        /// <remarks>
        /// Not necessarily the order they are tried in — see <see cref="RulesByPriority"/>, which
        /// puts a rule ahead of any rule whose pattern subsumes it. The two are equal today and
        /// <c>RulePriorityTest</c> holds them equal, so this is the order to read the set in and to
        /// index it by; <see cref="AsAddressable"/> and <see cref="Reversed"/> use it for that
        /// reason.
        /// </remarks>
        internal IReadOnlyList<MatchedRule> Rules => rules;

        /// <summary>
        /// The rules of this set that can be read backwards, read backwards — so that
        /// <see cref="ApplyHere(Entity)"/> on it answers <i>what could this expression have come
        /// from</i> rather than <i>what does it become</i>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Smaller than the set it comes from whenever some of its rules are one-way, and
        /// <see cref="MatchedRule.Reversal"/> on each says which. A set none of whose rules
        /// reverses has no rules here, which is the difference being reported rather than hidden.
        /// </para>
        /// <para>
        /// <b>Not a rule set to run.</b> It is first-match-wins like any other, so applying it and
        /// then the set it came from need not return what you started with — the two sets order
        /// their rules independently. And every rule in it undoes one in the set it came from, so
        /// composing the two does not terminate. What it is for is asking.
        /// </para>
        /// </remarks>
        internal MatchedRuleSet Reversed
            => reversed ??= new MatchedRuleSet(
                $"reversed {Name}",
                rules.Select(rule => rule.Reversed).OfType<MatchedRule>().ToArray());

        private MatchedRuleSet? reversed;

        /// <summary>
        /// The weakest tier any of its rules is justified at — derived rather than declared,
        /// so it cannot drift from the rules it is about.
        /// </summary>
        internal Soundness Soundness
            => rules.Length == 0 ? Soundness.Sound : rules.Max(rule => rule.Soundness);

        /// <summary>
        /// The first rule that applies at this node, or null — first in
        /// <see cref="ByPriority"/>'s order, which is the order it would be tried in.
        /// </summary>
        internal MatchedRule? FirstMatching(Entity expr)
        {
            foreach (var rule in byPriority)
                if (rule.TryApply(expr) is not null)
                    return rule;
            return null;
        }

        /// <summary>
        /// The rules in the order they are tried, which is <see cref="ByPriority"/>'s and not
        /// necessarily <see cref="Rules"/>'s. Exposed so that the two can be compared rather than
        /// assumed equal.
        /// </summary>
        internal IReadOnlyList<MatchedRule> RulesByPriority => byPriority;

        /// <summary>
        /// These rules as <see cref="RewriteRule"/> values, so that a set written as data can
        /// appear in the registry beside the sets written as a <c>switch</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>RuleRegistryGenerator</c> reads a <c>switch</c>'s arms and cannot read this, which
        /// is the half of
        /// <a href="https://github.com/asc-community/AngouriMath/issues/825">#825</a> still open.
        /// Its cost is not only that a converted set keeps its <c>switch</c>: a set the generator
        /// <i>declines</i> — an ordinary method with branches and locals — has <b>no addressable
        /// rules at all</b>, and nothing about it can be listed, checked or named.
        /// </para>
        /// <para>
        /// This is the same problem approached from the other side. The rules here are already
        /// values; what was missing was their rendering. Everything is read off the rule rather
        /// than restated: the pattern and the replacement render themselves, the line comes from
        /// the compiler through <c>[CallerLineNumber]</c>, and the node type comes from what the
        /// pattern requires at its root. A side condition is a delegate and cannot be quoted, so
        /// it is reported as being there.
        /// </para>
        /// </remarks>
        internal IReadOnlyList<RewriteRule> AsAddressable()
        {
            var built = new RewriteRule[rules.Length];
            for (var i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                var pattern = rule.Left.ToString() ?? "";
                var replacement = rule.Right?.ToString();
                built[i] = new RewriteRule(
                    source: Name,
                    index: i,
                    name: rule.Name,
                    description: rule.Description,
                    nodeTypes: rule.Left.RequiredRootType is { } root
                        ? new[] { root }
                        : Array.Empty<Type>(),
                    patternSource: pattern,
                    guardSource: rule.HasCondition ? "a side condition on the bindings" : null,
                    replacementSource: replacement ?? "(built by code)",
                    // The rule's own answer, not a proxy for it. This used to compare the lengths
                    // of the two rendered strings, which is what the generator has to do -- it
                    // reads C# source text and has no tree to count. Here there is a tree, and
                    // guessing from the rendering was wrong 23 times in 322 and in both
                    // directions: `Divf(var a, Sinf(var b))` to `Mulf(var a, Cosecantf(var b))`
                    // read as Expands because `Cosecantf` is a longer word than `Sinf`, while
                    // DivisionPreparing's two read as Collects for the same reason reversed. Worse
                    // for the thirteen Boolean rules that *declare* their growth on a code-built
                    // replacement: with no string to measure, the proxy answered Unknown and threw
                    // the author's declaration away.
                    growth: rule.Growth,
                    sourceLine: rule.SourceLine,
                    apply: rule.TryApply,
                    // Per rule, which is the whole point of a rule being a value. A set's tier is
                    // the minimum over its rules, so one conditional rule makes a set of a hundred
                    // report as conditional; 181 of these 322 are Sound.
                    soundness: rule.Soundness);
            }
            return built;
        }

        /// <summary>One rewrite at this node only, leaving children alone.</summary>
        internal Entity ApplyHere(Entity expr)
        {
            // The rules this node's type can possibly match, rather than all of them. See the
            // note on `applicable`.
            foreach (var rule in ApplicableTo(expr.GetType()))
                if (rule.TryApply(expr) is { } rewritten)
                    return rewritten;
            return expr;
        }

        /// <summary>
        /// The rules whose pattern requires a root type this one is, cached per runtime type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A rule that cannot match still costs a virtual call and a type test to say so, and a
        /// rewrite pass asks every rule of a set at every node of the tree. For a set of three
        /// that is nothing, which is why the note above records a per-type index as measured and
        /// rejected — and it was rejected on a set of three.
        /// </para>
        /// <para>
        /// Thirty sets are data now, so a pass asks several hundred rules at every node where it
        /// used to ask a handful, and the arithmetic has changed. Indexing is <b>−36.0% of
        /// <c>SimplifyEasy</c></b> — 148,223 ns to 94,904 ns — with allocation identical to the
        /// byte and <c>ParseEasy</c>, <c>SolveEasy</c> and <c>SimplifyHard</c> unmoved, so what it
        /// removes is dispatch and nothing else.
        /// </para>
        /// <para>
        /// What was rejected before was a <i>cache</i> that allocated per node. This allocates
        /// once per runtime type ever seen and then never again, so a pass over a tree of
        /// products and sums touches two entries and allocates nothing. The dictionary is only
        /// ever added to, and adding is done on a copy that replaces the field, so a reader
        /// racing a writer sees either the old map or the new one and never a torn one.
        /// </para>
        /// </remarks>
        private MatchedRule[] ApplicableTo(Type type)
        {
            var known = applicable;
            if (known.TryGetValue(type, out var found))
                return found;

            var matching = new List<MatchedRule>(byPriority.Length);
            foreach (var rule in byPriority)
                if (rule.Left.RequiredRootType is not { } required || required.IsAssignableFrom(type))
                    matching.Add(rule);
            found = matching.Count == byPriority.Length ? byPriority : matching.ToArray();

            applicable = new Dictionary<Type, MatchedRule[]>(known) { [type] = found };
            return found;
        }

        private Dictionary<Type, MatchedRule[]> applicable = new();
    }
}
