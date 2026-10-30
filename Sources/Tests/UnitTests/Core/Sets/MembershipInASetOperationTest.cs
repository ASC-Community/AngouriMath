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

namespace AngouriMath.Tests.Core.Sets
{
    /// <summary>
    /// Membership in a union, an intersection and a difference of sets is decided from each side's by Kleene's
    /// connectives, where a side may leave it open: <c>10^30 + 57</c>, a prime past what the prime test proves, has
    /// its membership in <c>PP</c> left open. A union of <c>PP</c> with a set excluding it was <c>False</c>. <a href="https://github.com/asc-community/AngouriMath/issues/2219">#2219</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class MembershipInASetOperationTest
    {
        [Theory]
        // One side open, the other excluding it: open, in either order.
        [InlineData("(10^30 + 57) in (PP \\/ [0; 1])")]
        [InlineData("(10^30 + 57) in ([0; 1] \\/ PP)")]
        [InlineData("(10^30 + 57) in (PP \\/ (-oo; 0))")]
        [InlineData("(10^30 + 57) in (PP \\/ ({2} \\/ [5; 7]))")]
        // One side open, the other containing it: open for an intersection, and for a difference whose right side is open.
        [InlineData("(10^30 + 57) in (PP /\\ [0; 10^31])")]
        [InlineData("(10^30 + 57) in ([0; 10^31] \\ PP)")]
        [InlineData("(10^30 + 57) in (PP \\ [0; 1])")]
        public void LeftOpen(string input)
            => Assert.IsType<Set.Inf>(input.ToEntity().Evaled);

        [Theory]
        // A union holds where either side does.
        [InlineData("(10^30 + 57) in (PP \\/ [0; 10^31])", "True")]
        [InlineData("2 in (PP \\/ [5; 7])", "True")]
        [InlineData("6 in (PP \\/ [5; 7])", "True")]
        [InlineData("4 in (PP \\/ [5; 7])", "False")]
        // An intersection fails where either side does.
        [InlineData("(10^30 + 57) in (PP /\\ (-oo; 0))", "False")]
        [InlineData("(10^30 + 57) in (PP /\\ [0; 1])", "False")]
        [InlineData("7 in (PP /\\ [5; 10^30])", "True")]
        // A difference fails where its left side does, or its right side holds.
        [InlineData("(10^30 + 57) in ([0; 1] \\ PP)", "False")]
        [InlineData("(10^30 + 57) in (PP \\ [0; 10^31])", "False")]
        [InlineData("7 in (PP \\ [0; 5])", "True")]
        public void Decided(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Evaled);
    }
}
