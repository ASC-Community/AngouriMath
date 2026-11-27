//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// The root of <c>a x + b = 0</c> was written <c>-b / a</c>, so where <c>a</c> was written with a minus sign the
    /// root carried two: <c>p V = n R T</c> solved for <c>T</c> was <c>{ -p * V / (-n * R) }</c>. The sign is taken off
    /// the denominator, which leaves the value as it was.
    /// https://github.com/asc-community/AngouriMath/issues/2566
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ALinearRootIsWrittenWithOneSignTest
    {
        [Theory]
        [InlineData("p V = n R T", "T", "{ p * V / (n * R) }")]
        [InlineData("b = a x", "x", "{ b / a }")]
        [InlineData("a x = b", "x", "{ b / a }")]
        [InlineData("a x + b = 0", "x", "{ -b / a }")]
        [InlineData("c - 2 x = 0", "x", "{ c / 2 }")]
        [InlineData("1/R = 1/a + 1/b", "R", "{ 1 / (1 / a + 1 / b) }")]
        public void TheRootHasOneSign(string equation, string variable, string expected)
            => Assert.Equal(expected, equation.Solve(variable).Stringize());

        [Theory]
        [InlineData("p V = n R T", "T", "p V / (n R)")]
        [InlineData("E = m v^2 / 2", "v", "sqrt(2 E / m)")]
        [InlineData("3 - 4 y x = 0", "x", "3 / (4 y)")]
        public void TheRootIsARoot(string equation, string variable, string root)
        {
            // Compared at a point, since Simplify need not prove sqrt(E / (m * 1/2)) and sqrt(2 E / m) one.
            static System.Numerics.Complex At(Entity e)
            {
                foreach (var (name, value) in new[] { ("p", 2), ("V", 3), ("n", 5), ("R", 7), ("E", 11), ("m", 13), ("y", 17) })
                    e = e.Substitute(name, value);
                return e.EvalNumerical().ToNumerics();
            }
            var roots = Assert.IsType<Entity.Set.FiniteSet>(equation.Solve(variable));
            var expected = At(root.ToEntity());
            Assert.Contains(roots, r => System.Numerics.Complex.Abs(At(r) - expected) < 1e-12);
        }
    }
}
