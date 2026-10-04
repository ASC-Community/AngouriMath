//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Core.Transformations.Matching;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    [Trait("Area", "Core")]
    public sealed class MatchedRulesAllTest
    {
        [Fact]
        public void AllListsMoreThanTheParameterlessProperties()
        {
            // Sort and CommonDenominator are methods, not properties -- a naive property-only
            // reflection would miss both, at three SortLevel values each.
            var names = MatchedRules.All.Select(set => set.Name).ToList();
            Assert.Contains(names, name => name.StartsWith("Sort"));
            Assert.Contains(names, name => name.StartsWith("CommonDenominator"));
        }

        [Fact]
        public void AllIsSortedByName()
        {
            var names = MatchedRules.All.Select(set => set.Name).ToList();
            var sorted = names.OrderBy(n => n, System.StringComparer.Ordinal).ToList();
            Assert.Equal(sorted, names);
        }

        [Fact]
        public void AllContainsAKnownOrdinarySet()
        {
            Assert.Contains(MatchedRules.All, set => set.Name == MatchedRules.CollapseMultipleFractions.Name);
        }
    }
}
