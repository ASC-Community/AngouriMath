//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core.Transformations.Matching;
using AngouriMath.Extensions;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Core.Transformations
{
    /// <summary>
    /// One e-node: an operator, and the e-classes of its children.
    /// </summary>
    internal readonly struct ENode : IEquatable<ENode>, IComparable<ENode>
    {
        internal ENode(string op, int[] children, Domain? codomain = null)
        {
            Op = op ?? throw new ArgumentNullException(nameof(op));
            Children = children ?? throw new ArgumentNullException(nameof(children));
            Codomain = codomain;
        }

        internal string Op { get; }
        internal int[] Children { get; }

        /// <summary>
        /// The <see cref="Entity.Codomain"/> the node carries where it differs from its type's
        /// default, and <see langword="null"/> where it does not. <b>Part of the node's identity,
        /// deliberately.</b> An e-class is the graph's assertion that its members are equal, and
        /// <c>abs(x)</c> is not equal to <c>domain(abs(x), Any)</c> — they evaluate differently —
        /// so two nodes differing only here must hash apart, or the graph unions values that are
        /// not the same value. This used to live in a side table keyed on shape alone; both then
        /// landed in one class and extraction returned whichever was inserted last, and
        /// <c>Rebuild</c> dropped it altogether. <c>ENodeIdentityTest</c> holds all three.
        /// </summary>
        internal Domain? Codomain { get; }

        public bool Equals(ENode other)
        {
            if (Op != other.Op || Codomain != other.Codomain || Children.Length != other.Children.Length)
                return false;
            for (var i = 0; i < Children.Length; i++)
                if (Children[i] != other.Children[i]) return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is ENode node && Equals(node);

        /// <summary>
        /// A total order on e-nodes, so that the members of one e-class can be visited in a
        /// defined sequence. <see cref="HashSet{T}"/> enumeration order is an unspecified
        /// implementation detail and a string's hash code is randomised per process, so a cost
        /// tie between two members would otherwise be settled differently from one run to the
        /// next -- against the premise that a bounded computation is reproducible given a defined
        /// algorithm order. Ordinal on <see cref="Op"/>, so it does not move with the culture
        /// either.
        /// </summary>
        public int CompareTo(ENode other)
        {
            var byOp = string.CompareOrdinal(Op, other.Op);
            if (byOp != 0) return byOp;
            var byCodomain = Nullable.Compare(Codomain, other.Codomain);
            if (byCodomain != 0) return byCodomain;
            if (Children.Length != other.Children.Length)
                return Children.Length.CompareTo(other.Children.Length);
            for (var i = 0; i < Children.Length; i++)
                if (Children[i] != other.Children[i])
                    return Children[i].CompareTo(other.Children[i]);
            return 0;
        }

        public override int GetHashCode()
        {
            var hash = Op.GetHashCode();
            hash = unchecked(hash * 31 + (Codomain is { } domain ? (int)domain + 1 : 0));
            foreach (var child in Children) hash = unchecked(hash * 31 + child);
            return hash;
        }
    }

    /// <summary>
    /// An e-graph: e-classes over a union-find, e-nodes keyed by operator, child class and
    /// non-default codomain, hash-consed. The codomain is in the key because an e-class is an
    /// equality claim and a codomain is something two unequal entities can differ in.
    /// </summary>
    /// <remarks>
    /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2's e-graph,
    /// moved from the <c>work/egraph</c> measurement harness into the library once the harness had
    /// answered what it was built to answer — see that harness's own report for the measurement
    /// this design rests on.
    /// </remarks>
    internal sealed class EGraph
    {
        private readonly List<int> parent = new();
        private readonly Dictionary<ENode, int> hashcons = new();
        private readonly Dictionary<int, HashSet<ENode>> classes = new();

        /// <summary>
        /// Each leaf this graph was given, under its key, so that it is built back as the entity it
        /// was: a printed form need not parse back, and the integrator's <c>u_sub_1</c>,
        /// <c>u_exp_1</c> and <c>u_rad_1</c> do not -- re-parsed, every class holding one could
        /// not be built at all.
        /// </summary>
        private readonly Dictionary<string, Entity> leafEntities = new();

        /// <summary>How many e-classes this graph currently has.</summary>
        internal int ClassCount => classes.Count;

        /// <summary>How many distinct e-nodes this graph has ever created.</summary>
        internal int NodeCount => hashcons.Count;

        /// <summary>The representative of the e-class <paramref name="id"/> currently belongs to.</summary>
        internal int Find(int id)
        {
            while (parent[id] != id) { parent[id] = parent[parent[id]]; id = parent[id]; }
            return id;
        }

        private int NewClass()
        {
            var id = parent.Count;
            parent.Add(id);
            classes[id] = new HashSet<ENode>();
            return id;
        }

        /// <summary>
        /// Adds a leaf or an operator over already-added children, and returns the e-class it
        /// belongs to -- an existing one if an equal e-node is already hash-consed, otherwise a
        /// fresh one.
        /// </summary>
        /// <param name="op">
        /// The operator identity: a leaf's printed self, so that two different variables or
        /// numbers never share a class, or a node type's name otherwise.
        /// </param>
        /// <param name="children">The e-classes of this e-node's children, already added.</param>
        internal int Add(string op, params int[] children) => Add(op, children, null);

        private int Add(string op, int[] children, Domain? codomain)
        {
            var canonical = new ENode(op, children.Select(Find).ToArray(), codomain);
            if (hashcons.TryGetValue(canonical, out var existing))
                return Find(existing);
            // Folding on insertion: a neutral-element application denotes exactly the value its
            // other operand already has, so it is unioned into that operand's class instead of
            // being given a fresh e-node. Proven against 16 corpus expressions in the #746 tier 2
            // measurement harness before moving here: it closed eight of the nine blow-ups that
            // harness found in equality saturation over the full, undirected rule set. Only
            // catches the shape at the moment it is added -- see Rebuild for what a union
            // afterwards does not reach back and fold.
            if (NeutralClass(canonical) is { } folded)
                return folded;
            // And an arithmetic operator over two rational leaves denotes exactly the rational it
            // evaluates to, so it is that number's class rather than a fresh e-node. Found by
            // running the safe ceiling over the growth corpus: every one of the 54 inputs that did
            // not saturate was constant-only arithmetic -- `2 - 0 + 0 * 2`, `1/2 * 2 * sin(y)` --
            // because the rearranging rules respell a number for ever when nothing folds it.
            if (ConstantClass(canonical) is { } evaluated)
                return evaluated;
            var id = NewClass();
            hashcons[canonical] = id;
            classes[id].Add(canonical);
            return id;
        }

        private bool Holds(int id, string leaf)
            => classes.TryGetValue(Find(id), out var set)
               && set.Any(n => n.Children.Length == 0 && n.Op == leaf);

        /// <summary>
        /// The leaves worth trying as an identity. The additive and multiplicative identities,
        /// which is what the arithmetic operators have; a fold that needed some other constant
        /// would be a fact about that operator rather than about neutrality.
        /// </summary>
        [ConstantField]
        private static readonly string[] NeutralLeaves = { "0", "1" };

        /// <summary>
        /// Which operator folds away which leaf on which side, asked of
        /// <see cref="Entity.InnerSimplified"/> rather than written out a second time.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This used to be a hand-written table over <c>Sumf</c>, <c>Minusf</c>, <c>Mulf</c>,
        /// <c>Divf</c> and <c>Powf</c>, restating identities each of those types already
        /// implements and tests in its own <c>InnerSimplify</c>. Nothing kept the two in step, and
        /// the divergence would have been silent in the worst direction: the e-graph would go on
        /// asserting an equivalence the rest of the library had stopped believing, merging two
        /// classes that are no longer equal.
        /// </para>
        /// <para>
        /// Asking instead of restating also settles the cases that make a hand-written table
        /// fragile, without anybody having to remember them. <c>0 - x</c> is a negation and
        /// <c>1 / x</c> a reciprocal, so neither folds to its other operand; <c>1 ^ x</c> is the
        /// constant 1 rather than <c>x</c>. And if an arm ever answers with a condition attached
        /// — <c>Powf</c>'s <c>1 ^ x</c> already carries a <c>Providedf</c> domain condition — the
        /// answer is not the bare operand, so no fold is claimed for it. A hand-written table has
        /// no way to learn any of that.
        /// </para>
        /// </remarks>
        [ConstantField]
        private static readonly HashSet<(string Op, string Leaf, int LeafSide)> neutralFolds
            = BuildNeutralFolds();

        private static HashSet<(string Op, string Leaf, int LeafSide)> BuildNeutralFolds()
        {
            var folds = new HashSet<(string, string, int)>();
            Entity operand = MathS.Var("x");
            foreach (var type in MatchPattern.BuildableNodeTypes)
                foreach (var leafText in NeutralLeaves)
                    for (var side = 0; side < 2; side++)
                    {
                        var leaf = leafText.ToEntity();
                        var children = side == 0
                            ? new[] { leaf, operand }
                            : new[] { operand, leaf };
                        // A node this cannot build contributes no fold -- the table is what was
                        // observed.
                        if (MatchPattern.ConstructNode(type, children) is not { } built) continue;
                        if (built.InnerSimplified.Equals(operand))
                            folds.Add((type.Name, leafText, side));
                    }
            return folds;
        }

        /// <summary>
        /// The folds this graph performs on insertion, as <c>Op(first, second)</c> — for the test
        /// that names them, so that a change in <c>InnerSimplify</c> shows up as a changed list
        /// rather than as e-graph folding quietly gaining or losing a case.
        /// </summary>
        internal static IEnumerable<string> NeutralFolds
            => neutralFolds.Select(fold => fold.LeafSide == 0
                ? $"{fold.Op}({fold.Leaf}, x)"
                : $"{fold.Op}(x, {fold.Leaf})");

        /// <summary>
        /// If <paramref name="node"/> is a neutral element applied to something, the class that
        /// already denotes its value -- <see langword="null"/> otherwise.
        /// </summary>
        private int? NeutralClass(ENode node)
        {
            if (node.Children.Length != 2) return null;
            // A node carrying a codomain of its own is not the value of its other operand: folding
            // `domain(x * 1, Any)` into x's class would union it with a plain `x`, which is the
            // same conflation the identity above exists to prevent.
            if (node.Codomain is not null) return null;
            foreach (var leaf in NeutralLeaves)
            {
                if (neutralFolds.Contains((node.Op, leaf, 1)) && Holds(node.Children[1], leaf))
                    return Find(node.Children[0]);
                if (neutralFolds.Contains((node.Op, leaf, 0)) && Holds(node.Children[0], leaf))
                    return Find(node.Children[1]);
            }
            return null;
        }

        /// <summary>
        /// The operators a rational fold is asked of: the arithmetic ones. A function of a
        /// rational is rarely a rational, and a factorial evaluated on insertion would be paying
        /// for an answer nobody asked for.
        /// </summary>
        [ConstantField]
        private static readonly HashSet<string> FoldableOps = new()
        {
            nameof(Entity.Sumf), nameof(Entity.Minusf), nameof(Entity.Mulf), nameof(Entity.Divf),
            nameof(Entity.Powf),
        };

        /// <summary>
        /// A power is folded only for a whole exponent no larger than this in magnitude, so that
        /// inserting <c>2 ^ 100000000</c> does not compute it.
        /// </summary>
        private const int LargestFoldedExponent = 4096;

        [ConcurrentField]
        private static readonly ConcurrentDictionary<string, Rational?> rationalLeaves = new();

        private static Rational? RationalLeaf(string op)
            => rationalLeaves.GetOrAdd(op, printed => ParseLeaf(printed) as Rational);

        /// <summary>The rational the class <paramref name="id"/> holds as a plain leaf, if any.</summary>
        private Rational? RationalOf(int id)
        {
            if (!classes.TryGetValue(Find(id), out var set)) return null;
            foreach (var node in set)
                if (node.Children.Length == 0 && node.Codomain is null && RationalLeaf(node.Op) is { } value)
                    return value;
            return null;
        }

        /// <summary>
        /// If <paramref name="node"/> is an arithmetic operator over two rational leaves whose
        /// value is itself a rational, the class of that rational -- <see langword="null"/>
        /// otherwise, which includes a quotient by zero, an irrational root and any node carrying
        /// a codomain of its own.
        /// </summary>
        private int? ConstantClass(ENode node)
        {
            if (node.Codomain is not null || node.Children.Length != 2 || !FoldableOps.Contains(node.Op))
                return null;
            if (RationalOf(node.Children[0]) is not { } left || RationalOf(node.Children[1]) is not { } right)
                return null;
            if (node.Op == nameof(Entity.Powf)
                && (right is not Integer exponent || exponent.EInteger.Abs() > LargestFoldedExponent))
                return null;
            var value = MatchPattern.ConstructNode(OperatorType(node.Op), new Entity[] { left, right })?.Evaled;
            return value is Rational folded ? Add(Key(folded), Array.Empty<int>(), null) : null;
        }

        /// <summary>Every e-class currently in the graph.</summary>
        internal IEnumerable<int> Classes => classes.Keys.ToList();

        /// <summary>The e-nodes belonging to the e-class <paramref name="id"/> is a member of.</summary>
        internal IReadOnlyCollection<ENode> NodesOf(int id) => classes[Find(id)];

        /// <summary>
        /// Merges the e-classes of <paramref name="left"/> and <paramref name="right"/>, and
        /// answers whether they were different classes before this call. Does not itself restore
        /// congruence between e-nodes that now share a child class -- see <see cref="Rebuild"/>.
        /// </summary>
        internal bool Union(int left, int right)
        {
            left = Find(left);
            right = Find(right);
            if (left == right) return false;
            extractMemo = null;
            parent[right] = left;
            foreach (var node in classes[right]) classes[left].Add(node);
            classes.Remove(right);
            return true;
        }

        /// <summary>
        /// Restores congruence: after a union, e-nodes whose children are now in the same class
        /// as each other are congruent and are merged too, repeated until nothing more merges.
        /// This is the step that makes an e-graph an e-graph rather than a set of terms related
        /// only by the unions asked for directly.
        /// </summary>
        internal void Rebuild()
        {
            bool changed;
            do
            {
                changed = false;
                var seen = new Dictionary<ENode, int>();
                foreach (var pair in classes.ToList())
                    foreach (var node in pair.Value.ToList())
                    {
                        var canonical = new ENode(node.Op, node.Children.Select(Find).ToArray(), node.Codomain);
                        if (seen.TryGetValue(canonical, out var other))
                        {
                            if (Union(other, pair.Key)) changed = true;
                        }
                        else seen[canonical] = pair.Key;
                    }
            } while (changed);
        }

        /// <summary>
        /// A key naming <see cref="Entity.Constant.EulerIntrinsic"/> specifically, distinct from
        /// any string a real leaf can print as (a leaf's printed form never starts with a NUL) --
        /// so it shares nothing with the ordinary named constant <c>e</c>, which prints the same
        /// text <see cref="Entity.Constant.EulerIntrinsic"/> does. Both denote the same number and
        /// both are kept out of this key's collision space on purpose:
        /// <see cref="Entity.Constant.EulerIntrinsic"/> is a distinguished reference a binder over
        /// the name <c>e</c> must not capture, and merging the two into one e-class -- which
        /// <see cref="Key"/> keying on <c>Entity.Stringize()</c> alone would do -- silently
        /// discards that distinction on the next extraction. Caught in code review before this PR
        /// was merged.
        /// </summary>
        private const string EulerIntrinsicKey = "\0EulerIntrinsic";

        /// <summary>
        /// The operator identity of a node: a leaf's printed self, so that two different
        /// variables or numbers never share a class, or the node's runtime type otherwise, so
        /// that <c>x + y</c> and <c>a + b</c> are the same operator over different children --
        /// except <see cref="Entity.Constant.EulerIntrinsic"/>, which prints as the ordinary named
        /// constant <c>e</c> does and is kept out of its class regardless.
        /// </summary>
        private static string Key(Entity expr)
            => ReferenceEquals(expr, Entity.Constant.EulerIntrinsic) ? EulerIntrinsicKey
             : expr.DirectChildren.Count == 0 ? expr.StringizeAscii() : expr.GetType().Name;

        /// <summary>Adds <paramref name="expr"/> and every one of its subexpressions, bottom-up.</summary>
        internal int AddEntity(Entity expr)
        {
            var children = expr.DirectChildren.Select(AddEntity).ToArray();
            var codomain = expr.Codomain == expr.DefaultCodomain ? (Domain?)null : expr.Codomain;
            var key = Key(expr);
            if (children.Length == 0 && codomain is null && !leafEntities.ContainsKey(key))
                leafEntities[key] = expr;
            return Add(key, children, codomain);
        }

        /// <summary>
        /// The cheapest entity the e-class <paramref name="id"/> can be built as under
        /// <paramref name="cost"/>, or <see langword="null"/> where nothing in it can be built --
        /// every member refers, directly or through a cycle only unions can create, to a node
        /// type <see cref="MatchPattern.ConstructNode"/> does not build.
        /// </summary>
        internal Entity? Extract(int id, Func<Entity, double> cost)
        {
            // One extraction of the whole graph, kept for as long as the graph does not change
            // and the cost is the same delegate. Only a union can change what an existing class
            // extracts as -- a new class never alters an old one -- so Union is where it is
            // dropped. Before this, every witness an e-match attempt wanted was a fresh walk,
            // once per bound name per binding and once per predicated hole per candidate class,
            // and one such walk of a 253-node graph took 1.9 s: the walk was top-down with a
            // cycle guard, and a graph full of union-made cycles defeats a memo that fills only
            // on completion. https://github.com/asc-community/AngouriMath/issues/1199
            if (!ReferenceEquals(cost, memoCost) || extractMemo is null)
            {
                memoCost = cost;
                extractMemo = ExtractAll(new Cheapest(cost));
            }
            return extractMemo.TryGetValue(Find(id), out var best) ? best : null;
        }

        private Dictionary<int, Entity>? extractMemo;
        private Func<Entity, double>? memoCost;

        /// <summary>
        /// The least entity the e-class <paramref name="id"/> can be built as under
        /// <paramref name="order"/>, or <see langword="null"/> where nothing in it can be built.
        /// </summary>
        /// <remarks>
        /// The same walk as <see cref="Extract(int, Func{Entity, double})"/>, choosing by an order
        /// rather than by a number. That is not a stylistic difference: a cost model ties, and a
        /// tie is settled by whichever member was reached first, so "the cheapest member" is not a
        /// well-defined expression where "the least member" is. See <see cref="EntityOrder"/>.
        /// </remarks>
        internal Entity? ExtractLeast(int id, IComparer<Entity> order)
            => ExtractAll(new Least(order)).TryGetValue(Find(id), out var least) ? least : null;

        /// <summary>
        /// How <c>Extract</c> chooses between the members of one e-class. A
        /// <see langword="struct"/> under a generic constraint rather than an interface reference,
        /// so the choice is compiled in rather than dispatched, and no selection is allocated for
        /// the classes an extraction walks.
        /// </summary>
        private interface ISelection
        {
            /// <summary>Forgets the previous class's incumbent.</summary>
            void Begin();

            /// <summary>Offers a candidate; the incumbent afterwards is the better of the two.</summary>
            void Offer(Entity candidate);

            /// <summary>The incumbent, or <see langword="null"/> where nothing was admissible.</summary>
            Entity? Best { get; }
        }

        private struct Cheapest : ISelection
        {
            private readonly Func<Entity, double> cost;
            private Entity? best;
            private double bestCost;

            internal Cheapest(Func<Entity, double> cost)
                => (this.cost, best, bestCost) = (cost, null, double.MaxValue);

            public void Begin() => (best, bestCost) = (null, double.MaxValue);

            public void Offer(Entity candidate)
            {
                var here = cost(candidate);
                // A model that answers NaN has not ranked this candidate, and declines it. Without
                // this, the comparison below is false for NaN on either side (IEEE-754), so NaN
                // becomes the incumbent cheapest and every later candidate then beats it
                // unconditionally -- the answer stops being the cheapest and becomes whichever
                // member came last.
                if (double.IsNaN(here)) return;
                if (here >= bestCost) return;
                (best, bestCost) = (candidate, here);
            }

            public readonly Entity? Best => best;
        }

        private struct Least : ISelection
        {
            private readonly IComparer<Entity> order;
            private Entity? best;

            internal Least(IComparer<Entity> order) => (this.order, best) = (order, null);

            public void Begin() => best = null;

            // No admissibility test to make: an order ranks every pair, where a cost model can
            // decline one. Nothing here can throw that Compare does not.
            public void Offer(Entity candidate)
            {
                if (best is null || order.Compare(candidate, best) < 0) best = candidate;
            }

            public readonly Entity? Best => best;
        }

        /// <summary>
        /// The best member of every class under <paramref name="seed"/>, from the leaves up: a
        /// round recomputes each class from its children's current best, and the rounds stop
        /// when one changes nothing. No recursion, so no depth to guard and no stack to exhaust;
        /// a member that reaches back into its own class through a union is built from that
        /// class's current best and offered like any other, and loses to it under any selection
        /// that does not prefer the larger of two writings of one thing. Rounds are bounded by
        /// the longest chain of classes, and capped at the class count for a selection that is
        /// not monotone in its children.
        /// </summary>
        private Dictionary<int, Entity> ExtractAll<TSelection>(TSelection seed)
            where TSelection : struct, ISelection
        {
            var best = new Dictionary<int, Entity>();
            var leaves = new Dictionary<string, Entity?>();
            // Ordered, not as the dictionary enumerates: where the selection ties, the answer is
            // whichever candidate was reached first, and dictionary and set order are neither
            // specified nor stable across processes.
            var ids = classes.Keys.OrderBy(id => id).ToList();
            for (var round = 0; round <= ids.Count; round++)
            {
                var changed = false;
                foreach (var id in ids)
                {
                    var selection = seed;
                    selection.Begin();
                    foreach (var node in classes[id].OrderBy(node => node))
                    {
                        var parts = new Entity[node.Children.Length];
                        var ok = true;
                        for (var i = 0; i < parts.Length && ok; i++)
                            if (best.TryGetValue(Find(node.Children[i]), out var part)) parts[i] = part;
                            else ok = false;
                        if (!ok) continue;
                        Entity? built;
                        if (parts.Length == 0)
                        {
                            if (!leaves.TryGetValue(node.Op, out built))
                                leaves[node.Op] = built = Leaf(node.Op);
                        }
                        else built = MatchPattern.ConstructNode(OperatorType(node.Op), parts);
                        if (built is null) continue;
                        if (node.Codomain is { } domain) built = built.WithCodomain(domain);
                        selection.Offer(built);
                    }
                    var chosen = selection.Best;
                    if (chosen is null) continue;
                    if (!best.TryGetValue(id, out var incumbent) || !chosen.Equals(incumbent))
                    {
                        best[id] = chosen;
                        changed = true;
                    }
                }
                if (!changed) break;
            }
            return best;
        }

        /// <summary>The leaf under <paramref name="key"/>: the one this graph was given, else its parse.</summary>
        private Entity? Leaf(string key)
            => leafEntities.TryGetValue(key, out var kept) ? kept : ParseLeaf(key);

        /// <summary>
        /// A leaf read back from its printed form, or <see langword="null"/> where it does not
        /// parse -- a number a fold wrote, which was never given to the graph as an entity.
        /// </summary>
        private static Entity? ParseLeaf(string printed)
        {
            if (printed == EulerIntrinsicKey) return Entity.Constant.EulerIntrinsic;
            return Parser.ParseSilent(printed).Is<Entity>(out var parsed) ? parsed : null;
        }

        /// <summary>
        /// <see cref="MatchPattern.ConstructNode"/> takes the runtime <see cref="Type"/> a
        /// <see cref="Key"/> string names, and <see cref="MatchPattern.BuildableNodeTypes"/> is
        /// where those types are named -- once, beside the <c>Construct</c> that builds them.
        /// This used to hold a second copy of that list, which nothing kept in step: a type added
        /// to one and not the other silently stopped being reachable from here, with no compiler
        /// error to say so.
        /// </summary>
        private static Type OperatorType(string op)
            => MatchPattern.NodeTypeNamed(op) ?? typeof(void);

        /// <summary>
        /// Whether the e-class <paramref name="id"/> already contains a leaf equal to
        /// <paramref name="leaf"/> -- the same check <see cref="NeutralClass"/> uses for a
        /// neutral element, offered for <see cref="Matching.MatchPattern"/>'s
        /// <c>ExactPattern.EMatch</c> to use for a literal.
        /// </summary>
        internal bool ContainsLeaf(int id, Entity leaf) => Holds(id, Key(leaf));

        /// <summary>
        /// The runtime <see cref="Type"/> an e-node builds as: a leaf's, the leaf's own, or a
        /// non-leaf's operator type, by the same lookup <c>Extract</c> uses to reconstruct one.
        /// <c>typeof(void)</c> where neither succeeds.
        /// </summary>
        internal Type RuntimeType(ENode node)
            => node.Children.Length == 0
                ? Leaf(node.Op)?.GetType() ?? typeof(void)
                : OperatorType(node.Op);
    }
}
