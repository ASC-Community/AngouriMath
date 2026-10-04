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
    /// Intersections and unions of intervals were left as written.
    /// https://github.com/asc-community/AngouriMath/issues/415
    /// </summary>
    /// <remarks>
    /// The behaviour was fixed by PR #677, but the example the issue actually reports -- a
    /// screenshot, which is why it was easy to skip -- was never among the cases tested. An
    /// intersection of my own choosing was tested instead, and the maintainer said so on the
    /// issue. This pins the reporter's expression, character for character.
    /// </remarks>
    [Trait("Area", "Common")]
    public sealed class IntervalSimplificationTest
    {
        /// <summary>
        /// The expression from the screenshot on #415, in the syntax it was written in.
        /// <code>
        ///     (-1; 1) /\ (((-(sqrt(33) + 3)) / 6; (sqrt(33) - 3) / 6) \/ (1; +oo))
        /// </code>
        /// The left endpoint of the inner interval is about -1.457 and the right about
        /// 0.457, so intersecting with (-1; 1) keeps (-1; (sqrt(33) - 3) / 6), and the
        /// (1; +oo) branch contributes nothing.
        /// </summary>
        [Fact]
        public void TheReportersOwnExample()
        {
            var simplified = @"(-1; 1) /\ (((-(sqrt(33) + 3)) / 6; (sqrt(33) - 3) / 6) \/ (1; +oo))"
                .ToEntity().Simplify();
            Assert.Equal(@"(-1; (sqrt(33) - 3) / 6)".ToEntity(), simplified);
        }

        /// <summary>
        /// The endpoints are surds, and the comparison that decides the intersection has to
        /// evaluate them rather than compare them as written -- which is what PR #677 fixed.
        /// Checked by membership rather than by shape, so that a differently spelled but
        /// equal answer still passes and a wrong one still fails.
        /// </summary>
        [Theory]
        [InlineData("-0.9", true)]
        [InlineData("0", true)]
        [InlineData("0.45", true)]
        [InlineData("0.46", false)]   // just past (sqrt(33) - 3) / 6 = 0.4574...
        [InlineData("-1", false)]     // open at the left end
        [InlineData("0.99", false)]
        [InlineData("2", false)]      // the (1; +oo) branch is cut away by (-1; 1)
        public void TheAnswerHoldsTheRightPoints(string point, bool expected)
        {
            var simplified = (Entity.Set)@"(-1; 1) /\ (((-(sqrt(33) + 3)) / 6; (sqrt(33) - 3) / 6) \/ (1; +oo))"
                .ToEntity().Simplify();
            Assert.Equal(expected, simplified.Contains(point.ToEntity()));
        }

        /// <summary>
        /// Intersection distributing over a union, which is the other half of what PR #677
        /// had to add for the example above to reduce at all.
        /// </summary>
        [Theory]
        [InlineData(@"[3; 7] \/ [5; 10]", "[3; 10]")]
        [InlineData(@"[1; 5] /\ [3; 8]", "[3; 5]")]
        [InlineData(@"[0; 10] /\ ([1; 2] \/ [3; 4])", @"[1; 2] \/ [3; 4]")]
        public void IntervalsCombine(string expr, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), expr.ToEntity().Simplify());
    }
}
