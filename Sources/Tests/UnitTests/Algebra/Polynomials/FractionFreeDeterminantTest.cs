//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using AngouriMath.Extensions;
using AngouriMath.Functions;
using Xunit;

namespace AngouriMath.Tests.Algebra.Polynomials
{
    /// <summary>
    /// The determinant by Bareiss' fraction-free elimination, and its agreement with Laplace
    /// expansion wherever both apply.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/999">#999</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two algorithms for one quantity is how a library acquires a case where they differ, so
    /// the agreement is generated rather than listed — and compared as a difference that
    /// simplifies to zero rather than as trees, because the two group the same polynomial
    /// differently and that is not a disagreement.
    /// </para>
    /// <para>
    /// The other half is that a matrix this cannot read is <em>declined</em> rather than
    /// answered wrongly. Both directions are asserted: what it takes, and what it refuses.
    /// </para>
    /// </remarks>
    [Trait("Area", "Algebra")]
    public sealed class FractionFreeDeterminantTest
    {
        private static Entity.Matrix Parse(string source) => (Entity.Matrix)source.ToEntity();

        private static Entity? ByElimination(Entity.Matrix matrix)
            => PolynomialDeterminant.Of(matrix.RowCount, (row, column) => matrix[row, column]);

        /// <summary>
        /// The determinants everybody knows, so that a sign convention cannot be wrong in a way
        /// only a generated comparison would notice.
        /// </summary>
        [Theory]
        [InlineData("[[1, 2], [3, 4]]", "-2")]
        [InlineData("[[a, b], [c, d]]", "a * d - b * c")]
        [InlineData("[[0, 1], [1, 0]]", "-1")]
        [InlineData("[[1, 1], [1, 1]]", "0")]
        [InlineData("[[x, 1], [1, x]]", "x ^ 2 - 1")]
        [InlineData("[[x, 1, 0], [1, x, 1], [0, 1, x]]", "x ^ 3 - 2 * x")]
        [InlineData("[[1/2, 1/3], [1/4, 1/5]]", "1/60")]
        [InlineData("[[2, 0, 0], [0, 3, 0], [0, 0, 5]]", "30")]
        public void TheDeterminantIsWhatItShouldBe(string source, string expected)
            => Assert.Equal(expected.ToEntity(), Parse(source).Determinant);

        /// <summary>
        /// A row swap is needed when a pivot vanishes, and getting its sign wrong is the classic
        /// way to write an elimination that is right on most inputs. The first column here is
        /// zero at the top, so the swap happens.
        /// </summary>
        [Theory]
        [InlineData("[[0, 1], [1, 0]]", "-1")]
        [InlineData("[[0, 2], [3, 0]]", "-6")]
        [InlineData("[[0, 0, 1], [0, 1, 0], [1, 0, 0]]", "-1")]
        [InlineData("[[0, 1, 0], [0, 0, 1], [1, 0, 0]]", "1")]
        public void ARowSwapCarriesItsSign(string source, string expected)
            => Assert.Equal(expected.ToEntity(), Parse(source).Determinant);

        /// <summary>
        /// A singular matrix is zero rather than a failure — a whole column with nothing to
        /// bring up is an answer.
        /// </summary>
        [Theory]
        [InlineData("[[0, 0], [0, 0]]")]
        [InlineData("[[0, 1], [0, 2]]")]
        [InlineData("[[1, 2], [2, 4]]")]
        [InlineData("[[a, a], [b, b]]")]
        public void ASingularMatrixIsZero(string source)
            => Assert.Equal(Entity.Number.Integer.Zero, Parse(source).Determinant);

        /// <summary>
        /// What the elimination declines, and why. Each is a refusal to try rather than a wrong
        /// answer, and <see cref="Entity.Matrix.Determinant"/> still answers all of them by
        /// Laplace expansion.
        /// </summary>
        [Theory]
        // Not a polynomial over the rationals.
        [InlineData("[[sin(x), 1], [1, cos(x)]]")]
        [InlineData("[[1 / x, 1], [1, x]]")]
        [InlineData("[[2 ^ x, 1], [1, 1]]")]
        // A constant is a value, not an indeterminate, and this ring cannot hold one.
        [InlineData("[[e, 1], [1, e]]")]
        [InlineData("[[pi, 1], [1, 1]]")]
        public void WhatItCannotReadItDeclines(string source)
        {
            var matrix = Parse(source);
            Assert.Null(ByElimination(matrix));
            // And the caller still gets an answer, from the other method.
            Assert.NotNull(matrix.Determinant);
        }

        /// <summary>
        /// More indeterminates than the packed monomial has room for is also a decline. Nine
        /// distinct names, so this is one past the ceiling.
        /// </summary>
        [Fact]
        public void MoreVariablesThanTheRingHoldsIsDeclined()
        {
            var cells = new Entity[3, 3];
            // Single letters, and none of them e, i or pi: a name ending in a digit is not a
            // variable at all -- `q1` parses as `q ^ 1` -- and a constant would be declined for
            // the other reason, which would make this test pass for the wrong one.
            var names = new[] { "q", "r", "s", "t", "u", "v", "w", "y", "z" };
            for (var row = 0; row < 3; row++)
                for (var column = 0; column < 3; column++)
                    cells[row, column] = MathS.Var(names[row * 3 + column]);
            var matrix = MathS.Matrix(cells);

            Assert.Null(ByElimination(matrix));
            Assert.NotNull(matrix.Determinant);
        }

        /// <summary>
        /// The two algorithms agree on every matrix where both apply. Generated, because the
        /// disagreements worth finding are the ones nobody thought to write down, and
        /// deterministic, so a failure reproduces.
        /// </summary>
        [Fact]
        public void EliminationAgreesWithLaplaceWhereverBothApply()
        {
            var random = new Random(4242);
            var names = new[] { "a", "b", "c" };
            var compared = 0;
            var disagreements = new List<string>();

            for (var trial = 0; trial < 300; trial++)
            {
                var size = 2 + trial % 3;
                var cells = new Entity[size, size];
                for (var row = 0; row < size; row++)
                    for (var column = 0; column < size; column++)
                        cells[row, column] = random.Next(0, 4) == 0
                            ? MathS.Var(names[random.Next(0, names.Length)])
                            : random.Next(-4, 5);
                var matrix = MathS.Matrix(cells);

                if (ByElimination(matrix) is not { } byElimination)
                    continue;
                compared++;
                // Through the guard rather than straight to GenericTensor, whose Laplace
                // expansion takes its minors in a process-wide scratch matrix. xUnit runs test
                // classes in parallel and MatrixConcurrencyTest expands forty matrices across
                // every core, so an unguarded call here reads that test's minors and reports the
                // determinant of an integer matrix as containing sin(a).
                var byLaplace = GenTensorGuard.DeterminantLaplace(matrix.InnerMatrix)
                    .InnerSimplified;
                if ((byElimination - byLaplace).Simplify() != 0)
                    disagreements.Add($"{matrix.Stringize()}: "
                        + $"{byElimination.Stringize()} vs {byLaplace.Stringize()}");
            }

            Assert.True(disagreements.Count == 0,
                $"of {compared} matrices where both apply, these disagree:\n  "
                + string.Join("\n  ", disagreements));
            // Not an assertion about the generator's luck -- an assertion that it generated
            // anything at all, so that a change making the elimination decline everything would
            // fail here rather than pass vacuously.
            Assert.True(compared > 100,
                $"only {compared} of 300 generated matrices were read by the elimination");
        }

        /// <summary>
        /// Where the elimination declines, <c>Determinant</c> expands along the first row itself,
        /// since GenericTensor's expansion cannot be published to a native build, and it has to
        /// agree with GenericTensor's on what neither reads as a polynomial.
        /// https://github.com/asc-community/AngouriMath/issues/2147
        /// </summary>
        [Fact]
        public void TheExpansionAgreesWithGenericTensorsWhereTheEliminationDeclines()
        {
            var random = new System.Random(2147);
            var atoms = new Entity[] { MathS.Sqrt("a"), MathS.Sin("b"), MathS.Var("c"), MathS.Ln("d") };
            var disagreements = new List<string>();
            for (var trial = 0; trial < 60; trial++)
            {
                var size = 2 + trial % 3;
                var cells = new Entity[size, size];
                for (var row = 0; row < size; row++)
                    for (var column = 0; column < size; column++)
                        cells[row, column] = random.Next(0, 2) == 0
                            ? atoms[random.Next(0, atoms.Length)] * random.Next(1, 4)
                            : random.Next(-4, 5);
                cells[0, 0] = atoms[0];
                var matrix = MathS.Matrix(cells);
                var byLaplace = GenTensorGuard.DeterminantLaplace(matrix.InnerMatrix).InnerSimplified;
                // Compared by value at a point: a 4x4 expansion is past what Simplify proves zero.
                var difference = (matrix.Determinant! - byLaplace)
                    .Substitute("a", 2).Substitute("b", 3).Substitute("c", 5).Substitute("d", 7).EvalNumerical();
                if (Entity.Number.Abs(difference).EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-40")) > 0)
                    disagreements.Add($"{matrix.Stringize()}: {matrix.Determinant!.Stringize()} vs {byLaplace.Stringize()}");
            }
            Assert.True(disagreements.Count == 0, string.Join("\n  ", disagreements));
        }

        /// <summary>
        /// The size the issue is about. Laplace is <c>O(n!)</c>, so a 10×10 took about fourteen
        /// seconds and an 11×11 did not return; this is the same work in milliseconds.
        /// </summary>
        /// <remarks>
        /// A timing is not asserted — this machine is not that machine. What is asserted is the
        /// answer, at a size where the previous algorithm could not produce one at all, which is
        /// the claim that matters and does not depend on a clock.
        /// </remarks>
        [Theory]
        [InlineData(11)]
        [InlineData(14)]
        [InlineData(20)]
        public void ALargeNumericMatrixHasADeterminant(int size)
        {
            var cells = new Entity[size, size];
            for (var row = 0; row < size; row++)
                for (var column = 0; column < size; column++)
                    cells[row, column] = row == column ? 2 : 1;
            // A matrix of ones with 2 on the diagonal has determinant n + 1.
            Assert.Equal((Entity)(size + 1), MathS.Matrix(cells).Determinant);
        }

        /// <summary>
        /// And the elimination is what answered it, rather than Laplace having got quick.
        /// </summary>
        [Fact]
        public void ALargeNumericMatrixIsTheEliminationsToAnswer()
        {
            var cells = new Entity[11, 11];
            for (var row = 0; row < 11; row++)
                for (var column = 0; column < 11; column++)
                    cells[row, column] = row == column ? 2 : 1;
            Assert.NotNull(ByElimination(MathS.Matrix(cells)));
        }
    }
}
