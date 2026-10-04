//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// The characteristic polynomial of a square matrix, <c>det(x I - A)</c>.
    /// https://github.com/asc-community/AngouriMath/issues/381
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class CharacteristicPolynomialTest
    {
        private static Entity Of(Entity[,] entries) => MathS.Matrix(entries).CharacteristicPolynomial("x")!;

        /// <summary>The textbook one: <c>x^2 - (a + d) x + a d - b c</c>, the trace and the determinant.</summary>
        [Fact]
        public void TwoByTwoIsTheTraceAndTheDeterminant()
            => Assert.Equal(Integer.Zero,
                (Of(new Entity[,] { { "a", "b" }, { "c", "d" } }) - "x^2 - (a + d) * x + a * d - b * c".ToEntity()).Simplify());

        [Theory]
        [InlineData("x ^ 2 - 5 * x - 2")]
        public void ItIsWrittenAsAPolynomialInTheVariable(string written)
        {
            Assert.Equal(written, Of(new Entity[,] { { 1, 2 }, { 3, 4 } }).Stringize());
            Assert.Equal("x ^ 2 - (a + d) * x + d * a - b * c", Of(new Entity[,] { { "a", "b" }, { "c", "d" } }).Stringize());
        }

        /// <summary>
        /// A value everywhere, at the diagonal entries included: there Gaussian elimination's
        /// pivots, <c>x - 1</c> and the like, vanish, and a polynomial computed through them had
        /// no value.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        public void ItHasAValueAtEveryPoint(int at)
            => Assert.Equal(Integer.Create(-6), Of(new Entity[,] { { 1, 2 }, { 3, 4 } }).Substitute("x", at).Evaled);

        /// <summary>
        /// A triangular matrix's eigenvalues are its diagonal entries, so they are the roots, and
        /// the constant term is <c>(-1)^n det(A)</c>: here <c>-(2 · 3 · -1)</c>, which is <c>6</c>.
        /// </summary>
        [Fact]
        public void ATriangularMatrixHasItsDiagonalForRoots()
        {
            var polynomial = Of(new Entity[,] { { 2, 5, 7 }, { 0, 3, 1 }, { 0, 0, -1 } });
            foreach (var root in new[] { 2, 3, -1 })
                Assert.Equal(Integer.Zero, polynomial.Substitute("x", root).Evaled);
            Assert.Equal(Integer.Create(6), polynomial.Substitute("x", 0).Evaled);
        }

        [Fact]
        public void OnlyForASquareMatrixWithoutTheVariable()
        {
            Assert.Null(MathS.Matrix(new Entity[,] { { 1, 2, 3 }, { 4, 5, 6 } }).CharacteristicPolynomial("x"));
            Assert.Null(MathS.Matrix(new Entity[,] { { "x", 1 }, { 0, 1 } }).CharacteristicPolynomial("x"));
        }
    }
}
