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
    /// A cube shifted, <c>a(x + h)^3 = k</c>, has <c>D0 = b^2 - 3ac = 0</c>, and Cardano's
    /// <c>C^3 = (D1 + sqrt(D1^2 - 4 D0^3)) / 2</c> is then <c>(D1 + |D1|) / 2</c>, which is 0 for a
    /// negative <c>D1</c>. Every root was taken for <c>-b/(3a)</c>, none was one, and
    /// <c>(x + 1)^3 = 2</c> was answered <c>{ }</c>. Found by a cross-check of equations in
    /// logarithms, one of which exponentiates to <c>3x^3 + 9x^2 + 9x + 1</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AShiftedCubeTest
    {
        [Theory]
        [InlineData("(x + 1)^3 = 2", "-1", "2")]
        [InlineData("x^3 + 3x^2 + 3x - 1 = 0", "-1", "2")]
        [InlineData("3x^3 + 9x^2 + 9x + 1 = 0", "-1", "2/3")]
        [InlineData("x^3 - 3x^2 + 3x - 3 = 0", "1", "2")]
        // D1 positive, which was answered: (x + 1)^3 = -2.
        [InlineData("x^3 + 3x^2 + 3x + 3 = 0", "-1", "-2")]
        public void ItsRootsAreTheCubeRootsShifted(string equation, string shift, string cube)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(3, answer.Count);
            foreach (var unity in new[] { "1", "-1/2 + sqrt(3) / 2 * i", "-1/2 - sqrt(3) / 2 * i" })
            {
                // The real cube root of the cube, times each cube root of unity, shifted.
                var root = $"({shift}) + ({unity}) * sgn({cube}) * abs({cube})^(1/3)".ToEntity();
                Assert.Contains(answer, found => Number.IsZero((found - root).EvalNumerical()));
            }
        }

        [Fact]
        public void ATripleRootIsStillOne()
            => Assert.Equal("{ -1 }".ToEntity(), "x^3 + 3x^2 + 3x + 1 = 0".ToEntity().Solve("x"));
    }
}
