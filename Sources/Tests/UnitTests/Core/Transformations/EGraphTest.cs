//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    [Trait("Area", "Core")]
    public sealed class EGraphTest
    {
        [Fact]
        public void AddingTheSameLeafTwiceReturnsTheSameClass()
        {
            var graph = new EGraph();
            var first = graph.Add("x");
            var second = graph.Add("x");
            Assert.Equal(first, second);
        }

        [Fact]
        public void AddingTwoDifferentLeavesReturnsDifferentClasses()
        {
            var graph = new EGraph();
            var x = graph.Add("x");
            var y = graph.Add("y");
            Assert.NotEqual(x, y);
        }

        [Fact]
        public void UnionMakesFindAgreeOnBothClasses()
        {
            var graph = new EGraph();
            var x = graph.Add("x");
            var y = graph.Add("y");
            graph.Union(x, y);
            Assert.Equal(graph.Find(x), graph.Find(y));
        }

        [Fact]
        public void UnionOfTheSameClassReportsNoChange()
        {
            var graph = new EGraph();
            var x = graph.Add("x");
            Assert.False(graph.Union(x, x));
        }

        [Fact]
        public void UnionOfTwoDifferentClassesReportsAChange()
        {
            var graph = new EGraph();
            var x = graph.Add("x");
            var y = graph.Add("y");
            Assert.True(graph.Union(x, y));
        }

        [Fact]
        public void UnionReducesTheClassCountByOne()
        {
            var graph = new EGraph();
            var x = graph.Add("x");
            var y = graph.Add("y");
            var before = graph.ClassCount;
            graph.Union(x, y);
            Assert.Equal(before - 1, graph.ClassCount);
        }

        [Fact]
        public void RebuildMergesNodesThatBecomeCongruentAfterAUnion()
        {
            // f(x) and f(y) are different nodes until x and y are unioned; the congruence
            // rebuild is what then notices the two applications of f agree too.
            var graph = new EGraph();
            var x = graph.Add("x");
            var y = graph.Add("y");
            var fx = graph.Add("f", x);
            var fy = graph.Add("f", y);
            Assert.NotEqual(graph.Find(fx), graph.Find(fy));

            graph.Union(x, y);
            graph.Rebuild();

            Assert.Equal(graph.Find(fx), graph.Find(fy));
        }

        [Fact]
        public void NodesOfReturnsTheNodeJustAdded()
        {
            var graph = new EGraph();
            var x = graph.Add("x");
            Assert.Contains(new ENode("x", System.Array.Empty<int>()), graph.NodesOf(x));
        }

        [Fact]
        public void ClassesListsEveryClassAdded()
        {
            var graph = new EGraph();
            var x = graph.Add("x");
            var y = graph.Add("y");
            Assert.Equal(new[] { x, y }, graph.Classes.OrderBy(id => id));
        }

        [Theory]
        // x + 0 -> x, and commutatively 0 + x -> x.
        [InlineData("Sumf", "x", "0", true)]
        [InlineData("Sumf", "0", "x", true)]
        // x - 0 -> x, but 0 - x is a different value (its negation), not a fold.
        [InlineData("Minusf", "x", "0", true)]
        [InlineData("Minusf", "0", "x", false)]
        // x * 1 -> x, and commutatively 1 * x -> x.
        [InlineData("Mulf", "x", "1", true)]
        [InlineData("Mulf", "1", "x", true)]
        // x / 1 -> x, but 1 / x is a different value (its reciprocal), not a fold.
        [InlineData("Divf", "x", "1", true)]
        [InlineData("Divf", "1", "x", false)]
        // x ^ 1 -> x, but 1 ^ x is the constant 1, not this operand -- not this fold either.
        [InlineData("Powf", "x", "1", true)]
        [InlineData("Powf", "1", "x", false)]
        public void NeutralElementFoldsIntoTheOtherOperandsClassOnlyWhenTheValuesAgree(
            string op, string leftLeaf, string rightLeaf, bool shouldFold)
        {
            var graph = new EGraph();
            var left = graph.Add(leftLeaf);
            var right = graph.Add(rightLeaf);
            var before = graph.NodeCount;

            var result = graph.Add(op, left, right);

            var nonLeafOperand = leftLeaf == "x" ? left : right;
            if (shouldFold)
            {
                Assert.Equal(graph.Find(nonLeafOperand), graph.Find(result));
                Assert.Equal(before, graph.NodeCount);
            }
            else
            {
                Assert.NotEqual(graph.Find(nonLeafOperand), graph.Find(result));
                Assert.Equal(before + 1, graph.NodeCount);
            }
        }

        [Theory]
        [InlineData("x + 1")]
        [InlineData("sin(x) * cos(y)")]
        [InlineData("(x + 1) ^ 2")]
        public void AnEntityAddedAndExtractedUnchangedRoundTrips(string source)
        {
            var expr = source.ToEntity();
            var graph = new EGraph();
            var root = graph.AddEntity(expr);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.NotNull(extracted);
            Assert.True(expr.Equals(extracted));
        }

        [Fact]
        public void FoldingOnInsertionMeansNoRuleIsNeededToDropAnAddedZero()
        {
            var expr = "x + 0".ToEntity();
            var graph = new EGraph();
            var root = graph.AddEntity(expr);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.True("x".ToEntity().Equals(extracted));
        }

        /// <summary>
        /// <see cref="EGraph.Extract"/> rebuilds a non-leaf node through
        /// <see cref="AngouriMath.Core.Transformations.Matching.MatchPattern.ConstructNode"/>,
        /// which never restores <see cref="Entity.Codomain"/> -- an init-settable, per-node-type
        /// property the rest of the codebase treats as load-bearing and explicitly preserves on
        /// every <c>Replace</c>. Caught in code review before this PR was merged.
        /// </summary>
        [Fact]
        public void ExtractPreservesANarrowedCodomain()
        {
            Entity narrowed = MathS.Sqrt(-1).WithCodomain(Domain.Real);
            var graph = new EGraph();
            var root = graph.AddEntity(narrowed);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.NotNull(extracted);
            Assert.Equal(Domain.Real, extracted!.Codomain);
            Assert.Equal(narrowed, extracted);
            Assert.Equal(narrowed.Evaled, extracted.Evaled);
        }

        /// <summary>
        /// <see cref="EGraph.Key"/> keys a leaf by <see cref="Entity.Stringize"/>, and
        /// <see cref="Entity.Constant.EulerIntrinsic"/> prints identically to the ordinary named
        /// constant <c>e</c> -- the two are <see cref="object.Equals(object)"/>-equal by design,
        /// but only <see cref="EulerIntrinsic"/> is meant to stay outside what a binder over the
        /// name <c>e</c> can capture. Re-parsing the printed form silently swaps it for the named
        /// constant, which is invisible to every equality-based check and only shows up at a
        /// binder. Caught in code review before this PR was merged.
        /// </summary>
        [Fact]
        public void ExtractPreservesEulerIntrinsicIdentity()
        {
            var graph = new EGraph();
            var id = graph.AddEntity(Entity.Constant.EulerIntrinsic);

            var extracted = graph.Extract(id, CostModel.Default.Cost);

            Assert.True(ReferenceEquals(Entity.Constant.EulerIntrinsic, extracted));
        }

        /// <summary>
        /// A cost model that answers <see cref="double.NaN"/> has not ranked the candidate, and
        /// every IEEE-754 comparison against <see cref="double.NaN"/> is false -- so the
        /// cheapest-so-far test never declines it. A model that <i>throws</i> is already declined
        /// by the surrounding <see langword="catch"/>; one that answers
        /// <see cref="double.NaN"/> is saying the same thing and is declined the same way.
        /// </summary>
        [Fact]
        public void ExtractDeclinesACandidateItsCostModelCannotRank()
        {
            var graph = new EGraph();
            var root = graph.AddEntity("x".ToEntity());

            var extracted = graph.Extract(root, static _ => double.NaN);

            Assert.Null(extracted);
        }

        /// <summary>
        /// The consequence of the above when a class holds more than one candidate: an unranked
        /// candidate that is not declined becomes the incumbent cheapest, and because every later
        /// comparison against it is false, every candidate after it wins unconditionally --
        /// <see cref="EGraph.Extract"/> stops answering with the cheapest and answers with
        /// whichever member the enumeration reached last.
        /// </summary>
        [Fact]
        public void ExtractPicksTheCheapestPastACandidateItCannotRank()
        {
            var graph = new EGraph();
            var cheap = graph.AddEntity("x".ToEntity());
            graph.Union(cheap, graph.AddEntity("y".ToEntity()));
            graph.Union(cheap, graph.AddEntity("z".ToEntity()));

            var extracted = graph.Extract(cheap, static candidate => candidate.Stringize() switch
            {
                "x" => 1,
                "y" => double.NaN,
                _ => 100
            });

            Assert.True("x".ToEntity().Equals(extracted));
        }

        /// <summary>
        /// <see cref="System.Collections.Generic.HashSet{T}"/> enumeration order is an
        /// unspecified implementation detail, and a string's hash code is randomised per process
        /// -- so without a defined order over a class's members, an exact cost tie is settled
        /// differently from one run to the next. <see cref="CostModel"/>'s own remarks say ties
        /// are common enough to design the models against, and a bounded computation is meant to
        /// be reproducible given a defined algorithm order. A tie goes to the ordinally-first
        /// e-node.
        /// </summary>
        [Fact]
        public void ExtractSettlesACostTieOnADefinedOrder()
        {
            var graph = new EGraph();
            var id = graph.AddEntity("b".ToEntity());
            graph.Union(id, graph.AddEntity("c".ToEntity()));
            graph.Union(id, graph.AddEntity("a".ToEntity()));

            var extracted = graph.Extract(id, static _ => 1);

            Assert.True("a".ToEntity().Equals(extracted));
        }

        /// <summary>
        /// <see cref="EGraph.Extract"/> used to recurse through an e-class's children and decline
        /// to build past 256 levels, because a <see cref="System.StackOverflowException"/> cannot
        /// be caught. It is a fixed point from the leaves up now, with no stack to exhaust, so a
        /// chain three hundred deep extracts as itself -- this test asserted <see langword="null"/>
        /// for that input while the guard existed.
        /// </summary>
        [Fact]
        public void ExtractBuildsAChainDeeperThanTheOldCap()
        {
            Entity deep = "x".ToEntity();
            for (var i = 0; i < 300; i++) deep += 1;
            var graph = new EGraph();
            var root = graph.AddEntity(deep);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.Equal(deep, extracted);
        }

        /// <summary>
        /// The cap is a crash guard, not a quality knob: an expression of ordinary depth must
        /// still extract normally.
        /// </summary>
        [Fact]
        public void ExtractStillBuildsAnExpressionOfOrdinaryDepth()
        {
            Entity ordinary = "x".ToEntity();
            for (var i = 0; i < 30; i++) ordinary += 1;
            var graph = new EGraph();
            var root = graph.AddEntity(ordinary);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.NotNull(extracted);
        }

        /// <summary>
        /// <see cref="EGraph.Extract"/> rebuilds through the node types
        /// <c>MatchPattern.Construct</c> knows, and that used to be fourteen arithmetic and
        /// trigonometric ones. Every other root -- a comparison, a condition attached to a value,
        /// a set operation -- extracted as <see langword="null"/>, so
        /// <c>Transformation.EqualitySaturation</c> handed back its input reporting
        /// <c>Changed = false</c>, which is indistinguishable from "already at its cheapest" even
        /// where the graph had proved an improvement inside it.
        /// </summary>
        [Theory]
        [InlineData("x > 0")]
        [InlineData("x = y")]
        [InlineData("x >= y + 1")]
        [InlineData("a and b")]
        [InlineData("not a")]
        [InlineData("x mod y")]
        [InlineData("floor(x)")]
        [InlineData("arcsin(x)")]
        [InlineData("x!")]
        // A special set is a leaf, so both operands rebuild and the union over them does too.
        [InlineData("RR unite ZZ")]
        public void ExtractRebuildsRootsThatUsedToFallOutsideTheBuildableTypes(string source)
        {
            var expr = source.ToEntity();
            var graph = new EGraph();
            var root = graph.AddEntity(expr);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.NotNull(extracted);
            Assert.True(expr.Equals(extracted), $"{source} came back as {extracted!.Stringize()}");
        }

        /// <summary>
        /// The case the review named specifically: a rule that does not hold unconditionally
        /// wraps its result in a <see cref="Entity.Providedf"/>, which is the registry's own
        /// documented convention -- and that wrapper used to be unbuildable, so the class it was
        /// unioned onto was a dead end and the improvement never reached extraction.
        /// </summary>
        [Fact]
        public void ExtractRebuildsAConditionAttachedToAValue()
        {
            Entity provided = new Entity.Providedf("x".ToEntity(), "x > 0".ToEntity());
            var graph = new EGraph();
            var root = graph.AddEntity(provided);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.NotNull(extracted);
            Assert.IsType<Entity.Providedf>(extracted);
            Assert.True(provided.Equals(extracted));
        }

        /// <summary>
        /// A binder is still not rebuilt, and that is deliberate rather than pending: the e-graph
        /// has no notion of a bound variable's scope, and <c>DirectChildren</c> hands out a
        /// capture-avoidingly renamed body, so rebuilding one from an e-class would produce a term
        /// meaning something else.
        /// </summary>
        /// <remarks>
        /// The variable-arity nodes are declined for a different reason -- the table keys on a
        /// type and an arity of one or two, so an n-child node has no entry -- and a union over
        /// two finite sets shows that this reaches the root through its operands: the
        /// <c>Unionf</c> itself is buildable and its operands are not.
        /// </remarks>
        [Theory]
        [InlineData("{ x : x > 0 }")]                 // a binder: no notion of scope here
        [InlineData("{ 1, 2 }")]                      // variable arity
        [InlineData("{ 1, 2 } unite { 2, 3 }")]       // buildable root, unbuildable operands
        public void ExtractStillDeclinesWhatItCannotFaithfullyRebuild(string source)
        {
            var graph = new EGraph();
            var root = graph.AddEntity(source.ToEntity());

            Assert.Null(graph.Extract(root, CostModel.Default.Cost));
        }

        /// <summary>
        /// The folds the e-graph performs on insertion are read off <see cref="Entity.InnerSimplified"/>
        /// rather than written out again, so the two cannot disagree. This names what that
        /// derivation currently finds — not to restate the table, but so that a change in
        /// <c>InnerSimplify</c> shows up here as a changed list rather than as e-graph folding
        /// quietly gaining or losing a case.
        /// </summary>
        /// <remarks>
        /// The asymmetries are the point. <c>x + 0</c> and <c>0 + x</c> both fold, because
        /// addition is commutative; <c>x - 0</c> folds and <c>0 - x</c> does not, because that is
        /// a negation and not this operand; <c>x ^ 1</c> folds and <c>1 ^ x</c> does not, because
        /// that is the constant 1.
        /// </remarks>
        [Fact]
        public void TheFoldsAreExactlyWhatInnerSimplifyDoes()
            => Assert.Equal(
                new[]
                {
                    "Divf(x, 1)", "Minusf(x, 0)", "Mulf(1, x)", "Mulf(x, 1)",
                    "Powf(x, 1)", "Sumf(0, x)", "Sumf(x, 0)",
                },
                EGraph.NeutralFolds.OrderBy(fold => fold, System.StringComparer.Ordinal).ToArray());

        [Theory]
        // The e-graph folds exactly where InnerSimplify does, so these agree by construction.
        [InlineData("x + 0")]
        [InlineData("0 + x")]
        [InlineData("x - 0")]
        [InlineData("x * 1")]
        [InlineData("1 * x")]
        [InlineData("x / 1")]
        [InlineData("x ^ 1")]
        public void AFoldedInsertionAgreesWithInnerSimplify(string source)
        {
            var expr = source.ToEntity();
            var graph = new EGraph();
            var root = graph.AddEntity(expr);

            var extracted = graph.Extract(root, CostModel.Default.Cost);

            Assert.Equal(expr.InnerSimplified, extracted);
        }

        [Fact]
        public void ContainsLeafFindsAMatchingLiteral()
        {
            var graph = new EGraph();
            var id = graph.AddEntity("2".ToEntity());
            Assert.True(graph.ContainsLeaf(id, "2".ToEntity()));
            Assert.False(graph.ContainsLeaf(id, "3".ToEntity()));
        }

        [Fact]
        public void RuntimeTypeOfALeafIsItsOwnType()
        {
            var graph = new EGraph();
            var id = graph.AddEntity("x".ToEntity());
            var node = graph.NodesOf(id).Single();
            Assert.Equal(typeof(Entity.Variable), graph.RuntimeType(node));
        }

        [Fact]
        public void RuntimeTypeOfANonLeafIsItsNodeType()
        {
            var graph = new EGraph();
            var id = graph.AddEntity("x + y".ToEntity());
            var node = graph.NodesOf(id).Single();
            Assert.Equal(typeof(Entity.Sumf), graph.RuntimeType(node));
        }
    }
}
