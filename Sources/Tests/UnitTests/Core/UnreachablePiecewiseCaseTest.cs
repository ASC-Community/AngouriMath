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

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A piecewise takes its first matching case, so a case whose predicate entails an earlier
    /// one is unreachable and is dropped. https://github.com/asc-community/AngouriMath/issues/1212
    /// </summary>
    /// <remarks>
    /// The count assertions below are the point of the change, but the value assertions are what
    /// make it safe: dropping a case that was reachable would shorten the answer and change it,
    /// and a test that only counted cases could not tell the two apart.
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class UnreachablePiecewiseCaseTest
    {
        private static int CaseCount(Entity piecewise)
            => Assert.IsType<Piecewise>(piecewise).Cases.Count();

        // Distributing a binder over a piecewise gives one case per subset of the conditions.
        // Most are unreachable: `2 < a` cannot be reached past `1 < a`.
        [Fact]
        public void ASummedPiecewiseKeepsOnlyItsReachableCases()
        {
            var summed = "sum(piecewise(k provided k < a, 0), k, 0, 5)".ToEntity().Simplify();
            Assert.Equal(6, CaseCount(summed));

            // And it answers what the sum actually is: the k below a, added up.
            foreach (var (a, expected) in new[]
                { (0.5, 0), (1.5, 1), (2.5, 3), (3.5, 6), (4.5, 10), (5.5, 15), (6.5, 15) })
                Assert.Equal(
                    Number.Integer.Create(expected),
                    summed.Substitute("a", a.ToString(System.Globalization.CultureInfo.InvariantCulture).ToEntity()).Evaled);
        }

        [Fact]
        public void TheSameOverAShorterRange()
        {
            var summed = "sum(piecewise(k provided k < a, 0), k, 0, 2)".ToEntity().Simplify();
            Assert.Equal(3, CaseCount(summed));
            foreach (var (a, expected) in new[] { (0.5, 0), (1.5, 1), (2.5, 3), (3.5, 3) })
                Assert.Equal(
                    Number.Integer.Create(expected),
                    summed.Substitute("a", a.ToString(System.Globalization.CultureInfo.InvariantCulture).ToEntity()).Evaled);
        }

        // A tighter bound after a looser one on the same side is unreachable.
        [Theory]
        [InlineData("piecewise(1 provided x > 1, 2 provided x > 2, 3)", 2)]
        [InlineData("piecewise(1 provided x > 1, 2 provided x >= 2, 3)", 2)]
        [InlineData("piecewise(1 provided x >= 1, 2 provided x > 1, 3)", 2)]
        [InlineData("piecewise(1 provided x < 2, 2 provided x < 1, 3)", 2)]
        [InlineData("piecewise(1 provided 1 < x, 2 provided 2 < x, 3)", 2)]
        public void ATighterBoundAfterALooserOneIsDropped(string written, int expected)
            => Assert.Equal(expected, CaseCount(written.ToEntity().Simplify()));

        // The other order is not unreachable and must survive: a looser bound after a tighter
        // one still catches the values between them.
        [Theory]
        [InlineData("piecewise(1 provided x > 2, 2 provided x > 1, 3)", 3)]
        [InlineData("piecewise(1 provided x < 1, 2 provided x < 2, 3)", 3)]
        // `>` after `>=` at the same bound differs exactly at the endpoint, so it is reachable.
        [InlineData("piecewise(1 provided x > 1, 2 provided x >= 1, 3)", 3)]
        // Opposite sides say nothing about each other.
        [InlineData("piecewise(1 provided x > 1, 2 provided x < 5, 3)", 3)]
        public void WhatIsStillReachableIsKept(string written, int expected)
            => Assert.Equal(expected, CaseCount(written.ToEntity().Simplify()));

        // A looser bound after a tighter one keeps the values between them, which is the
        // property the count above is standing in for.
        [Fact]
        public void ALooserBoundAfterATighterOneStillAnswersBetweenThem()
        {
            var piecewise = "piecewise(1 provided x > 2, 2 provided x > 1, 3)".ToEntity().Simplify();
            Assert.Equal(Number.Integer.Create(1), piecewise.Substitute("x", 3).Evaled);
            Assert.Equal(Number.Integer.Create(2), piecewise.Substitute("x", 1.5.ToString(System.Globalization.CultureInfo.InvariantCulture).ToEntity()).Evaled);
            Assert.Equal(Number.Integer.Create(3), piecewise.Substitute("x", 0).Evaled);
        }

        // A conjunction is at least as strong as either half, and a disjunction is at most.
        [Theory]
        [InlineData("piecewise(1 provided x > 1, 2 provided x > 1 and x < 5, 3)", 2)]
        [InlineData("piecewise(1 provided x > 1 or x < 0, 2 provided x > 1, 3)", 2)]
        // But a disjunction after one of its halves is reachable through the other half.
        [InlineData("piecewise(1 provided x > 1, 2 provided x > 1 or x < 0, 3)", 3)]
        public void ConjunctionsAndDisjunctionsAreComparedToo(string written, int expected)
            => Assert.Equal(expected, CaseCount(written.ToEntity().Simplify()));
    }
}
