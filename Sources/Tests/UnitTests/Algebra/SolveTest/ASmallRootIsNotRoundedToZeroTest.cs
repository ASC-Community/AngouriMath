//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// Each root is offered as a simple rational guessed under a tolerance of 1e-7, and the guess is
    /// kept where the equation holds at it. A root below 1e-7 was guessed to be 0, and where 0 is
    /// another root the equation holds there, so the two became one: <c>x^2 - 10^-8 x = 0</c> was
    /// <c>{ 0 }</c>, from 10^-8 down, while <c>x (x - 10^-8) = 0</c> had both. Found by solving
    /// generated equations with <c>x</c> written as <c>10^12 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ASmallRootIsNotRoundedToZeroTest
    {
        [Theory]
        [InlineData("x^2 - 10^(-8) * x = 0", "0", "1/100000000")]
        [InlineData("x^2 - 10^(-12) * x = 0", "0", "1/1000000000000")]
        [InlineData("x^3 - 10^(-12) * x^2 = 0", "0", "1/1000000000000")]
        [InlineData("6 * 10^24 * x^2 - 4 * 10^12 * x = 0", "0", "1/1500000000000")]
        [InlineData("sqrt(4 * 10^12 * x - 2 * 10^24 * x^2) = 2 * 10^12 * x", "0", "1/1500000000000")]
        [InlineData("x^2 - 10^(-3) * x = 0", "0", "1/1000")]
        public void BothRootsAreThere(string equation, params string[] roots)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(roots.Length, answer.Count);
            foreach (var root in roots)
                Assert.Contains(answer, found => (found - root.ToEntity()).Evaled == Number.Integer.Zero);
        }
    }
}
