//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Core.Transformations;
using AngouriMath.Core.Transformations.Matching;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    [Trait("Area", "Core")]
    public sealed class MatchedRuleTryEMatchApplyTest
    {
        private static readonly System.Func<Entity, double> Cost = AngouriMath.Core.CostModel.Default.Cost;

        [Fact]
        public void TryEMatchApplyAgreesWithTryApplyOnANonEGraphExpression()
        {
            // Rather than taking the first rule whose shape is right and then hoping the
            // corpus has an entry for it -- which does not hold here; probing found the first
            // few such rules (by registry order) fire on none of the eighteen corpus rows --
            // pick the first (rule, corpus entry) pair that actually fires together. This is
            // the "find a rule/corpus combination that actually exercises the path" the task
            // brief asks for when the escape hatch would otherwise always trigger.
            var corpus = TransformationTest.Corpus.Select(row => (string)row[0]).Select(text => text.ToEntity()).ToList();
            var (rule, source) = MatchedRules.All
                .SelectMany(set => set.Rules)
                .Where(r => r.Left.CanEMatch && r.Right is not null && r.Right.CanEMatch)
                .Select(r => (rule: r, source: corpus.FirstOrDefault(entity => r.TryApply(entity) is not null)))
                .First(pair => pair.source is not null);

            var expected = rule.TryApply(source!);

            var graph = new EGraph();
            var root = graph.AddEntity(source!);
            graph.Rebuild();
            Assert.True(rule.TryEMatchApply(graph, root, Cost, out var resultClass));
            var actual = graph.Extract(resultClass, Cost);

            Assert.NotNull(actual);
            Assert.Equal(expected!.Evaled, actual!.Evaled);
        }

        [Fact]
        public void TryEMatchApplyThrowsWhenTheRuleCannotEMatch()
        {
            // RationalizeDenominator's own rules turned out e-matchable (probed directly), so
            // this needs a rule whose Left actually contains a GatheredPattern.
            // PythagoreanIdentity's is the one such rule in the whole registry (also probed):
            // its Left gathers sin(x)^2 and cos(x)^2 out of an arbitrary-length sum, and
            // GatheredPattern.CanEMatch is unconditionally false (see MatchPattern.cs).
            var rule = MatchedRules.PythagoreanIdentity.Rules
                .First(r => !r.Left.CanEMatch);
            var graph = new EGraph();
            var root = graph.AddEntity("x".ToEntity());
            Assert.Throws<System.InvalidOperationException>(
                () => rule.TryEMatchApply(graph, root, Cost, out _));
        }
    }
}
