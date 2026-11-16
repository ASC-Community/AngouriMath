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
    /// <c>Solve</c> of <c>f(x) in S</c>: the members of a listed <c>S</c> each as an equation,
    /// an interval as its bounds. It answered the <b>empty set</b> for every one of these, and
    /// for every statement it had no arm for -- a claim that no <c>x</c> exists, made about
    /// <c>x^2 in (0; 1)</c>. A statement the solver cannot read is the set of <c>x</c> with the
    /// property now, left as written. Sullivan and Mackey's pre-images
    /// (Ex 7.3.10, §7.3.5 Try 1) are this.
    /// <see href="https://github.com/asc-community/AngouriMath/issues/1409"/>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class MembershipSolvingTest
    {
        private static Set Solved(string statement) => statement.ToEntity().Solve("x");

        /// <summary>Ex 7.3.10: the pre-images of <c>x^2</c>; the book prints <c>(-1, 1)</c> for the last, which is wrong at <c>0</c>.</summary>
        [Theory]
        [InlineData("x^2 in {1}", "{-1, 1}")]
        [InlineData("x^2 in {1, 4}", "{-2, -1, 1, 2}")]
        [InlineData("x^2 in (-oo; 0)", "{}")]
        [InlineData("x^2 in [0; +oo)", "RR")]
        [InlineData("x^2 in (0; 1)", "(-1; 0) \\/ (0; 1)")]
        [InlineData("x + 1 in [2; 3]", "[1; 2]")]
        [InlineData("2 x in (0; 4]", "(0; 2]")]
        public void APreImageIsSolvedThroughTheMembersOrTheBounds(string statement, string expected)
        {
            // Compared at points rather than as printed forms: the solver writes a union of
            // intervals in its own order, and the same set two ways is the same set.
            var solved = Solved(statement);
            var wanted = (Set)expected.ToEntity().Evaled;
            foreach (var at in new[] { -3.0, -2.0, -1.0, -0.5, 0.0, 0.5, 1.0, 1.5, 2.0, 3.0 })
            {
                var point = (Entity)at;
                Assert.True(solved.TryContains(point, out var got), $"{solved} does not decide {at}");
                Assert.True(wanted.TryContains(point, out var want), $"{wanted} does not decide {at}");
                Assert.True(got == want, $"{statement} solved as {solved}: {at} is {(got ? "in" : "out")}, and should be {(want ? "in" : "out")}");
            }
        }

        /// <summary>A statement with no arm is the set of <c>x</c> with the property, not the empty set.</summary>
        [Fact]
        public void WhatIsNotReadIsLeftAsWritten()
        {
            Assert.Equal("{ x : x^2 in ZZ }".ToEntity(), Solved("x^2 in ZZ"));
            Assert.Equal("{ x : x^2 in QQ }".ToEntity(), Solved("x^2 in QQ"));
            // And a quantifier that asked the solver no longer reads that emptiness as a proof:
            // forall x in RR : x^2 in ZZ was True.
            Assert.Equal(Boolean.False, "forall x in RR : x^2 in ZZ".ToEntity().Evaled);
        }

        /// <summary>The trigonometric equation's family is the pre-image of <c>{0}</c> under the sine.</summary>
        [Fact]
        public void AMemberEquationKeepsItsFamily()
        {
            // Written as a union over the whole numbers, which decides membership.
            // https://github.com/asc-community/AngouriMath/issues/2319
            var family = Solved("sin(x) in {0}");
            Assert.Equal(Boolean.True, MathS.pi.In(family).Simplify());
            Assert.NotEqual(Boolean.True, (MathS.pi / 2).In(family).Simplify());
        }
    }
}
