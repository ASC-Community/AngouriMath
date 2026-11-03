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
    /// The row echelon forms asked whether a pivot was the literal 0, and an entry the elimination had made, like
    /// <c>c + -c / a * a</c>, is 0 without being written so: it was chosen and divided by. The reduced form of
    /// <c>[[a, b], [c, d]]</c> simplified to NaN, and the rank of <c>[[a, b], [2 a, 2 b]]</c>, whose second row is
    /// twice the first, was 2. The rank of a symbolic matrix is the rank for general values of its symbols.
    /// https://github.com/asc-community/AngouriMath/issues/2345
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ASymbolicPivotIsDecidedBeforeItIsChosenTest
    {
        [Theory]
        [InlineData("[[a, b], [2a, 2b]]", 1)]
        [InlineData("[[a, b, c], [d, f, g], [a + d, b + f, c + g]]", 2)]
        [InlineData("[[a, b, c], [d, f, g], [2a - d, 2b - f, 2c - g]]", 2)]
        [InlineData("[[x, x^2], [1, x]]", 1)]
        [InlineData("[[sin(t)^2 + cos(t)^2, 1], [1, 1]]", 1)]
        // Independent, to the end.
        [InlineData("[[a, b], [c, d]]", 2)]
        [InlineData("[[x, 1, 0], [1, x, 1], [0, 1, x]]", 3)]
        public void HasItsRank(string matrix, int rank)
            => Assert.Equal(rank, ((Matrix)matrix.ToEntity()).Rank);

        [Theory]
        [InlineData("[[a, b], [c, d]]")]
        [InlineData("[[1, b], [c, d]]")]
        public void AnIndependentMatrixReducesToTheIdentity(string matrix)
            => Assert.Equal(MathS.IdentityMatrix(2), ((Matrix)matrix.ToEntity()).ReducedRowEchelonForm.Simplify());

        [Fact]
        public void ADependentRowReducesToZeros()
        {
            var reduced = (Matrix)((Matrix)"[[a, b], [2a, 2b]]".ToEntity()).ReducedRowEchelonForm.Simplify();
            Assert.Equal(Number.Integer.One, reduced[0, 0]);
            // b / a, which holds where a is not 0, and says so.
            var difference = (reduced[0, 1] - "b / a".ToEntity()).Simplify();
            while (difference is Providedf(var inner, _))
                difference = inner;
            Assert.Equal(Number.Integer.Zero, difference);
            Assert.Equal(Number.Integer.Zero, reduced[1, 0]);
            Assert.Equal(Number.Integer.Zero, reduced[1, 1]);
        }

        [Theory]
        [InlineData("[[a, b], [c, d]]")]
        [InlineData("[[a, b], [2a, 2b]]")]
        [InlineData("[[a, b, c], [d, f, g], [a + d, b + f, c + g]]")]
        public void NoFormHasANaN(string matrix)
        {
            var m = (Matrix)matrix.ToEntity();
            Assert.DoesNotContain("NaN", m.RowEchelonForm.Simplify().Stringize());
            Assert.DoesNotContain("NaN", m.ReducedRowEchelonForm.Simplify().Stringize());
        }
    }
}
