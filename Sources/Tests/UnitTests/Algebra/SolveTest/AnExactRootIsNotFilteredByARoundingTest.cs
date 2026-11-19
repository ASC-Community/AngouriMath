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
    /// A root found under a condition is kept where the condition holds there, and an equality of
    /// two numbers is decided under a tolerance of 1e-16. <c>x^0</c> is 1 where <c>x</c> is not 0,
    /// so an exact root below 1e-16 was read as 0 and dropped: <c>x - 10^-17 x^0 = 0</c> was
    /// <c>{ }</c>, which says it has no root. Found by solving generated equations with <c>x</c>
    /// written as <c>10^12 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnExactRootIsNotFilteredByARoundingTest
    {
        [Theory]
        [InlineData("x - 10^(-17) * x^0 = 0", "1/10^17")]
        [InlineData("x^0 * (x - 10^(-20)) = 0", "1/10^20")]
        [InlineData("x^2 - 10^(-40) * x^0 = 0", "1/10^20", "-1/10^20")]
        [InlineData("x^0 * (x^2 - 10^(-34)) = 0", "1/10^17", "-1/10^17")]
        [InlineData("x - 10^(-16) * x^0 = 0", "1/10^16")]
        public void TheRootIsKept(string equation, params string[] roots)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(roots.Length, answer.Count);
            foreach (var root in roots)
                Assert.Contains(root.ToEntity().Evaled, answer);
        }

        /// <summary>Where the condition fails at the root exactly, the root is still dropped.</summary>
        [Theory]
        [InlineData("x^0 * x = 0")]
        [InlineData("x^0 * x^3 = 0")]
        public void ARootTheConditionExcludesIsNot(string equation)
            => Assert.Equal(0, Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x")).Count);
    }
}
