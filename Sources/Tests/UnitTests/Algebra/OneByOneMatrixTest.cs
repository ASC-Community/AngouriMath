//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A 1x1 matrix has a rank and row echelon forms, where they threw <c>InvalidCastException</c>: a 1x1 matrix simplifies
    /// to its entry, and the simplified matrix was cast back.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2269">#2269</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class OneByOneMatrixTest
    {
        private static Entity.Matrix Of(Entity entry) => MathS.Matrix(new Entity[,] { { entry } });

        [Theory]
        [InlineData("2", 1, "1")]
        [InlineData("1/2", 1, "1")]
        [InlineData("-3", 1, "1")]
        [InlineData("0", 0, "0")]
        public void ItHasARankAndARowEchelonForm(string entry, int rank, string reduced)
        {
            var matrix = Of(entry);
            Assert.Equal(rank, matrix.Rank);
            Assert.Equal(Of(reduced), matrix.ReducedRowEchelonForm);
            Assert.Equal(1, matrix.RowEchelonForm.RowCount);
        }

        [Fact]
        public void APointwiseProductOfTwoIsOne()
            => Assert.Equal(Of(6), MathS.Matrices.PointwiseMultiplication(Of(2), Of(3)));
    }
}
