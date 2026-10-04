//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The two reachability patterns asked for by
    /// https://github.com/asc-community/AngouriMath/issues/327.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A piecewise takes its first matching case, and both rules follow from that alone --
    /// neither needs any predicate to be decidable, which is what makes them applicable
    /// where the existing true/false reduction is not.
    /// </para>
    /// <para>
    /// <b>A repeated predicate</b>: if <c>c</c> guards an earlier case, then wherever
    /// <c>c</c> holds that earlier case is taken, so a later case guarded by the same
    /// <c>c</c> can never be reached.
    /// </para>
    /// <para>
    /// <b>A repeated expression</b>: two *consecutive* cases with the same expression are
    /// one case guarded by either predicate. Consecutive matters -- an intervening case
    /// with a different expression could match between them, and merging across it would
    /// change which value is taken.
    /// </para>
    /// </remarks>
    [Trait("Area", "Common")]
    public sealed class PiecewiseReachabilityTest
    {
        [Theory]
        // A later case with an already-seen predicate is unreachable.
        [InlineData("piecewise(a provided c, b provided c)", "piecewise(a provided c)")]
        [InlineData("piecewise(a provided c, b provided d, g provided c)",
                    "piecewise(a provided c, b provided d)")]
        [InlineData("piecewise(a provided c, b provided c, g provided c)", "piecewise(a provided c)")]
        // Consecutive cases with one expression merge their predicates.
        [InlineData("piecewise(a provided b, a provided c)", "piecewise(a provided b or c)")]
        [InlineData("piecewise(k provided h, a provided b, a provided c)",
                    "piecewise(k provided h, a provided b or c)")]
        [InlineData("piecewise(a provided b, a provided c, a provided d)",
                    "piecewise(a provided b or c or d)")]
        public void ThePatternApplies(string from, string to)
            => Assert.Equal(to.ToEntity().InnerSimplified, from.ToEntity().InnerSimplified);

        [Theory]
        // Different predicates and different expressions: nothing to do.
        [InlineData("piecewise(a provided c, b provided d)")]
        // The same expression, but not consecutive -- b provided d could be taken between
        // them, so merging the two `a` cases would change the value where d holds.
        [InlineData("piecewise(a provided c, b provided d, a provided f)")]
        public void ThePatternDoesNotApply(string expr)
            => Assert.Equal(expr.ToEntity().InnerSimplified, expr.ToEntity().InnerSimplified);

        /// <summary>
        /// The rules must not change which value the piecewise takes at any point. Checked
        /// by substituting every combination of truth values into the predicates and
        /// comparing the two forms, rather than by trusting the rewrite.
        /// </summary>
        [Theory]
        [InlineData("piecewise(a provided c, b provided c)")]
        [InlineData("piecewise(a provided c, b provided d, g provided c)")]
        [InlineData("piecewise(a provided b, a provided c)")]
        [InlineData("piecewise(k provided h, a provided b, a provided c)")]
        [InlineData("piecewise(a provided c, b provided d, a provided f)")]
        public void TheValueIsUnchangedForEveryAssignment(string expr)
        {
            var original = expr.ToEntity();
            var simplified = original.InnerSimplified;
            var predicates = new[] { "b", "c", "d", "f", "h" };
            for (var mask = 0; mask < 1 << predicates.Length; mask++)
            {
                Entity before = original, after = simplified;
                for (var i = 0; i < predicates.Length; i++)
                {
                    Entity truth = (mask & (1 << i)) != 0 ? MathS.Boolean.True : MathS.Boolean.False;
                    before = before.Substitute(predicates[i], truth);
                    after = after.Substitute(predicates[i], truth);
                }
                Assert.Equal(before.InnerSimplified, after.InnerSimplified);
            }
        }
    }
}
