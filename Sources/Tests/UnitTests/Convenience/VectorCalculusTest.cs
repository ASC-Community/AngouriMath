//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>laplacian</c>, <c>jacobian</c> and <c>hessian</c>, taken in the variables listed and made
    /// of derivatives when they are parsed. Each was refused by name.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class VectorCalculusTest
    {
        private static void AssertSameValue(Entity expected, Entity actual)
            => Assert.Equal(Number.Integer.Zero, (actual - expected).Simplify());

        private static void AssertSameMatrix(string expected, string actual)
        {
            var want = Assert.IsType<Matrix>(expected.ToEntity());
            var got = Assert.IsType<Matrix>(actual.ToEntity());
            Assert.Equal((want.RowCount, want.ColumnCount), (got.RowCount, got.ColumnCount));
            for (var i = 0; i < want.RowCount; i++)
                for (var j = 0; j < want.ColumnCount; j++)
                    AssertSameValue(want[i, j], got[i, j]);
        }

        [Theory]
        [InlineData("laplacian(x^2 y + y^3, [x, y])", "8 y")]
        [InlineData("laplacian(x^2 - y^2, [x, y])", "0")]
        [InlineData("laplacian(x^2 + y^2 + z^2, [x, y, z])", "6")]
        [InlineData("laplacian(sin(x), x)", "-sin(x)")]
        [InlineData("laplacian(e^x sin(y), [x, y])", "0")]
        public void TheLaplacianIsTheSumOfTheSecondDerivatives(string written, string expected)
            => AssertSameValue(expected.ToEntity(), written.ToEntity());

        /// <summary>
        /// 1/r is harmonic in space away from the origin. The derivatives are taken before a point
        /// is substituted, since substituting for the variable of a derivative leaves it as it is.
        /// </summary>
        [Fact]
        public void TheReciprocalOfTheDistanceIsHarmonic()
        {
            var laplacian = "laplacian(1 / sqrt(x^2 + y^2 + z^2), [x, y, z])".ToEntity().InnerSimplified;
            var at = laplacian.Substitute("x", 1).Substitute("y", 2).Substitute("z", 3).Evaled;
            Assert.True(at is Number.Complex value && Number.Abs(value).EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-20")) < 0, at.Stringize());
        }

        [Theory]
        [InlineData("jacobian([x y, x + y], [x, y])", "[[y, x], [1, 1]]")]
        [InlineData("jacobian([x, y^2, x z], [x, y, z])", "[[1, 0, 0], [0, 2 y, 0], [z, 0, x]]")]
        [InlineData("jacobian(x^2 y, [x, y])", "[[2 x y, x^2]]")]
        [InlineData("jacobian([sin(t), cos(t)], t)", "[[cos(t)], [-sin(t)]]")]
        public void TheJacobianHasARowForEachComponentAndAColumnForEachVariable(string written, string expected)
            => AssertSameMatrix(expected, written);

        /// <summary>The polar map's Jacobian determinant is <c>r</c>.</summary>
        [Fact]
        public void ThePolarMapsJacobianDeterminantIsR()
            => AssertSameValue("r".ToEntity(), "det(jacobian([r cos(t), r sin(t)], [r, t]))".ToEntity());

        [Theory]
        [InlineData("hessian(x^3 y^2, [x, y])", "[[6 x y^2, 6 x^2 y], [6 x^2 y, 2 x^3]]")]
        [InlineData("hessian(x y z, [x, y, z])", "[[0, z, y], [z, 0, x], [y, x, 0]]")]
        [InlineData("hessian(x^4, x)", "[[12 x^2]]")]
        public void TheHessianIsTheMatrixOfTheSecondDerivatives(string written, string expected)
            => AssertSameMatrix(expected, written);

        /// <summary>The entry in row i and column j is the derivative in x_i of the derivative in x_j.</summary>
        [Fact]
        public void TheHessiansEntriesAreTakenInTheOrderTheyAreWritten()
        {
            var hessian = Assert.IsType<Matrix>("hessian(f, [x, y])".ToEntity());
            Assert.Equal(MathS.Derivative(MathS.Derivative("f", "y"), "x"), hessian[0, 1]);
            Assert.Equal(MathS.Derivative(MathS.Derivative("f", "x"), "y"), hessian[1, 0]);
            Assert.Equal(MathS.Derivative("f", "x", 2), hessian[0, 0]);
        }

        /// <summary>
        /// The variables are a vector of distinct names, or one name; a set has no order to give the
        /// columns. A Laplacian and a Hessian are taken of a scalar function.
        /// </summary>
        [Theory]
        [InlineData("jacobian(x y, {x, y})")]
        [InlineData("jacobian(x y, [x, 2])")]
        [InlineData("hessian(x y, [x, x])")]
        [InlineData("laplacian(x, pi)")]
        [InlineData("laplacian([x, y], [x, y])")]
        [InlineData("hessian([x, y], [x, y])")]
        [InlineData("jacobian([[x, y], [y, x]], [x, y])")]
        public void WhatIsNotAnOperatorsArgumentIsRefused(string written)
            => Assert.Throws<InvalidArgumentParseException>(() => written.ToEntity());

        [Theory]
        [InlineData("laplacian(x)")]
        [InlineData("jacobian(x, [x], [y])")]
        public void EachTakesAFunctionAndItsVariables(string written)
            => Assert.Throws<FunctionArgumentCountException>(() => written.ToEntity());
    }
}
