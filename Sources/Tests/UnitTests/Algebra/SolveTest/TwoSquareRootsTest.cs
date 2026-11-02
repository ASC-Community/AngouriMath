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

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation in two different square roots, <c>m_1 sqrt(A) + m_2 sqrt(B) + P = 0</c>, is squared twice into a
    /// polynomial, and each of that polynomial's roots is put back into the equation. Two different radicals were not
    /// raised at all, and such an equation went to Newton's method, which cannot reach a root where a radicand is 0:
    /// <c>sqrt(-4 - 4x) + sqrt(2 + x) = -3 - 4x</c> was answered without its root -1. No issue was filed; found by a
    /// cross-check of random radical equations against mpmath.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class TwoSquareRootsTest
    {
        private static Entity.Set.FiniteSet Roots(string equation)
            => Assert.IsType<Entity.Set.FiniteSet>(equation.ToEntity().Solve("x"));

        // Each answer is a root, put back into the equation it came from.
        private static void AreRoots(string equation, Entity.Set.FiniteSet roots)
        {
            var (left, right) = equation.ToEntity() is Entity.Equalsf(var l, var r) ? (l, r) : (equation.ToEntity(), (Entity)0);
            foreach (var root in roots)
                Assert.True(((Entity.Number.Complex)(left - right).Substitute("x", root).EvalNumerical()).Abs().EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-30")) < 0,
                    $"{root} is not a root of {equation}");
        }

        [Fact]
        public void ARootWhereARadicandIsZero()
        {
            const string equation = "sqrt(-4 - 4x) + sqrt(2 + x) = -3 - 4x";
            var roots = Roots(equation);
            Assert.Contains((Entity)(-1), roots);
            Assert.Equal(2, roots.Count);
            AreRoots(equation, roots);
        }

        [Theory]
        [InlineData("sqrt(2x + 3) - sqrt(x + 1) = 1", new[] { -1, 3 })]
        [InlineData("sqrt(1 - x) + sqrt(x) = 1 - 2x", new[] { 0 })]
        [InlineData("sqrt(-4 - 4x) + sqrt(2 + x) = 1", new[] { -1 })]
        public void AreTheWholeRoots(string equation, int[] expected)
        {
            var roots = Roots(equation);
            Assert.Equal(expected.Select(root => (Entity)root).ToHashSet(), roots.ToHashSet());
        }

        [Theory]
        [InlineData("sqrt(x + 5) + sqrt(x) = x")]
        [InlineData("2 sqrt(x) + 3 sqrt(x + 1) = x")]
        public void AnIrrationalRootIsExactAndARoot(string equation)
        {
            var roots = Roots(equation);
            Assert.Single(roots);
            AreRoots(equation, roots);
        }

        // Squared, these are x = x - 1 and x + 1 = x, which have no roots, and neither has the equation.
        [Theory]
        [InlineData("sqrt(x) + sqrt(x - 1) = 0")]
        [InlineData("sqrt(x + 1) - sqrt(x) = 0")]
        public void NoRoot(string equation) => Assert.Empty(Roots(equation));
    }
}
