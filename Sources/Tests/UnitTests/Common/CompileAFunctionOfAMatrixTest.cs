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
using AngouriMath.Core.Compilation.IntoLinq;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using GenericTensor.Core;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A function of a matrix argument compiles to what evaluation makes of it once the matrix is put in: the
    /// function of each entry, except that a power with a matrix for its base is the matrix power and the modulus of a
    /// column vector is its norm. Vectors pass the delegate's boundary as one-dimensional arrays.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/526">#526</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class CompileAFunctionOfAMatrixTest
    {
        private static readonly double[,] RealA = { { 0.5, 2 }, { 3, -4 } };
        private static readonly double[,] RealB = { { -1, 0.7 }, { 2, 1.5 } };
        private static readonly Complex[,] ComplexA = { { new(1, 1), 2 }, { new(0, -3), -4 } };
        private static readonly Complex[,] ComplexB = { { -1, new(0.5, 2) }, { 2, new(0, 1) } };

        private static Entity AsEntity(Complex[,] a) => MathS.Matrix(a.GetLength(0), a.GetLength(1), (r, c) => (Entity)a[r, c]);
        private static Entity AsEntity(double[,] a) => MathS.Matrix(a.GetLength(0), a.GetLength(1), (r, c) => (Entity)a[r, c]);
        private static Entity AsEntity(Complex[] v) => MathS.Vector(v.Select(z => (Entity)z).ToArray());
        private static Entity AsEntity(double[] v) => MathS.Vector(v.Select(z => (Entity)z).ToArray());

        // What evaluation gives, after putting the arguments in: a matrix entry by entry, or a number as a 1 x 1 matrix.
        // An entry evaluation leaves unevaluated, as max(1 + i, -1), is NaN, which is what a compiled function gives
        // where it has no value.
        private static Complex[,] Evaluated(string expression, params (string Name, Entity Value)[] arguments)
        {
            static Complex Number(Entity entry) => entry.EvaluableNumerical ? entry.EvalNumerical().ToNumerics() : double.NaN;
            var value = arguments.Aggregate(expression.ToEntity(), (e, a) => e.Substitute(a.Name, a.Value)).Evaled;
            if (value is not Entity.Matrix matrix)
                return new[,] { { Number(value) } };
            var result = new Complex[matrix.RowCount, matrix.ColumnCount];
            for (var r = 0; r < matrix.RowCount; r++)
                for (var c = 0; c < matrix.ColumnCount; c++)
                    result[r, c] = Number(matrix[r, c]);
            return result;
        }

        private static Complex[,] AsComplex(double[,] a)
        {
            var result = new Complex[a.GetLength(0), a.GetLength(1)];
            for (var r = 0; r < a.GetLength(0); r++)
                for (var c = 0; c < a.GetLength(1); c++)
                    result[r, c] = a[r, c];
            return result;
        }

        private static Complex[,] Column<T>(T[] v, Func<T, Complex> widen)
        {
            var result = new Complex[v.Length, 1];
            for (var i = 0; i < v.Length; i++)
                result[i, 0] = widen(v[i]);
            return result;
        }

        // Entry by entry. Over double, an entry evaluation gives off the real line has no real value, and is NaN as a
        // compiled function of a real number is there.
        private static void AssertAgrees(Complex[,] expected, Complex[,] actual, bool overReals)
        {
            Assert.Equal(expected.GetLength(0), actual.GetLength(0));
            Assert.Equal(expected.GetLength(1), actual.GetLength(1));
            for (var r = 0; r < expected.GetLength(0); r++)
                for (var c = 0; c < expected.GetLength(1); c++)
                    if (double.IsNaN(expected[r, c].Real))
                        Assert.True(double.IsNaN(actual[r, c].Real), $"entry ({r}, {c}): evaluation gives no value, and compiled gives {actual[r, c]}");
                    else if (overReals && Math.Abs(expected[r, c].Imaginary) > 1e-12 * Math.Max(1, Complex.Abs(expected[r, c])))
                        Assert.True(double.IsNaN(actual[r, c].Real), $"entry ({r}, {c}): evaluation gives {expected[r, c]}, off the real line, and compiled over double gives {actual[r, c]}");
                    else
                        Assert.True(Complex.Abs(expected[r, c] - actual[r, c]) <= 1e-9 * Math.Max(1, Complex.Abs(expected[r, c])),
                            $"entry ({r}, {c}): expected {expected[r, c]}, compiled {actual[r, c]}");
        }

        // Functions of matrix arguments, and of a matrix argument mixed with a number and with a matrix written into
        // the expression, over double and over Complex, at a negative and a non-real x.
        [Theory]
        [InlineData("sin(A)")]
        [InlineData("cos(A) - tan(B)")]
        [InlineData("exp(A)")]
        [InlineData("e ^ A")]
        [InlineData("ln(A)")]
        [InlineData("abs(A)")]
        [InlineData("sgn(A)")]
        [InlineData("arcsin(B)")]
        [InlineData("arctan(A)")]
        [InlineData("conj(A)")]
        [InlineData("sin(A + B)")]
        [InlineData("sin(A) * B")]
        [InlineData("sin(A) ^ 2")]
        [InlineData("exp(A) ^ (-1)")]
        [InlineData("2 ^ A")]
        [InlineData("x ^ A")]
        [InlineData("log(2, A)")]
        [InlineData("log(A, 3)")]
        [InlineData("max(A, B)")]
        [InlineData("min(A, x)")]
        [InlineData("A * sin(x) + B")]
        [InlineData("A + sin(x)")]
        [InlineData("abs(A) * x - B")]
        [InlineData("A + [[1, 2], [3, 4]]")]
        [InlineData("sin(x) * [[1, 2], [3, 4]] * A")]
        [InlineData("A * sin(x) + [[1, 0], [0, 1]]")]
        [InlineData("exp([[x, 1], [0, x]] + A)")]
        [InlineData("[[1, 0]] * A * [[0], [1]]")]
        public void AFunctionOfAMatrixArgumentAgreesWithEvaluation(string expression)
        {
            var e = expression.ToEntity();
            var overReals = e.Compile<double[,], double[,], double, double[,]>("A", "B", "x");
            foreach (var x in new[] { -1.3, 0.7 })
                AssertAgrees(Evaluated(expression, ("A", AsEntity(RealA)), ("B", AsEntity(RealB)), ("x", x)), AsComplex(overReals(RealA, RealB, x)), overReals: true);
            var overComplex = e.Compile<Complex[,], Complex[,], Complex, Complex[,]>("A", "B", "x");
            foreach (var x in new[] { new Complex(-1.3, 0), new Complex(0.5, 1.2) })
                AssertAgrees(Evaluated(expression, ("A", AsEntity(ComplexA)), ("B", AsEntity(ComplexB)), ("x", x)), overComplex(ComplexA, ComplexB, x), overReals: false);
        }

        [Fact]
        public void ATensorArgumentIsMappedAsAnArrayIs()
        {
            var f = "sin(A) + abs(A)".ToEntity().Compile<GenTensor<Complex, ComplexOperations>, GenTensor<Complex, ComplexOperations>>("A");
            var result = TensorCompilation.ToArray(f(GenTensor<Complex, ComplexOperations>.CreateMatrix(ComplexA)));
            AssertAgrees(Evaluated("sin(A) + abs(A)", ("A", AsEntity(ComplexA))), result, overReals: false);
        }

        // [[1, 0]] * A * [[0], [1]] is the entry of A in the first row and the second column. Simplified with A read as a
        // number, it was multiplied out to 0, and that was what the compiled delegate gave.
        [Fact]
        public void A1By1MatrixIsANumber()
        {
            var f = "[[1, 0]] * A * [[0], [1]]".ToEntity().Compile<double[,], double>("A");
            Assert.Equal(2.0, f(RealA));
            var g = "[[1, 0]] * A * [[0], [1]] + x".ToEntity().Compile<Complex[,], Complex, Complex>("A", "x");
            Assert.Equal(new Complex(2, 1), g(ComplexA, Complex.ImaginaryOne));
            // A matrix whose shape is not known to be 1 x 1 is not a number.
            Assert.Throws<UncompilableNodeException>(() => "sin(A) + 1".ToEntity().Compile<double[,], double>("A"));
            Assert.Throws<UncompilableNodeException>(() => "[[1, 0]] * A".ToEntity().Compile<double[,], double>("A"));
        }

        // Vectors taken and given as one-dimensional arrays, columns as the library's vectors are.
        [Theory]
        [InlineData("sin(v)")]
        [InlineData("v * x + [1, 2]")]
        [InlineData("exp(v) - v")]
        [InlineData("A * v")]
        [InlineData("A ^ (-1) * v")]
        [InlineData("abs(A) * v")]
        [InlineData("[[1, 0], [0, 2]] * v + [x, 1]")]
        public void AVectorAgreesWithEvaluation(string expression)
        {
            var e = expression.ToEntity();
            var v = new double[] { 3, -4 };
            var overReals = e.Compile<double[], double[,], double, double[]>("v", "A", "x");
            AssertAgrees(Evaluated(expression, ("v", AsEntity(v)), ("A", AsEntity(RealA)), ("x", -1.3)),
                Column(overReals(v, RealA, -1.3), r => r), overReals: true);
            var cv = new Complex[] { new(1, 1), -2 };
            var overComplex = e.Compile<Complex[], Complex[,], Complex, Complex[]>("v", "A", "x");
            AssertAgrees(Evaluated(expression, ("v", AsEntity(cv)), ("A", AsEntity(ComplexA)), ("x", new Complex(0.5, 1.2))),
                Column(overComplex(cv, ComplexA, new Complex(0.5, 1.2)), z => z), overReals: false);
        }

        // Of a vector, abs is the norm, a number.
        [Theory]
        [InlineData("abs(v)", new double[] { 3, -4 })]
        [InlineData("abs(v) + x", new double[] { 1, -2, 2 })]
        [InlineData("abs(2 v + [1, 1])", new double[] { 3, -4 })]
        [InlineData("abs(A * v)", new double[] { 0.5, -1 })]
        [InlineData("abs(sin(v))", new double[] { 3, -4 })]
        [InlineData("abs(abs(v) * v)", new double[] { 3, -4 })]
        public void TheModulusOfAVectorIsItsNorm(string expression, double[] v)
        {
            var f = expression.ToEntity().Compile<double[], double[,], double, double>("v", "A", "x");
            var expected = Evaluated(expression, ("v", AsEntity(v)), ("A", AsEntity(RealA)), ("x", -1.3))[0, 0];
            Assert.Equal(expected.Real, f(v, RealA, -1.3), 12);
        }

        [Fact]
        public void TheNormScalesAVector()
        {
            var v = new double[] { 3, -4 };
            var f = "abs(v) * v".ToEntity().Compile<double[], double[]>("v");
            AssertAgrees(Evaluated("abs(v) * v", ("v", AsEntity(v))), Column(f(v), r => r), overReals: true);
        }

        // Over the complex numbers the norm is sqrt(sum |v_k|^2), https://github.com/asc-community/AngouriMath/issues/2423,
        // and not the root of the sum of the squares, which is sqrt(4 + 2i) for [1 + i, 2] and 0 for [1 + i, 1 - i].
        [Fact]
        public void TheNormOfAComplexVectorIsTheRootOfTheSumOfTheSquaredModuli()
        {
            var f = "abs(v)".ToEntity().Compile<Complex[], Complex>("v");
            Assert.Equal(Math.Sqrt(6), f(new Complex[] { new(1, 1), 2 }).Real, 12);
            Assert.Equal(2, f(new Complex[] { new(1, 1), new(1, -1) }).Real, 12);
            Assert.Equal(0, f(new Complex[] { new(1, 1), new(1, -1) }).Imaginary);
            // Entries whose squares overflow a double have a norm that does not.
            var huge = "abs(v)".ToEntity().Compile<double[], double>("v")(new[] { 3e200, -4e200 });
            Assert.True(Math.Abs(huge - 5e200) <= 1e-12 * 5e200, huge.ToString());
        }

        // A column or a row passed as a two-dimensional array has a shape known only at the call, and abs of it would be a
        // number where of any other matrix it is a matrix: it is refused at the call. A 1 x 1 matrix passes, its norm and
        // its entry's modulus being one number.
        [Fact]
        public void TheModulusOfAVectorWhoseShapeIsNotKnownIsRefusedAtTheCall()
        {
            var f = "abs(A)".ToEntity().Compile<double[,], double[,]>("A");
            Assert.Throws<InvalidMatrixOperationException>(() => f(new double[,] { { 3 }, { -4 } }));
            Assert.Throws<InvalidMatrixOperationException>(() => f(new double[,] { { 3, -4 } }));
            Assert.Equal(new double[,] { { 3 } }, f(new double[,] { { -3 } }));
            Assert.Equal(new double[,] { { 3, 4 }, { 1, 0.5 } }, f(new double[,] { { -3, 4 }, { 1, -0.5 } }));
            var g = "abs(A) + 1".ToEntity().Compile<Complex[,], Complex[,]>("A");
            Assert.Throws<InvalidMatrixOperationException>(() => g(new Complex[,] { { new(1, 1), 2, -3 } }));
        }

        // sqrt(sum |v_k|^2) of a matrix's entries, written out rather than asked of abs: the norm a row and a column have,
        // https://github.com/asc-community/AngouriMath/issues/2423.
        private static double NormOf(Complex[,] entries)
            => Math.Sqrt(entries.Cast<Complex>().Sum(z => z.Real * z.Real + z.Imaginary * z.Imaginary));

        // Of a row known to be one when compiling, abs is its norm, as a column's is. The row is evaluated, and its norm
        // taken directly, so that the check does not rest on how abs of a row evaluates.
        [Theory]
        [InlineData("abs([[1, 0]] * A)", "[[1, 0]] * A", "")]
        [InlineData("abs([[1, -1]] * A) * x", "[[1, -1]] * A", "* x")]
        [InlineData("abs(sin([[0, 1]] * A))", "sin([[0, 1]] * A)", "")]
        [InlineData("abs(x * [[1, 2]] * B + [[1, 0]] * A)", "x * [[1, 2]] * B + [[1, 0]] * A", "")]
        public void TheModulusOfARowIsItsNorm(string expression, string row, string scaledBy)
        {
            var e = expression.ToEntity();
            double Expected(Entity a, Entity b, Complex x) => NormOf(Evaluated(row, ("A", a), ("B", b), ("x", x))) * (scaledBy == "" ? 1 : x.Real);
            var overReals = e.Compile<double[,], double[,], double, double>("A", "B", "x");
            foreach (var x in new[] { -1.3, 0.7 })
                foreach (var a in new[] { RealA, new double[,] { { -2, 0.5, 3 }, { -1, -4, 0 } } })
                {
                    var b = a.GetLength(1) == 2 ? RealB : new double[,] { { 1, -1, 2 }, { 0, 3, -2 } };
                    Assert.Equal(Expected(AsEntity(a), AsEntity(b), x), overReals(a, b, x), 12);
                }
            var overComplex = e.Compile<Complex[,], Complex[,], Complex, Complex>("A", "B", "x");
            foreach (var x in new[] { new Complex(-1.3, 0), new Complex(2, 0) })
            {
                var value = overComplex(ComplexA, ComplexB, x);
                Assert.Equal(Expected(AsEntity(ComplexA), AsEntity(ComplexB), x), value.Real, 12);
                Assert.Equal(0, value.Imaginary);
            }
        }

        // The row's shape need not be known in full: [[1, 0]] * A is a row whatever the width of A.
        [Fact]
        public void AnyWidthOfRowHasANorm()
        {
            var f = "abs([[1, 0]] * A)".ToEntity().Compile<Complex[,], Complex>("A");
            var a = new Complex[,] { { new(1, 1), -2, new(0, -3), 4 }, { 9, 9, 9, 9 } };
            Assert.Equal(Math.Sqrt(2 + 4 + 9 + 16), f(a).Real, 12);
            // A vector argument stays a column, and abs of it the norm.
            Assert.Equal(Math.Sqrt(6), "abs(v)".ToEntity().Compile<Complex[], Complex>("v")(new Complex[] { new(1, 1), -2 }).Real, 12);
        }

        [Fact]
        public void AMatrixThatIsNotAColumnIsNotAVector()
        {
            Assert.Throws<InvalidMatrixOperationException>(() => "[[x, 1]]".ToEntity().Compile<double, double[]>("x"));
            Assert.Throws<InvalidMatrixOperationException>(() => "[[x, 1], [2, x]]".ToEntity().Compile<double, double[]>("x"));
            // Of an argument, the shape is known at the call.
            var f = "sin(A)".ToEntity().Compile<double[,], double[]>("A");
            Assert.Throws<InvalidMatrixOperationException>(() => f(RealA));
            Assert.Equal(new[] { Math.Sin(1), Math.Sin(2) }, f(new double[,] { { 1 }, { 2 } }));
            Assert.Throws<InvalidMatrixOperationException>(() => "v + 1".ToEntity().Compile<double[], double[]>("v")(new double[0]));
            Assert.Throws<UncompilableNodeException>(() => "sin(v)".ToEntity().Compile<double[], double>("v"));
            Assert.Throws<UncompilableNodeException>(() => "x + 1".ToEntity().Compile<double, double[]>("x"));
        }

        // What evaluation does not apply entry by entry is not compiled so: a power with a matrix for its base and an
        // exponent that is not whole is left unevaluated, a matrix to the power of a matrix too, equality of two
        // matrices is one statement, and a comparison of each entry is a matrix of statements.
        [Fact]
        public void WhatEvaluationDoesNotMapIsNotMapped()
        {
            Assert.Throws<InvalidMatrixOperationException>(() => "sqrt(A)".ToEntity().Compile<double[,], double[,]>("A")(RealA));
            Assert.Throws<InvalidMatrixOperationException>(() => "A ^ (1/3)".ToEntity().Compile<Complex[,], Complex[,]>("A")(ComplexA));
            Assert.Throws<UncompilableNodeException>(() => "A ^ B".ToEntity().Compile<double[,], double[,], double[,]>("A", "B"));
            Assert.Throws<UncompilableNodeException>(() => "A < 0".ToEntity().Compile<double[,], double[,]>("A"));
            Assert.Throws<UncompilableNodeException>(() => "A = B".ToEntity().Compile<double[,], double[,], bool>("A", "B"));
            Assert.Throws<UncompilableNodeException>(() => "not A".ToEntity().Compile<double[,], double[,]>("A"));
        }

        // Two matrices of different shapes have no entry-by-entry function; evaluation leaves it unevaluated.
        [Fact]
        public void ShapesThatDoNotFitAreRefused()
        {
            Assert.Throws<InvalidMatrixOperationException>(() => "max([[x, 1]], [[x], [2]])".ToEntity().Compile<double, double[,]>("x"));
            var f = "log(A, B)".ToEntity().Compile<double[,], double[,], double[,]>("A", "B");
            Assert.Throws<InvalidMatrixOperationException>(() => f(RealA, new double[,] { { 2, 3 } }));
        }
    }
}
