//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core.Sets
{
    /// <summary>
    /// An interval whose left end is above its right is empty, and a union with it is the other
    /// set. Two intervals were joined wherever they touched, before either was asked whether it was
    /// empty: <c>[0; 1] \/ [1; 0]</c> became <c>{ 1 }</c> and <c>[a; b] \/ [b; a]</c> became <c>{ b }</c>.
    /// https://github.com/asc-community/AngouriMath/issues/1634
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AnEmptyIntervalAddsNothingTest
    {
        private static Entity IsIn(string value, string set) => $"{value} in ({set})".ToEntity().Simplify();

        /// <summary>The union is decided at points on both sides of every end, not read off its printed shape.</summary>
        [Theory]
        [InlineData("[0; 1] \\/ [1; 0]", "0, 1/2, 1", "-1, 3/2")]
        [InlineData("[1; 0] \\/ [0; 1]", "0, 1/2, 1", "-1, 3/2")]
        [InlineData("[3; 1] \\/ [1; 2]", "1, 3/2, 2", "0, 5/2, 3")]
        [InlineData("[0; 1] \\/ [1; 1)", "0, 1/2, 1", "-1, 3/2")]
        [InlineData("[2; 2] \\/ [0; 1]", "0, 1, 2", "-1, 3/2, 3")]
        [InlineData("[0; 1] \\/ [1; 2]", "0, 1, 2", "-1, 3")]
        public void TheUnionHasTheMembersOfItsNonEmptyParts(string union, string members, string others)
        {
            var simplified = union.ToEntity().Simplify().Stringize();
            foreach (var member in members.Split(", "))
                Assert.Equal(Boolean.True, IsIn(member, simplified));
            foreach (var other in others.Split(", "))
                Assert.Equal(Boolean.False, IsIn(other, simplified));
        }

        /// <summary>
        /// With ends that are not numbers, either interval may be empty, so touching is not joining:
        /// the union is left as written. An end at an infinity leaves nothing to doubt.
        /// </summary>
        [Fact]
        public void EndsThatAreNotNumbersAreNotJoinedWhereEitherMayBeEmpty()
        {
            Assert.IsType<Set.Unionf>("[a; b] \\/ [b; a]".ToEntity().Simplify());
            Assert.IsType<Set.Unionf>("[a; b] \\/ [b; c]".ToEntity().Simplify());
            Assert.Equal("RR", "(-oo; x] \\/ [x; +oo)".ToEntity().Simplify().Stringize());
        }
    }
}
