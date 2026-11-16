//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A radical's derivative is infinite where its radicand is zero, and Newton's method does not converge to a root
    /// there. An equation answered by it has each point where a radicand is zero put back in, and kept where the
    /// equation is exactly zero: <c>sin(sqrt(x)) = x</c> was answered without its root 0.
    /// https://github.com/asc-community/AngouriMath/issues/2495
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARootWhereARadicandIsZeroTest
    {
        [Theory]
        [InlineData("sin(sqrt(x)) = x", "0.7686488567609494")]
        [InlineData("cos(1 - sqrt(x)) - x = cos(1)", "0.3900311302162786")]
        public void HasTheRootAtZeroBesideTheOneNewtonFinds(string equation, string other)
        {
            var roots = Assert.IsType<Entity.Set.FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Contains((Entity)0, roots);
            Assert.Equal(2, roots.Count);
            Assert.Contains(roots, root => ((Entity.Number.Complex)(root - other.ToEntity()).EvalNumerical()).Abs().EDecimal
                .CompareTo(PeterO.Numbers.EDecimal.FromString("1e-12")) < 0);
        }

        // A point where a radicand is zero and the equation is not stays out: sqrt(x) = sin(x) + 1 is 1 at 0.
        [Fact]
        public void AZeroOfARadicandThatIsNoRootStaysOut()
        {
            var answer = "sqrt(x) = sin(x) + 1".ToEntity().Solve("x");
            if (answer is Entity.Set.FiniteSet roots)
                Assert.DoesNotContain((Entity)0, roots);
        }

        // An answer the analytical routes give is not touched: sqrt(x) = x is { 0, 1 } as it was.
        [Fact]
        public void AnAnalyticalAnswerIsUnchanged()
            => Assert.Equal(new Entity.Set.FiniteSet((Entity)0, (Entity)1), "sqrt(x) = x".ToEntity().Solve("x"));
    }
}
