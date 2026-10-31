//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Compilation.IntoLinq;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using GenericTensor.Core;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A scalar over a matrix argument is not compiled, where it was compiled as the scalar over every element: evaluation
    /// takes it as the scalar times the inverse, and the inverse is not compiled, as a negative matrix power is not. A matrix
    /// written into the expression is inverted before it is compiled, and that is unchanged.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2287">#2287</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AScalarOverAMatrixArgumentTest
    {
        [Fact]
        public void ItIsRefused()
            => Assert.Throws<UncompilableNodeException>(() => "x / A".ToEntity()
                .Compile<GenTensor<double, DoubleOperations>, double, GenTensor<double, DoubleOperations>>("A", "x"));

        [Fact]
        public void AMatrixArgumentOverAScalarIsStillCompiled()
        {
            var matrix = GenTensor<double, DoubleOperations>.CreateMatrix(new double[,] { { 1, 2 }, { 3, 4 } });
            var divided = "A / x".ToEntity()
                .Compile<GenTensor<double, DoubleOperations>, double, GenTensor<double, DoubleOperations>>("A", "x")(matrix, 2);
            Assert.Equal(new[] { 0.5, 1, 1.5, 2 }, new[] { divided[0, 0], divided[0, 1], divided[1, 0], divided[1, 1] });
        }

        [Fact]
        public void AWrittenMatrixIsInvertedAsBefore()
        {
            var inverse = "x / [[1, 0], [2, 4]]".ToEntity().Compile<double, GenTensor<double, DoubleOperations>>("x")(1);
            Assert.Equal(new[] { 1, 0, -0.5, 0.25 }, new[] { inverse[0, 0], inverse[0, 1], inverse[1, 0], inverse[1, 1] });
        }
    }
}
