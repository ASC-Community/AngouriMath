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

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A conjunction whose one side is a listed solution set and whose other is a condition:
    /// the members are kept where the condition is decided <c>True</c> at them, dropped where
    /// <c>False</c>, and the conjunction stays written the moment one is undecided -- which is
    /// #1036's case, where the condition was a search left open. <c>x^2 = 4 and not x = 2</c>
    /// is <c>{ -2 }</c>; and with it the injectivity of a linear map is decided, the
    /// reference's Def 7.4.6 (Sullivan and Mackey).
    /// <see href="https://github.com/asc-community/AngouriMath/issues/1409"/>,
    /// <see href="https://github.com/asc-community/AngouriMath/issues/1036"/>.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ConjunctionFilterTest
    {
        [Theory]
        [InlineData("x^2 = 4 and not x = 2", "{-2}")]
        [InlineData("x^2 = 4 and not x = 3", "{-2, 2}")]
        [InlineData("x^2 = 4 and x > 0", "{2}")]
        [InlineData("(x - 1) (x - 2) = 0 and not x = 1 and not x = 2", "{}")]
        public void AListedSetIsFilteredByADecidedCondition(string statement, string expected)
        {
            var solved = statement.ToEntity().Solve("x");
            var wanted = (Set)expected.ToEntity().Evaled;
            Assert.Equal(Boolean.True, MathS.Sets.Subset(solved, wanted).Evaled);
            Assert.Equal(Boolean.True, MathS.Sets.Subset(wanted, solved).Evaled);
        }

        /// <summary>#1036: a member whose condition is not decided is not kept and not dropped -- the conjunction is left as written.</summary>
        [Fact]
        public void AnUndecidedConditionLeavesTheConjunctionWritten()
        {
            var solved = "x^6 + x*y + 1 = 0 and x - 1 = 0".ToEntity().Solve("x");
            Assert.IsType<Set.ConditionalSet>(solved);
        }

        /// <summary>
        /// Two listed sets meet only where their members agree: <c>x = y and x = 2</c> is
        /// <c>{ 2 }</c> at <c>y = 2</c> and empty elsewhere. It was <c>{ y }</c>, the second
        /// equation dropped, since a member of an intersection whose membership was undecided
        /// was kept. <see href="https://github.com/asc-community/AngouriMath/issues/1680"/>
        /// </summary>
        [Theory]
        [InlineData("x = y and x = 2", 2, "{2}")]
        [InlineData("x = y and x = 2", 0, "{}")]
        [InlineData("x + y = 3 and x - y = 1", 1, "{2}")]
        [InlineData("x + y = 3 and x - y = 1", 0, "{}")]
        [InlineData("x^2 = 4 and x = y", -2, "{-2}")]
        [InlineData("x^2 = 4 and x = y", 3, "{}")]
        public void ListedSetsWithAParameterMeetWhereTheyAgree(string statement, int y, string expected)
        {
            var solved = (Set)statement.ToEntity().Solve("x").Substitute("y", y).Evaled;
            var wanted = (Set)expected.ToEntity().Evaled;
            Assert.Equal(Boolean.True, MathS.Sets.Subset(solved, wanted).Evaled);
            Assert.Equal(Boolean.True, MathS.Sets.Subset(wanted, solved).Evaled);
        }

        /// <summary>
        /// The same for the intersection itself: a member it cannot decide stays intersected
        /// with the other set, beside the members it can.
        /// </summary>
        [Theory]
        [InlineData("{ y } /\\ { 2 }", 2, "{2}")]
        [InlineData("{ y } /\\ { 2 }", 3, "{}")]
        [InlineData("{ y, 3 } /\\ { 3 }", 2, "{3}")]
        [InlineData("{ 1, 2 } /\\ { 2, y }", 1, "{1, 2}")]
        public void AnUndecidedMemberOfAnIntersectionStaysInIt(string intersection, int y, string expected)
        {
            var met = (Set)intersection.ToEntity().Evaled.Substitute("y", y).Evaled;
            var wanted = (Set)expected.ToEntity().Evaled;
            Assert.Equal(Boolean.True, MathS.Sets.Subset(met, wanted).Evaled);
            Assert.Equal(Boolean.True, MathS.Sets.Subset(wanted, met).Evaled);
        }

        /// <summary>The inner statement of injectivity, and injectivity itself, for a linear map; the quadratic is refuted by a witness.</summary>
        [Fact]
        public void InjectivityOfALinearMapIsDecided()
        {
            Assert.Equal(Boolean.True, "forall v in RR : 2 v + 1 = 2 u + 1 implies v = u".ToEntity().Evaled);
            Assert.Equal(Boolean.True, "forall u in RR : forall v in RR : 2 u + 1 = 2 v + 1 implies u = v".ToEntity().Evaled);
            Assert.Equal(Boolean.False, "forall u in RR : forall v in RR : u^2 = v^2 implies u = v".ToEntity().Evaled);
        }
    }
}
