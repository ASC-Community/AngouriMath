//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Functions.TreeAnalyzer;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// The sort key of a node is spelt once per instance and level and kept on the instance.
    /// What has to stay true: the key is the same string it always was, a copy made with
    /// <c>with</c> spells its own key rather than inheriting the original's, and asking from
    /// several threads at once gives every thread the same string.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class SortKeyCacheTest
    {
        [Theory]
        [InlineData("x + y * sin(x) + 3 / x")]
        [InlineData("(a + b) * (a - b) + a ^ 2")]
        public void TheKeyIsTheSameOnEveryAsk(string source)
        {
            var expr = source.ToEntity();
            foreach (var level in new[] { SortLevel.HIGH_LEVEL, SortLevel.MIDDLE_LEVEL, SortLevel.LOW_LEVEL })
            {
                var first = expr.SortHash(level);
                var again = expr.SortHash(level);
                Assert.Equal(first, again);
                // The same tree built separately spells the same key: the cache changes nothing.
                Assert.Equal(first, source.ToEntity().SortHash(level));
            }
        }

        // A number's key at the exact level is its printed form, and the printed form carries
        // a codomain. WithCodomain is `this with { Codomain = ... }`, which copies every field
        // of the record -- a cache kept on the instance included -- so the copy has to notice
        // that the cache is not its own.
        [Fact]
        public void ACopyWithAnotherCodomainSpellsItsOwnKey()
        {
            Entity half = "1/2";
            var plain = half.SortHash(SortLevel.LOW_LEVEL);
            var annotated = half.WithCodomain(Domain.Complex);
            var annotatedKey = annotated.SortHash(SortLevel.LOW_LEVEL);
            Assert.NotEqual(plain, annotatedKey);
            Assert.Equal(annotated.Stringize() + " ", annotatedKey);
            // And the original is not disturbed by the copy having been asked.
            Assert.Equal(plain, half.SortHash(SortLevel.LOW_LEVEL));
        }

        // An Entity is a record and compares every field. The cache must not take part: the
        // simplifier recognises a repeated candidate by equality, and a tree that had been
        // sorted comparing unequal to the same tree that had not sent SimplifyHard to 30 GB.
        [Fact]
        public void SortingDoesNotDisturbEquality()
        {
            var sorted = "x + y * sin(x) + 3 / x".ToEntity();
            var untouched = "x + y * sin(x) + 3 / x".ToEntity();
            sorted.SortHash(SortLevel.LOW_LEVEL);
            Assert.Equal(sorted, untouched);
            Assert.Equal(sorted.GetHashCode(), untouched.GetHashCode());
            Assert.Single(new System.Collections.Generic.HashSet<Entity> { sorted, untouched });
            Assert.Equal(sorted.Simplify(), untouched.Simplify());
        }

        [Fact]
        public void EveryThreadGetsTheSameKey()
        {
            var expr = "x * y + sin(x) ^ 2 * cos(y) + (a + b) / (c - d)".ToEntity();
            var expected = expr.SortHash(SortLevel.HIGH_LEVEL);
            var keys = new string[64];
            Parallel.For(0, keys.Length, i => keys[i] = "x * y + sin(x) ^ 2 * cos(y) + (a + b) / (c - d)".ToEntity().SortHash(SortLevel.HIGH_LEVEL));
            Assert.All(keys, key => Assert.Equal(expected, key));
            var same = new string[64];
            Parallel.For(0, same.Length, i => same[i] = expr.SortHash(SortLevel.MIDDLE_LEVEL));
            Assert.Single(same.Distinct());
        }
    }
}
