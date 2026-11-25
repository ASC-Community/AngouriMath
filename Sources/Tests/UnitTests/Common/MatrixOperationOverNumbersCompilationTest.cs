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
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A scalar or a vector product, a determinant, a transpose, a trace or an entry of a name beside a written matrix,
    /// compiled with that name a number. Simplification leaves each as written, since the name may stand for a matrix;
    /// a compiled variable has a type, and one compiled as a number is one. The FastExpression compiler had no form for
    /// any of them and refused them, the LINQ compiler converted the number to the whole numbers of the written matrix
    /// and truncated it, and both read a transpose or a vector product beside a written matrix as a number. Every value
    /// is checked against the definition written out here, at real, negative and non-real points.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/105">#105</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class MatrixOperationOverNumbersCompilationTest
    {
        private static void Near(Complex expected, Complex actual)
            => Assert.True(Complex.Abs(expected - actual) <= 1e-9 * Math.Max(1, Complex.Abs(expected)), $"expected {expected}, got {actual}");

        private static Complex[] Cross(Complex[] u, Complex[] v)
            => new[] { u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0] };

        private static Complex Dot(Complex[] u, Complex[] v) => u.Zip(v, (a, b) => a * b).Aggregate(Complex.Zero, (s, t) => s + t);

        // The inclination reference's I_x and I_y take the third component of a vector built from vectors: a rotation of
        // v about g through p, cos(p) v + sin(p) / |g| (g x v). Its third component, by the definition: cos(p) z +
        // sin(p) / |g| (a y - b x), |g| being the norm sqrt(|a|^2 + |b|^2 + |c|^2) that abs takes of a vector.
        private const string Rotated = "cos(p) * [x, y, z] + sin(p) / abs([a, b, c]) * cross([a, b, c], [x, y, z])";

        private static Complex ThirdComponent(Complex a, Complex b, Complex c, Complex p, Complex x, Complex y, Complex z)
            => Complex.Cos(p) * z + Complex.Sin(p) / Math.Sqrt(Complex.Abs(a) * Complex.Abs(a) + Complex.Abs(b) * Complex.Abs(b) + Complex.Abs(c) * Complex.Abs(c)) * (a * y - b * x);

        private static readonly Complex[][] Points =
        {
            new Complex[] { 0.3, 1, 1.7, 2.4, 3.1, 3.8, 4.5 },
            new Complex[] { -1.5, 0.25, -4, -0.7, 2, -3, 0.5 },
            new Complex[] { new(1, 2), -3, new(0, -1), new(0.5, 0.25), new(2.5, 0.5), new(-1, 1), 2 },
        };

        [Theory]
        [InlineData("dot([0, 0, 1], " + Rotated + ")")]
        [InlineData("dot(" + Rotated + ", [0, 0, 1])")]
        [InlineData("[0, 0, 1]T * (" + Rotated + ")")]
        public void TheThirdComponentOfAVectorExpressionCompilesToAFastExpression(string expression)
        {
            var f = expression.ToEntity().Compile("a", "b", "c", "p", "x", "y", "z");
            foreach (var at in Points)
                Near(ThirdComponent(at[0], at[1], at[2], at[3], at[4], at[5], at[6]), f.Call(at));
        }

        [Fact]
        public void TheThirdComponentOfAVectorExpressionCompilesOverTheReals()
        {
            var f = ("dot([0, 0, 1], " + Rotated + ")").ToEntity().Compile<double, double, double, double, double, double, double, double>("a", "b", "c", "p", "x", "y", "z");
            foreach (var at in Points.Take(2))
            {
                var r = at.Select(c => c.Real).ToArray();
                Near(ThirdComponent(at[0], at[1], at[2], at[3], at[4], at[5], at[6]), f(r[0], r[1], r[2], r[3], r[4], r[5], r[6]));
            }
        }

        // Of a vector product of vectors built from names, compiled where the whole is a number.
        [Theory]
        [InlineData("dot([1, 2, 3], cross(x * [1, 0, -1] + [0, y, 0], [z, 1, x]))")]
        [InlineData("[1, 2, 3]T * cross(x * [1, 0, -1] + [0, y, 0], [z, 1, x])")]
        public void AVectorProductOfAVectorExpressionCompiles(string expression)
        {
            var f = expression.ToEntity().Compile("x", "y", "z");
            var g = expression.ToEntity().Compile<Complex, Complex, Complex, Complex>("x", "y", "z");
            foreach (var (x, y, z) in new (Complex, Complex, Complex)[] { (0.5, 2, -1), (-1.25, 0.5, 3), (new(1, 1), new(0, -2), 0.5) })
            {
                var expected = Dot(new Complex[] { 1, 2, 3 }, Cross(new[] { x, y, -x }, new[] { z, 1, x }));
                Near(expected, f.Call(x, y, z));
                Near(expected, g(x, y, z));
            }
        }

        // det(x M) = x^2 det(M), trace(x M) = x trace(M), the (2, 1) entry of x M is 3 x, and the (1, 2) entry of its
        // transpose too. The transpose beside written matrices compiled to 0 in both compilers, read as a number.
        [Theory]
        [InlineData("det(x * [[1, 2], [3, 4]])", -2, 2)]
        [InlineData("det(transpose(x * [[1, 2], [3, 4]]))", -2, 2)]
        [InlineData("trace(x * [[1, 2], [3, 4]])", 5, 1)]
        [InlineData("(x * [[1, 2], [3, 4]])[2, 1]", 3, 1)]
        [InlineData("[1, 0]T * transpose(x * [[1, 2], [3, 4]]) * [0, 1]", 3, 1)]
        [InlineData("dot(x * [1, 2], [3, 4])", 11, 1)]
        [InlineData("dot([0, 0, 1], x * [1, 2, 3])", 3, 1)]
        public void AMatrixOperationOfANameBesideAWrittenMatrixCompiles(string expression, double coefficient, int power)
        {
            var e = expression.ToEntity();
            var fast = e.Compile("x");
            var overReals = e.Compile<double, double>("x");
            var overComplex = e.Compile<Complex, Complex>("x");
            foreach (var x in new Complex[] { 0.5, -1.5, 2, new(0.25, -1) })
            {
                var expected = coefficient * Complex.Pow(x, power);
                Near(expected, fast.Call(x));
                Near(expected, overComplex(x));
                if (x.Imaginary == 0)
                    Near(expected, overReals(x.Real));
            }
        }

        // A number beside a matrix of whole numbers, as an argument, widens it; it was converted to a whole number, and
        // 0.5 times the matrix was the zero matrix.
        [Fact]
        public void ANumberBesideAMatrixOfWholeNumbersIsNotTruncated()
        {
            var a = new long[,] { { 1, 2 }, { 3, 4 } };
            Assert.Equal(-0.5, "det(x * A)".ToEntity().Compile<double, long[,], double>("x", "A")(0.5, a), 12);
            Assert.Equal(1.5, "(x * A)[2, 1]".ToEntity().Compile<double, long[,], double>("x", "A")(0.5, a), 12);
            Assert.Equal(2.5, "trace(A / x)".ToEntity().Compile<double, long[,], double>("x", "A")(2, a), 12);
            Assert.Equal(new[,] { { 0.5, 1 }, { 1.5, 2 } }, "x * A".ToEntity().Compile<double, long[,], double[,]>("x", "A")(0.5, a));
            // A complex number beside a matrix over double, which was refused.
            Near(new Complex(0, 5), "dot(z * a, [1, 2])".ToEntity().Compile<Complex, double[], Complex>("z", "a")(Complex.ImaginaryOne, new double[] { 1, 2 }));
        }

        // Two vectors over different element types are widened to the wider, as two matrices added are.
        [Fact]
        public void TwoVectorsOverDifferentTypesAreWidened()
        {
            var u = new double[] { 1, 2, 3 };
            var v = new Complex[] { Complex.ImaginaryOne, 1, new(2, -1) };
            var dot = "dot(a, b)".ToEntity().Compile<double[], Complex[], Complex>("a", "b");
            Near(Dot(u.Select(c => (Complex)c).ToArray(), v), dot(u, v));
            var cross = "cross(a, b)".ToEntity().Compile<double[], Complex[], Complex[]>("a", "b");
            var expected = Cross(u.Select(c => (Complex)c).ToArray(), v);
            var actual = cross(u, v);
            for (var i = 0; i < 3; i++)
                Near(expected[i], actual[i]);
            var crossOfWholeNumbers = "cross([1, 0, 0], x * [0, 1, 0])".ToEntity().Compile<double, double[]>("x");
            Assert.Equal(new[] { 0, 0, 0.5 }, crossOfWholeNumbers(0.5));
        }

        // Of a number there is no product, determinant or transpose: refused, where the transpose beside written
        // matrices was read as a number and compiled to 0.
        [Theory]
        [InlineData("dot(a, b)")]
        [InlineData("cross(a, b)")]
        [InlineData("det(a)")]
        [InlineData("trace(a) + b")]
        [InlineData("[1, 0]T * transpose(a) * [0, 1]")]
        [InlineData("[0, 0, 1]T * cross(a, b)")]
        public void AMatrixOperationOfANumberIsRefused(string expression)
        {
            var e = expression.ToEntity();
            var vars = e.Vars.ToArray();
            Assert.Contains("is a number where its variables are compiled as numbers", Assert.Throws<UncompilableNodeException>(() => e.Compile(vars)).Message);
            Assert.Throws<UncompilableNodeException>(() => IntoLinq(e, vars.Length));
        }

        private static Delegate IntoLinq(Entity e, int count) => count switch
        {
            1 => e.Compile<double, double>("a"),
            _ => e.Compile<double, double, double>("a", "b"),
        };

        // A FastExpression computes numbers, so what is not carried out when compiling is refused, saying where the
        // matrices can be had, rather than asking for a bug report.
        [Fact]
        public void WhatAFastExpressionCannotCarryOutIsRefusedAndSaysWhy()
        {
            var ofDifferentLengths = Assert.Throws<UncompilableNodeException>(() => MathS.Matrices.DotProduct("[x, 2]", "[1, 2, 3]").Compile("x"));
            Assert.Contains("double[]", ofDifferentLengths.Message);
            Assert.DoesNotContain("report", ofDifferentLengths.Message);
            // An index known only when called: the LINQ compiler indexes the matrix then.
            var atAnIndex = "([[1, 2], [3, 4]] * x)[k, 1]".ToEntity();
            Assert.Contains("index", Assert.Throws<UncompilableNodeException>(() => atAnIndex.Compile("k", "x")).Message);
            Assert.Equal(1.5, atAnIndex.Compile<double, double, double>("k", "x")(2, 0.5), 12);
        }

        // A name compiled as a matrix is not read as a number: the operation over it is compiled on the matrix.
        [Fact]
        public void ANameCompiledAsAMatrixIsNotReadAsANumber()
        {
            var a = new double[,] { { 1, 2 }, { 3, 4 } };
            Assert.Equal(-0.5, "det(x * A)".ToEntity().Compile<double, double[,], double>("x", "A")(0.5, a), 12);
            Assert.Equal(3, "[1, 0]T * transpose(A) * [0, 1]".ToEntity().Compile<double[,], double>("A")(a), 12);
            // x a = [1.5, 1.5], whose scalar product with [1, 2] is 4.5, and det(A) = 4.
            Assert.Equal(8.5, "dot(x * a, [1, 2]) + det(A)".ToEntity().Compile<double, double[], double[,], double>("x", "a", "A")(0.5, new double[] { 3, 3 }, new double[,] { { 2, 0 }, { 0, 2 } }), 12);
        }
    }
}
