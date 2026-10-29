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
    /// The principal n-th root of an n-th power, read with what the quantifiers establish: <c>(z^n)^(1/n)</c>
    /// is <c>z</c> where <c>z</c> is <c>0</c> or in the sector <c>-pi/n &lt; arg(z) &lt;= pi/n</c>, which is how
    /// the power's inversion states where its solution holds, or where <c>z &gt;= 0</c>.
    /// <see href="https://github.com/asc-community/AngouriMath/issues/212"/>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class RootByFactsTest
    {
        [Theory]
        [InlineData("forall t in CC : (t = 0 or -1/2 * pi < arg(t) and arg(t) <= 1/2 * pi) implies sqrt(t^2) = t")]
        [InlineData("forall t in CC : (arg(t) <= pi/2 and -pi/2 < arg(t) or t = 0) implies sqrt(t^2) = t")]
        [InlineData("forall t in CC : (t = 0 or -1/3 * pi < arg(t) and arg(t) <= 1/3 * pi) implies (t^3)^(1/3) = t")]
        [InlineData("forall t in RR : t >= 0 implies sqrt(t^2) = t")]
        public void DecidedByWhatTheQuantifiersEstablish(string statement)
            => Assert.Equal(Boolean.True, statement.ToEntity().Simplify());

        [Theory]
        // False at t = -1.
        [InlineData("forall t in RR : sqrt(t^2) = t")]
        // The sector of the square root is half of this: false at t = -1.
        [InlineData("forall t in CC : (-pi < arg(t) and arg(t) <= pi) implies sqrt(t^2) = t")]
        // The square root's sector, not the cube root's: false at t = i, where (i^3)^(1/3) is e^(-i pi/6).
        [InlineData("forall t in CC : (t = 0 or -1/2 * pi < arg(t) and arg(t) <= 1/2 * pi) implies (t^3)^(1/3) = t")]
        public void NotClaimedWhereTheFactsDoNotHold(string statement)
            => Assert.NotEqual(Boolean.True, statement.ToEntity().Simplify());

        [Fact]
        public void OutsideAQuantifierNothingIsAssumed()
            => Assert.NotEqual(MathS.Var("t"), "sqrt(t^2)".ToEntity().Simplify());
    }
}
