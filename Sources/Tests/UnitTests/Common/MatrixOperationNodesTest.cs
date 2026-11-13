//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using System.Numerics;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>det</c>, <c>transpose</c>, <c>trace</c> and <c>A[k, l]</c> of a name that stands for a matrix: nodes, computed
    /// once the name is a matrix, by substitution, simplification, evaluation or a compiled delegate. Every value is
    /// checked against one computed here from the entries, at real, negative and non-real matrices.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2147">#2147</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class MatrixOperationNodesTest
    {
        // Real, negative and non-real entries, square and not.
        private static readonly Complex[][,] Squares =
        {
            new Complex[,] { { 1, 2 }, { 3, 4 } },
            new Complex[,] { { -1.5, 2 }, { -3, -0.25 } },
            new Complex[,] { { new(1, 2), -3 }, { new(0, -1), new(2.5, 0.5) } },
            new Complex[,] { { 2, -1, 0.5 }, { -3, 4, 1 }, { 0, -2, -5 } },
            new Complex[,] { { new(1, -1), 2, 0 }, { -1, new(0, 3), 4 }, { 2, -2, new(-1, 1) } },
        };

        private static readonly Complex[][,] NotSquare =
        {
            new Complex[,] { { 1, 2, 3 }, { 4, 5, 6 } },
            new Complex[,] { { -1 }, { new(0, 2) } },
        };

        private static Entity.Matrix AsEntity(Complex[,] a) => MathS.Matrix(a.GetLength(0), a.GetLength(1), (r, c) => (Entity)a[r, c]);

        private static Complex Value(Entity e) => e.EvalNumerical().ToNumerics();

        private static void Near(Complex expected, Complex actual)
            => Assert.True(Complex.Abs(expected - actual) <= 1e-9 * Math.Max(1, Complex.Abs(expected)), $"expected {expected}, got {actual}");

        // The determinant by expansion along the first row, which is its definition, not the library's algorithm.
        private static Complex Det(Complex[,] a)
        {
            var n = a.GetLength(0);
            if (n == 1)
                return a[0, 0];
            var sum = Complex.Zero;
            for (var j = 0; j < n; j++)
            {
                var minor = new Complex[n - 1, n - 1];
                for (var r = 1; r < n; r++)
                    for (int c = 0, k = 0; c < n; c++)
                        if (c != j)
                            minor[r - 1, k++] = a[r, c];
                sum += (j % 2 == 0 ? 1 : -1) * a[0, j] * Det(minor);
            }
            return sum;
        }

        private static Complex[,] Product(Complex[,] a, Complex[,] b)
        {
            var result = new Complex[a.GetLength(0), b.GetLength(1)];
            for (var i = 0; i < a.GetLength(0); i++)
                for (var j = 0; j < b.GetLength(1); j++)
                    for (var k = 0; k < a.GetLength(1); k++)
                        result[i, j] += a[i, k] * b[k, j];
            return result;
        }

        private static Complex[,] Transposed(Complex[,] a)
        {
            var result = new Complex[a.GetLength(1), a.GetLength(0)];
            for (var i = 0; i < a.GetLength(0); i++)
                for (var j = 0; j < a.GetLength(1); j++)
                    result[j, i] = a[i, j];
            return result;
        }

        private static Complex[,] Entries(Entity value)
        {
            var matrix = Assert.IsType<Entity.Matrix>(value);
            var result = new Complex[matrix.RowCount, matrix.ColumnCount];
            for (var r = 0; r < matrix.RowCount; r++)
                for (var c = 0; c < matrix.ColumnCount; c++)
                    result[r, c] = Value(matrix[r, c]);
            return result;
        }

        private static void Near(Complex[,] expected, Complex[,] actual)
        {
            Assert.Equal(expected.GetLength(0), actual.GetLength(0));
            Assert.Equal(expected.GetLength(1), actual.GetLength(1));
            for (var r = 0; r < expected.GetLength(0); r++)
                for (var c = 0; c < expected.GetLength(1); c++)
                    Near(expected[r, c], actual[r, c]);
        }

        [Theory]
        [InlineData("det(A)", typeof(Entity.Determinantf))]
        [InlineData("transpose(A)", typeof(Entity.Transposef))]
        [InlineData("trace(A)", typeof(Entity.Tracef))]
        [InlineData("A[k, l]", typeof(Entity.MatrixEntryf))]
        [InlineData("(A + B)[1, 2]", typeof(Entity.MatrixEntryf))]
        [InlineData("det(A)[1, 1]", typeof(Entity.MatrixEntryf))]
        public void ANameForAMatrixParsesToTheNode(string input, Type node)
            => Assert.IsType(node, input.ToEntity());

        [Theory]
        [InlineData("det(A)")]
        [InlineData("transpose(A) * B")]
        [InlineData("trace(A * B)")]
        [InlineData("A[k, l]")]
        [InlineData("(A + B)[1, 2]")]
        [InlineData("transpose(A)[2, k + 1] ^ 2")]
        [InlineData("det(A)[1, 1]")]
        [InlineData("A[1, 2]!")]
        public void PrintingRoundTrips(string input)
        {
            var parsed = input.ToEntity();
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
        }

        // A matrix written out is computed as it is parsed, as it was without the nodes.
        [Theory]
        [InlineData("det([[1, 2], [3, 4]])", "-2")]
        [InlineData("trace([[1, 2], [3, 4]])", "5")]
        [InlineData("[[1, 2], [3, 4]][2, 1]", "3")]
        [InlineData("transpose([1, 2])[1, 2]", "2")]
        public void AWrittenMatrixIsComputedWhenParsed(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Evaled);

        [Theory]
        [InlineData("det(5)")]
        [InlineData("trace(x = 1)")]
        [InlineData("trace([[1, 2, 3]])")]
        [InlineData("det([[1, 2, 3]])")]
        [InlineData("[[1, 2], [3, 4]][3, 1]")]
        [InlineData("[[1, 2], [3, 4]][1, 0]")]
        [InlineData("[[1, 2], [3, 4]][1/2, 1]")]
        [InlineData("A[1]")]
        [InlineData("A[1, 2, 3]")]
        public void WhatNamesNoMatrixOrNoEntryIsRefused(string input)
            => Assert.Throws<InvalidArgumentParseException>(() => input.ToEntity());

        [Fact]
        public void TheDeterminantOfASubstitutedMatrixIsItsDeterminant()
        {
            foreach (var m in Squares)
                Near(Det(m), Value("det(A)".ToEntity().Substitute("A", AsEntity(m)).Evaled));
        }

        [Fact]
        public void TheTraceOfAProductDoesNotDependOnTheOrder()
        {
            foreach (var a in Squares)
                foreach (var b in Squares.Where(b => b.GetLength(0) == a.GetLength(0)))
                {
                    var ab = Value("trace(A * B)".ToEntity().Substitute("A", AsEntity(a)).Substitute("B", AsEntity(b)).Evaled);
                    var ba = Value("trace(B * A)".ToEntity().Substitute("A", AsEntity(a)).Substitute("B", AsEntity(b)).Evaled);
                    var p = Product(a, b);
                    var expected = Enumerable.Range(0, p.GetLength(0)).Aggregate(Complex.Zero, (s, i) => s + p[i, i]);
                    Near(expected, ab);
                    Near(expected, ba);
                }
        }

        [Fact]
        public void TheTransposeTimesAMatrixIsThatProduct()
        {
            foreach (var a in Squares)
                foreach (var b in Squares.Where(b => b.GetLength(0) == a.GetLength(0)))
                    Near(Product(Transposed(a), b),
                        Entries("transpose(A) * B".ToEntity().Substitute("A", AsEntity(a)).Substitute("B", AsEntity(b)).Evaled));
        }

        [Fact]
        public void AnEntryIsCountedFromOne()
        {
            foreach (var m in Squares.Concat(NotSquare))
                for (var i = 1; i <= m.GetLength(0); i++)
                    for (var j = 1; j <= m.GetLength(1); j++)
                        Near(m[i - 1, j - 1], Value("A[k, l]".ToEntity().Substitute("A", AsEntity(m)).Substitute("k", i).Substitute("l", j).Evaled));
        }

        // A vector is a column, so its transpose is a row, as ]T writes it.
        [Fact]
        public void TheTransposeOfAVectorIsARow()
        {
            var row = "transpose(v)".ToEntity().Substitute("v", "[1, -2, 3i]").Evaled;
            Assert.Equal("[1, -2, 3i]T".ToEntity().Evaled, row);
            Assert.Equal(1, ((Entity.Matrix)row).RowCount);
        }

        // No determinant, no trace, no entry: the node is left, never a number.
        [Fact]
        public void WhatHasNoValueIsLeftUnevaluated()
        {
            foreach (var m in NotSquare)
            {
                Assert.IsType<Entity.Determinantf>("det(A)".ToEntity().Substitute("A", AsEntity(m)).Evaled);
                Assert.IsType<Entity.Tracef>("trace(A)".ToEntity().Substitute("A", AsEntity(m)).Simplify());
            }
            foreach (var (i, j) in new[] { (3, 1), (1, 3), (0, 1), (-1, 1) })
                Assert.IsType<Entity.MatrixEntryf>("A[k, l]".ToEntity().Substitute("A", "[[1, 2], [3, 4]]").Substitute("k", i).Substitute("l", j).Evaled);
            Assert.IsType<Entity.MatrixEntryf>("A[1/2, 1]".ToEntity().Substitute("A", "[[1, 2], [3, 4]]").Evaled);
            // A number is not a matrix, here as when it is written.
            Assert.IsType<Entity.Determinantf>("det(A)".ToEntity().Substitute("A", 5).Evaled);
        }

        [Theory]
        [InlineData("transpose(transpose(A))", "A")]
        [InlineData("det(transpose(A))", "det(A)")]
        [InlineData("trace(transpose(A))", "trace(A)")]
        [InlineData("transpose(A)[1, 2]", "A[2, 1]")]
        public void ATransposeInsideAnotherOperationIsTakenOut(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        // Matrices do not commute, and a product with a transpose in it is simplified as written: each of these was
        // reordered, and the first two answered 0, before products of matrices were set aside.
        [Theory]
        [InlineData("transpose(A) * B - B * transpose(A)")]
        [InlineData("transpose(A) * A - A * transpose(A)")]
        [InlineData("transpose(A) * B")]
        [InlineData("transpose(A) * A")]
        [InlineData("A * transpose(A) * A")]
        [InlineData("transpose(A) * B + transpose(A) * C")]
        public void SimplifyingKeepsTheValueOfAProductOfMatrices(string input)
        {
            var expr = input.ToEntity();
            var simplified = expr.Simplify();
            var a = AsEntity(Squares[1]);
            var b = AsEntity(Squares[2]);
            var c = AsEntity(Squares[0]);
            Entity At(Entity e) => e.Substitute("A", a).Substitute("B", b).Substitute("C", c).Evaled;
            Near(Entries(At(expr)), Entries(At(simplified)));
        }

        // A name beside a written matrix may stand for a matrix: simplifying would read it as a number and multiply it
        // into the entries, so det(A [[2, 0], [0, 3]]) was 6 A^2, which is 6 det(A) only for a 1 x 1 A.
        [Theory]
        [InlineData("det(A * [[2, 0], [0, 3]])")]
        [InlineData("trace([[0, 1], [1, 0]] * A)")]
        [InlineData("([[1, 0]] * transpose(A))[1, 2]")]
        [InlineData("transpose(A + [[1, 2], [3, 4]]) * A")]
        public void ANameBesideAWrittenMatrixIsNotReadAsANumber(string input)
        {
            var expr = input.ToEntity();
            foreach (var m in Squares.Where(m => m.GetLength(0) == 2))
            {
                Entity At(Entity e) => e.Substitute("A", AsEntity(m)).Evaled;
                var expected = At(expr);
                foreach (var form in new[] { expr.InnerSimplified, expr.Simplify(), expr.Evaled })
                {
                    var actual = At(form);
                    if (expected is Entity.Matrix)
                        Near(Entries(expected), Entries(actual));
                    else
                        Near(Value(expected), Value(actual));
                }
            }
            Assert.Equal(6 * Det(Squares[1]), Value("det(A * [[2, 0], [0, 3]])".ToEntity().Substitute("A", AsEntity(Squares[1])).Evaled));
        }

        [Fact]
        public void AProductWrittenTwiceStillCancels()
            => Assert.Equal(0, "transpose(A) * B - transpose(A) * B".ToEntity().Simplify());

        [Fact]
        public void ADeterminantOfSymbolsSimplifiesToItsPolynomial()
        {
            var simplified = "det(A)".ToEntity().Substitute("A", "[[x, 1], [2, x]]").Simplify();
            foreach (var x in new Complex[] { 3, -2.5, new(1, -2) })
                Near(x * x - 2, Value(simplified.Substitute("x", x)));
        }

        // Linear in the matrix: the derivative of the trace and of the transpose is taken of the matrix.
        [Fact]
        public void TheTraceAndTheTransposeAreDifferentiatedThroughTheMatrix()
        {
            var a = AsEntity(Squares[1]);
            foreach (var x in new Complex[] { 2, -1.5 })
            {
                var trace = "trace(x^2 A)".ToEntity().Differentiate("x").Substitute("A", a).Substitute("x", x).Evaled;
                Near(2 * x * (Squares[1][0, 0] + Squares[1][1, 1]), Value(trace));
                var entry = "transpose(x^3 A)[1, 2]".ToEntity().Differentiate("x").Substitute("A", a).Substitute("x", x).Evaled;
                Near(3 * x * x * Squares[1][1, 0], Value(entry));
            }
            // Jacobi's formula needs the adjugate, which has no node: the derivative is left as one.
            Assert.IsType<Entity.Derivativef>("det(x A)".ToEntity().Differentiate("x"));
        }

        [Fact]
        public void TheNodesPrintToLatexAndSympy()
        {
            Assert.Equal(@"\det\left(A\right)", "det(A)".ToEntity().Latexize());
            Assert.Equal(@"\operatorname{tr}\left(A\right)", "trace(A)".ToEntity().Latexize());
            Assert.Equal(@"{\left(A+B\right)}^{\mathrm{T}}", "transpose(A + B)".ToEntity().Latexize());
            Assert.Equal(@"{A}_{k,l}", "A[k, l]".ToEntity().Latexize());
            var sympy = MathS.ToSympyCode("det(A) + trace(transpose(A)) + A[1, 2]");
            Assert.Contains("sympy.det(A)", sympy);
            Assert.Contains("sympy.trace(sympy.transpose(A))", sympy);
            Assert.Contains("(A)[(1) - 1, (2) - 1]", sympy);
        }

        [Fact]
        public void TheConstructorsBuildTheNodes()
        {
            Assert.Equal("det(A)".ToEntity(), MathS.Matrices.Determinant("A"));
            Assert.Equal("transpose(A)".ToEntity(), MathS.Matrices.Transpose("A"));
            Assert.Equal("trace(A)".ToEntity(), MathS.Matrices.Trace("A"));
            Assert.Equal("A[1, 2]".ToEntity(), MathS.Matrices.Entry("A", 1, 2));
        }

        // Compiled, each compared with evaluation at the same matrices.
        [Fact]
        public void TheDeterminantCompiles()
        {
            var overComplex = "det(A)".ToEntity().Compile<Complex[,], Complex>("A");
            foreach (var m in Squares)
                Near(Value("det(A)".ToEntity().Substitute("A", AsEntity(m)).Evaled), overComplex(m));
            var overReals = "det(A)".ToEntity().Compile<double[,], double>("A");
            var real = new double[,] { { 2, -1, 0.5 }, { -3, 4, 1 }, { 0, -2, -5 } };
            Near(Det(Squares[3]), overReals(real));
            // A zero first pivot is exchanged for a row below it.
            Near(-1, overReals(new double[,] { { 0, 1 }, { 1, 0 } }));
            Near(0, overReals(new double[,] { { 0, 1 }, { 0, 2 } }));
            // Fraction-free, so whole numbers stay whole.
            var overLongs = "det(A)".ToEntity().Compile<long[,], long>("A");
            Assert.Equal(-2L, overLongs(new long[,] { { 1, 2 }, { 3, 4 } }));
            Assert.Equal(77L, overLongs(new long[,] { { 0, 3, 1 }, { 2, -1, 4 }, { 5, 0, -2 } }));
            Assert.Throws<InvalidMatrixOperationException>(() => overReals(new double[,] { { 1, 2, 3 } }));
        }

        [Fact]
        public void TheTransposeTraceAndEntryCompile()
        {
            var product = "transpose(A) * B".ToEntity().Compile<Complex[,], Complex[,], Complex[,]>("A", "B");
            var trace = "trace(A * B)".ToEntity().Compile<Complex[,], Complex[,], Complex>("A", "B");
            var entry = "A[k, l]".ToEntity().Compile<Complex[,], double, double, Complex>("A", "k", "l");
            foreach (var a in Squares)
                foreach (var b in Squares.Where(b => b.GetLength(0) == a.GetLength(0)))
                {
                    Entity At(string e) => e.ToEntity().Substitute("A", AsEntity(a)).Substitute("B", AsEntity(b)).Evaled;
                    Near(Entries(At("transpose(A) * B")), product(a, b));
                    Near(Value(At("trace(A * B)")), trace(a, b));
                    for (var i = 1; i <= a.GetLength(0); i++)
                        for (var j = 1; j <= a.GetLength(1); j++)
                            Near(Value(At($"A[{i}, {j}]")), entry(a, i, j));
                }
            Assert.Throws<InvalidMatrixOperationException>(() => entry(Squares[0], 3, 1));
            Assert.Throws<InvalidMatrixOperationException>(() => entry(Squares[0], 0, 1));
            Assert.Throws<InvalidMatrixOperationException>(() => entry(Squares[0], 1.5, 1));
            var transposeOfARow = "transpose(v)".ToEntity().Compile<double[,], double[,]>("v")(new double[,] { { 1, -2, 3 } });
            Assert.Equal(new double[,] { { 1 }, { -2 }, { 3 } }, transposeOfARow);
        }

        // A matrix argument beside a matrix written out is not simplified as a number: det(A [[2, 0], [0, 3]]) is
        // 6 det(A), and [[1, 0]] transpose(A) [[0], [1]] is the entry A[2, 1], a 1 x 1 matrix compiled as a number.
        [Fact]
        public void AMatrixArgumentBesideAWrittenMatrixCompiles()
        {
            var det = "det(A * [[2, 0], [0, 3]])".ToEntity().Compile<Complex[,], Complex>("A");
            var trace = "trace([[0, 1], [1, 0]] * A)".ToEntity().Compile<Complex[,], Complex>("A");
            var entry = "[[1, 0]] * transpose(A) * [[0], [1]]".ToEntity().Compile<Complex[,], Complex>("A");
            var bracket = "([[1, 0]] * transpose(A))[1, 2]".ToEntity().Compile<Complex[,], Complex>("A");
            foreach (var a in Squares.Where(a => a.GetLength(0) == 2))
            {
                Near(6 * Det(a), det(a));
                Near(a[0, 1] + a[1, 0], trace(a));
                Near(a[1, 0], entry(a));
                Near(a[1, 0], bracket(a));
            }
        }

        // Shapes written into the expression are checked when compiling; a number is not a matrix argument.
        [Fact]
        public void WhatCannotBeComputedIsRefusedWhenCompiling()
        {
            Assert.Throws<InvalidMatrixOperationException>(() => MathS.Matrices.Determinant("[[x, 1, 2]]").Compile<double, double>("x"));
            Assert.Throws<InvalidMatrixOperationException>(() => MathS.Matrices.Entry("[[x, 1], [2, x]]", 3, 1).Compile<double, double>("x"));
            Assert.Throws<UncompilableNodeException>(() => "det(A)".ToEntity().Compile<double, double>("A"));
        }
    }
}
