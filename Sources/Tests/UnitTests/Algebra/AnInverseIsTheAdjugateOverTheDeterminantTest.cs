//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// The inverse took its determinant by elimination, which divides by the first pivot and multiplies it back, so
    /// every entry of the inverse of <c>[[a, b], [c, d]]</c> simplified with <c>provided not a = 0</c>, and the rotation's
    /// with <c>cos t ≠ 0</c>, though each inverse exists wherever the determinant is not 0. It is the adjugate over the
    /// determinant now. https://github.com/asc-community/AngouriMath/issues/2346
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnInverseIsTheAdjugateOverTheDeterminantTest
    {
        [Theory]
        [InlineData("[[a, b], [c, d]]")]
        [InlineData("[[cos(t), -sin(t)], [sin(t), cos(t)]]")]
        [InlineData("[[x, 1], [1, 0]]")]
        public void CarriesNoConditionOfAPivot(string matrix)
        {
            var inverse = Assert.IsAssignableFrom<Matrix>(((Matrix)matrix.ToEntity()).Inverse);
            Assert.DoesNotContain("provided", inverse.Simplify().Stringize());
        }

        [Fact]
        public void IsTheAdjugateOverTheDeterminant()
        {
            var inverse = Assert.IsAssignableFrom<Matrix>(((Matrix)"[[a, b], [c, d]]".ToEntity()).Inverse);
            var expected = (Matrix)"[[d, -b], [-c, a]]".ToEntity();
            for (var row = 0; row < 2; row++)
                for (var column = 0; column < 2; column++)
                {
                    // Zero where the inverse exists, a d - b c not 0, and the difference says so.
                    var difference = (inverse[row, column] - expected[row, column] / "a d - b c".ToEntity()).Simplify();
                    while (difference is Providedf(var inner, _))
                        difference = inner;
                    Assert.Equal(Number.Integer.Zero, difference);
                }
        }

        [Fact]
        public void TheRotationsInverseHoldsWhereTheCosineIsZero()
        {
            var inverse = Assert.IsAssignableFrom<Matrix>(((Matrix)"[[cos(t), -sin(t)], [sin(t), cos(t)]]".ToEntity()).Inverse);
            Assert.Equal("[[0, 1], [-1, 0]]".ToEntity(), inverse.Substitute("t", MathS.pi / 2).Evaled);
        }

        [Fact]
        public void ANumericInverseIsAsItWas()
            => Assert.Equal("[[-2, 1], [3/2, -1/2]]".ToEntity(), ((Matrix)"[[1, 2], [3, 4]]".ToEntity()).Inverse);

        [Fact]
        public void ASingularMatrixHasNone()
            => Assert.Null(((Matrix)"[[1, 2], [2, 4]]".ToEntity()).Inverse);
    }
}
