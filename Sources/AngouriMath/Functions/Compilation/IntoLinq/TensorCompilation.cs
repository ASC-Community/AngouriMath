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
    public static class TensorCompilation
    {
        /// <summary>The closed methods for one element type.</summary>
        private sealed class Kit
        {
            internal Type Tensor { get; }
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

            private Kit(Type tensor, MethodInfo empty, MethodInfo set, MethodInfo add, MethodInfo subtract, MethodInfo multiply,
                MethodInfo addScalar, MethodInfo subtractScalar, MethodInfo subtractFromScalar, MethodInfo multiplyScalar,
                MethodInfo divideScalar, MethodInfo power)
                => (Tensor, Empty, Set, Add, Subtract, Multiply, AddScalar, SubtractScalar, SubtractFromScalar, MultiplyScalar, DivideScalar, Power)
                    = (tensor, empty, set, add, subtract, multiply, addScalar, subtractScalar, subtractFromScalar, multiplyScalar, divideScalar, power);

            // An expression tree is how C# names a method without a string: the call inside it
            // is resolved at compile time, closed over T and TWrapper, and rooted for the trimmer.
            private static MethodInfo Named<TDelegate>(Expression<TDelegate> e) => ((MethodCallExpression)e.Body).Method;

            internal static Kit For<T, TWrapper>() where TWrapper : struct, IOperations<T>
                => new(typeof(GenTensor<T, TWrapper>),
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
                    Named<Func<GenTensor<T, TWrapper>, int, GenTensor<T, TWrapper>>>((a, n) => TensorCompilation.Power(a, n)));
        }

        [ConstantField]
        private static readonly Dictionary<Type, Kit> kits = new()
        {
            { typeof(double), Kit.For<double, DoubleOperations>() },
            { typeof(float), Kit.For<float, SingleOperations>() },
            { typeof(Complex), Kit.For<Complex, ComplexOperations>() },
            { typeof(int), Kit.For<int, Int32Operations>() },
            { typeof(long), Kit.For<long, Int64Operations>() },
            { typeof(BigInteger), Kit.For<BigInteger, BigIntegerOperations>() },
        };

        [ConstantField]
        private static readonly Dictionary<Type, Kit> kitsByTensor = kits.Values.ToDictionary(kit => kit.Tensor);

        /// <summary>The tensor type a matrix over <paramref name="element"/> compiles to, or <see langword="null"/> where there is none.</summary>
        public static Type? TensorTypeOf(Type element)
            => kits.TryGetValue(element, out var kit) ? kit.Tensor : null;

        internal static bool IsTensor(Type type) => kitsByTensor.ContainsKey(type);

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
        /// <summary>The matrix power, a non-negative whole exponent, by repeated squaring.</summary>
        public static GenTensor<T, TWrapper> Power<T, TWrapper>(GenTensor<T, TWrapper> a, int exponent) where TWrapper : struct, IOperations<T>
        {
            if (a.Shape[0] != a.Shape[1])
                throw new InvalidMatrixOperationException($"A matrix of shape {a.Shape} is not square and has no power");
            if (exponent < 0)
                throw new NotSupportedException("A negative matrix power is not compiled: invert the matrix first");
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
                    _ => null,
                };
            }
            if (leftTensor)
            {
                var scalar = node is Entity.Powf ? convert(right, typeof(int)) : convert(right, ElementOf(left.Type));
                return node switch
                {
                    Entity.Sumf => Expression.Call(leftKit!.AddScalar, left, scalar),
                    Entity.Minusf => Expression.Call(leftKit!.SubtractScalar, left, scalar),
                    Entity.Mulf => Expression.Call(leftKit!.MultiplyScalar, left, scalar),
                    Entity.Divf => Expression.Call(leftKit!.DivideScalar, left, scalar),
                    Entity.Powf => Expression.Call(leftKit!.Power, left, scalar),
                    _ => null,
                };
            }
            var value = convert(left, ElementOf(right.Type));
            return node switch
            {
                Entity.Sumf => Expression.Call(rightKit!.AddScalar, right, value),
                Entity.Minusf => Expression.Call(rightKit!.SubtractFromScalar, value, right),
                Entity.Mulf => Expression.Call(rightKit!.MultiplyScalar, right, value),
                // A scalar over a matrix is the scalar times its inverse, as evaluation has it, and the
                // inverse is not compiled, as a negative power is not: dividing by every element
                // instead gave 2 / [[1, 2], [3, 4]] as [[2, 1], [2/3, 1/2]].
                // https://github.com/asc-community/AngouriMath/issues/2287
                Entity.Divf => throw new UncompilableNodeException(
                    "A scalar over a matrix is the scalar times its inverse, which is not compiled: invert the matrix first"),
                _ => null,
            };
        }
    }
}
