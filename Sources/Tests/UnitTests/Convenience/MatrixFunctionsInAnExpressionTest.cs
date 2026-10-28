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
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>det</c>, <c>transpose</c>, <c>dot</c>, <c>cross</c> and <c>resultant</c> written in an
    /// expression. Each existed in C#, as <c>Matrix.Determinant</c>, <c>Matrix.T</c> and
    /// <c>MathS.Polynomials.Resultant</c>, or not at all, and none could be written: <c>det(M)</c> was
    /// refused, and the rest were read as products. Modenov's <i>Problems in Geometry</i> computes
    /// areas, collinearity and elimination with them.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2147">#2147</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class MatrixFunctionsInAnExpressionTest
    {
        [Theory]
        [InlineData("det([[1, 2], [3, 4]])", "-2")]
        [InlineData("det([[a, b], [c, d]])", "a * d - b * c")]
        [InlineData("det(2 * [[1, 2], [3, 4]])", "-8")]
        [InlineData("det([[5]])", "5")]
        // The oriented area of the triangle (0, 0), (1, 0), (0, 1), as Modenov writes it.
        [InlineData("det([[0, 0, 1], [1, 0, 1], [0, 1, 1]]) / 2", "1/2")]
        [InlineData("dot([1, 2, 3], [4, 5, 6])", "32")]
        [InlineData("dot([a, b], [c, d])", "a * c + b * d")]
        [InlineData("dot([1, 2]T, [3, 4])", "11")]
        // Not conjugated: dot([i], [i]) is i * i.
        [InlineData("dot([i], [i])", "-1")]
        // The plane's pseudo-scalar product, which the book writes (a, b).
        [InlineData("dot(cross([a_1, a_2]), [b_1, b_2])", "a_1 * b_2 - a_2 * b_1")]
        [InlineData("resultant(x^2 - 1, x - a, x)", "a ^ 2 - 1")]
        [InlineData("resultant(x^2 + y^2 - 1, x + y - 1, y)", "2 * x ^ 2 - 2 * x")]
        public void TheValueIsComputedFromTheEntries(string written, string expected)
            => Assert.Equal(Integer.Zero, (written.ToEntity() - expected.ToEntity()).Simplify());

        [Theory]
        [InlineData("transpose([1, 2])", "[1, 2]T")]
        [InlineData("transpose([[1, 2], [3, 4]])", "[[1, 3], [2, 4]]")]
        [InlineData("transpose([1, 2]T)", "[1, 2]")]
        [InlineData("cross([1, 0, 0], [0, 1, 0])", "[0, 0, 1]")]
        [InlineData("cross([a, b])", "[-b, a]")]
        public void AMatrixComesBackAsAMatrix(string written, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), written.ToEntity().Simplify());

        /// <summary>
        /// The vector product is perpendicular to both of its factors.
        /// </summary>
        [Fact]
        public void TheVectorProductIsPerpendicularToBothFactors()
        {
            Assert.Equal(Integer.Zero, "dot(cross([a, b, c], [d, e_1, f]), [a, b, c])".ToEntity().Simplify());
            Assert.Equal(Integer.Zero, "dot(cross([a, b, c], [d, e_1, f]), [d, e_1, f])".ToEntity().Simplify());
        }

        /// <summary>
        /// There is no node for any of these, so a symbol, a matrix of the wrong shape, or vectors
        /// of different lengths have nothing to be, and are refused at parsing rather than read as
        /// something else.
        /// </summary>
        [Theory]
        [InlineData("det(A)")]
        [InlineData("det([[1, 2, 3], [4, 5, 6]])")]
        [InlineData("transpose(x)")]
        [InlineData("dot([1, 2], [1, 2, 3])")]
        [InlineData("dot([[1, 2], [3, 4]], [1, 2])")]
        [InlineData("cross([1, 2], [3, 4])")]
        [InlineData("cross([1, 2, 3])")]
        [InlineData("resultant(x^2 - 1, x - 1, 2)")]
        [InlineData("resultant(sin(x), x - 1, x)")]
        public void WhatIsNotAMatrixOfTheRightShapeIsRefused(string written)
            => Assert.Throws<InvalidArgumentParseException>(() => written.ToEntity());

        [Theory]
        [InlineData("det([[1, 2], [3, 4]], [[1, 2], [3, 4]])")]
        [InlineData("dot([1, 2])")]
        [InlineData("cross([1, 2, 3], [1, 2, 3], [1, 2, 3])")]
        [InlineData("resultant(x, x)")]
        public void TheWrongNumberOfArgumentsIsRefused(string written)
            => Assert.Throws<FunctionArgumentCountException>(() => written.ToEntity());
    }
}
