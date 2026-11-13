//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using AngouriMath.Core.Exceptions;
using GenericTensor.Core;
using Complex = System.Numerics.Complex;

namespace AngouriMath.Core.Compilation.IntoLinq
{
    /// <summary>
    /// The element arithmetic a compiled matrix runs on: <see cref="GenTensor{T, TWrapper}"/>
    /// takes its operations from a struct, and these are the ones for the numeric types the
    /// compiler produces. A matrix compiled over <see cref="double"/> is a
    /// <c>GenTensor&lt;double, DoubleOperations&gt;</c>, and that is the type to compile to
    /// and to pass a matrix in as.
    /// https://github.com/asc-community/AngouriMath/issues/526
    /// </summary>
    public readonly struct DoubleOperations : IOperations<double>
    {
        /// <inheritdoc/>
        public double Add(double a, double b) => a + b;
        /// <inheritdoc/>
        public double Subtract(double a, double b) => a - b;
        /// <inheritdoc/>
        public double Multiply(double a, double b) => a * b;
        /// <inheritdoc/>
        public double Negate(double a) => -a;
        /// <inheritdoc/>
        public double Divide(double a, double b) => b == 0 ? double.NaN : a / b;
        /// <inheritdoc/>
        public double CreateOne() => 1;
        /// <inheritdoc/>
        public double CreateZero() => 0;
        /// <inheritdoc/>
        public double Copy(double a) => a;
        /// <inheritdoc/>
        public bool AreEqual(double a, double b) => a == b;
        /// <inheritdoc/>
        public bool IsZero(double a) => a == 0;
        /// <inheritdoc/>
        public string ToString(double a) => a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public byte[] Serialize(double a) => BitConverter.GetBytes(a);
        /// <inheritdoc/>
        public double Deserialize(byte[] data) => BitConverter.ToDouble(data, 0);
    }

    /// <summary>The element arithmetic of a matrix compiled over <see cref="float"/>. See <see cref="DoubleOperations"/>.</summary>
    public readonly struct SingleOperations : IOperations<float>
    {
        /// <inheritdoc/>
        public float Add(float a, float b) => a + b;
        /// <inheritdoc/>
        public float Subtract(float a, float b) => a - b;
        /// <inheritdoc/>
        public float Multiply(float a, float b) => a * b;
        /// <inheritdoc/>
        public float Negate(float a) => -a;
        /// <inheritdoc/>
        public float Divide(float a, float b) => b == 0 ? float.NaN : a / b;
        /// <inheritdoc/>
        public float CreateOne() => 1;
        /// <inheritdoc/>
        public float CreateZero() => 0;
        /// <inheritdoc/>
        public float Copy(float a) => a;
        /// <inheritdoc/>
        public bool AreEqual(float a, float b) => a == b;
        /// <inheritdoc/>
        public bool IsZero(float a) => a == 0;
        /// <inheritdoc/>
        public string ToString(float a) => a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public byte[] Serialize(float a) => BitConverter.GetBytes(a);
        /// <inheritdoc/>
        public float Deserialize(byte[] data) => BitConverter.ToSingle(data, 0);
    }

    /// <summary>The element arithmetic of a matrix compiled over <see cref="System.Numerics.Complex"/>. See <see cref="DoubleOperations"/>.</summary>
    public readonly struct ComplexOperations : IOperations<Complex>
    {
        /// <inheritdoc/>
        public Complex Add(Complex a, Complex b) => a + b;
        /// <inheritdoc/>
        public Complex Subtract(Complex a, Complex b) => a - b;
        /// <inheritdoc/>
        public Complex Multiply(Complex a, Complex b) => a * b;
        /// <inheritdoc/>
        public Complex Negate(Complex a) => -a;
        /// <inheritdoc/>
        public Complex Divide(Complex a, Complex b) => a / b;
        /// <inheritdoc/>
        public Complex CreateOne() => Complex.One;
        /// <inheritdoc/>
        public Complex CreateZero() => Complex.Zero;
        /// <inheritdoc/>
        public Complex Copy(Complex a) => a;
        /// <inheritdoc/>
        public bool AreEqual(Complex a, Complex b) => a == b;
        /// <inheritdoc/>
        public bool IsZero(Complex a) => a == Complex.Zero;
        /// <inheritdoc/>
        public string ToString(Complex a) => a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public byte[] Serialize(Complex a)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(a.Real).CopyTo(bytes, 0);
            BitConverter.GetBytes(a.Imaginary).CopyTo(bytes, 8);
            return bytes;
        }
        /// <inheritdoc/>
        public Complex Deserialize(byte[] data) => new(BitConverter.ToDouble(data, 0), BitConverter.ToDouble(data, 8));
    }

    /// <summary>The element arithmetic of a matrix compiled over <see cref="int"/>. See <see cref="DoubleOperations"/>.</summary>
    public readonly struct Int32Operations : IOperations<int>
    {
        /// <inheritdoc/>
        public int Add(int a, int b) => a + b;
        /// <inheritdoc/>
        public int Subtract(int a, int b) => a - b;
        /// <inheritdoc/>
        public int Multiply(int a, int b) => a * b;
        /// <inheritdoc/>
        public int Negate(int a) => -a;
        /// <inheritdoc/>
        public int Divide(int a, int b) => a / b;
        /// <inheritdoc/>
        public int CreateOne() => 1;
        /// <inheritdoc/>
        public int CreateZero() => 0;
        /// <inheritdoc/>
        public int Copy(int a) => a;
        /// <inheritdoc/>
        public bool AreEqual(int a, int b) => a == b;
        /// <inheritdoc/>
        public bool IsZero(int a) => a == 0;
        /// <inheritdoc/>
        public string ToString(int a) => a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public byte[] Serialize(int a) => BitConverter.GetBytes(a);
        /// <inheritdoc/>
        public int Deserialize(byte[] data) => BitConverter.ToInt32(data, 0);
    }

    /// <summary>The element arithmetic of a matrix compiled over <see cref="long"/>. See <see cref="DoubleOperations"/>.</summary>
    public readonly struct Int64Operations : IOperations<long>
    {
        /// <inheritdoc/>
        public long Add(long a, long b) => a + b;
        /// <inheritdoc/>
        public long Subtract(long a, long b) => a - b;
        /// <inheritdoc/>
        public long Multiply(long a, long b) => a * b;
        /// <inheritdoc/>
        public long Negate(long a) => -a;
        /// <inheritdoc/>
        public long Divide(long a, long b) => a / b;
        /// <inheritdoc/>
        public long CreateOne() => 1;
        /// <inheritdoc/>
        public long CreateZero() => 0;
        /// <inheritdoc/>
        public long Copy(long a) => a;
        /// <inheritdoc/>
        public bool AreEqual(long a, long b) => a == b;
        /// <inheritdoc/>
        public bool IsZero(long a) => a == 0;
        /// <inheritdoc/>
        public string ToString(long a) => a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public byte[] Serialize(long a) => BitConverter.GetBytes(a);
        /// <inheritdoc/>
        public long Deserialize(byte[] data) => BitConverter.ToInt64(data, 0);
    }

    /// <summary>The element arithmetic of a matrix compiled over <see cref="BigInteger"/>. See <see cref="DoubleOperations"/>.</summary>
    public readonly struct BigIntegerOperations : IOperations<BigInteger>
    {
        /// <inheritdoc/>
        public BigInteger Add(BigInteger a, BigInteger b) => a + b;
        /// <inheritdoc/>
        public BigInteger Subtract(BigInteger a, BigInteger b) => a - b;
        /// <inheritdoc/>
        public BigInteger Multiply(BigInteger a, BigInteger b) => a * b;
        /// <inheritdoc/>
        public BigInteger Negate(BigInteger a) => -a;
        /// <inheritdoc/>
        public BigInteger Divide(BigInteger a, BigInteger b) => a / b;
        /// <inheritdoc/>
        public BigInteger CreateOne() => BigInteger.One;
        /// <inheritdoc/>
        public BigInteger CreateZero() => BigInteger.Zero;
        /// <inheritdoc/>
        public BigInteger Copy(BigInteger a) => a;
        /// <inheritdoc/>
        public bool AreEqual(BigInteger a, BigInteger b) => a == b;
        /// <inheritdoc/>
        public bool IsZero(BigInteger a) => a.IsZero;
        /// <inheritdoc/>
        public string ToString(BigInteger a) => a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public byte[] Serialize(BigInteger a) => a.ToByteArray();
        /// <inheritdoc/>
        public BigInteger Deserialize(byte[] data) => new(data);
    }

    /// <summary>
    /// How a matrix and the arithmetic on it compile: a <see cref="Entity.Matrix"/> node becomes
    /// a <see cref="GenTensor{T, TWrapper}"/> over its elements' common type, filled element by
    /// element from the compiled elements, and a sum, difference, product, quotient or power
    /// with a matrix in it becomes the tensor operation of the same name -- <c>PiecewiseAdd</c>,
    /// <c>MatrixMultiply</c>, <c>PiecewiseMultiply</c> by a scalar, <c>MatrixPower</c> -- so
    /// that the compiled delegate runs GenericTensor's arithmetic rather than a tree of scalar
    /// operations.
    /// </summary>
    /// <remarks>
    /// The methods the compiled tree calls are the public generic ones below, closed over each
    /// element type at compile time in the kit for that type and named through expression
    /// trees, the way <see cref="CompilationProtocol"/> keeps its own tables: no generic type or
    /// method is constructed at run time, which a trimmed or NativeAOT build could not do.
    /// https://github.com/asc-community/AngouriMath/issues/363
    /// https://github.com/asc-community/AngouriMath/issues/526
    /// </remarks>
    public static partial class TensorCompilation
    {
        /// <summary>The closed methods for one element type.</summary>
        private sealed class Kit
        {
            internal Type Tensor { get; }
            internal Type Array { get; }
            internal MethodInfo Empty { get; }
            internal MethodInfo Set { get; }
            internal MethodInfo Add { get; }
            internal MethodInfo Subtract { get; }
            internal MethodInfo Multiply { get; }
            internal MethodInfo AddScalar { get; }
            internal MethodInfo SubtractScalar { get; }
            internal MethodInfo SubtractFromScalar { get; }
            internal MethodInfo MultiplyScalar { get; }
            internal MethodInfo DivideScalar { get; }
            internal MethodInfo Power { get; }
            internal MethodInfo ToArray { get; }
            internal MethodInfo FromArray { get; }
            internal MethodInfo ToScalar { get; }
            /// <summary>Where the elements are a field, which the whole numbers are not: the inverse, and division by a matrix.</summary>
            internal MethodInfo? Inverse { get; }
            internal MethodInfo? DivideByMatrix { get; }
            internal MethodInfo? ScalarOverMatrix { get; }
            internal MethodInfo Transpose { get; }
            internal MethodInfo Determinant { get; }
            internal MethodInfo Trace { get; }
            internal MethodInfo Entry { get; }

            private Kit(Type tensor, Type array, MethodInfo empty, MethodInfo set, MethodInfo add, MethodInfo subtract, MethodInfo multiply,
                MethodInfo addScalar, MethodInfo subtractScalar, MethodInfo subtractFromScalar, MethodInfo multiplyScalar,
                MethodInfo divideScalar, MethodInfo power, MethodInfo toArray, MethodInfo fromArray, MethodInfo toScalar,
                MethodInfo? inverse, MethodInfo? divideByMatrix, MethodInfo? scalarOverMatrix,
                MethodInfo transpose, MethodInfo determinant, MethodInfo trace, MethodInfo entry)
            {
                (Transpose, Determinant, Trace, Entry) = (transpose, determinant, trace, entry);
                (Tensor, Array, Empty, Set, Add, Subtract, Multiply, AddScalar, SubtractScalar, SubtractFromScalar, MultiplyScalar, DivideScalar, Power)
                    = (tensor, array, empty, set, add, subtract, multiply, addScalar, subtractScalar, subtractFromScalar, multiplyScalar, divideScalar, power);
                (ToArray, FromArray, ToScalar, Inverse, DivideByMatrix, ScalarOverMatrix) = (toArray, fromArray, toScalar, inverse, divideByMatrix, scalarOverMatrix);
            }

            // An expression tree is how C# names a method without a string: the call inside it
            // is resolved at compile time, closed over T and TWrapper, and rooted for the trimmer.
            private static MethodInfo Named<TDelegate>(Expression<TDelegate> e) => ((MethodCallExpression)e.Body).Method;

            internal static Kit For<T, TWrapper>(bool field) where TWrapper : struct, IOperations<T>
                => new(typeof(GenTensor<T, TWrapper>), typeof(T[,]),
                    Named<Func<int, int, GenTensor<T, TWrapper>>>((r, c) => TensorCompilation.Empty<T, TWrapper>(r, c)),
                    Named<Func<GenTensor<T, TWrapper>, int, T, GenTensor<T, TWrapper>>>((t, i, v) => TensorCompilation.Set(t, i, v)),
                    Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>((a, b) => TensorCompilation.Add(a, b)),
                    Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>((a, b) => TensorCompilation.Subtract(a, b)),
                    Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>((a, b) => TensorCompilation.Multiply(a, b)),
                    Named<Func<GenTensor<T, TWrapper>, T, GenTensor<T, TWrapper>>>((a, s) => TensorCompilation.AddScalar(a, s)),
                    Named<Func<GenTensor<T, TWrapper>, T, GenTensor<T, TWrapper>>>((a, s) => TensorCompilation.SubtractScalar(a, s)),
                    Named<Func<T, GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>((s, a) => TensorCompilation.SubtractFromScalar(s, a)),
                    Named<Func<GenTensor<T, TWrapper>, T, GenTensor<T, TWrapper>>>((a, s) => TensorCompilation.MultiplyScalar(a, s)),
                    Named<Func<GenTensor<T, TWrapper>, T, GenTensor<T, TWrapper>>>((a, s) => TensorCompilation.DivideScalar(a, s)),
                    Named<Func<GenTensor<T, TWrapper>, int, GenTensor<T, TWrapper>>>((a, n) => TensorCompilation.Power(a, n)),
                    Named<Func<GenTensor<T, TWrapper>, T[,]>>(a => TensorCompilation.ToArray(a)),
                    Named<Func<T[,], GenTensor<T, TWrapper>>>(a => TensorCompilation.FromArray<T, TWrapper>(a)),
                    Named<Func<GenTensor<T, TWrapper>, T>>(a => TensorCompilation.ToScalar(a)),
                    field ? Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>(a => TensorCompilation.Inverse(a)) : null,
                    field ? Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>((a, b) => TensorCompilation.DivideByMatrix(a, b)) : null,
                    field ? Named<Func<T, GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>((s, a) => TensorCompilation.ScalarOverMatrix(s, a)) : null,
                    Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>(a => TensorCompilation.Transpose(a)),
                    Named<Func<GenTensor<T, TWrapper>, T>>(a => TensorCompilation.Determinant(a)),
                    Named<Func<GenTensor<T, TWrapper>, T>>(a => TensorCompilation.Trace(a)),
                    Named<Func<GenTensor<T, TWrapper>, int, int, T>>((a, i, j) => TensorCompilation.Entry(a, i, j)));
        }

        [ConstantField]
        private static readonly Dictionary<Type, Kit> kits = new()
        {
            { typeof(double), Kit.For<double, DoubleOperations>(field: true) },
            { typeof(float), Kit.For<float, SingleOperations>(field: true) },
            { typeof(Complex), Kit.For<Complex, ComplexOperations>(field: true) },
            { typeof(int), Kit.For<int, Int32Operations>(field: false) },
            { typeof(long), Kit.For<long, Int64Operations>(field: false) },
            { typeof(BigInteger), Kit.For<BigInteger, BigIntegerOperations>(field: false) },
        };

        [ConstantField]
        private static readonly Dictionary<Type, Kit> kitsByTensor = kits.Values.ToDictionary(kit => kit.Tensor);

        [ConstantField]
        private static readonly Dictionary<Type, Kit> kitsByArray = kits.Values.ToDictionary(kit => kit.Array);

        /// <summary>The tensor type a matrix over <paramref name="element"/> compiles to, or <see langword="null"/> where there is none.</summary>
        public static Type? TensorTypeOf(Type element)
            => kits.TryGetValue(element, out var kit) ? kit.Tensor : null;

        internal static bool IsTensor(Type type) => kitsByTensor.ContainsKey(type);

        /// <summary>
        /// Whether <paramref name="type"/> is a two-dimensional array of an element type a matrix compiles over, as
        /// <c>double[,]</c> is: an argument or a result of that type is a matrix, converted to and from the tensor at
        /// the delegate's boundary, so that a caller need not know GenericTensor.
        /// </summary>
        internal static bool IsMatrixArray(Type type) => kitsByArray.ContainsKey(type) || IsVectorArray(type);

        /// <summary>Whether a value of <paramref name="type"/> is a matrix, as a tensor or as a two-dimensional array.</summary>
        internal static bool IsMatrixType(Type type) => IsTensor(type) || IsMatrixArray(type);

        /// <summary>The element type of a matrix type, or <see langword="null"/> where it is not one.</summary>
        internal static Type? ElementOfMatrixType(Type type)
            => kitsByTensor.TryGetValue(type, out var kit) || kitsByArray.TryGetValue(type, out kit) ? ElementOf(kit.Tensor) : ElementOfVectorType(type);

        /// <summary>A two-dimensional array argument as the tensor the compiled arithmetic runs on.</summary>
        internal static Expression FromArrayArgument(Expression array)
            => IsVectorArray(array.Type) ? FromVectorArgument(array) : Expression.Call(kitsByArray[array.Type].FromArray, array);

        /// <summary>
        /// A tensor as the two-dimensional array of the same element type, or <see langword="null"/> where
        /// <paramref name="type"/> is not that array.
        /// </summary>
        internal static Expression? AsArray(Expression tensor, Type type)
            => kitsByTensor.TryGetValue(tensor.Type, out var kit) && kit.Array == type ? Expression.Call(kit.ToArray, tensor) : AsVector(tensor, type);

        private static Type ElementOf(Type tensor) => tensor.GetGenericArguments()[0];

        /// <summary>A matrix of the shape, to be filled.</summary>
        public static GenTensor<T, TWrapper> Empty<T, TWrapper>(int rows, int columns) where TWrapper : struct, IOperations<T>
            => GenTensor<T, TWrapper>.CreateMatrix(rows, columns);
        /// <summary>The matrix with its element at a row-major index set; the same matrix, for chaining.</summary>
        public static GenTensor<T, TWrapper> Set<T, TWrapper>(GenTensor<T, TWrapper> matrix, int index, T value) where TWrapper : struct, IOperations<T>
        {
            matrix[index / matrix.Shape[1], index % matrix.Shape[1]] = value;
            return matrix;
        }

        // The arithmetic is written as plain loops over the elements rather than handed to
        // GenericTensor's PiecewiseAdd, MatrixMultiply and the rest: those build and compile an
        // expression tree for their loops, which is exactly what a NativeAOT build cannot do,
        // and the AOT smoke test refuses the assembly the moment they are reachable. A loop over
        // a wrapper's Add is what they compile to anyway.

        private static void SameShape<T, TWrapper>(GenTensor<T, TWrapper> a, GenTensor<T, TWrapper> b) where TWrapper : struct, IOperations<T>
        {
            if (a.Shape[0] != b.Shape[0] || a.Shape[1] != b.Shape[1])
                throw new InvalidMatrixOperationException($"Matrices of shapes {a.Shape} and {b.Shape} cannot be combined element by element");
        }

        private static GenTensor<T, TWrapper> Elementwise<T, TWrapper>(GenTensor<T, TWrapper> a, Func<T, T> map) where TWrapper : struct, IOperations<T>
        {
            var result = GenTensor<T, TWrapper>.CreateMatrix(a.Shape[0], a.Shape[1]);
            for (var i = 0; i < a.Shape[0]; i++)
                for (var j = 0; j < a.Shape[1]; j++)
                    result[i, j] = map(a[i, j]);
            return result;
        }

        private static GenTensor<T, TWrapper> Elementwise<T, TWrapper>(GenTensor<T, TWrapper> a, GenTensor<T, TWrapper> b, Func<T, T, T> combine) where TWrapper : struct, IOperations<T>
        {
            SameShape(a, b);
            var result = GenTensor<T, TWrapper>.CreateMatrix(a.Shape[0], a.Shape[1]);
            for (var i = 0; i < a.Shape[0]; i++)
                for (var j = 0; j < a.Shape[1]; j++)
                    result[i, j] = combine(a[i, j], b[i, j]);
            return result;
        }

        /// <summary>Element by element.</summary>
        public static GenTensor<T, TWrapper> Add<T, TWrapper>(GenTensor<T, TWrapper> a, GenTensor<T, TWrapper> b) where TWrapper : struct, IOperations<T>
            => Elementwise(a, b, static (x, y) => default(TWrapper).Add(x, y));
        /// <summary>Element by element.</summary>
        public static GenTensor<T, TWrapper> Subtract<T, TWrapper>(GenTensor<T, TWrapper> a, GenTensor<T, TWrapper> b) where TWrapper : struct, IOperations<T>
            => Elementwise(a, b, static (x, y) => default(TWrapper).Subtract(x, y));
        /// <summary>The matrix product.</summary>
        public static GenTensor<T, TWrapper> Multiply<T, TWrapper>(GenTensor<T, TWrapper> a, GenTensor<T, TWrapper> b) where TWrapper : struct, IOperations<T>
        {
            if (a.Shape[1] != b.Shape[0])
                throw new InvalidMatrixOperationException($"Matrices of shapes {a.Shape} and {b.Shape} cannot be multiplied");
            var ops = default(TWrapper);
            var result = GenTensor<T, TWrapper>.CreateMatrix(a.Shape[0], b.Shape[1]);
            for (var i = 0; i < a.Shape[0]; i++)
                for (var j = 0; j < b.Shape[1]; j++)
                {
                    var sum = ops.CreateZero();
                    for (var k = 0; k < a.Shape[1]; k++)
                        sum = ops.Add(sum, ops.Multiply(a[i, k], b[k, j]));
                    result[i, j] = sum;
                }
            return result;
        }
        /// <summary>The scalar added to every element.</summary>
        public static GenTensor<T, TWrapper> AddScalar<T, TWrapper>(GenTensor<T, TWrapper> a, T scalar) where TWrapper : struct, IOperations<T>
            => Elementwise(a, x => default(TWrapper).Add(x, scalar));
        /// <summary>The scalar subtracted from every element.</summary>
        public static GenTensor<T, TWrapper> SubtractScalar<T, TWrapper>(GenTensor<T, TWrapper> a, T scalar) where TWrapper : struct, IOperations<T>
            => Elementwise(a, x => default(TWrapper).Subtract(x, scalar));
        /// <summary>Every element subtracted from the scalar.</summary>
        public static GenTensor<T, TWrapper> SubtractFromScalar<T, TWrapper>(T scalar, GenTensor<T, TWrapper> a) where TWrapper : struct, IOperations<T>
            => Elementwise(a, x => default(TWrapper).Subtract(scalar, x));
        /// <summary>Every element times the scalar.</summary>
        public static GenTensor<T, TWrapper> MultiplyScalar<T, TWrapper>(GenTensor<T, TWrapper> a, T scalar) where TWrapper : struct, IOperations<T>
            => Elementwise(a, x => default(TWrapper).Multiply(x, scalar));
        /// <summary>Every element over the scalar.</summary>
        public static GenTensor<T, TWrapper> DivideScalar<T, TWrapper>(GenTensor<T, TWrapper> a, T scalar) where TWrapper : struct, IOperations<T>
            => Elementwise(a, x => default(TWrapper).Divide(x, scalar));
        /// <summary>The scalar over every element.</summary>
        /// <remarks>
        /// Not what a scalar over a matrix means, which is the scalar times the inverse, and so not what the compiler
        /// makes of one. https://github.com/asc-community/AngouriMath/issues/2287
        /// </remarks>
        public static GenTensor<T, TWrapper> DivideFromScalar<T, TWrapper>(T scalar, GenTensor<T, TWrapper> a) where TWrapper : struct, IOperations<T>
            => Elementwise(a, x => default(TWrapper).Divide(scalar, x));
        /// <summary>
        /// The matrix power of a whole exponent, by repeated squaring; a negative exponent is that power of the inverse,
        /// as evaluation has it.
        /// </summary>
        /// <exception cref="InvalidMatrixOperationException">
        /// The matrix is not square, or the exponent is negative and the elements are whole numbers, which have no inverse
        /// as a matrix of whole numbers.
        /// </exception>
        public static GenTensor<T, TWrapper> Power<T, TWrapper>(GenTensor<T, TWrapper> a, int exponent) where TWrapper : struct, IOperations<T>
        {
            if (a.Shape[0] != a.Shape[1])
                throw new InvalidMatrixOperationException($"A matrix of shape {a.Shape} is not square and has no power");
            // A negative exponent was NotSupportedException, outside the library's own exceptions and thrown only when
            // the compiled delegate was called. https://github.com/asc-community/AngouriMath/issues/526
            if (exponent < 0)
                return Power(Inverse(a), exponent == int.MinValue ? throw new InvalidMatrixOperationException($"A matrix power of {exponent} is not compiled") : -exponent);
            var result = GenTensor<T, TWrapper>.CreateIdentityMatrix(a.Shape[0]);
            var square = a;
            for (var remaining = exponent; remaining > 0; remaining >>= 1)
            {
                if ((remaining & 1) == 1)
                    result = Multiply(result, square);
                if (remaining > 1)
                    square = Multiply(square, square);
            }
            return result;
        }

        /// <summary>
        /// A compiled exponent as the whole number a matrix power takes. It was converted with a cast, which truncates, so
        /// <c>A ^ x</c> at <c>x = 0.7</c> was the identity, where evaluation leaves a matrix to a power that is not whole
        /// unevaluated: a power that is not whole is no answer here either.
        /// </summary>
        /// <exception cref="InvalidMatrixOperationException">The exponent is not a whole number that fits an <see cref="int"/>.</exception>
        public static int WholeExponent(double exponent)
            => Math.Floor(exponent) == exponent && exponent >= -int.MaxValue && exponent <= int.MaxValue
                ? (int)exponent
                : throw new InvalidMatrixOperationException($"A matrix power is compiled for a whole exponent, and {exponent.ToString(System.Globalization.CultureInfo.InvariantCulture)} is not one");

        /// <inheritdoc cref="WholeExponent(double)"/>
        public static int WholeExponent(Complex exponent)
            => exponent.Imaginary == 0
                ? WholeExponent(exponent.Real)
                : throw new InvalidMatrixOperationException($"A matrix power is compiled for a whole exponent, and {exponent} is not one");

        /// <summary>The tensor as a two-dimensional array, rows first.</summary>
        public static T[,] ToArray<T, TWrapper>(GenTensor<T, TWrapper> matrix) where TWrapper : struct, IOperations<T>
        {
            var result = new T[matrix.Shape[0], matrix.Shape[1]];
            for (var i = 0; i < matrix.Shape[0]; i++)
                for (var j = 0; j < matrix.Shape[1]; j++)
                    result[i, j] = matrix[i, j];
            return result;
        }

        /// <summary>A 1 x 1 matrix as its entry, which is what evaluation makes of one.</summary>
        /// <exception cref="InvalidMatrixOperationException">The matrix is not 1 x 1.</exception>
        internal static T ToScalar<T, TWrapper>(GenTensor<T, TWrapper> matrix) where TWrapper : struct, IOperations<T>
            => matrix.Shape[0] == 1 && matrix.Shape[1] == 1
                ? matrix[0, 0]
                : throw new InvalidMatrixOperationException($"A matrix of shape {matrix.Shape} is not a number");

        /// <summary>A 1 x 1 matrix as its entry, checked when the delegate is called.</summary>
        internal static Expression ToScalar(Expression tensor) => Expression.Call(kitsByTensor[tensor.Type].ToScalar, tensor);

        /// <summary>
        /// <paramref name="expr"/> with every part that does not mention a matrix argument simplified, and the rest as
        /// written. Simplification reads a variable as a number, so with a matrix argument in it, it multiplied
        /// <c>[[1, 0]] * A * [[0], [1]]</c> out to 0, whatever <c>A</c> was. A part with no matrix argument in it
        /// simplifies as before (https://github.com/asc-community/AngouriMath/issues/425).
        /// </summary>
        internal static Entity SimplifiedAroundMatrixArguments(Entity expr, Func<Entity.Variable, bool> isMatrixArgument)
            => expr.Vars.Any(isMatrixArgument)
                ? expr.Replace(node => node.Nodes.Any(n => n is Entity.Matrix) && !node.Vars.Any(isMatrixArgument) ? node.InnerSimplified : node)
                : expr.InnerSimplified;

        /// <summary>A two-dimensional array, rows first, as a tensor.</summary>
        /// <exception cref="InvalidMatrixOperationException">The array has no elements: there is no empty matrix.</exception>
        public static GenTensor<T, TWrapper> FromArray<T, TWrapper>(T[,] matrix) where TWrapper : struct, IOperations<T>
        {
            if (matrix.GetLength(0) == 0 || matrix.GetLength(1) == 0)
                throw new InvalidMatrixOperationException("An array with no elements is not a matrix");
            var result = GenTensor<T, TWrapper>.CreateMatrix(matrix.GetLength(0), matrix.GetLength(1));
            for (var i = 0; i < matrix.GetLength(0); i++)
                for (var j = 0; j < matrix.GetLength(1); j++)
                    result[i, j] = matrix[i, j];
            return result;
        }

        // The size of an element, to choose the largest pivot by. Only the element types a matrix is inverted over have
        // one; a typeof test on T is decided when the method is compiled for T, ahead of time as well.
        private static double Magnitude<T>(T value)
        {
            if (typeof(T) == typeof(double)) return Math.Abs((double)(object)value!);
            if (typeof(T) == typeof(float)) return Math.Abs((float)(object)value!);
            if (typeof(T) == typeof(Complex)) return Complex.Abs((Complex)(object)value!);
            throw new InvalidMatrixOperationException($"A matrix over {typeof(T).Name} is not inverted: the inverse of a matrix of whole numbers is not one");
        }

        /// <summary>
        /// The inverse, by Gauss-Jordan elimination with partial pivoting. Where a pivot is exactly zero the matrix is
        /// singular, and every entry is <c>0 / 0</c> in the element arithmetic -- NaN over <see cref="double"/>,
        /// <see cref="float"/> and <see cref="Complex"/> -- as a compiled division by zero has no value. A matrix that is
        /// singular only up to rounding is inverted as rounding leaves it.
        /// </summary>
        /// <exception cref="InvalidMatrixOperationException">The matrix is not square, or its elements are whole numbers.</exception>
        public static GenTensor<T, TWrapper> Inverse<T, TWrapper>(GenTensor<T, TWrapper> a) where TWrapper : struct, IOperations<T>
        {
            if (a.Shape[0] != a.Shape[1])
                throw new InvalidMatrixOperationException($"A matrix of shape {a.Shape} is not square and has no inverse");
            var ops = default(TWrapper);
            var n = a.Shape[0];
            var left = new T[n, n];
            var right = new T[n, n];
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                {
                    left[i, j] = a[i, j];
                    right[i, j] = i == j ? ops.CreateOne() : ops.CreateZero();
                }
            for (var column = 0; column < n; column++)
            {
                var pivot = column;
                for (var row = column + 1; row < n; row++)
                    if (Magnitude(left[row, column]) > Magnitude(left[pivot, column]))
                        pivot = row;
                if (ops.IsZero(left[pivot, column]))
                {
                    var none = ops.Divide(ops.CreateZero(), ops.CreateZero());
                    return GenTensor<T, TWrapper>.CreateMatrix(n, n, (_, _) => none);
                }
                if (pivot != column)
                    for (var j = 0; j < n; j++)
                    {
                        (left[pivot, j], left[column, j]) = (left[column, j], left[pivot, j]);
                        (right[pivot, j], right[column, j]) = (right[column, j], right[pivot, j]);
                    }
                var p = left[column, column];
                for (var j = 0; j < n; j++)
                {
                    left[column, j] = ops.Divide(left[column, j], p);
                    right[column, j] = ops.Divide(right[column, j], p);
                }
                for (var row = 0; row < n; row++)
                {
                    if (row == column || ops.IsZero(left[row, column]))
                        continue;
                    var factor = left[row, column];
                    for (var j = 0; j < n; j++)
                    {
                        left[row, j] = ops.Subtract(left[row, j], ops.Multiply(factor, left[column, j]));
                        right[row, j] = ops.Subtract(right[row, j], ops.Multiply(factor, right[column, j]));
                    }
                }
            }
            return GenTensor<T, TWrapper>.CreateMatrix(n, n, (i, j) => right[i, j]);
        }

        /// <summary>A matrix over a matrix, the first times the inverse of the second, as evaluation has it.</summary>
        public static GenTensor<T, TWrapper> DivideByMatrix<T, TWrapper>(GenTensor<T, TWrapper> a, GenTensor<T, TWrapper> b) where TWrapper : struct, IOperations<T>
            => Multiply(a, Inverse(b));

        /// <summary>A scalar over a matrix, the scalar times its inverse, as evaluation has it.</summary>
        public static GenTensor<T, TWrapper> ScalarOverMatrix<T, TWrapper>(T scalar, GenTensor<T, TWrapper> a) where TWrapper : struct, IOperations<T>
            => MultiplyScalar(Inverse(a), scalar);

        /// <summary>The transpose, the rows of the one being the columns of the other.</summary>
        public static GenTensor<T, TWrapper> Transpose<T, TWrapper>(GenTensor<T, TWrapper> a) where TWrapper : struct, IOperations<T>
        {
            var result = GenTensor<T, TWrapper>.CreateMatrix(a.Shape[1], a.Shape[0]);
            for (var i = 0; i < a.Shape[0]; i++)
                for (var j = 0; j < a.Shape[1]; j++)
                    result[j, i] = a[i, j];
            return result;
        }

        /// <summary>
        /// The determinant, by Bareiss' fraction-free elimination: every division it makes is exact, so it is exact
        /// over the whole numbers as well, and a row is exchanged for the pivot of largest size where the elements
        /// have one, as partial pivoting chooses it, and for the first that is not zero where they do not. A zero
        /// column gives 0.
        /// </summary>
        /// <exception cref="InvalidMatrixOperationException">The matrix is not square, and has no determinant.</exception>
        public static T Determinant<T, TWrapper>(GenTensor<T, TWrapper> a) where TWrapper : struct, IOperations<T>
        {
            if (a.Shape[0] != a.Shape[1])
                throw new InvalidMatrixOperationException($"A matrix of shape {a.Shape} is not square and has no determinant");
            var ops = default(TWrapper);
            var n = a.Shape[0];
            var m = new T[n, n];
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    m[i, j] = a[i, j];
            var sizable = typeof(T) == typeof(double) || typeof(T) == typeof(float) || typeof(T) == typeof(Complex);
            var negated = false;
            var previous = ops.CreateOne();
            for (var k = 0; k < n - 1; k++)
            {
                var pivot = -1;
                for (var row = k; row < n; row++)
                    if (!ops.IsZero(m[row, k]) && (pivot < 0 || sizable && Magnitude(m[row, k]) > Magnitude(m[pivot, k])))
                    {
                        pivot = row;
                        if (!sizable)
                            break;
                    }
                if (pivot < 0)
                    return ops.CreateZero();
                if (pivot != k)
                {
                    for (var j = 0; j < n; j++)
                        (m[pivot, j], m[k, j]) = (m[k, j], m[pivot, j]);
                    negated = !negated;
                }
                for (var i = k + 1; i < n; i++)
                {
                    for (var j = k + 1; j < n; j++)
                        m[i, j] = ops.Divide(ops.Subtract(ops.Multiply(m[i, j], m[k, k]), ops.Multiply(m[i, k], m[k, j])), previous);
                    m[i, k] = ops.CreateZero();
                }
                previous = m[k, k];
            }
            return negated ? ops.Negate(m[n - 1, n - 1]) : m[n - 1, n - 1];
        }

        /// <summary>The trace, the sum of the diagonal.</summary>
        /// <exception cref="InvalidMatrixOperationException">The matrix is not square, and has no trace.</exception>
        public static T Trace<T, TWrapper>(GenTensor<T, TWrapper> a) where TWrapper : struct, IOperations<T>
        {
            if (a.Shape[0] != a.Shape[1])
                throw new InvalidMatrixOperationException($"A matrix of shape {a.Shape} is not square and has no trace");
            var ops = default(TWrapper);
            var sum = ops.CreateZero();
            for (var i = 0; i < a.Shape[0]; i++)
                sum = ops.Add(sum, a[i, i]);
            return sum;
        }

        /// <summary>The entry in a row and a column, both counted from 1.</summary>
        /// <exception cref="InvalidMatrixOperationException">Either index is out of the matrix.</exception>
        public static T Entry<T, TWrapper>(GenTensor<T, TWrapper> a, int row, int column) where TWrapper : struct, IOperations<T>
            => row >= 1 && row <= a.Shape[0] && column >= 1 && column <= a.Shape[1]
                ? a[row - 1, column - 1]
                : throw new InvalidMatrixOperationException($"A matrix of shape {a.Shape} has no entry at ({row}, {column}): its rows and columns are counted from 1");

        /// <summary>A compiled index as the whole number it must be, checked when the delegate is called.</summary>
        /// <exception cref="InvalidMatrixOperationException">The index is not a whole number that fits an <see cref="int"/>.</exception>
        public static int WholeIndex(double index)
            => Math.Floor(index) == index && index >= int.MinValue && index <= int.MaxValue
                ? (int)index
                : throw new InvalidMatrixOperationException($"An entry is indexed by whole numbers, and {index.ToString(System.Globalization.CultureInfo.InvariantCulture)} is not one");

        /// <inheritdoc cref="WholeIndex(double)"/>
        public static int WholeIndex(Complex index)
            => index.Imaginary == 0
                ? WholeIndex(index.Real)
                : throw new InvalidMatrixOperationException($"An entry is indexed by whole numbers, and {index} is not one");

        [ConstantField]
        private static readonly MethodInfo wholeRealIndex = ((MethodCallExpression)((Expression<Func<double, int>>)(e => WholeIndex(e))).Body).Method;
        [ConstantField]
        private static readonly MethodInfo wholeComplexIndex = ((MethodCallExpression)((Expression<Func<Complex, int>>)(e => WholeIndex(e))).Body).Method;

        /// <summary>
        /// The compiled form of a transpose, a determinant, a trace or an entry, whose first operand is the matrix, or
        /// <see langword="null"/> where the node is none of them.
        /// https://github.com/asc-community/AngouriMath/issues/2147
        /// </summary>
        internal static Expression? MatrixOperation(Entity node, IReadOnlyList<Expression> operands, Func<Expression, Type, Expression> convert)
        {
            if (node is not (Entity.Transposef or Entity.Determinantf or Entity.Tracef or Entity.MatrixEntryf))
                return null;
            if (!kitsByTensor.TryGetValue(operands[0].Type, out var kit))
                throw new UncompilableNodeException(
                    $"`{node.Stringize()}` is taken of a matrix, and its argument compiles to {operands[0].Type.Name}: compile it with a matrix type, such as double[,], for the argument");
            Expression Index(Expression index)
                => Expression.Call(
                    index.Type == typeof(Complex) ? wholeComplexIndex : wholeRealIndex,
                    index.Type == typeof(Complex) ? index : convert(index, typeof(double)));
            return node switch
            {
                Entity.Transposef => Expression.Call(kit.Transpose, operands[0]),
                Entity.Determinantf => Expression.Call(kit.Determinant, operands[0]),
                Entity.Tracef => Expression.Call(kit.Trace, operands[0]),
                _ => Expression.Call(kit.Entry, operands[0], Index(operands[1]), Index(operands[2])),
            };
        }

        /// <summary>The compiled form of a matrix node over the given compiled elements, all of one type.</summary>
        internal static Expression Build(Entity.Matrix matrix, IReadOnlyList<Expression> elements)
        {
            var element = elements[0].Type;
            if (!kits.TryGetValue(element, out var kit))
                throw new UncompilableNodeException($"A matrix over {element} has no compiled form; the elements compile to double, float, Complex, int, long or BigInteger");
            Expression tensor = Expression.Call(kit.Empty, Expression.Constant(matrix.RowCount), Expression.Constant(matrix.ColumnCount));
            for (var i = 0; i < elements.Count; i++)
                tensor = Expression.Call(kit.Set, tensor, Expression.Constant(i), elements[i]);
            return tensor;
        }

        /// <summary>
        /// The compiled form of a binary node one of whose operands is a tensor, or
        /// <see langword="null"/> where the node has none.
        /// </summary>
        internal static Expression? Binary(Entity node, Expression left, Expression right, Func<Expression, Type, Expression> convert)
        {
            var leftTensor = kitsByTensor.TryGetValue(left.Type, out var leftKit);
            var rightTensor = kitsByTensor.TryGetValue(right.Type, out var rightKit);
            if (leftTensor && rightTensor)
            {
                if (left.Type != right.Type)
                    throw new UncompilableNodeException($"Two matrices over different element types, {ElementOf(left.Type)} and {ElementOf(right.Type)}, cannot be combined");
                return node switch
                {
                    Entity.Sumf => Expression.Call(leftKit!.Add, left, right),
                    Entity.Minusf => Expression.Call(leftKit!.Subtract, left, right),
                    Entity.Mulf => Expression.Call(leftKit!.Multiply, left, right),
                    // A matrix over a matrix is the first times the inverse of the second, as evaluation has it.
                    Entity.Divf => Expression.Call(Field(leftKit!, leftKit!.DivideByMatrix), left, right),
                    _ => null,
                };
            }
            if (leftTensor)
            {
                if (node is Entity.Powf)
                    return Expression.Call(leftKit!.Power, left, Exponent(right, convert));
                var scalar = convert(right, ElementOf(left.Type));
                return node switch
                {
                    Entity.Sumf => Expression.Call(leftKit!.AddScalar, left, scalar),
                    Entity.Minusf => Expression.Call(leftKit!.SubtractScalar, left, scalar),
                    Entity.Mulf => Expression.Call(leftKit!.MultiplyScalar, left, scalar),
                    Entity.Divf => Expression.Call(leftKit!.DivideScalar, left, scalar),
                    _ => null,
                };
            }
            var value = convert(left, ElementOf(right.Type));
            return node switch
            {
                Entity.Sumf => Expression.Call(rightKit!.AddScalar, right, value),
                Entity.Minusf => Expression.Call(rightKit!.SubtractFromScalar, value, right),
                Entity.Mulf => Expression.Call(rightKit!.MultiplyScalar, right, value),
                // A scalar over a matrix is the scalar times its inverse, as evaluation has it. Dividing by every element
                // instead gave 2 / [[1, 2], [3, 4]] as [[2, 1], [2/3, 1/2]], and it was refused until the inverse compiled.
                // https://github.com/asc-community/AngouriMath/issues/2287
                Entity.Divf => Expression.Call(Field(rightKit!, rightKit!.ScalarOverMatrix), value, right),
                _ => null,
            };
        }

        // The exponent of a matrix power as the whole number it must be, checked when the delegate is called: over the
        // complex numbers a constant is complex too, and it was refused for want of a conversion to int.
        private static Expression Exponent(Expression exponent, Func<Expression, Type, Expression> convert)
            => Expression.Call(
                exponent.Type == typeof(Complex) ? wholeComplexExponent : wholeRealExponent,
                exponent.Type == typeof(Complex) ? exponent : convert(exponent, typeof(double)));

        [ConstantField]
        private static readonly MethodInfo wholeRealExponent = ((MethodCallExpression)((Expression<Func<double, int>>)(e => WholeExponent(e))).Body).Method;
        [ConstantField]
        private static readonly MethodInfo wholeComplexExponent = ((MethodCallExpression)((Expression<Func<Complex, int>>)(e => WholeExponent(e))).Body).Method;

        private static MethodInfo Field(Kit kit, MethodInfo? method)
            => method ?? throw new UncompilableNodeException(
                $"A matrix over {ElementOf(kit.Tensor).Name} is not divided by a matrix: the inverse of a matrix of whole numbers is not one. Compile over double or Complex");

        /// <summary>
        /// The shape of every matrix in <paramref name="expr"/> that can be read off it before compiling, and a
        /// refusal where two of them cannot be combined. Simplification has already carried out every operation it
        /// can on matrices written into the expression, so one that is left with written matrices for operands is
        /// usually left because their shapes do not fit -- <c>[[x, 1]] + [x, y]</c> -- and the compiled delegate
        /// would only have thrown when called, at every point. A matrix argument's shape is known only when the
        /// delegate is called, and is checked then.
        /// </summary>
        /// <param name="expr">The expression about to be compiled.</param>
        /// <param name="isMatrix">Whether a variable is a matrix argument.</param>
        /// <exception cref="InvalidMatrixOperationException">Two matrices whose shapes are known cannot be combined.</exception>
        internal static void CheckShapes(Entity expr, Func<Entity.Variable, bool> isMatrix) => ShapeOf(expr, variable => isMatrix(variable) ? (0, 0) : null);

        /// <inheritdoc cref="CheckShapes(Entity, Func{Entity.Variable, bool})"/>
        /// <param name="expr">The expression about to be compiled.</param>
        /// <param name="argument">The shape of an argument, as <see cref="ShapeOfArgument"/> gives it.</param>
        internal static void CheckShapes(Entity expr, Func<Entity.Variable, (int Rows, int Columns)?> argument) => ShapeOf(expr, argument);

        /// <summary>
        /// Whether <paramref name="expr"/> is known before the delegate is called to be a 1 x 1 matrix, as
        /// <c>[[1, 0]] * A * [[0], [1]]</c> is whatever the shape of <c>A</c>: such a matrix is a number.
        /// </summary>
        internal static bool IsOneByOne(Entity expr, Func<Entity.Variable, (int Rows, int Columns)?> argument) => ShapeOf(expr, argument) == (1, 1);

        // null: a number. Otherwise rows and columns, 0 for a count not known until the delegate is called: (0, 0) is a
        // matrix argument, and (0, 1) a vector argument, a column of a length not known yet.
        private static (int Rows, int Columns)? ShapeOf(Entity expr, Func<Entity.Variable, (int Rows, int Columns)?> isMatrix)
        {
            static bool Known((int Rows, int Columns)? shape) => shape is { Rows: > 0, Columns: > 0 };
            // What two shapes that must be equal say together.
            static (int Rows, int Columns)? Merged((int Rows, int Columns)? a, (int Rows, int Columns)? b)
                => a is { } x && b is { } y ? (x.Rows > 0 ? x.Rows : y.Rows, x.Columns > 0 ? x.Columns : y.Columns) : a ?? b;
            static string Show((int Rows, int Columns)? shape) => $"{shape!.Value.Rows}x{shape.Value.Columns}";
            switch (expr)
            {
                case Entity.Matrix matrix:
                    // By index: enumerating a matrix gives its rows, and a row of one element is itself.
                    for (var i = 0; i < matrix.RowCount; i++)
                        for (var j = 0; j < matrix.ColumnCount; j++)
                            ShapeOf(matrix[i, j], isMatrix);
                    return (matrix.RowCount, matrix.ColumnCount);
                case Entity.Variable variable:
                    return isMatrix(variable);
                case Entity.Sumf or Entity.Minusf:
                {
                    var operands = (IBinaryNode)expr;
                    var (a, b) = (ShapeOf(operands.NodeFirstChild, isMatrix), ShapeOf(operands.NodeSecondChild, isMatrix));
                    if (Known(a) && Known(b) && a != b)
                        throw new InvalidMatrixOperationException(
                            $"`{expr.Stringize()}` has no value: matrices of shapes {Show(a)} and {Show(b)} are not {(expr is Entity.Sumf ? "added" : "subtracted")}");
                    return Merged(a, b);
                }
                case Entity.Mulf(var multiplier, var multiplicand):
                {
                    var (a, b) = (ShapeOf(multiplier, isMatrix), ShapeOf(multiplicand, isMatrix));
                    if (a is null || b is null)
                        return a ?? b;
                    if (Known(a) && Known(b))
                        return a.Value.Columns == b.Value.Rows
                            ? (a.Value.Rows, b.Value.Columns)
                            : throw new InvalidMatrixOperationException(
                                $"`{expr.Stringize()}` has no value: a {Show(a)} matrix is not multiplied by a {Show(b)} one");
                    return (a.Value.Rows, b.Value.Columns);
                }
                case Entity.Divf(var dividend, var divisor):
                {
                    var (a, b) = (ShapeOf(dividend, isMatrix), ShapeOf(divisor, isMatrix));
                    if (Known(b) && b!.Value.Rows != b.Value.Columns)
                        throw new InvalidMatrixOperationException($"`{expr.Stringize()}` has no value: a {Show(b)} matrix has no inverse to divide by");
                    if (b is null)
                        return a;
                    if (a is null)
                        return b;
                    if (Known(a) && Known(b))
                        return a.Value.Columns == b.Value.Rows
                            ? a
                            : throw new InvalidMatrixOperationException(
                                $"`{expr.Stringize()}` has no value: a {Show(a)} matrix is not multiplied by the inverse of a {Show(b)} one");
                    return (a.Value.Rows, b.Value.Columns);
                }
                case Entity.Powf(var @base, var exponent):
                {
                    var (a, b) = (ShapeOf(@base, isMatrix), ShapeOf(exponent, isMatrix));
                    if (Known(a) && a!.Value.Rows != a.Value.Columns)
                        throw new InvalidMatrixOperationException($"`{expr.Stringize()}` has no value: a {Show(a)} matrix is not square and has no power");
                    // A number to the power of a matrix is the power of each entry, the shape of the exponent.
                    return a ?? b;
                }
                // The modulus of a vector, a column or a row, is its norm, a number; any other function of one argument
                // is the function of each entry, the shape of the argument.
                case Entity.Absf(var argument) when IsVector(ShapeOf(argument, isMatrix)):
                    return null;
                case IUnaryNode function:
                    return ShapeOf(function.NodeChild, isMatrix);
                case IBinaryNode function when IsElementwiseBinary(expr, firstIsMatrix: false):
                {
                    var (a, b) = (ShapeOf(function.NodeFirstChild, isMatrix), ShapeOf(function.NodeSecondChild, isMatrix));
                    if (Known(a) && Known(b) && a != b)
                        throw new InvalidMatrixOperationException(
                            $"`{expr.Stringize()}` has no value: matrices of shapes {Show(a)} and {Show(b)} are not combined entry by entry");
                    return Merged(a, b);
                }
                case Entity.Transposef(var argument):
                    return ShapeOf(argument, isMatrix) is { } transposed ? (transposed.Columns, transposed.Rows) : null;
                case Entity.Determinantf or Entity.Tracef:
                {
                    var a = ShapeOf(expr.DirectChildren[0], isMatrix);
                    if (Known(a) && a!.Value.Rows != a.Value.Columns)
                        throw new InvalidMatrixOperationException($"`{expr.Stringize()}` has no value: a {Show(a)} matrix is not square");
                    return null;
                }
                case Entity.MatrixEntryf(var argument, var row, var column):
                {
                    var a = ShapeOf(argument, isMatrix);
                    ShapeOf(row, isMatrix);
                    ShapeOf(column, isMatrix);
                    if (Known(a) && (row is Entity.Number.Integer i && (i < 1 || i > a!.Value.Rows) || column is Entity.Number.Integer j && (j < 1 || j > a!.Value.Columns)))
                        throw new InvalidMatrixOperationException($"`{expr.Stringize()}` has no value: a {Show(a)} matrix has no such entry, its rows and columns counted from 1");
                    return null;
                }
                default:
                    foreach (var child in expr.DirectChildren)
                        ShapeOf(child, isMatrix);
                    return null;
            }
        }
    }
}
