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

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A whole number of periods taken out of a trigonometric function's argument, where the
    /// quantifiers around it establish that the multiple is whole: <c>sin(x + 2 pi n)</c> is
    /// <c>sin(x)</c> for every whole <c>n</c>.
    /// <see href="https://github.com/asc-community/AngouriMath/issues/212"/>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class PeriodByFactsTest
    {
        [Theory]
        [InlineData("forall n in ZZ : sin(x + 2 pi n) = sin(x)")]
        [InlineData("forall n in ZZ : cos(x - 4 pi n) = cos(x)")]
        [InlineData("forall n in ZZ : cos(2 pi n) = 1")]
        [InlineData("forall n in ZZ : sin(pi/6 + 2 pi n) - 1/2 = 0")]
        [InlineData("forall n in ZZ : tan(pi/4 + pi n) = 1")]
        [InlineData("forall n in ZZ : sec(2 pi n) = 1")]
        [InlineData("forall n in ZZ : forall m in ZZ : sin(x + 2 pi n - 2 pi m) = sin(x)")]
        public void DecidedByWhatTheQuantifiersEstablish(string statement)
            => Assert.Equal(Boolean.True, statement.ToEntity().Simplify());

        [Theory]
        // pi is half a period of the sine: false at n = 1 for x = pi/2.
        [InlineData("forall n in ZZ : sin(x + pi n) = sin(x)")]
        // n is not whole, so 2 pi n is not a period: false at n = 1/4.
        [InlineData("forall n in RR : sin(2 pi n) = 0")]
        // pi/2 is half a period of the tangent: undefined at n = 1.
        [InlineData("forall n in ZZ : tan(pi/4 + pi n / 2) = 1")]
        public void NotClaimedWhereTheFactsDoNotHold(string statement)
            => Assert.NotEqual(Boolean.True, statement.ToEntity().Simplify());

        [Fact]
        public void OutsideAQuantifierNothingIsAssumed()
            => Assert.Contains(MathS.Var("n"), "sin(x + 2 pi n)".ToEntity().Simplify().Vars);
    }
}
