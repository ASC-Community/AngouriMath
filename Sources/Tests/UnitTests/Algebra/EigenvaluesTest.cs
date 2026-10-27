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
    /// <see cref="Matrix.Eigenvalues"/>: the roots of the characteristic polynomial, which divides by nothing,
    /// so no pivot leaves a condition on them. Each one is checked by making <c>A - lambda I</c> singular.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1676">#1676</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class EigenvaluesTest
    {
        [Fact]
        public void OfASymmetricMatrixAreItsRealRoots()
            => Assert.Equal(new FiniteSet(1, 2, 11), MathS.Matrix(new Entity[,] { { 2, 0, 0 }, { 0, 3, 4 }, { 0, 4, 9 } }).Eigenvalues);

        [Fact]
        public void ARepeatedRootIsOneMember()
            => Assert.Equal(new FiniteSet(1), MathS.Matrix(new Entity[,] { { 1, 1 }, { 0, 1 } }).Eigenvalues);

        [Fact]
        public void OfASymbolicMatrixCarryNoPivotCondition()
        {
            var eigenvalues = MathS.Matrix(new Entity[,] { { 0, "J" }, { "J", 0 } }).Eigenvalues;
            // lambda^2 = J^2 is rooted exactly, so these are J and -J rather than the square under
            // a root. https://github.com/asc-community/AngouriMath/issues/2140
            Assert.Equal(new FiniteSet("-J", "J"), eigenvalues);
            Assert.DoesNotContain("provided", eigenvalues!.Stringize());
        }

        [Fact]
        public void OfANonSquareMatrixAreNull()
            => Assert.Null(MathS.Matrix(new Entity[,] { { 1, 2, 3 }, { 4, 5, 6 } }).Eigenvalues);

        [Theory]
        [InlineData(1, 2, 3, 4)]
        [InlineData(2, -1, 5, 7)]
        [InlineData(0, 1, -1, 0)]
        public void EachMakesTheShiftedMatrixSingular(int a, int b, int c, int d)
        {
            var matrix = MathS.Matrix(new Entity[,] { { a, b }, { c, d } });
            var eigenvalues = Assert.IsType<FiniteSet>(matrix.Eigenvalues);
            Assert.NotEmpty(eigenvalues);
            foreach (var lambda in eigenvalues)
            {
                var shifted = MathS.Matrix(new Entity[,] { { a - lambda, b }, { c, d - lambda } });
                Assert.True(shifted.Determinant!.EvalNumerical().Abs().EDecimal.ToDouble() < 1e-12,
                    $"det(A - {lambda} I) is {shifted.Determinant.EvalNumerical()}");
            }
        }
    }
}
