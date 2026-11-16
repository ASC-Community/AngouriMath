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
    /// A radical is raised away only where nothing else in the equation holds one. In
    /// <c>sqrt(x) + x = cos(sqrt(x)) - 1</c> the rest has <c>sqrt(x)</c> inside the cosine, so squaring cleared
    /// nothing, Newton's method answered the square with a point that is no root, and the empty set left after it was
    /// dropped claimed the equation has none, where <c>x = 0</c> is one.
    /// https://github.com/asc-community/AngouriMath/issues/2495
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARadicalTheRestStillHoldsTest
    {
        [Fact]
        public void IsNotAnsweredWithNoRoots()
        {
            var answer = "sqrt(x) + x = cos(sqrt(x)) - 1".ToEntity().Solve("x");
            Assert.False(answer is Entity.Set.FiniteSet { Count: 0 }, $"{answer} claims no roots, and 0 is one");
            Assert.IsType<Entity.Set.ConditionalSet>(answer);
        }

        [Fact]
        public void ZeroIsARootOfIt()
            => Assert.Equal(0, "sqrt(x) + x - (cos(sqrt(x)) - 1)".ToEntity().Substitute("x", 0).EvalNumerical());

        // The same radical inside a function, where another route answers: both roots, 0 among them. The other is
        // found by Newton's method in doubles, 1.7e-17 from the root, so it is held to 1e-12.
        [Fact]
        public void AnotherRouteStillAnswersItsNeighbour()
        {
            var roots = Assert.IsType<Entity.Set.FiniteSet>("x + sqrt(x) = e^(sqrt(x)) - 1".ToEntity().Solve("x"));
            Assert.Equal(2, roots.Count);
            Assert.Contains((Entity)0, roots);
            foreach (var root in roots)
                Assert.True(((Entity.Number.Complex)"x + sqrt(x) - (e^(sqrt(x)) - 1)".ToEntity().Substitute("x", root).EvalNumerical()).Abs().EDecimal
                    .CompareTo(PeterO.Numbers.EDecimal.FromString("1e-12")) < 0, $"{root} is not a root");
        }
    }
}
