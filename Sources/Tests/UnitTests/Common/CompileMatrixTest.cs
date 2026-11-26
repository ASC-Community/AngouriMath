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
    /// A matrix compiles to a <see cref="GenTensor{T, TWrapper}"/> over its elements' type, and
    /// the arithmetic on it to GenericTensor's operations: matrices are values a compiled
    /// delegate takes and returns. <a href="https://github.com/asc-community/AngouriMath/issues/526">#526</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class CompileMatrixTest
    {
        private static GenTensor<double, DoubleOperations> Doubles(params double[][] rows)
            => GenTensor<double, DoubleOperations>.CreateMatrix(rows.Length, rows[0].Length, (r, c) => rows[r][c]);

        private static void AssertSame(GenTensor<double, DoubleOperations> expected, GenTensor<double, DoubleOperations> actual)
        {
            Assert.Equal(expected.Shape, actual.Shape);
            for (var r = 0; r < expected.Shape[0]; r++)
                for (var c = 0; c < expected.Shape[1]; c++)
                    Assert.Equal(expected[r, c], actual[r, c], 12);
        }

        [Fact]
        public void AMatrixOfExpressionsCompilesToATensor()
        {
            var f = "[[x, 2 x], [x + 1, x^2]]".ToEntity().Compile<double, GenTensor<double, DoubleOperations>>("x");
            AssertSame(Doubles(new[] { 3.0, 6.0 }, new[] { 4.0, 9.0 }), f(3));
        }

        [Fact]
        public void TheProductOfTwoMatrixArgumentsIsTheMatrixProduct()
        {
            var f = "A * B".ToEntity().Compile<GenTensor<double, DoubleOperations>, GenTensor<double, DoubleOperations>, GenTensor<double, DoubleOperations>>("A", "B");
            var a = Doubles(new[] { 1.0, 2.0 }, new[] { 3.0, 4.0 });
            var b = Doubles(new[] { 0.0, 1.0 }, new[] { 1.0, 0.0 });
            AssertSame(Doubles(new[] { 2.0, 1.0 }, new[] { 4.0, 3.0 }), f(a, b));
        }

        [Fact]
        public void ScalarsAndMatricesMix()
        {
            var f = "2 A + B - 1".ToEntity().Compile<GenTensor<double, DoubleOperations>, GenTensor<double, DoubleOperations>, GenTensor<double, DoubleOperations>>("A", "B");
            var a = Doubles(new[] { 1.0, 2.0 }, new[] { 3.0, 4.0 });
            var b = Doubles(new[] { 10.0, 10.0 }, new[] { 10.0, 10.0 });
            AssertSame(Doubles(new[] { 11.0, 13.0 }, new[] { 15.0, 17.0 }), f(a, b));
        }

        [Fact]
        public void AScalarVariableScalesALiteralMatrix()
        {
            var f = "x * [[1, 2], [3, 4]] / 2".ToEntity().Compile<double, GenTensor<double, DoubleOperations>>("x");
            AssertSame(Doubles(new[] { 2.0, 4.0 }, new[] { 6.0, 8.0 }), f(4));
        }

        [Fact]
        public void AMatrixPowerIsTheMatrixPower()
        {
            var f = "A ^ 3".ToEntity().Compile<GenTensor<double, DoubleOperations>, GenTensor<double, DoubleOperations>>("A");
            var a = Doubles(new[] { 1.0, 1.0 }, new[] { 0.0, 1.0 });
            AssertSame(Doubles(new[] { 1.0, 3.0 }, new[] { 0.0, 1.0 }), f(a));
        }

        [Fact]
        public void AnExpressionOfMatricesWithAScalarValueStillCompilesToTheScalar()
        {
            // #425's case: the matrix is simplified away before compiling, as before.
            var f = "[[1, 0]] * [[a, b], [c, d]] * [[0], [1]]".ToEntity().Compile<double, double, double, double, double>("a", "b", "c", "d");
            Assert.Equal(2.0, f(1, 2, 3, 4));
        }

        [Fact]
        public void ComplexAndIntegerElementsHaveTheirOwnTensors()
        {
            var g = "[[x, i x]]".ToEntity().Compile<Complex, GenTensor<Complex, ComplexOperations>>("x");
            var z = g(new Complex(1, 1));
            Assert.Equal(new Complex(1, 1), z[0, 0]);
            Assert.Equal(new Complex(-1, 1), z[0, 1]);
            var h = "[[n, 2 n]]".ToEntity().Compile<long, GenTensor<long, Int64Operations>>("n");
            Assert.Equal(6L, h(3)[0, 1]);
            Assert.Equal(typeof(GenTensor<double, DoubleOperations>), TensorCompilation.TensorTypeOf(typeof(double)));
            Assert.Null(TensorCompilation.TensorTypeOf(typeof(string)));
        }

        // The value evaluation gives each entry, after substituting the arguments.
        private static Complex[,] Evaluated(Entity expression, params (string Name, Entity Value)[] arguments)
        {
            var value = arguments.Aggregate(expression, (e, a) => e.Substitute(a.Name, a.Value)).Evaled;
            var matrix = Assert.IsType<Entity.Matrix>(value);
            var result = new Complex[matrix.RowCount, matrix.ColumnCount];
            for (var r = 0; r < matrix.RowCount; r++)
                for (var c = 0; c < matrix.ColumnCount; c++)
                    result[r, c] = matrix[r, c].EvalNumerical().ToNumerics();
            return result;
        }

        private static void AssertAgrees(Complex[,] expected, Complex[,] actual)
        {
            Assert.Equal(expected.GetLength(0), actual.GetLength(0));
            Assert.Equal(expected.GetLength(1), actual.GetLength(1));
            for (var r = 0; r < expected.GetLength(0); r++)
                for (var c = 0; c < expected.GetLength(1); c++)
                    Assert.True(Complex.Abs(expected[r, c] - actual[r, c]) <= 1e-9 * Math.Max(1, Complex.Abs(expected[r, c])),
                        $"entry ({r}, {c}): expected {expected[r, c]}, compiled {actual[r, c]}");
        }

        private static Complex[,] AsComplex(double[,] a)
        {
            var result = new Complex[a.GetLength(0), a.GetLength(1)];
            for (var r = 0; r < a.GetLength(0); r++)
                for (var c = 0; c < a.GetLength(1); c++)
                    result[r, c] = a[r, c];
            return result;
        }

        private static Entity.Matrix AsEntity(Complex[,] a) => MathS.Matrix(a.GetLength(0), a.GetLength(1), (r, c) => (Entity)a[r, c]);

        // Matrix-valued expressions of two scalars, compiled to two-dimensional arrays and compared, entry by entry, with
        // evaluation at negative, positive and (over Complex) non-real points.
        [Theory]
        [InlineData("[[x, 1], [2, y^2]]")]
        [InlineData("[x, 2 y]")]
        [InlineData("[x, y]T")]
        [InlineData("[[x, 1], [2, y]] + [[1, x], [y, 1]]")]
        [InlineData("x - [[x, 1], [2, y]]")]
        [InlineData("[[x, 1], [2, y]] * [[x], [y]]")]
        [InlineData("[x, y]T * [[1, x], [y, 1]]")]
        [InlineData("[x, y] * [x, y]T")]
        [InlineData("x * [[1, 2], [3, y]]")]
        [InlineData("transpose([[x, 1], [2, y]])")]
        [InlineData("transpose([[x, 1], [2, y]]) * [[x, 1], [2, y]]")]
        [InlineData("[[x, 1], [0, y]] ^ 3")]
        [InlineData("[[x, 1], [0, y]] ^ (-1)")]
        [InlineData("1 / [[x, 1], [0, y]]")]
        [InlineData("cross([x, 1, 0], [0, y, x])")]
        public void AMatrixValueAgreesWithEvaluation(string expression)
        {
            var e = expression.ToEntity();
            var overReals = e.Compile<double, double, double[,]>("x", "y");
            foreach (var (x, y) in new[] { (0.7, -1.3), (-1.3, 2.5), (2.5, 0.7) })
                AssertAgrees(Evaluated(e, ("x", x), ("y", y)), AsComplex(overReals(x, y)));
            var overComplex = e.Compile<Complex, Complex, Complex[,]>("x", "y");
            foreach (var (x, y) in new[] { (new Complex(-1.3, 0), new Complex(2.5, 0)), (new Complex(0.5, 1.2), new Complex(-0.3, 0.4)) })
                AssertAgrees(Evaluated(e, ("x", x), ("y", y)), overComplex(x, y));
        }

        // Matrices passed in, as arrays and as tensors, over double and Complex.
        [Theory]
        [InlineData("A + B")]
        [InlineData("A - 2 B")]
        [InlineData("A * B")]
        [InlineData("2 A - 1")]
        [InlineData("A ^ 3")]
        [InlineData("A ^ (-1)")]
        [InlineData("B ^ (-2)")]
        [InlineData("A / B")]
        [InlineData("1 / A")]
        [InlineData("A * B * A")]
        public void AMatrixArgumentAgreesWithEvaluation(string expression)
        {
            var e = expression.ToEntity();
            var a = new double[,] { { 1, 2 }, { 3, -4 } };
            var b = new double[,] { { -1, 0.5 }, { 2, 0 } };
            var overReals = e.Compile<double[,], double[,], double[,]>("A", "B");
            AssertAgrees(Evaluated(e, ("A", AsEntity(AsComplex(a))), ("B", AsEntity(AsComplex(b)))), AsComplex(overReals(a, b)));
            var ca = new Complex[,] { { new(1, 1), 2 }, { new(0, -3), -4 } };
            var cb = new Complex[,] { { -1, new(0.5, 2) }, { 2, new(0, 1) } };
            var overComplex = e.Compile<Complex[,], Complex[,], Complex[,]>("A", "B");
            AssertAgrees(Evaluated(e, ("A", AsEntity(ca)), ("B", AsEntity(cb))), overComplex(ca, cb));
            var asTensors = e.Compile<GenTensor<Complex, ComplexOperations>, GenTensor<Complex, ComplexOperations>, GenTensor<Complex, ComplexOperations>>("A", "B");
            var tensor = asTensors(GenTensor<Complex, ComplexOperations>.CreateMatrix(ca), GenTensor<Complex, ComplexOperations>.CreateMatrix(cb));
            AssertAgrees(overComplex(ca, cb), TensorCompilation.ToArray(tensor));
        }

        // Shapes written into the expression are checked when compiling; simplification leaves these as written.
        [Theory]
        [InlineData("[[x, 1]] + [[x], [y]]")]
        [InlineData("[[x, 1]] - [[x], [y]]")]
        [InlineData("[[x, 1]] * [[x, y]]")]
        [InlineData("[[x, 1, y]] ^ 2")]
        [InlineData("x / [[x, 1]]")]
        [InlineData("x * ([[x, 1]] + [[x], [y]]) + 1")]
        public void ShapesThatDoNotFitAreRefusedWhenCompiling(string expression)
        {
            Assert.Throws<InvalidMatrixOperationException>(() => expression.ToEntity().Compile<double, double, double[,]>("x", "y"));
            Assert.Throws<InvalidMatrixOperationException>(() => expression.ToEntity().Compile<Complex, Complex, Complex[,]>("x", "y"));
        }

        // An argument's shape is known only when the delegate is called, and is checked then.
        [Theory]
        [InlineData("A + B")]
        [InlineData("A * B")]
        [InlineData("B ^ 2")]
        [InlineData("1 / B")]
        public void ArgumentShapesThatDoNotFitAreRefusedWhenCalled(string expression)
        {
            var f = expression.ToEntity().Compile<double[,], double[,], double[,]>("A", "B");
            Assert.Throws<InvalidMatrixOperationException>(() => f(new double[,] { { 1, 2 }, { 3, 4 } }, new double[,] { { 1, 2, 3 } }));
        }

        // A matrix power was compiled by a cast of the exponent, which truncates: A ^ x at 0.7 was the identity.
        // Evaluation leaves a matrix to a power that is not whole unevaluated.
        [Fact]
        public void AMatrixPowerTakesAWholeExponentOnly()
        {
            var f = "[[x, 1], [0, 2]] ^ x".ToEntity().Compile<double, double[,]>("x");
            Assert.Throws<InvalidMatrixOperationException>(() => f(0.7));
            Assert.Throws<InvalidMatrixOperationException>(() => f(double.NaN));
            AssertAgrees(Evaluated("[[x, 1], [0, 2]] ^ x".ToEntity(), ("x", 3)), AsComplex(f(3)));
            AssertAgrees(Evaluated("[[x, 1], [0, 2]] ^ x".ToEntity(), ("x", -2)), AsComplex(f(-2)));
            var g = "A ^ n".ToEntity().Compile<Complex[,], Complex, Complex[,]>("A", "n");
            var a = new Complex[,] { { 1, new(0, 1) }, { 0, 2 } };
            AssertAgrees(Evaluated("A ^ 2".ToEntity(), ("A", AsEntity(a))), g(a, 2));
            Assert.Throws<InvalidMatrixOperationException>(() => g(a, new Complex(2, 1)));
        }

        // A singular matrix has no inverse, and a compiled division by zero has no value: every entry is NaN.
        [Fact]
        public void ASingularMatrixHasNoInverse()
        {
            var inverse = "A ^ (-1)".ToEntity().Compile<double[,], double[,]>("A")(new double[,] { { 1, 2 }, { 2, 4 } });
            Assert.All(inverse.Cast<double>(), entry => Assert.True(double.IsNaN(entry)));
            var overComplex = "1 / A".ToEntity().Compile<Complex[,], Complex[,]>("A")(new Complex[,] { { 1, new(0, 1) }, { new(0, 1), -1 } });
            Assert.All(overComplex.Cast<Complex>(), entry => Assert.True(double.IsNaN(entry.Real)));
        }

        // The inverse of a matrix of whole numbers is computed over double, as a quotient of two whole numbers is, and is
        // not a matrix of whole numbers: asked for as one, it is refused. A negative exponent that arrives only at the
        // call finds the power compiled over the whole numbers, and has no inverse there.
        [Fact]
        public void TheInverseOfAMatrixOfWholeNumbersIsNotWhole()
        {
            Assert.Throws<UncompilableNodeException>(() => "1 / A".ToEntity().Compile<long[,], long[,]>("A"));
            Assert.Throws<UncompilableNodeException>(() => "A ^ (-1)".ToEntity().Compile<long[,], long[,]>("A"));
            var power = "A ^ n".ToEntity().Compile<long[,], long, long[,]>("A", "n");
            Assert.Equal(new long[,] { { 1, 4 }, { 0, 1 } }, power(new long[,] { { 1, 2 }, { 0, 1 } }, 2));
            Assert.Throws<InvalidMatrixOperationException>(() => power(new long[,] { { 1, 2 }, { 0, 1 } }, -1));
        }

        private static Complex[,] AsComplex<T>(T[,] a, Func<T, Complex> entry)
        {
            var result = new Complex[a.GetLength(0), a.GetLength(1)];
            for (var r = 0; r < a.GetLength(0); r++)
                for (var c = 0; c < a.GetLength(1); c++)
                    result[r, c] = entry(a[r, c]);
            return result;
        }

        private static void Near(Complex expected, Complex actual, string where)
            => Assert.True(Complex.Abs(expected - actual) <= 1e-9 * Math.Max(1, Complex.Abs(expected)), $"{where}: expected {expected}, compiled {actual}");

        private static readonly long[][,] WholeMatrices =
        {
            new long[,] { { 1, 2 }, { 3, 4 } },
            new long[,] { { -3, 5 }, { 7, -2 } },
        };

        // A matrix argument over the whole numbers is computed over them only where the value stays whole. A constant
        // was converted to the matrix's element type, so 0.5 was 0, pi 3 and sqrt(2) 2 ^ 0; and a quotient divided each
        // entry with truncation, where a quotient of two whole numbers is computed over double. trace(A * 0.5) was 0 and
        // trace(A / 2) was 2 at A = [[1, 2], [3, 4]], where both are 2.5. Checked against evaluation with A substituted,
        // over long, int and BigInteger.
        [Theory]
        [InlineData("trace(A * 0.5)")]
        [InlineData("trace(0.5 * A)")]
        [InlineData("trace(A / 2)")]
        [InlineData("trace(A / 3)")]
        [InlineData("det(A / 2)")]
        [InlineData("(A / 2)[2, 1]")]
        [InlineData("trace(A) * 0.5")]
        [InlineData("trace(pi * A)")]
        [InlineData("trace(sqrt(2) * A)")]
        [InlineData("[[0.5, 1]] * A * [[1], [0]]")]
        [InlineData("trace(A + 1 / 4)")]
        [InlineData("trace(1 / A)")]
        [InlineData("trace(A ^ (-1))")]
        [InlineData("det(A ^ (-2))")]
        [InlineData("trace(2 / A + A / 4)")]
        public void ANumberOfAMatrixOfWholeNumbersAgreesWithEvaluation(string expression)
        {
            var e = expression.ToEntity();
            var overLongs = e.Compile<long[,], double>("A");
            var overInts = e.Compile<int[,], double>("A");
            var overBigIntegers = e.Compile<BigInteger[,], double>("A");
            foreach (var a in WholeMatrices)
            {
                var expected = e.Substitute("A", AsEntity(AsComplex(a, v => v))).Evaled.EvalNumerical().ToNumerics();
                Near(expected, overLongs(a), $"long, A = {AsEntity(AsComplex(a, v => v))}");
                Near(expected, overInts(AsInts(a)), $"int, A = {AsEntity(AsComplex(a, v => v))}");
                Near(expected, overBigIntegers(AsBigIntegers(a)), $"BigInteger, A = {AsEntity(AsComplex(a, v => v))}");
            }
        }

        private static int[,] AsInts(long[,] a)
        {
            var result = new int[a.GetLength(0), a.GetLength(1)];
            for (var r = 0; r < a.GetLength(0); r++)
                for (var c = 0; c < a.GetLength(1); c++)
                    result[r, c] = checked((int)a[r, c]);
            return result;
        }

        private static BigInteger[,] AsBigIntegers(long[,] a)
        {
            var result = new BigInteger[a.GetLength(0), a.GetLength(1)];
            for (var r = 0; r < a.GetLength(0); r++)
                for (var c = 0; c < a.GetLength(1); c++)
                    result[r, c] = a[r, c];
            return result;
        }

        // The same, where the value is a matrix: it is a matrix over double. "A / 2" over long[,] asked for as double[,]
        // was refused, the quotient being a matrix over long, and [[0.5, 1]] * A was [[3, 4]].
        [Theory]
        [InlineData("A / 2")]
        [InlineData("A * 0.5 + 1")]
        [InlineData("[[0.5, 1]] * A")]
        [InlineData("1 / A")]
        [InlineData("A ^ (-1)")]
        [InlineData("A / 4 - 2 / A")]
        public void AMatrixOfWholeNumbersAgreesWithEvaluation(string expression)
        {
            var e = expression.ToEntity();
            var overLongs = e.Compile<long[,], double[,]>("A");
            foreach (var a in WholeMatrices)
                AssertAgrees(Evaluated(e, ("A", AsEntity(AsComplex(a, v => v)))), AsComplex(overLongs(a)));
        }

        // A quotient of two matrices of whole numbers, and of one by a whole number passed in, is computed over double
        // as the quotient by a constant is.
        [Fact]
        public void AQuotientOfArgumentsOverTheWholeNumbersAgreesWithEvaluation()
        {
            var (a, b) = (WholeMatrices[0], WholeMatrices[1]);
            var (entityA, entityB) = (AsEntity(AsComplex(a, v => v)), AsEntity(AsComplex(b, v => v)));
            var quotient = "A / B".ToEntity();
            AssertAgrees(Evaluated(quotient, ("A", entityA), ("B", entityB)), AsComplex(quotient.Compile<long[,], long[,], double[,]>("A", "B")(a, b)));
            var byANumber = "trace(A / n)".ToEntity();
            Near(byANumber.Substitute("A", entityA).Substitute("n", 2).Evaled.EvalNumerical().ToNumerics(),
                byANumber.Compile<long[,], long, double>("A", "n")(a, 2), "trace(A / n) at n = 2");
        }

        // What stays whole is still computed over the whole numbers, exactly, where a double would round: 2 A + 1 at an
        // entry of 2 ^ 60 is 2 ^ 61 + 1, which a double does not hold. A quotient or a constant that is not whole, asked
        // for as a matrix of whole numbers, is refused rather than truncated; a power that is not whole has no value, as
        // over double, where 0.5 was a whole exponent of 0 and A ^ 0.5 was the identity.
        [Fact]
        public void AMatrixOfWholeNumbersStaysWholeWhereItIs()
        {
            var a = new long[,] { { 1L << 60, 1 }, { 0, 1 } };
            Assert.Equal(new long[,] { { (1L << 61) + 1, 3 }, { 1, 3 } }, "2 A + 1".ToEntity().Compile<long[,], long[,]>("A")(a));
            Assert.Equal((1L << 60) + 1, "trace(A)".ToEntity().Compile<long[,], long>("A")(a));
            Assert.Throws<UncompilableNodeException>(() => "A / 2".ToEntity().Compile<long[,], long[,]>("A"));
            Assert.Throws<UncompilableNodeException>(() => "A * 0.5".ToEntity().Compile<long[,], long[,]>("A"));
            Assert.Throws<InvalidMatrixOperationException>(() => "A ^ 0.5".ToEntity().Compile<long[,], long[,]>("A")(WholeMatrices[0]));
        }

        // A row times a matrix argument times a column is a 1 x 1 matrix, an entry of A, and a number. Simplified before
        // compiling with A read as a number, [[1, 0]] * A * [[0], [1]] was multiplied out to 0, which the delegate returned
        // whatever A was. Only the parts with no matrix argument are simplified.
        [Theory]
        [InlineData("[[1, 0]] * A * [[0], [1]]")]
        [InlineData("[[1, 1]] * A * [[1], [-1]]")]
        [InlineData("[[0, 2]] * A ^ 2 * [[1], [0]] - 1")]
        public void ARowTimesAMatrixArgumentTimesAColumnIsANumber(string expression)
        {
            var e = expression.ToEntity();
            static Complex Evaluate(Entity e, Entity.Matrix a) => e.Substitute("A", a).Evaled.EvalNumerical().ToNumerics();
            var overReals = e.Compile<double[,], double>("A");
            foreach (var a in new[] { new double[,] { { 1, 2 }, { 3, -4 } }, new double[,] { { -0.5, -7 }, { 2.5, 0 } } })
                Assert.True(Complex.Abs(Evaluate(e, AsEntity(AsComplex(a))) - overReals(a)) <= 1e-12, $"{overReals(a)} at A = {AsEntity(AsComplex(a))}");
            var overComplex = e.Compile<Complex[,], Complex>("A");
            foreach (var a in new[] { new Complex[,] { { new(1, 1), -2 }, { new(0, -3), 4 } }, new Complex[,] { { -1, new(0.5, 2) }, { 2, new(0, 1) } } })
                Assert.True(Complex.Abs(Evaluate(e, AsEntity(a)) - overComplex(a)) <= 1e-12, $"{overComplex(a)} at A = {AsEntity(a)}");
            // As a matrix, it is the 1 x 1 matrix of that entry.
            var asMatrix = e.Compile<double[,], double[,]>("A")(new double[,] { { 1, 2 }, { 3, -4 } });
            Assert.Equal(overReals(new double[,] { { 1, 2 }, { 3, -4 } }), Assert.Single(asMatrix.Cast<double>()));
        }

        [Fact]
        public void AnArrayWithNoElementsIsNotAMatrix()
            => Assert.Throws<InvalidMatrixOperationException>(() => "A + 1".ToEntity().Compile<double[,], double[,]>("A")(new double[0, 0]));

        // A complex matrix is not given back as a real one; a real argument widens to a complex matrix, as it widens to a
        // complex number.
        [Fact]
        public void AnArrayOfANarrowerElementTypeIsRefused()
        {
            Assert.Throws<UncompilableNodeException>(() => "[[x, 1]]".ToEntity().Compile<Complex, double[,]>("x"));
            Assert.Equal(new Complex[,] { { 2, 1 } }, "[[x, 1]]".ToEntity().Compile<double, Complex[,]>("x")(2));
        }

        // FastExpression's value is a number. An expression of matrices whose value is one compiles, as it does in the
        // LINQ compiler, and one whose value is a matrix is refused and says where the matrix can be had.
        [Fact]
        public void FastExpressionCompilesAScalarMadeOfMatrices()
        {
            var f = "[1, 0]T * [[a, b], [c, d]] * [0, 1]".ToEntity().Compile("a", "b", "c", "d");
            Assert.Equal(new Complex(2, 0), f.Call(1, 2, 3, 4));
            var thrown = Assert.Throws<UncompilableNodeException>(() => "[[a, 1]] * 2".ToEntity().Compile("a"));
            Assert.Contains("double[,]", thrown.Message);
        }

        [Fact]
        public void AMatrixIsNotANumberAndSaysSo()
        {
            Assert.Throws<UncompilableNodeException>(() => "[[x, 2 x]]".ToEntity().Compile<double, double>("x"));
            Assert.Throws<UncompilableNodeException>(() => "sin(A)".ToEntity().Compile<GenTensor<double, DoubleOperations>, double>("A"));
        }
    }
}
