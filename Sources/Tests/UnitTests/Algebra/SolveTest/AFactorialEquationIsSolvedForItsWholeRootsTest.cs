//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation in a factorial and a polynomial has infinitely many roots, two between some
    /// pairs of the poles at the negative integers, so no finite set is its answer. x + x! = x^3
    /// was answered with 13 decimals from Newton's method, short of -3.0200 inside its own box.
    /// Its whole roots are found exactly and the rest left as the equation.
    /// https://github.com/asc-community/AngouriMath/issues/2538
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AFactorialEquationIsSolvedForItsWholeRootsTest
    {
        private static (FiniteSet? Whole, ConditionalSet? Others) Parts(Set answer)
        {
            var nodes = answer.InnerSimplified.Nodes.ToList();
            Assert.DoesNotContain(nodes, node => node is Number.Real and not Number.Rational);
            return (nodes.OfType<FiniteSet>().SingleOrDefault(), nodes.OfType<ConditionalSet>().SingleOrDefault());
        }

        [Theory]
        [InlineData("x + x! = x^3", new[] { 5 })]
        [InlineData("x! = 24", new[] { 4 })]
        [InlineData("x! = x^2 + 5", new int[0])]
        [InlineData("gamma(x) = 1", new[] { 1, 2 })]
        [InlineData("2 * (x + 2)! = 12 x + 24", new[] { 2 })]
        public void TheWholeRootsAndTheEquationForTheRest(string equation, int[] whole)
        {
            var answer = (Set)equation.ToEntity().Solve("x");
            var (found, rest) = Parts(answer);
            Assert.NotNull(rest);
            var roots = found?.Select(root => root.InnerSimplified).ToList() ?? new();
            Assert.Equal(whole.Length, roots.Count);
            foreach (var root in whole)
                Assert.Contains((Entity)root, roots);
        }

        [Fact]
        public void AFactorialIsNeverZero()
            => Assert.Equal(Empty, ((Set)"x! = 0".ToEntity().Solve("x")).InnerSimplified);
    }
}
