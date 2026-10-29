//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq.Expressions;
using Complex = System.Numerics.Complex;
using System.Reflection;

namespace AngouriMath.Core.Compilation.IntoLinq
{
    /// <summary>
    /// The compiled statements, with a third truth value for one that has none: <c>i &gt; 1</c>
    /// compares numbers that are not ordered, and <c>NaN &gt; 1</c> compares with no number at all.
    /// Each is <see langword="null"/>, as <c>EvalNumerical</c> leaves either as <c>NaN</c>, and the
    /// connectives follow Kleene's tables, as <c>EvalNumerical</c>'s do: <c>(i &gt; 1) or true</c> is
    /// true, <c>(i &gt; 1) and false</c> is false, and <c>not (i &gt; 1)</c> has no value.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class ThreeValuedLogic
    {
        internal static bool? Less(Complex a, Complex b) => Ordered(a, b) ? a.Real < b.Real : null;
        internal static bool? LessOrEqual(Complex a, Complex b) => Ordered(a, b) ? a.Real <= b.Real : null;
        internal static bool? Greater(Complex a, Complex b) => Ordered(a, b) ? a.Real > b.Real : null;
        internal static bool? GreaterOrEqual(Complex a, Complex b) => Ordered(a, b) ? a.Real >= b.Real : null;
        internal static bool? Equal(Complex a, Complex b) => IsNaN(a) || IsNaN(b) ? null : a == b;

        internal static bool? Less(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? null : a < b;
        internal static bool? LessOrEqual(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? null : a <= b;
        internal static bool? Greater(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? null : a > b;
        internal static bool? GreaterOrEqual(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? null : a >= b;
        internal static bool? Equal(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? null : a == b;

        internal static bool? Less(float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? null : a < b;
        internal static bool? LessOrEqual(float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? null : a <= b;
        internal static bool? Greater(float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? null : a > b;
        internal static bool? GreaterOrEqual(float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? null : a >= b;
        internal static bool? Equal(float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? null : a == b;

        internal static bool? Not(bool? a) => a is { } value ? !value : null;
        internal static bool? And(bool? a, bool? b) => a == false || b == false ? false : a == true && b == true ? true : null;
        internal static bool? Or(bool? a, bool? b) => a == true || b == true ? true : a == false && b == false ? false : null;
        internal static bool? Xor(bool? a, bool? b) => a is { } left && b is { } right ? left != right : null;
        internal static bool? Implies(bool? a, bool? b) => Or(Not(a), b);

        /// <summary>Whether the statement holds: <see langword="false"/> where it does not, and where it has no truth value.</summary>
        internal static bool Holds(bool? a) => a == true;

        /// <summary>The Iverson bracket of a statement, which has no value where the statement has none.</summary>
        internal static int? Iverson(bool? a) => a is { } value ? (value ? 1 : 0) : null;

        // Two numbers are ordered where both are real: the complex numbers are not.
        private static bool Ordered(Complex a, Complex b)
            => a.Imaginary == 0 && b.Imaginary == 0 && !double.IsNaN(a.Real) && !double.IsNaN(b.Real);

        private static bool IsNaN(Complex a) => double.IsNaN(a.Real) || double.IsNaN(a.Imaginary);

        /// <summary>
        /// The comparison of two operands of one type, for the types whose comparisons can have no
        /// truth value, or <see langword="null"/>. Named rather than looked up by name, for the trimmer.
        /// </summary>
        internal static MethodInfo? Comparison(ExpressionType kind, Type type)
            => comparisons.TryGetValue((kind, type), out var method) ? method : null;

        [ConstantField] internal static readonly MethodInfo NotMethod = Unary(a => Not(a));
        [ConstantField] internal static readonly MethodInfo AndMethod = Binary((a, b) => And(a, b));
        [ConstantField] internal static readonly MethodInfo OrMethod = Binary((a, b) => Or(a, b));
        [ConstantField] internal static readonly MethodInfo XorMethod = Binary((a, b) => Xor(a, b));
        [ConstantField] internal static readonly MethodInfo ImpliesMethod = Binary((a, b) => Implies(a, b));
        [ConstantField] internal static readonly MethodInfo HoldsMethod = ((MethodCallExpression)((Expression<Func<bool?, bool>>)(a => Holds(a))).Body).Method;
        [ConstantField] internal static readonly MethodInfo IversonMethod = ((MethodCallExpression)((Expression<Func<bool?, int?>>)(a => Iverson(a))).Body).Method;

        [ConstantField] private static readonly Dictionary<(ExpressionType Kind, Type Type), MethodInfo> comparisons = new()
            {
                { (ExpressionType.LessThan, typeof(Complex)), Compared<Complex>((a, b) => Less(a, b)) },
                { (ExpressionType.LessThanOrEqual, typeof(Complex)), Compared<Complex>((a, b) => LessOrEqual(a, b)) },
                { (ExpressionType.GreaterThan, typeof(Complex)), Compared<Complex>((a, b) => Greater(a, b)) },
                { (ExpressionType.GreaterThanOrEqual, typeof(Complex)), Compared<Complex>((a, b) => GreaterOrEqual(a, b)) },
                { (ExpressionType.Equal, typeof(Complex)), Compared<Complex>((a, b) => Equal(a, b)) },
                { (ExpressionType.LessThan, typeof(double)), Compared<double>((a, b) => Less(a, b)) },
                { (ExpressionType.LessThanOrEqual, typeof(double)), Compared<double>((a, b) => LessOrEqual(a, b)) },
                { (ExpressionType.GreaterThan, typeof(double)), Compared<double>((a, b) => Greater(a, b)) },
                { (ExpressionType.GreaterThanOrEqual, typeof(double)), Compared<double>((a, b) => GreaterOrEqual(a, b)) },
                { (ExpressionType.Equal, typeof(double)), Compared<double>((a, b) => Equal(a, b)) },
                { (ExpressionType.LessThan, typeof(float)), Compared<float>((a, b) => Less(a, b)) },
                { (ExpressionType.LessThanOrEqual, typeof(float)), Compared<float>((a, b) => LessOrEqual(a, b)) },
                { (ExpressionType.GreaterThan, typeof(float)), Compared<float>((a, b) => Greater(a, b)) },
                { (ExpressionType.GreaterThanOrEqual, typeof(float)), Compared<float>((a, b) => GreaterOrEqual(a, b)) },
                { (ExpressionType.Equal, typeof(float)), Compared<float>((a, b) => Equal(a, b)) },
            };

        private static MethodInfo Compared<T>(Expression<Func<T, T, bool?>> e) => ((MethodCallExpression)e.Body).Method;
        private static MethodInfo Unary(Expression<Func<bool?, bool?>> e) => ((MethodCallExpression)e.Body).Method;
        private static MethodInfo Binary(Expression<Func<bool?, bool?, bool?>> e) => ((MethodCallExpression)e.Body).Method;
    }
}
