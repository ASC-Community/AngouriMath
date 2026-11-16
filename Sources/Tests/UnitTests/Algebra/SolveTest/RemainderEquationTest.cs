//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation in a remainder, <c>f mod a = r</c>, solved as the congruence it is:
    /// <c>f = r + a n</c> for every whole <c>n</c>, where <c>r</c> is a value the floored remainder
    /// takes, and no solution where it is not. Algebrite's issue 87, from #180.
    /// https://github.com/asc-community/AngouriMath/issues/1629
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class RemainderEquationTest
    {
        /// <summary>Every member of the family satisfies the equation, at several values of its parameter.</summary>
        [Theory]
        [InlineData("(x mod 4) + 1 = 3")]
        [InlineData("x mod 4 = 0")]
        [InlineData("x mod (-4) = -1")]
        [InlineData("(2x + 1) mod 4 = 3")]
        [InlineData("x mod 2 = 1/2")]
        public void EverySolutionSatisfiesIt(string equation)
        {
            var solutions = equation.ToEntity().Solve("x").Members(-2, -1, 0, 1, 2);
            Assert.NotEmpty(solutions);
            foreach (var solution in solutions)
                Assert.Equal(Boolean.True, equation.ToEntity().Substitute("x", solution).Evaled);
        }

        /// <summary>
        /// And the family is all of them: <c>x mod 4 = 2</c> holds exactly when <c>x - 2</c> is a
        /// multiple of 4, so at <c>n</c> from -2 to 2 the family is -6, -2, 2, 6 and 10.
        /// </summary>
        [Fact]
        public void TheFamilyIsEverySolution()
        {
            var family = Assert.IsType<Set.IndexedUnionf>("(x mod 4) + 1 = 3".ToEntity().Solve("x"));
            Assert.Equal(MathS.Sets.Z, family.Over);
            Assert.Equal(new Entity[] { -6, -2, 2, 6, 10 },
                family.Members(-2, -1, 0, 1, 2).Select(member => member.Evaled).ToArray());
        }

        /// <summary>
        /// A value solves it exactly when the family reaches it at a whole parameter: for
        /// <c>x mod 3 = 1</c>, 4 is reached at <c>n = 1</c>, and 3 only at <c>n = 2/3</c>. The
        /// family is a union over the whole numbers, so membership in it says yes for the first;
        /// for the second it is not decided, and must not be claimed.
        /// </summary>
        [Theory]
        [InlineData("4", true)]
        [InlineData("3", false)]
        public void AValueSolvesItWhenTheFamilyReachesItAtAWholeParameter(string value, bool solves)
        {
            var member = value.ToEntity().In("x mod 3 = 1".ToEntity().Solve("x")).Simplify();
            if (solves)
                Assert.Equal(Boolean.True, member);
            else
                Assert.NotEqual(Boolean.True, member);
        }

        /// <summary>
        /// A value the remainder never takes has no solution: modulo 4 it lies in [0, 4), modulo
        /// -4 in (-4, 0].
        /// </summary>
        [Theory]
        [InlineData("x mod 4 = 5")]
        [InlineData("x mod 4 = -1")]
        [InlineData("x mod (-4) = 1")]
        public void AValueTheRemainderNeverTakesHasNoSolution(string equation)
            => Assert.Empty(Assert.IsType<Set.FiniteSet>(equation.ToEntity().Solve("x")).Elements);

        /// <summary>A divisor or a value that is not a number leaves it unsolved, which is not the same as no solution.</summary>
        [Theory]
        [InlineData("x mod a = 1")]
        [InlineData("x mod 4 = y")]
        public void WhatItCannotReadIsLeftUnsolved(string equation)
            => Assert.IsType<Set.ConditionalSet>(equation.ToEntity().Solve("x"));
    }
}
