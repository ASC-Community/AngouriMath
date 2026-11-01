//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// A union or an intersection of a sequence computes each element once. Asking whether
    /// there was a first and then folding enumerated a lazy sequence twice, and the solvers hand
    /// these a sequence of solves, so the first of them was solved twice. No issue was filed.
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class FoldEnumerationTest
    {
        /// <summary>
        /// Computed only as it is enumerated, as a solver's sequence of solves is. A selection
        /// over a range or a list would not do: its count is known without computing anything,
        /// so asking whether it has a first element computes none.
        /// </summary>
        private sealed class Computed
        {
            internal int Count { get; private set; }

            internal IEnumerable<Set> Sets(params Set[] sets)
            {
                foreach (var set in sets)
                {
                    Count++;
                    yield return set;
                }
            }
        }

        [Fact]
        public void AUnionComputesEachSetOnce()
        {
            var computed = new Computed();
            var union = computed.Sets(new FiniteSet((Entity)1), new FiniteSet((Entity)2), new FiniteSet((Entity)3)).Unite();
            Assert.Equal("{ 1, 2, 3 }".ToEntity(), union.InnerSimplified);
            Assert.Equal(3, computed.Count);
        }

        [Fact]
        public void AnIntersectionComputesEachSetOnce()
        {
            var computed = new Computed();
            var intersection = computed.Sets(new FiniteSet((Entity)1, 2), new FiniteSet((Entity)1, 3), new FiniteSet((Entity)1)).Intersect();
            Assert.Equal("{ 1 }".ToEntity(), intersection.InnerSimplified);
            Assert.Equal(3, computed.Count);
        }

        [Fact]
        public void TheEmptyFoldsAreStillEmpty()
        {
            Assert.Equal(Empty, Enumerable.Empty<Set>().Unite());
            Assert.Equal(Empty, Enumerable.Empty<Set>().Intersect());
        }
    }
}
