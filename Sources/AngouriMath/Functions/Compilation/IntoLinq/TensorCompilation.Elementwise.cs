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
    // A function of a matrix argument, and vectors at the delegate's boundary.
    //
    // A function of a matrix means here what evaluation makes of it, which is the function of each entry:
    // ExpandOnOneArgument maps a one-argument function over a matrix, and ExpandOnTwoArguments maps a two-argument
    // one over a matrix and a number, or over two matrices of one shape. sin(A) is not the power series of A, and
    // exp(A) is not the matrix exponential; those are other functions, and nothing here computes them. The
    // exceptions are evaluation's own: a power with a matrix for its base is the matrix power, the whole-exponent
    // one being the only one evaluation answers, and the modulus of a column vector is its norm.
    // https://github.com/asc-community/AngouriMath/issues/526
    public static partial class TensorCompilation
    {
        /// <summary>The closed methods that map one element type onto another, entry by entry.</summary>
        private sealed class MapKit
        {
            internal MethodInfo Map { get; }
            internal MethodInfo Zip { get; }
            internal Func<Expression, ParameterExpression, LambdaExpression> Unary { get; }
            internal Func<Expression, ParameterExpression, ParameterExpression, LambdaExpression> Binary { get; }

            private MapKit(MethodInfo map, MethodInfo zip, Func<Expression, ParameterExpression, LambdaExpression> unary,
                Func<Expression, ParameterExpression, ParameterExpression, LambdaExpression> binary)
                => (Map, Zip, Unary, Binary) = (map, zip, unary, binary);

            private static MethodInfo Named<TDelegate>(Expression<TDelegate> e) => ((MethodCallExpression)e.Body).Method;

            // The delegate types are closed here too, so that no Func<,> is constructed when the tree is built.
            internal static KeyValuePair<(Type, Type), MapKit> For<T, TWrapper, U, UWrapper>()
                where TWrapper : struct, IOperations<T> where UWrapper : struct, IOperations<U>
                => new((typeof(T), typeof(U)), new MapKit(
                    Named<Func<GenTensor<T, TWrapper>, Func<T, U>, GenTensor<U, UWrapper>>>((a, f) => TensorCompilation.Map<T, TWrapper, U, UWrapper>(a, f)),
                    Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>, Func<T, T, U>, GenTensor<U, UWrapper>>>((a, b, f) => TensorCompilation.Zip<T, TWrapper, U, UWrapper>(a, b, f)),
                    (body, x) => Expression.Lambda<Func<T, U>>(body, x),
                    (body, x, y) => Expression.Lambda<Func<T, T, U>>(body, x, y)));
        }

        // Each element type onto itself, and the widenings a number takes: onto double from the narrower real types,
        // and onto Complex from every real one.
        [ConstantField]
        private static readonly Dictionary<(Type From, Type To), MapKit> mapKits = new[]
        {
            MapKit.For<double, DoubleOperations, double, DoubleOperations>(),
            MapKit.For<float, SingleOperations, float, SingleOperations>(),
            MapKit.For<Complex, ComplexOperations, Complex, ComplexOperations>(),
            MapKit.For<int, Int32Operations, int, Int32Operations>(),
            MapKit.For<long, Int64Operations, long, Int64Operations>(),
            MapKit.For<BigInteger, BigIntegerOperations, BigInteger, BigIntegerOperations>(),
            MapKit.For<float, SingleOperations, double, DoubleOperations>(),
            MapKit.For<int, Int32Operations, double, DoubleOperations>(),
            MapKit.For<long, Int64Operations, double, DoubleOperations>(),
            MapKit.For<BigInteger, BigIntegerOperations, double, DoubleOperations>(),
            MapKit.For<double, DoubleOperations, Complex, ComplexOperations>(),
            MapKit.For<float, SingleOperations, Complex, ComplexOperations>(),
            MapKit.For<int, Int32Operations, Complex, ComplexOperations>(),
            MapKit.For<long, Int64Operations, Complex, ComplexOperations>(),
            MapKit.For<BigInteger, BigIntegerOperations, Complex, ComplexOperations>(),
        }.ToDictionary(pair => pair.Key, pair => pair.Value);

        /// <summary>The closed methods for a vector, a one-dimensional array, of one element type.</summary>
        private sealed class VectorKit
        {
            internal Type Vector { get; }
            internal MethodInfo FromVector { get; }
            internal MethodInfo ToVector { get; }
            internal MethodInfo Norm { get; }
            internal MethodInfo NotAVector { get; }

            private VectorKit(Type vector, MethodInfo fromVector, MethodInfo toVector, MethodInfo norm, MethodInfo notAVector)
                => (Vector, FromVector, ToVector, Norm, NotAVector) = (vector, fromVector, toVector, norm, notAVector);

            private static MethodInfo Named<TDelegate>(Expression<TDelegate> e) => ((MethodCallExpression)e.Body).Method;

            internal static KeyValuePair<Type, VectorKit> For<T, TWrapper>() where TWrapper : struct, IOperations<T>
                => new(typeof(GenTensor<T, TWrapper>), new VectorKit(typeof(T[]),
                    Named<Func<T[], GenTensor<T, TWrapper>>>(v => TensorCompilation.FromVector<T, TWrapper>(v)),
                    Named<Func<GenTensor<T, TWrapper>, T[]>>(m => TensorCompilation.ToVector(m)),
                    Named<Func<GenTensor<T, TWrapper>, double>>(m => TensorCompilation.Norm(m)),
                    Named<Func<GenTensor<T, TWrapper>, GenTensor<T, TWrapper>>>(m => TensorCompilation.NotAVector(m))));
        }

        [ConstantField]
        private static readonly Dictionary<Type, VectorKit> vectorKitsByTensor = new[]
        {
            VectorKit.For<double, DoubleOperations>(),
            VectorKit.For<float, SingleOperations>(),
            VectorKit.For<Complex, ComplexOperations>(),
            VectorKit.For<int, Int32Operations>(),
            VectorKit.For<long, Int64Operations>(),
            VectorKit.For<BigInteger, BigIntegerOperations>(),
        }.ToDictionary(pair => pair.Key, pair => pair.Value);

        [ConstantField]
        private static readonly Dictionary<Type, (Type Tensor, VectorKit Kit)> vectorKitsByVector
            = vectorKitsByTensor.ToDictionary(pair => pair.Value.Vector, pair => (pair.Key, pair.Value));

        /// <summary>
        /// Whether <paramref name="type"/> is a one-dimensional array of an element type a matrix compiles over, as
        /// <c>double[]</c> is: an argument or a result of that type is a column vector, an n x 1 matrix, which is what
        /// a vector is in this library (<see cref="Entity.Matrix.IsVector"/>).
        /// </summary>
        internal static bool IsVectorArray(Type type) => vectorKitsByVector.ContainsKey(type);

        private static Expression FromVectorArgument(Expression vector) => Expression.Call(vectorKitsByVector[vector.Type].Kit.FromVector, vector);

        private static Expression? AsVector(Expression tensor, Type type)
            => vectorKitsByTensor.TryGetValue(tensor.Type, out var kit) && kit.Vector == type ? Expression.Call(kit.ToVector, tensor) : null;

        private static Type? ElementOfVectorType(Type type) => vectorKitsByVector.TryGetValue(type, out var kit) ? ElementOf(kit.Tensor) : null;

        /// <summary>A one-dimensional array as the column vector it is, an n x 1 matrix.</summary>
        /// <exception cref="InvalidMatrixOperationException">The array has no elements: there is no empty vector.</exception>
        internal static GenTensor<T, TWrapper> FromVector<T, TWrapper>(T[] vector) where TWrapper : struct, IOperations<T>
        {
            if (vector.Length == 0)
                throw new InvalidMatrixOperationException("An array with no elements is not a vector");
            var result = GenTensor<T, TWrapper>.CreateMatrix(vector.Length, 1);
            for (var i = 0; i < vector.Length; i++)
                result[i, 0] = vector[i];
            return result;
        }

        /// <summary>A column vector as a one-dimensional array.</summary>
        /// <exception cref="InvalidMatrixOperationException">The matrix has more than one column.</exception>
        internal static T[] ToVector<T, TWrapper>(GenTensor<T, TWrapper> matrix) where TWrapper : struct, IOperations<T>
        {
            if (matrix.Shape[1] != 1)
                throw new InvalidMatrixOperationException($"A matrix of shape {matrix.Shape} is not a column vector, and is given back as a two-dimensional array");
            var result = new T[matrix.Shape[0]];
            for (var i = 0; i < result.Length; i++)
                result[i] = matrix[i, 0];
            return result;
        }

        // |z| of an entry, for every element type a matrix compiles over. A typeof test on T is decided when the method
        // is compiled for T, ahead of time as well.
        private static double Modulus<T>(T value)
        {
            if (typeof(T) == typeof(double)) return Math.Abs((double)(object)value!);
            if (typeof(T) == typeof(float)) return Math.Abs((float)(object)value!);
            if (typeof(T) == typeof(Complex)) return Complex.Abs((Complex)(object)value!);
            if (typeof(T) == typeof(int)) return Math.Abs((double)(int)(object)value!);
            if (typeof(T) == typeof(long)) return Math.Abs((double)(long)(object)value!);
            if (typeof(T) == typeof(BigInteger)) return Math.Abs((double)(BigInteger)(object)value!);
            throw new AngouriBugException($"No modulus of {typeof(T)}");
        }

        /// <summary>
        /// The Euclidean norm of a vector, a column or a row, <c>sqrt(sum |v_k|^2)</c>: what <c>abs</c> of a vector
        /// evaluates to, over the complex numbers as well (https://github.com/asc-community/AngouriMath/issues/2423). Scaled by the
        /// largest modulus first, so that entries whose squares overflow a double still have a norm that does not.
        /// </summary>
        internal static double Norm<T, TWrapper>(GenTensor<T, TWrapper> vector) where TWrapper : struct, IOperations<T>
        {
            var scale = 0.0;
            for (var i = 0; i < vector.Shape[0]; i++)
                for (var j = 0; j < vector.Shape[1]; j++)
                {
                    var modulus = Modulus(vector[i, j]);
                    if (double.IsNaN(modulus))
                        return double.NaN;
                    scale = Math.Max(scale, modulus);
                }
            if (scale == 0 || double.IsInfinity(scale))
                return scale;
            var sum = 0.0;
            for (var i = 0; i < vector.Shape[0]; i++)
                for (var j = 0; j < vector.Shape[1]; j++)
                {
                    var ratio = Modulus(vector[i, j]) / scale;
                    sum += ratio * ratio;
                }
            return scale * Math.Sqrt(sum);
        }

        /// <summary>
        /// The matrix itself where <c>abs</c> of it is the modulus of each entry. A column or a row of more than one
        /// entry is a vector, and evaluation takes its norm, a number -- which a matrix whose shape is known only at the
        /// call cannot be compiled to. A 1 x 1 matrix passes: its norm and its entry's modulus are one number.
        /// </summary>
        /// <exception cref="InvalidMatrixOperationException">The matrix is a column or a row of more than one entry.</exception>
        internal static GenTensor<T, TWrapper> NotAVector<T, TWrapper>(GenTensor<T, TWrapper> matrix) where TWrapper : struct, IOperations<T>
            => (matrix.Shape[1] == 1) != (matrix.Shape[0] == 1)
                ? throw new InvalidMatrixOperationException(
                    $"abs of a matrix of shape {matrix.Shape} is the norm of a vector, a number: pass a column as a one-dimensional array, or write the row's shape into the expression")
                : matrix;

        /// <summary>The function of every entry.</summary>
        internal static GenTensor<U, UWrapper> Map<T, TWrapper, U, UWrapper>(GenTensor<T, TWrapper> matrix, Func<T, U> function)
            where TWrapper : struct, IOperations<T> where UWrapper : struct, IOperations<U>
        {
            var result = GenTensor<U, UWrapper>.CreateMatrix(matrix.Shape[0], matrix.Shape[1]);
            for (var i = 0; i < matrix.Shape[0]; i++)
                for (var j = 0; j < matrix.Shape[1]; j++)
                    result[i, j] = function(matrix[i, j]);
            return result;
        }

        /// <summary>The function of every pair of entries in one place.</summary>
        /// <exception cref="InvalidMatrixOperationException">The matrices are not of one shape.</exception>
        internal static GenTensor<U, UWrapper> Zip<T, TWrapper, U, UWrapper>(GenTensor<T, TWrapper> a, GenTensor<T, TWrapper> b, Func<T, T, U> function)
            where TWrapper : struct, IOperations<T> where UWrapper : struct, IOperations<U>
        {
            SameShape(a, b);
            var result = GenTensor<U, UWrapper>.CreateMatrix(a.Shape[0], a.Shape[1]);
            for (var i = 0; i < a.Shape[0]; i++)
                for (var j = 0; j < a.Shape[1]; j++)
                    result[i, j] = function(a[i, j], b[i, j]);
            return result;
        }

        // The element type a function's value at an entry is kept in: its own where the argument's widens to it -- a
        // sine of whole numbers is a matrix of doubles -- and the argument's where the value is real and the argument
        // complex, as the modulus of a complex entry is. A statement at each entry is a matrix of truth values, which
        // has no compiled form.
        private static Type ResultElement(Type argument, Type value, Entity node)
        {
            if ((Nullable.GetUnderlyingType(value) ?? value) == typeof(bool))
                throw new UncompilableNodeException($"{node.GetType().Name} of a matrix is a matrix of statements, which has no compiled form");
            if (Nullable.GetUnderlyingType(value) is not null)
                value = typeof(double);
            if (mapKits.ContainsKey((argument, value)))
                return value;
            if (argument == typeof(Complex))
                return argument;
            throw new UncompilableNodeException($"{node.GetType().Name} of a matrix over {argument.Name} has no compiled form, its value being {value.Name}");
        }

        /// <summary>
        /// A one-argument function of a matrix, compiled as the function of each entry, which is what evaluation makes
        /// of it. <paramref name="atAnEntry"/> compiles the function of one entry.
        /// </summary>
        internal static Expression ElementwiseUnary(Expression tensor, Entity node, Func<Expression, Expression> atAnEntry, Func<Expression, Type, Expression> convert)
        {
            // Of a column or a row known when compiling, abs is its norm, taken before this is reached. Here the shape
            // is not known until the call.
            if (node is Entity.Absf)
                tensor = Expression.Call(vectorKitsByTensor[tensor.Type].NotAVector, tensor);
            var element = ElementOf(tensor.Type);
            var entry = Expression.Parameter(element);
            var value = atAnEntry(entry);
            var result = ResultElement(element, value.Type, node);
            var kit = mapKits[(element, result)];
            return Expression.Call(kit.Map, tensor, kit.Unary(convert(value, result), entry));
        }

        /// <summary>
        /// Whether evaluation applies the two-argument function <paramref name="node"/> entry by entry where one or
        /// both of its arguments are matrices. A power with a matrix for its base is the matrix power, and equality of
        /// two matrices is one statement about the pair, so neither is.
        /// </summary>
        private static bool IsElementwiseBinary(Entity node, bool firstIsMatrix)
            => node switch
            {
                Entity.Powf => !firstIsMatrix,
                Entity.Logf or Entity.Maxf or Entity.Minf or Entity.Modf or Entity.Binomialf or Entity.FallingFactorialf
                    or Entity.RisingFactorialf or Entity.LambertWf or Entity.Polygammaf or Entity.DivisorSigmaf
                    or Entity.Gcdf or Entity.Lcmf => true,
                _ => false,
            };

        /// <summary>
        /// A two-argument function of a matrix and a number, or of two matrices of one shape, compiled as the function
        /// of each entry where evaluation applies it so, and <see langword="null"/> where it does not.
        /// <paramref name="atAnEntry"/> compiles the function of one pair.
        /// </summary>
        internal static Expression? ElementwiseBinary(Entity node, Expression left, Expression right,
            Func<Expression, Expression, Expression> atAnEntry, Func<Expression, Type, Expression> convert)
        {
            var (leftTensor, rightTensor) = (IsTensor(left.Type), IsTensor(right.Type));
            if (!IsElementwiseBinary(node, leftTensor))
                return null;
            var element = ElementOf((leftTensor ? left : right).Type);
            var x = Expression.Parameter(element);
            if (leftTensor && rightTensor)
            {
                var y = Expression.Parameter(element);
                var both = atAnEntry(x, y);
                var zipped = ResultElement(element, both.Type, node);
                var zip = mapKits[(element, zipped)];
                return Expression.Call(zip.Zip, left, right, zip.Binary(convert(both, zipped), x, y));
            }
            var value = leftTensor ? atAnEntry(x, right) : atAnEntry(left, x);
            var result = ResultElement(element, value.Type, node);
            var kit = mapKits[(element, result)];
            return Expression.Call(kit.Map, leftTensor ? left : right, kit.Unary(convert(value, result), x));
        }

        /// <summary>
        /// Two matrices over different element types, the narrower widened to the other's, as a number is: a matrix
        /// argument over double and a matrix of whole numbers written into the expression are added as two matrices
        /// over double. Unchanged where neither widens to the other.
        /// </summary>
        internal static (Expression Left, Expression Right) Widened(Expression left, Expression right, Func<Expression, Type, Expression> convert)
        {
            if (!IsTensor(left.Type) || !IsTensor(right.Type) || left.Type == right.Type)
                return (left, right);
            var (a, b) = (ElementOf(left.Type), ElementOf(right.Type));
            if (mapKits.ContainsKey((a, b)))
                return (Widen(left, b, convert), right);
            if (mapKits.ContainsKey((b, a)))
                return (left, Widen(right, a, convert));
            return (left, right);
        }

        private static Expression Widen(Expression tensor, Type to, Func<Expression, Type, Expression> convert)
        {
            var element = ElementOf(tensor.Type);
            var entry = Expression.Parameter(element);
            var kit = mapKits[(element, to)];
            return Expression.Call(kit.Map, tensor, kit.Unary(convert(entry, to), entry));
        }

        /// <summary>The norm of a column vector, as a <see cref="double"/>.</summary>
        internal static Expression Norm(Expression vector) => Expression.Call(vectorKitsByTensor[vector.Type].Norm, vector);

        /// <summary>
        /// The shape a compiled argument of <paramref name="type"/> has, as <see cref="ShapeOf"/> reads it: a vector
        /// is a column of a length not known until the call, a matrix is of a shape not known until then, and
        /// anything else is a number.
        /// </summary>
        internal static (int Rows, int Columns)? ShapeOfArgument(Type? type)
            => type is null ? null : IsVectorArray(type) ? (0, 1) : IsMatrixType(type) ? (0, 0) : null;

        /// <summary>The shape of <paramref name="expr"/>, as far as it can be read before the delegate is called.</summary>
        internal static (int Rows, int Columns)? Shape(Entity expr, Func<Entity.Variable, (int Rows, int Columns)?> argument)
            => ShapeOf(expr, argument);

        /// <summary>
        /// Every <c>abs</c> in <paramref name="expr"/> whose argument is known when compiling to be a vector, a column
        /// or a row, the innermost first: its value is the norm of the vector, a number, where of any other matrix it is a
        /// matrix.
        /// </summary>
        internal static IEnumerable<Entity.Absf> Norms(Entity expr, Func<Entity.Variable, (int Rows, int Columns)?> argument)
        {
            foreach (var child in expr.DirectChildren)
                foreach (var norm in Norms(child, argument))
                    yield return norm;
            if (expr is Entity.Absf abs && IsVector(ShapeOf(abs.Argument, argument)))
                yield return abs;
        }

        // A column, or a row: abs of either is its norm.
        // https://github.com/asc-community/AngouriMath/issues/2423
        internal static bool IsVector((int Rows, int Columns)? shape) => shape is { Columns: 1 } or { Rows: 1 };
    }
}
