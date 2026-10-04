//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The three spellings <c>BREAKING-CHANGES.md</c> lists under *Spellings known to be at risk*,
    /// pinned to the readings that section states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The section says what each of these parses as **today**, which is a claim about the grammar
    /// and therefore a claim that can go stale. It is here so that it cannot: change any of these
    /// readings and this test fails, which is the prompt to update the section — or to delete it,
    /// if what changed is the thing the section is warning about.
    /// </para>
    /// <para>
    /// <b>These assertions are not endorsements.</b> Every one of them records a reading that is
    /// well-formed, silent and unlike what was written, which is the whole reason the spelling is
    /// on the list. The docket for changing them is
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1019">#1019</a> items 6 and 7,
    /// out of the notation survey on
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1212">#1212</a>; the
    /// <c>!=</c> half is <a href="https://github.com/asc-community/AngouriMath/issues/1225">#1225</a>.
    /// When one of them is settled, the failure here is the reminder that the documentation moves
    /// with it.
    /// </para>
    /// </remarks>
    [Trait("Area", "Convenience")]
    public sealed class SpellingsAtRiskTest
    {
        // `|` is an alias for `or`, so a divisibility statement is read as a disjunction.
        [Fact]
        public void TheBarIsDivisibility()
        {
            Assert.Equal("2 divides 6".ToEntity(), "2 | 6".ToEntity());
            Assert.IsType<Dividesf>("2 | 6".ToEntity());
            Assert.Equal(Boolean.True, "2 | 6".ToEntity().Evaled);
            // It was a disjunction until this changed, so `or` is what that input now needs.
            Assert.IsType<Orf>("2 or 6".ToEntity());
        }

        // Ordinary set-builder notation is still not read as a set builder — the bar changing
        // meaning did not fix this, it changed what the one element is. The library's own set
        // builder is `{ x : x > 0 }` and remains the spelling that works.
        [Fact]
        public void SetBuilderWithABarIsAOneElementFiniteSet()
        {
            var written = "{ x | x > 0 }".ToEntity();
            var set = Assert.IsType<Set.FiniteSet>(written);
            // Divisibility binds tighter than a comparison, as `divides` always has, so the one
            // element is `(x divides x) > 0` — a comparison whose left side is a statement.
            Assert.Equal("x divides x > 0".ToEntity(), Assert.Single(set.Elements));
            // What was meant, and how to say it today.
            Assert.IsType<Set.ConditionalSet>("{ x : x > 0 }".ToEntity());
        }

        // `!=` is not a token, so the lexer takes `!` as the postfix factorial and `=` as
        // equality. `<>` is the spelling that means what `!=` looks like it means.
        [Fact]
        public void NotEqualIsReadAsAFactorialEquation()
        {
            Assert.Equal("a! = b".ToEntity(), "a != b".ToEntity());
            Assert.IsType<Equalsf>("a != b".ToEntity());
            Assert.Equal("not a = b".ToEntity(), "a <> b".ToEntity());
        }
    }
}
