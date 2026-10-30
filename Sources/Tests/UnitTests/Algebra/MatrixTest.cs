//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using Xunit;
using static AngouriMath.Entity;
using AngouriMath.Extensions;

namespace AngouriMath.Tests.Algebra
{
    [Trait("Area", "Algebra")]
    public sealed class MatrixTest
    {
        public static readonly Entity.Matrix A = MathS.Matrices.Matrix(4, 2,
            1, 2,
            3, 4,
            5, 6,
            7, 8
        );
        public static readonly Entity.Matrix B = MathS.Matrices.Matrix(4, 2,
            1, 2,
            3, 4,
            5, 6,
            7, 8
            );
        public static readonly Entity.Matrix C = MathS.Matrices.Matrix(2, 4,
            1, 2, 3, 4,
            5, 6, 7, 8
        );

        [Fact]
        public void DotProduct()
        {
            var R = MathS.Matrices.Matrix(2, 2,
                84, 100,
                100, 120
            );
            Assert.Equal(R, A.T * B);
        }
        [Fact]
        public void DotPointwiseProduct1()
        {
            var C = MathS.Matrices.Matrix(4, 2,
                1, 4,
                9, 16,
                25, 36,
                49, 64
                );
            Assert.Equal(C, MathS.Matrices.PointwiseMultiplication(A, B));
        }

        [Fact]
        public void SumProduct1() => Assert.Equal((2 * A).Evaled, (A + B).Evaled);

        [Fact]
        public void ScalarProduct1()
        {
            var a = MathS.Vector(1, 2, 3);
            var b = MathS.Vector(1, 2, 4);
            var scalar = a.T * b;
            Assert.Equal(17, scalar.EvalNumerical());
        }

        [Fact]
        public void Determ()
        {
            var A = MathS.Matrix(new Entity[,]
            {
                {"A", "B"},
                {"C", "D"}
            });
            var v = TestExtensions.AsNotNull(A.Determinant)
                .Substitute("A", 1)
                .Substitute("B", 2)
                .Substitute("C", 3)
                .Substitute("D", 4);           
            Assert.Equal(-2, v.EvalNumerical());
        }

        [Fact]
        public void TransposeMatrix()
        {
            var a = MathS.Matrix(new Entity[,]
                {
                    { 1, 2 },
                    { 3, 4 }
                });
            Assert.Equal(1, a[0, 0]);
            Assert.Equal(2, a[0, 1]);
            Assert.Equal(3, a[1, 0]);
            Assert.Equal(4, a[1, 1]);

            Assert.Equal(1, a.T[0, 0]);
            Assert.Equal(3, a.T[0, 1]);
            Assert.Equal(2, a.T[1, 0]);
            Assert.Equal(4, a.T[1, 1]);
        }

        [Fact]
        public void TransposeVector()
        {
            var a = MathS.Vector(1, 2);
            Assert.Equal(1, a[0]);
            Assert.Equal(2, a[1]);

            Assert.Equal(1, a.T[0, 0]);
            Assert.Equal(2, a.T[0, 1]);
        }

        [Fact]
        public void TransposeImmutability()
        {
            var a = MathS.Vector(1, 2);
            var aT = MathS.Matrix(new Entity[,] { { 1, 2 } });
            Assert.Equal(aT, a.T);
            var origin = MathS.Vector(1, 2);
            Assert.Equal(origin, a);
        }

        [Fact]
        public void TransposeDoubleOperationVector()
        {
            var a = MathS.Vector(1, 2);
            Assert.Equal(a, a.T.T);
        }

        [Fact]
        public void TransposeDoubleOperationMatrix()
        {
            var a = MathS.Matrix(new Entity[,]
                {
                    { 1, 2 },
                    { 3, 4 }
                });
            Assert.Equal(a, a.T.T);
        }

        [Fact]
        public void TransposeDoubleOperationMatrixImmutability()
        {
            var a = MathS.Matrix(new Entity[,]
                {
                    { 1, 2 },
                    { 3, 4 }
                });
            var b = MathS.Matrix(new Entity[,]
                {
                    { 1, 2 },
                    { 3, 4 }
                });
            var c = a.T;
            Assert.Equal(b, c.T);
        }

        [Fact]
        public void Identity2()
        {
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0 },
                    { 0, 1 }
                }),
                MathS.IdentityMatrix(2));
        }

        [Fact]
        public void Identity3()
        {
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 0 },
                    { 0, 1, 0 },
                    { 0, 0, 1 }
                }),
                MathS.IdentityMatrix(3));
        }

        [Fact]
        public void Identity2x3()
        {
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 0 },
                    { 0, 1, 0 },
                }),
                MathS.IdentityMatrix(2, 3));
        }

        [Fact]
        public void Identity3x2()
        {
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0 },
                    { 0, 1 },
                    { 0, 0 }
                }),
                MathS.IdentityMatrix(3, 2));
        }

        [Fact] public void MatrixMultiplicationIdentity()
            => Assert.Equal(MathS.I_3, MathS.I_3 * MathS.I_3);

        [Fact] public void MatrixIdentityT()
            => Assert.Equal(Matrix.I(3), Matrix.I(3).T);

        [Fact] public void MatrixIdentityT2x3()
            => Assert.Equal(Matrix.I(2, 3), Matrix.I(3, 2).T);

        public static readonly Matrix H = MathS.Matrix(new Entity[,]
            {
                { 1, 1 },
                { 1, -1 },
            });

        [Fact] public void MatrixMultiplication1()
            => Assert.Equal(MathS.Vector(1, -1), H * MathS.Vector(0, 1));

        [Fact] public void MatrixMultiplication2()
            => Assert.Equal(MathS.I_2 + MathS.I_2, H * H);

        [Fact] public void UnsuccessfulMultiplicationEvaled()
            => Assert.Equal(A * (Entity)B, (A * (Entity)B).Evaled);

        [Fact] public void UnsuccessfulMultiplicationInnerSimplified()
            => Assert.Equal(A * (Entity)B, (A * (Entity)B).InnerSimplified);

        [Fact] public void MatrixAddition1()
            => Assert.Equal(MathS.Vector(3, 2), MathS.Vector(1, 2) + MathS.Vector(2, 0));

        [Fact] public void UnsuccessfulMatrixAddition2Evaled()
            => Assert.Equal(MathS.Vector(1, 2) + (Entity)MathS.Vector(2, 0, 3), (MathS.Vector(1, 2) + (Entity)MathS.Vector(2, 0, 3)).Evaled);

        [Fact] public void UnsuccessfulMatrixAddition2InnerSimplified()
            => Assert.Equal(MathS.Vector(1, 2) + (Entity)MathS.Vector(2, 0, 3), (MathS.Vector(1, 2) + (Entity)MathS.Vector(2, 0, 3)).InnerSimplified);

        [Fact] public void MatrixSubtraction1()
            => Assert.Equal(MathS.Vector(-1, 2), MathS.Vector(1, 2) - MathS.Vector(2, 0));

        [Fact] public void UnsuccessfulMatrixSubtraction2Evaled()
            => Assert.Equal(MathS.Vector(1, 2) - (Entity)MathS.Vector(2, 0, 3), (MathS.Vector(1, 2) - (Entity)MathS.Vector(2, 0, 3)).Evaled);

        [Fact] public void UnsuccessfulMatrixSubtraction2InnerSimplified()
            => Assert.Equal(MathS.Vector(1, 2) - (Entity)MathS.Vector(2, 0, 3), (MathS.Vector(1, 2) - (Entity)MathS.Vector(2, 0, 3)).InnerSimplified);

        [Fact] public void LeftScalarMulitiplication()
            => Assert.Equal(MathS.Vector(6, 8), (2 * MathS.Vector(3, 4)).InnerSimplified);

        [Fact] public void RightScalarMulitiplication()
            => Assert.Equal(MathS.Vector(6, 8), (MathS.Vector(3, 4) * 2).InnerSimplified);

        [Fact] public void RightScalarDivision()
            => Assert.Equal(MathS.Vector("1.5", 2), (MathS.Vector(3, 4) / 2).InnerSimplified);

        [Fact] public void LeftScalarAddition()
            => Assert.Equal(MathS.Vector(5, 6), (2 + MathS.Vector(3, 4)).InnerSimplified);

        [Fact] public void RightScalarAddition()
            => Assert.Equal(MathS.Vector(5, 6), (MathS.Vector(3, 4) + 2).InnerSimplified);

        [Fact] public void LeftScalarSubtraction()
            => Assert.Equal(MathS.Vector(-1, -2), (2 - MathS.Vector(3, 4)).InnerSimplified);

        [Fact] public void RightScalarSubtraction()
            => Assert.Equal(MathS.Vector(1, 2), (MathS.Vector(3, 4) - 2).InnerSimplified);

        [Fact] public void Adjugate1()
        {
            var H = MathS.Matrix(new Entity[,]
            {
                { "1", "2", "6" },
                { "3", "2", "9" },
                { "1", "1", "9" },
            });
            var actual = H.Adjugate is { } adj && H.Determinant is { } det ? (adj / det).Evaled : null;
            var expected = H.Inverse;

            Assert.Equal(expected, actual);
        }

        [Fact] public void Adjugate2()
            => Assert.Equal(MathS.I_2, MathS.I_2.Adjugate);

        [Fact] public void GaussianElimination()
        {
            var m = MathS.Matrix(new Entity[,] {
                    { 32, 41, 1 },
                    { 3,  4,  1 },
                    { 3,  1,  4 }
                });
            var g = m.RowEchelonForm;
            Assert.Equal(m.Determinant, (g.MainDiagonal[0] * g.MainDiagonal[1] * g.MainDiagonal[2]).InnerSimplified);
        }

        [Fact] public void ZeroSquareMatrix1()
            => Assert.Equal(MathS.O_3, (MathS.I_3 * 0).InnerSimplified);

        [Fact] public void ZeroSquareMatrix2()
            => Assert.Equal(MathS.O_4, (MathS.I_4 * 0).InnerSimplified);

        [Fact] public void ZeroVector()
            => Assert.Equal(MathS.Vector(0, 0, 0, 0), MathS.ZeroVector(4));

        [Fact] public void ZeroRectMatrix()
        {
            var m = MathS.Matrix(new Entity[,]{
                { 0, 0, 0 },
                { 0, 0, 0 }
            });
            Assert.Equal(m, MathS.ZeroMatrix(2, 3));
        }

        [Fact] public void WithElementVector1()
        {
            var v = MathS.Vector(1, 2, 3);
            Assert.Equal(5, v.WithElement(2, 5)[2]);
            Assert.Equal(MathS.Vector(1, 2, 3), v);
        }
        [Fact] public void WithElementVector2()
        {
            var v = MathS.ZeroVector(5);
            Assert.Equal(MathS.Vector(0, 0, 5, 6, 0), v.WithElement(2, 5).WithElement(3, 6));
            Assert.Equal(MathS.Vector(0, 0, 0, 0, 0), v);
        }

        [Fact] public void WithElementMatrix1()
        {
            var jordan = MathS.I_3.WithElement(0, 1, 1).WithElement(1, 2, 1);
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 1, 0 },
                    { 0, 1, 1 },
                    { 0, 0, 1 }
                }),
                jordan);
        }

        [Fact] public void WithElementMatrix2()
        {
            var jordan = MathS.I_3.WithElement(0, 1, 5).WithElement(1, 2, 8);
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 5, 0 },
                    { 0, 1, 8 },
                    { 0, 0, 1 }
                }),
                jordan);
        }

        [Fact] public void WithRowVector()
            => Assert.Equal(MathS.Vector(0, 0, 3, 0), MathS.ZeroVector(4).WithRow(2, MathS.Vector(3)));

        [Fact] public void WithRowMatrix1()
        {
            var m = MathS.I_4.WithRow(2, MathS.Vector(1, 2, 3, 4).T);
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 0, 0 },
                    { 0, 1, 0, 0 },
                    { 1, 2, 3, 4 },
                    { 0, 0, 0, 1 }
                }),
                m);
        }

        [Fact] public void WithRowMatrix2()
        {
            var m = MathS.IdentityMatrix(5)
                .WithRow(2, MathS.Vector(1, 2, 3, 4, 5).T)
                .WithRow(4, MathS.Vector(1, 3, 5, 9, 1).T)
                .WithRow(4, MathS.Vector(5, 6, 7, 8, 9).T);
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 0, 0, 0 },
                    { 0, 1, 0, 0, 0 },
                    { 1, 2, 3, 4, 5 },
                    { 0, 0, 0, 1, 0 },
                    { 5, 6, 7, 8, 9 }
                }),
                m);
        }

        [Fact] public void WithColumnVector()
            => Assert.Equal(MathS.Vector(1, 2, 3), MathS.ZeroVector(3).WithColumn(0, MathS.Vector(1, 2, 3)));

        [Fact] public void WithColumnMatrix1()
        {
            var m = MathS.I_4.WithColumn(2, MathS.Vector(1, 2, 3, 4));
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 1, 0 },
                    { 0, 1, 2, 0 },
                    { 0, 0, 3, 0 },
                    { 0, 0, 4, 1 }
                }),
                m);
        }

        [Fact] public void WithColumnMatrix2()
        {
            var m = MathS.IdentityMatrix(5)
                .WithColumn(2, MathS.Vector(1, 2, 3, 4, 5))
                .WithColumn(4, MathS.Vector(1, 3, 5, 9, 1))
                .WithColumn(4, MathS.Vector(5, 6, 7, 8, 9));
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 1, 0, 5 },
                    { 0, 1, 2, 0, 6 },
                    { 0, 0, 3, 0, 7 },
                    { 0, 0, 4, 1, 8 },
                    { 0, 0, 5, 0, 9 }
                }),
                m);
        }

        [Fact] public void WithMatrix1()
        {
            var m = MathS.ZeroMatrix(6).With
                ((r, c, e) => (r, c, e) switch
                {
                    (_, < 1, _) => 1,
                    (>= 1 and <= 4, >= 1 and <= 3, _) => 2,
                    (>= 1 and <= 4, > 3, _) => 3,
                    (5, 5, _) => 4,
                    _ => e
                });
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 0, 0, 0, 0 },
                    { 1, 2, 2, 2, 3, 3 },
                    { 1, 2, 2, 2, 3, 3 },
                    { 1, 2, 2, 2, 3, 3 },
                    { 1, 2, 2, 2, 3, 3 },
                    { 1, 0, 0, 0, 0, 4 },
                }),
                m);
        }

        [Fact] public void ReducedRowEchelonForm1()
        {
            var m2 = MathS.Matrix(new Entity[,]
                {
                    {  1,  -1,  0, -2,  0 },
                    { -3,  -6, -2,  1,  3 },
                    {  0,  -7, -1, -5,  2 },
                    {  3,   4,  1, -1, -2 },
                    { -6, -19, -5, -3,  8 }
                }
            );
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 0, 0, -1, "-1/5".Simplify() },
                    { 0, 1, 0,  1, "-1/5".Simplify() },
                    { 0, 0, 1, -2, "-3/5".Simplify() },
                    { 0, 0, 0,  0, 0 },
                    { 0, 0, 0,  0, 0 },
                }),
                m2.ReducedRowEchelonForm);
        }

        [Fact] public void ReducedRowEchelonForm2()
        {
            var m2 = MathS.Matrix(new Entity[,]
                {
                    { 1,  3,  1,  9 },
                    { 1,  1, -1,  1 },
                    { 3, 11,  5, 25 },
                    { 5, 6,  78, 23 }
                }
            );
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1,  0,  0,  0 },
                    { 0,  1,  0,  0 },
                    { 0,  0,  1,  0 },
                    { 0,  0,  0,  1 }
                }),
                m2.ReducedRowEchelonForm);
        }

        [Fact] public void RowEchelonForm1()
        {   
            var m2 = MathS.Matrix(new Entity[,]
                {
                    { 1,  3,  0,  9 },
                    { 0,  0,  0,  1 },
                    { 3, 11,  0, 25 },
                }
            );
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 1, 3, 0,  9 },
                    { 0, 2, 0, -2 },
                    { 0, 0, 0,  1 }
                }),
                m2.RowEchelonForm);
        }

        [Fact] public void RowEchelonForm2()
        {   
            var m2 = MathS.Matrix(new Entity[,]
                {
                    { 8,  3 },
                    { 9,  1 },
                    { 3,  7 },
                    { 5,  6 }
                }
            );
            Assert.Equal(
                MathS.Matrix(new Entity[,]
                {
                    { 8,  3 },
                    { 0, "-19/8".Simplify() },
                    { 0,  0 },
                    { 0,  0 }
                }),
                m2.RowEchelonForm);
        }

        [Fact] public void Rank1()
        {
            var m2 = MathS.Matrix(new Entity[,]
                {
                    {  1,  -1,  0, -2,  0 },
                    { -3,  -6, -2,  1,  3 },
                    {  0,  -7, -1, -5,  2 },
                    {  3,   4,  1, -1, -2 },
                    { -6, -19, -5, -3,  8 }
                }
            );
            Assert.Equal(3, m2.Rank);
        }

        [Fact] public void Rank2()
        {
            Assert.Equal(6, MathS.IdentityMatrix(6).Rank);
        }

        // The elimination kept each entry as an unreduced numerator and denominator, the
        // denominator of row 0 reached 1e-246, and 0 over it was NaN: the form reduced to NaN and
        // Rank threw InvalidCastException. https://github.com/asc-community/AngouriMath/issues/2270
        [Theory]
        [InlineData("[[1/2, -2/3, 0, -2/3], [3, -2, 0, -2/3], [2, 2, 1, -3], [-2/3, 0, -2/3, 1]]")]
        [InlineData("[[-2/3, 1, 1, -2], [1/2, -2/3, -2/3, -2/3], [-2/3, -2/3, 1, -1], [1/2, -2, -3, -2/3]]")]
        [InlineData("[[-2/3, 1/2, -2/3, 1/2], [1, -2/3, -2/3, -2], [1, -2/3, 1, -3], [-2, -2/3, -1, -2/3]]")]
        public void AnInvertibleMatrixReducesToTheIdentity(string matrix)
        {
            var m = (Matrix)matrix.ToEntity();
            Assert.Equal(MathS.IdentityMatrix(4), m.ReducedRowEchelonForm);
            Assert.Equal(4, m.Rank);
        }

        // Unreduced, the back substitution squared the pairs' sizes at every row, and this did not
        // return in two minutes; bounded, so that it fails rather than hangs the run.
        // https://github.com/asc-community/AngouriMath/issues/2270
        [Fact] public void AnEightByEightReduces()
        {
            var reduced = System.Threading.Tasks.Task.Factory.StartNew(Reduce,
                System.Threading.CancellationToken.None, System.Threading.Tasks.TaskCreationOptions.LongRunning,
                System.Threading.Tasks.TaskScheduler.Default);
            Assert.True(reduced.Wait(System.TimeSpan.FromSeconds(60)), "the reduced row echelon form of an 8x8 did not return");
            Assert.Equal(MathS.IdentityMatrix(8), reduced.Result.Reduced);
            Assert.Equal(8, reduced.Result.Rank);
        }

        private static (Matrix Reduced, int Rank) Reduce()
        {
            var m = MathS.Matrix(new Entity[,]
            {
                { 2, -1, 3, 1, -2, 1, 3, -3 },
                { 1, 2, -1, 3, 1, -2, 1, 2 },
                { -3, 1, 2, -1, 3, 2, -2, 1 },
                { 1, -2, 1, 2, -1, 3, 2, -1 },
                { 2, 3, -2, 1, 2, -1, -3, 1 },
                { -1, 1, 3, -2, 1, 2, 1, 3 },
                { 3, -1, 1, 2, -3, 1, 2, -2 },
                { 1, 2, 2, -1, 1, -3, 1, 1 },
            });
            Assert.NotEqual(Number.Integer.Zero, m.Determinant);
            return (m.ReducedRowEchelonForm, m.Rank);
        }

        [Theory]
        [InlineData("[a, b]", "[c, d]", "[a c, a d, b c, b d]")]
        [InlineData("[[a, b], [c, d]]", "[[e, f], [g, h]]", "[ [a e, a f, b e, b f], [a g, a h, b g, b h], [c e, c f, d e, d f], [c g, c h, d g, d h] ]")]
        [InlineData("[a, b]", "[c, d, e]", "[a c, a d, a e, b c, b d, b e]")]
        [InlineData("[a, b, c]", "[d, e]", "[a d, a e, b d, b e, c d, c e]")]
        [InlineData("[a, b, c]T", "[d, e]", "[[a d, a e], [b d, b e], [c d, c e]]T")]
        public void TensorProductTest(string a, string b, string expectedRaw)
        {
            Matrix A = a;
            Matrix B = b;
            Matrix expected = expectedRaw;
            var actual = Matrix.TensorProduct(A, B);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("[2]", 6, "[64]")]
        [InlineData("[2, 3]", 2, "[4, 6, 6, 9]")]
        [InlineData("[a, b]", 2, "[a 2, a b, b a, b 2]")]
        [InlineData("[[a, b], [c, d]]", 2, "[[a 2, a b, b a, b 2], [a c, a d, b c, b d], [c a, c b, d a, d b], [c 2, c d, d c, d 2]]")]
        [InlineData("[a, b]T", 2, "[a 2, a b, b a, b 2]T")]
        [InlineData("[a, b, c]T", 2, "[a 2, a b, a c, b a, b 2, b c, c a, c b, c 2]T")]
        [InlineData("[[a, b], [c, d], [e, f]]T", 2, "[[a 2, a b, b a, b 2], [a c, a d, b c, b d], [a e, a f, b e, b f], [c a, c b, d a, d b], [c 2, c d, d c, d 2], [c e, c f, d e, d f], [e a, e b, f a, f b], [e c, e d, f c, f d], [e 2, e f, f e, f 2]]T")]
        public void TensorPowerTest(string baseRaw, int power, string expectedRaw)
        {
            Matrix @base = baseRaw;
            var actual = @base.TensorPower(power).InnerSimplified;
            Matrix expected = expectedRaw;

            actual.ShouldBe(expected.InnerSimplified);
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(1, 2, 2)]
        [InlineData(2, 1, 2)]
        [InlineData(3, 4, 2)]
        [InlineData(3, 4, 3)]
        [InlineData(3, 4, 4)]
        [InlineData(1, 2, 3)]
        [InlineData(2, 1, 3)]
        [InlineData(1, 2, 4)]
        [InlineData(2, 1, 4)]
        [InlineData(1, 2, 5)]
        [InlineData(2, 1, 5)]
        [InlineData(1, 2, 6)]
        [InlineData(2, 1, 6)]
        [InlineData(2, 3, 5)]
        public void TensorPowerSizeTest(int rowCount, int columnCount, int n)
        {
            var m = MathS.ZeroMatrix(rowCount, columnCount);
            var actual = m.TensorPower(n);
            Assert.Equal((int)System.Math.Pow(rowCount, n), actual.RowCount);
            Assert.Equal((int)System.Math.Pow(columnCount, n), actual.ColumnCount);
        }

        [Theory]
        [InlineData("[[1, 0, 70], [0, 1, 0], [0, 0, 1]]")]
        [InlineData("[[1, 70, 0], [0, 1, 0], [0, 0, 1]]")]
        [InlineData("[[1, 70], [0, 1]]")]
        [InlineData("[[1, 0, 0, 0], [0, 1, 70, 0], [0, 0, 1, 0], [0, 0, 0, 1]]")]
        [InlineData("[6]")]
        public void TestInverseWithGT101Issue400(string matrixRaw)
        {
            Matrix a = matrixRaw;
            var inverse = TestExtensions.AsNotNull(a.Inverse);
            var product = (a * inverse).Simplify();
            if (a.RowCount == 1)
                Assert.Equal(1, product);
            else
                Assert.Equal(MathS.IdentityMatrix(a.RowCount), product);
        }

        [Theory]
        [InlineData("[1, 2, 3]")]
        [InlineData("[1, 2, 3]T")]
        [InlineData("[[1, 3], [2, 5], [3, 6]]")]
        public void CasesWhenDeterminantIsNull(string matrixRaw)
        {
            Matrix m = matrixRaw;
            Assert.Null(m.Determinant);
        }

        [Theory]
        [InlineData("[1, 2, 3]")]
        [InlineData("[1, 2, 3]T")]
        [InlineData("[[1, 3], [2, 5], [3, 6]]")]
        [InlineData("[[0, 0, 0], [3, 4, 5], [6, 7, 81]]")]
        public void CasesWhenInverseIsNull(string matrixRaw)
        {
            Matrix m = matrixRaw;
            Assert.Null(m.Inverse);
        }

        [Fact] public void TestMatrixMapped1()
            => Assert.Equal(MathS.I_3, MathS.Matrix(3, 3, (a, b) => a == b ? 1 : 0));

        [Fact] public void TestMatrixMapped2()
            => Assert.Equal("[[11, 12, 13], [21, 22, 23], [31, 32, 33]]", MathS.Matrix(3, 3, (a, b) => (a + 1) * 10 + (b + 1)));

        [Fact] public void TestMatrixMapped3()
            => Assert.Equal("[1616]", MathS.Matrix(1, 1, (a, b) => 1616));

        [Fact] public void TestMatrixMapped4()
            => Assert.Equal("[[1, 2], [3, 4], [5, 6]]", MathS.Matrix(3, 2, (a, b) => 1 + a * 2 + b));

        [Fact] public void Concat01()
            => Assert.Equal(
                @"
                    [[1, 2],
                    [3, 4]]
                ",
                MathS.Matrices.Concat(MathS.Matrices.Direction.Vertical,
                    "[[1, 2]]",
                    "[[3, 4]]"
                )
            );
        
        [Fact] public void Concat02()
            => Assert.Equal(
                @"
                    [[1, 2],
                    [3, 4],
                    [5, 6],
                    [7, 8],
                    [9, 10]]
                ",
                MathS.Matrices.Concat(MathS.Matrices.Direction.Vertical,
                    "[[1, 2]]",
                    "[[3, 4]]",
                    @"
                     [[5, 6],
                      [7, 8],
                      [9, 10]]
                    "
                )
            );
        
        [Fact] public void Concat03()
            => Assert.Equal(
                @"
                    [[1, 2],
                    [3, 4],
                    [5, 6],
                    [7, 8],
                    [9, 10]]
                ",
                MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal,
                    "[1, 3, 5, 7, 9]",
                    "[2, 4, 6, 8, 10]"
                )
            );
        [Fact] public void Concat04()
            => Assert.Equal(
                @"
                    [[1, 2, 11],
                    [3, 4, 12],
                    [5, 6, 13],
                    [7, 8, 14],
                    [9, 10, 15]]
                ",
                MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal,
                    "[1, 3, 5, 7, 9]",
                    @"
                     [[2, 11],
                      [4, 12],
                      [6, 13],
                      [8, 14],
                      [10, 15]]
                    "
                )
            );
        
        [Fact] public void Concat05()
            => Assert.Equal(
                @"
                    [1, 2, 3, 4, 5, 6, 7]
                ",
                MathS.Matrices.Concat(MathS.Matrices.Direction.Vertical,
                    "[1, 2, 3]",
                    "[4, 5, 6, 7]"
                )
            );
        
        [Fact] public void Concat06()
            => Assert.Throws<BadMatrixShapeException>(
                () =>
                    MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal,
                        "[1, 2, 3]",
                        "[4, 5, 6, 7]"
                    )
            );
        
        [Fact] public void Concat07()
            => Assert.Throws<BadMatrixShapeException>(
                () =>
                    MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal,
                        "[1, 2, 3]",
                        "[4, 5, 6, 7]"
                    )
            );
        
        [Fact] public void Concat08()
            => Assert.Throws<BadMatrixShapeException>(
                () =>
                    MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal,
                        @"[[1, 2], 
                           [3, 4]]",
                        "[1, 2, 3]"
                    )
            );
        
        [Fact] public void Concat09()
            => Assert.Throws<BadMatrixShapeException>(
                () =>
                    MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal,
                        @"[[1, 2], 
                           [3, 4],
                           [5, 6]]",
                        "[[4, 5]]",
                        "[[1]]"
                    )
            );
            
        [Fact] public void Concat10()
            => Assert.Equal(
                @"
                    [[1, 2],
                     [3, 4],
                     [5, 6],
                     [7, 8],
                     [9, 10],
                     [11, 12]]
                ",
                MathS.Matrices.Concat(MathS.Matrices.Direction.Vertical,
                    @"[[1, 2],
                       [3, 4],
                       [5, 6]]",
                       
                    @"[[7, 8],
                       [9, 10]]",
                       
                    @"[[11, 12]]"
                )
            );
        
        [Fact] public void Concat11()
            => Assert.Equal(
                @"
                    [[1, 2, 3, 4,  5,  6],
                     [7, 8, 9, 10, 11, 12]]
                ",
                MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal,
                    @"[[1, 2, 3],
                       [7, 8, 9]]",
                       
                    @"[[4],
                       [10]]",
                       
                    @"[[5,  6],
                       [11, 12]]"
                )
            );
        
        [Fact] public void Concat12()
            => Assert.Equal(
                MathS.Matrix(new Entity[,]
                    {
                        { 1, 2, 3 },
                        { 4, 5, 6 },
                        { 7, 8, 9 }
                    }
                ),
                MathS.Matrix(new Entity[,]
                    {
                        { 1, 2, 3 },
                        { 4, 5, 6 },
                    }
                ).ConcatToTheBottom(
                    MathS.Matrix(new Entity[,]
                        {
                            { 7, 8, 9 }
                        }
                    )
                )
            );
        
        [Fact] public void Concat13()
            => Assert.Equal(
                MathS.Matrix(new Entity[,]
                    {
                        { 1, 2, 3 },
                        { 4, 5, 6 },
                        { 7, 8, 9 }
                    }
                ),
                MathS.Matrix(new Entity[,]
                    {
                        { 1, 2 },
                        { 4, 5 },
                    }
                ).ConcatToTheRight(
                    MathS.Matrix(new Entity[,]
                        {
                            { 3 },
                            { 6 }
                        }
                    )
                ).ConcatToTheBottom(
                    MathS.Matrix(new Entity[,]
                        {
                            { 7, 8, 9 }
                        }
                    )
                )
            );

        // The determinant of a matrix over a commutative ring is a polynomial in
        // its entries, so it is defined wherever the entries are. Computing it by
        // Gaussian elimination left the pivots as literal divisions, which made the
        // returned expression undefined wherever a pivot vanishes -- so substituting
        // an ordinary matrix into a symbolic determinant gave NaN for a determinant
        // that exists. Laplace expansion never divides, so there is nothing to
        // exclude. https://github.com/asc-community/AngouriMath/issues/992
        [Theory]
        // the 2x2 of the issue: the pivot is x, and x = 0 is an ordinary point
        [InlineData("[[x, 1], [2, y]]", "x", 0, "y", 5, -2)]
        [InlineData("[[x, 1], [2, y]]", "x", 3, "y", 4, 10)]
        // a vanishing pivot in the other position
        [InlineData("[[1, x], [y, 0]]", "x", 2, "y", 0, 0)]
        public void SymbolicDeterminantIsDefinedWhereAPivotVanishes(
            string matrixRaw, string a, int aVal, string b, int bVal, int expected)
        {
            Matrix m = matrixRaw;
            var det = TestExtensions.AsNotNull(m.Determinant);
            Assert.Equal(expected, det.Substitute(a, aVal).Substitute(b, bVal).EvalNumerical());
        }

        [Theory]
        // both of these are singular, and both defeated the divided form: the first
        // has a zero in the pivot position, the second a vanishing 2x2 leading minor
        [InlineData("[[0, 1, 2], [3, 4, 5], [6, 7, 8]]", 0)]
        [InlineData("[[1, 2, 3], [2, 4, 6], [1, 1, 1]]", 0)]
        [InlineData("[[1, 2, 3], [4, 5, 6], [7, 8, 10]]", -3)]
        [InlineData("[[2, 1, 0], [1, 2, 1], [0, 1, 2]]", 4)]
        public void GeneralSymbolicDeterminantAgreesWithTheNumericOne(string matrixRaw, int expected)
        {
            Matrix numeric = matrixRaw;
            Matrix symbolic = "[[a, b, c], [d, e, f], [g, h, i]]";
            var det = TestExtensions.AsNotNull(symbolic.Determinant);
            foreach (var (name, value) in new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i" }
                .Select((name, index) => (name, numeric[index / 3, index % 3])))
                det = det.Substitute(name, value);
            Assert.Equal(expected, det.EvalNumerical());
            Assert.Equal(expected, TestExtensions.AsNotNull(numeric.Determinant).EvalNumerical());
        }

        [Theory]
        [InlineData("[[x, 1], [2, y]]", "x * y - 2")]
        [InlineData("[[a, b], [c, d]]", "a * d - b * c")]
        public void SymbolicDeterminantCarriesNoCondition(string matrixRaw, string expected)
        {
            Matrix m = matrixRaw;
            var det = TestExtensions.AsNotNull(m.Determinant).Simplify();
            Assert.DoesNotContain(det.Nodes, node => node is Providedf);
            Assert.Equal(0, ((Entity)expected - det).Simplify().EvalNumerical());
        }

        // Division by a matrix, a power of a matrix and the equality of two matrices each fell
        // through to the elementwise branch every operation shares wherever the operation itself
        // had no answer: A / A was all ones, [1, 2]^2 was [1, 4], and [1, 2] = [1, 3] was the
        // vector [True, False]. Division multiplies by the inverse, as the wiki says it does; a
        // power of anything but a square matrix stays written; and an equality of matrices is
        // one statement. https://github.com/asc-community/AngouriMath/issues/2141
        [Theory]
        [InlineData("[[1, 2], [3, 4]] / [[1, 2], [3, 4]]", "[[1, 0], [0, 1]]")]
        [InlineData("[[2, 1], [7, 4]] / [[1, 2], [3, 4]]", "[[-5/2, 3/2], [-8, 5]]")]
        [InlineData("[1, 2] = [1, 3]", "false")]
        [InlineData("[1, 2] = [1, 2]", "true")]
        [InlineData("[1, 2] = [1, 2, 3]", "false")]
        [InlineData("[a, b] = [c, d]", "a = c and b = d")]
        public void AMatrixIsDividedAndComparedAsAMatrix(string expression, string expected)
        {
            Assert.Equal(expected.ToEntity(), expression.ToEntity().InnerSimplified);
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());
        }

        [Theory]
        [InlineData("[1, 2] ^ 2")]
        [InlineData("[1, 2] * [1, 2]")]
        [InlineData("[[1, 2], [2, 4]] / [[1, 2], [2, 4]]")]
        [InlineData("[[1, 2], [2, 4]] ^ (-1)")]
        [InlineData("1 / [[1, 2], [2, 4]]")]
        [InlineData("2 / [1, 2]")]
        public void WhatIsNotDefinedOnMatricesStaysWritten(string expression)
        {
            Assert.IsNotType<Matrix>(expression.ToEntity().Evaled);
            Assert.IsNotType<Matrix>(expression.ToEntity().Simplify());
        }

        // A number over a matrix is the number times its inverse, so that it agrees with the
        // matrix to the power -1, which Simplify writes it as. A matrix over a number is still
        // spread over the entries.
        [Theory]
        [InlineData("2 / [[1, 2], [3, 4]]", "[[-4, 2], [3, -1]]")]
        [InlineData("[[1, 2], [3, 4]] ^ (-1)", "[[-2, 1], [3/2, -1/2]]")]
        [InlineData("[[1, 2], [3, 4]] / 2", "[[1/2, 1], [3/2, 2]]")]
        public void ANumberOverAMatrixIsItsInverseScaled(string expression, string expected)
        {
            Assert.Equal(expected.ToEntity(), expression.ToEntity().InnerSimplified);
            Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());
        }
    }
}
