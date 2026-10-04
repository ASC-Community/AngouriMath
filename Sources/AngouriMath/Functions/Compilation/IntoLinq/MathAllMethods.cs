
//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

/* THIS FILE IS AUTO-GENERATED */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace AngouriMath.Core.Compilation.IntoLinq
{
    // TODO: to improve some of those functions
    internal static class MathAllMethods
    {

        /* POWERS */

        public static System.Numerics.Complex Log(System.Numerics.Complex a, System.Numerics.Complex b)
            => System.Numerics.Complex.Log(b) / System.Numerics.Complex.Log(a);
        public static double Log(double a, double b)
            => Math.Log(b, a);
        public static float Log(float a, float b)
            => (float)Math.Log(b, a);
        public static long Log(long a, long b)
            => (long)Math.Log(b, a);
        public static int Log(int a, int b)
            => (int)Math.Log(b, a);
        public static BigInteger Log(BigInteger a, BigInteger b)
            => (BigInteger)Math.Log((double)b, (double)a);


        public static System.Numerics.Complex Pow(System.Numerics.Complex a, System.Numerics.Complex b)
            => System.Numerics.Complex.Pow(a, b);
        public static double Pow(double a, double b)
            => Math.Pow(a, b);
        public static float Pow(float a, float b)
            => (float)Math.Pow(a, b);
        public static long Pow(long a, long b)
            => (long)Math.Pow(a, b);
        public static int Pow(int a, int b)
            => (int)Math.Pow(a, b);
        public static BigInteger Pow(BigInteger a, BigInteger b)
            => (BigInteger)Math.Pow((double)a, (double)b);

        /* BUILT-IN TRIGONOMETRY */


        public static System.Numerics.Complex Sin(System.Numerics.Complex a)
            => System.Numerics.Complex.Sin(a);
        public static double Sin(double a)
            => Math.Sin(a);
        public static float Sin(float a)
            => (float)Math.Sin(a);
        public static long Sin(long a)
            => (long)Math.Sin(a);
        public static int Sin(int a)
            => (int)Math.Sin(a);
        public static BigInteger Sin(BigInteger a)
            => (BigInteger)Math.Sin((double)a);


        public static System.Numerics.Complex Cos(System.Numerics.Complex a)
            => System.Numerics.Complex.Cos(a);
        public static double Cos(double a)
            => Math.Cos(a);
        public static float Cos(float a)
            => (float)Math.Cos(a);
        public static long Cos(long a)
            => (long)Math.Cos(a);
        public static int Cos(int a)
            => (int)Math.Cos(a);
        public static BigInteger Cos(BigInteger a)
            => (BigInteger)Math.Cos((double)a);


        public static System.Numerics.Complex Tan(System.Numerics.Complex a)
            => System.Numerics.Complex.Tan(a);
        public static double Tan(double a)
            => Math.Tan(a);
        public static float Tan(float a)
            => (float)Math.Tan(a);
        public static long Tan(long a)
            => (long)Math.Tan(a);
        public static int Tan(int a)
            => (int)Math.Tan(a);
        public static BigInteger Tan(BigInteger a)
            => (BigInteger)Math.Tan((double)a);


        public static System.Numerics.Complex Asin(System.Numerics.Complex a)
            => AngouriMath.Core.Compilation.ComplexBranches.Arcsin(a);
        public static double Asin(double a)
            => Math.Asin(a);
        public static float Asin(float a)
            => (float)Math.Asin(a);
        public static long Asin(long a)
            => (long)Math.Asin(a);
        public static int Asin(int a)
            => (int)Math.Asin(a);
        public static BigInteger Asin(BigInteger a)
            => (BigInteger)Math.Asin((double)a);


        public static System.Numerics.Complex Acos(System.Numerics.Complex a)
            => System.Numerics.Complex.Acos(a);
        public static double Acos(double a)
            => Math.Acos(a);
        public static float Acos(float a)
            => (float)Math.Acos(a);
        public static long Acos(long a)
            => (long)Math.Acos(a);
        public static int Acos(int a)
            => (int)Math.Acos(a);
        public static BigInteger Acos(BigInteger a)
            => (BigInteger)Math.Acos((double)a);


        public static System.Numerics.Complex Atan(System.Numerics.Complex a)
            => System.Numerics.Complex.Atan(a);
        public static double Atan(double a)
            => Math.Atan(a);
        public static float Atan(float a)
            => (float)Math.Atan(a);
        public static long Atan(long a)
            => (long)Math.Atan(a);
        public static int Atan(int a)
            => (int)Math.Atan(a);
        public static BigInteger Atan(BigInteger a)
            => (BigInteger)Math.Atan((double)a);


        /* POST-INVERSE TRIGONOMETRY */


        public static System.Numerics.Complex Cot(System.Numerics.Complex a)
            => 1 / System.Numerics.Complex.Tan(a);
        public static double Cot(double a)
            => 1 / Math.Tan(a);
        public static float Cot(float a)
            => (float)(1 / Math.Tan(a));
        public static long Cot(long a)
            => (long)(1 / Math.Tan(a));
        public static int Cot(int a)
            => (int)(1 / Math.Tan(a));
        public static BigInteger Cot(BigInteger a)
            => (BigInteger)(1 / Math.Tan((double)a));


        public static System.Numerics.Complex Sec(System.Numerics.Complex a)
            => 1 / System.Numerics.Complex.Cos(a);
        public static double Sec(double a)
            => 1 / Math.Cos(a);
        public static float Sec(float a)
            => (float)(1 / Math.Cos(a));
        public static long Sec(long a)
            => (long)(1 / Math.Cos(a));
        public static int Sec(int a)
            => (int)(1 / Math.Cos(a));
        public static BigInteger Sec(BigInteger a)
            => (BigInteger)(1 / Math.Cos((double)a));


        public static System.Numerics.Complex Csc(System.Numerics.Complex a)
            => 1 / System.Numerics.Complex.Sin(a);
        public static double Csc(double a)
            => 1 / Math.Sin(a);
        public static float Csc(float a)
            => (float)(1 / Math.Sin(a));
        public static long Csc(long a)
            => (long)(1 / Math.Sin(a));
        public static int Csc(int a)
            => (int)(1 / Math.Sin(a));
        public static BigInteger Csc(BigInteger a)
            => (BigInteger)(1 / Math.Sin((double)a));


        /* PRE-INVERSE TRIGONOMETRY */


        public static System.Numerics.Complex Acot(System.Numerics.Complex a)
            => System.Numerics.Complex.Atan(1 / a);
        public static double Acot(double a)
            => Math.Atan(1 / a);
        public static float Acot(float a)
            => (float)Math.Atan(1 / a);
        public static long Acot(long a)
            => (long)Math.Atan(1 / a);
        public static int Acot(int a)
            => (int)Math.Atan(1 / a);
        public static BigInteger Acot(BigInteger a)
            => (BigInteger)Math.Atan(1 / (double)a);


        public static System.Numerics.Complex Asec(System.Numerics.Complex a)
            => System.Numerics.Complex.Acos(1 / a);
        public static double Asec(double a)
            => Math.Acos(1 / a);
        public static float Asec(float a)
            => (float)Math.Acos(1 / a);
        public static long Asec(long a)
            => (long)Math.Acos(1 / a);
        public static int Asec(int a)
            => (int)Math.Acos(1 / a);
        public static BigInteger Asec(BigInteger a)
            => (BigInteger)Math.Acos(1 / (double)a);


        public static System.Numerics.Complex Acsc(System.Numerics.Complex a)
            => AngouriMath.Core.Compilation.ComplexBranches.Arccosecant(a);
        public static double Acsc(double a)
            => Math.Asin(1 / a);
        public static float Acsc(float a)
            => (float)Math.Asin(1 / a);
        public static long Acsc(long a)
            => (long)Math.Asin(1 / a);
        public static int Acsc(int a)
            => (int)Math.Asin(1 / a);
        public static BigInteger Acsc(BigInteger a)
            => (BigInteger)Math.Asin(1 / (double)a);

        
        /* OTHER */
        public static System.Numerics.Complex Abs(System.Numerics.Complex a) => System.Numerics.Complex.Abs(a);
        public static double Abs(double a) => Math.Abs(a);
        public static float Abs(float a) => Math.Abs(a);
        public static long Abs(long a) => Math.Abs(a);
        public static int Abs(int a) => Math.Abs(a);
        public static BigInteger Abs(BigInteger a) => BigInteger.Abs(a);

        public static System.Numerics.Complex Sgn(System.Numerics.Complex a) => a / System.Numerics.Complex.Abs(a);
        public static double Sgn(double a) => a switch { > 0 or double.PositiveInfinity => 1, 0 => 0, < 0 or double.NegativeInfinity => -1, _ => double.NaN };
        public static float Sgn(float a) => a switch { > 0 or float.PositiveInfinity => 1, 0 => 0, < 0 or float.NegativeInfinity => -1, _ => float.NaN };
        public static long Sgn(long a) => a switch { > 0 => 1, 0 => 0, < 0 => -1 };
        public static int Sgn(int a) => a switch { > 0 => 1, 0 => 0, < 0 => -1 };
        public static BigInteger Sgn(BigInteger a)
        {
            if (a > BigInteger.Zero)
                return 1;
            if (a < BigInteger.Zero)
                return -1;
            return 0;
        }

        // The floors and the rounding, componentwise on a complex number and to even at a half,
        // as the interpreter takes them; an integer is its own. And the extremes, which have no
        // value for two complex numbers that differ and are not both real, since those are not
        // ordered. https://github.com/asc-community/AngouriMath/issues/1603
        public static System.Numerics.Complex Floor(System.Numerics.Complex a) => new(Math.Floor(a.Real), Math.Floor(a.Imaginary));
        public static double Floor(double a) => Math.Floor(a);
        public static float Floor(float a) => (float)Math.Floor(a);
        public static long Floor(long a) => a;
        public static int Floor(int a) => a;
        public static BigInteger Floor(BigInteger a) => a;

        public static System.Numerics.Complex Ceil(System.Numerics.Complex a) => new(Math.Ceiling(a.Real), Math.Ceiling(a.Imaginary));
        public static double Ceil(double a) => Math.Ceiling(a);
        public static float Ceil(float a) => (float)Math.Ceiling(a);
        public static long Ceil(long a) => a;
        public static int Ceil(int a) => a;
        public static BigInteger Ceil(BigInteger a) => a;

        public static System.Numerics.Complex Round(System.Numerics.Complex a)
            => new(Math.Round(a.Real, MidpointRounding.ToEven), Math.Round(a.Imaginary, MidpointRounding.ToEven));
        public static double Round(double a) => Math.Round(a, MidpointRounding.ToEven);
        public static float Round(float a) => (float)Math.Round(a, MidpointRounding.ToEven);
        public static long Round(long a) => a;
        public static int Round(int a) => a;
        public static BigInteger Round(BigInteger a) => a;

        public static System.Numerics.Complex Max(System.Numerics.Complex a, System.Numerics.Complex b) => AngouriMath.Core.Compilation.RealOnly.Max(a, b);
        public static double Max(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? double.NaN : Math.Max(a, b);
        public static float Max(float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? float.NaN : Math.Max(a, b);
        public static long Max(long a, long b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static BigInteger Max(BigInteger a, BigInteger b) => BigInteger.Max(a, b);

        public static System.Numerics.Complex Min(System.Numerics.Complex a, System.Numerics.Complex b) => AngouriMath.Core.Compilation.RealOnly.Min(a, b);
        public static double Min(double a, double b) => double.IsNaN(a) || double.IsNaN(b) ? double.NaN : Math.Min(a, b);
        public static float Min(float a, float b) => float.IsNaN(a) || float.IsNaN(b) ? float.NaN : Math.Min(a, b);
        public static long Min(long a, long b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static BigInteger Min(BigInteger a, BigInteger b) => BigInteger.Min(a, b);

        // Euler's totient: exact over the integers, and over Complex and double a value at a whole
        // number and NaN at any other. https://github.com/asc-community/AngouriMath/issues/1607
        public static System.Numerics.Complex Phi(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Phi(a);
        public static double Phi(double a) => AngouriMath.Numerics.SpecialFunctions.Phi(a);
        public static float Phi(float a) => (float)AngouriMath.Numerics.SpecialFunctions.Phi(a);
        public static long Phi(long a) => a.Phi();
        public static int Phi(int a) => (int)((long)a).Phi();
        public static BigInteger Phi(BigInteger a) => PeterO.Numbers.EInteger.FromBytes(a.ToByteArray(), littleEndian: true).Phi().ToBigInteger();

        // The special functions, in double precision as the kernels define them, and the factorial,
        // the gamma function one along. https://github.com/asc-community/AngouriMath/issues/1607
        public static System.Numerics.Complex Erf(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Erf(a);
        public static double Erf(double a) => AngouriMath.Numerics.SpecialFunctions.Erf(a);
        public static System.Numerics.Complex Erfc(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Erfc(a);
        public static double Erfc(double a) => AngouriMath.Numerics.SpecialFunctions.Erfc(a);
        public static System.Numerics.Complex Erfi(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Erfi(a);
        public static double Erfi(double a) => AngouriMath.Numerics.SpecialFunctions.Erfi(a);
        public static System.Numerics.Complex Ei(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Ei(a);
        public static double Ei(double a) => AngouriMath.Numerics.SpecialFunctions.Ei(a);
        public static System.Numerics.Complex Li(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Li(a);
        public static double Li(double a) => AngouriMath.Numerics.SpecialFunctions.Li(a);
        public static System.Numerics.Complex Si(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Si(a);
        public static double Si(double a) => AngouriMath.Numerics.SpecialFunctions.Si(a);
        public static System.Numerics.Complex Ci(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Ci(a);
        public static double Ci(double a) => AngouriMath.Numerics.SpecialFunctions.Ci(a);
        public static System.Numerics.Complex Shi(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Shi(a);
        public static double Shi(double a) => AngouriMath.Numerics.SpecialFunctions.Shi(a);
        public static System.Numerics.Complex Chi(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Chi(a);
        public static double Chi(double a) => AngouriMath.Numerics.SpecialFunctions.Chi(a);
        public static System.Numerics.Complex Factorial(System.Numerics.Complex a) => AngouriMath.Numerics.SpecialFunctions.Factorial(a);
        public static double Factorial(double a) => AngouriMath.Numerics.SpecialFunctions.Factorial(a);

        /// <summary>
        /// True where <c>System.Numerics.Complex.IsNaN</c> is, spelled out because that overload is
        /// .NET 7 and later and this assembly also targets netstandard2.0. A complex number is NaN
        /// when neither part is infinite and at least one of them is not finite -- (NaN, +oo) is an
        /// infinity, not a NaN.
        /// </summary>
        public static bool IsNaN(System.Numerics.Complex a)
            => !(double.IsInfinity(a.Real) || double.IsInfinity(a.Imaginary))
               && !(IsFinite(a.Real) && IsFinite(a.Imaginary));

        private static bool IsFinite(double d) => !double.IsNaN(d) && !double.IsInfinity(d);

        /// <summary>
        /// What <see cref="CompilationProtocol"/> dispatches through, in place of looking a method
        /// up on this class by name at run time. A name resolved at run time is invisible to the
        /// trimmer and to the NativeAOT compiler, which is what
        /// https://github.com/asc-community/AngouriMath/issues/363 is about; every entry here is a
        /// direct reference the compiler emits an ldtoken for, so the method is reachable by
        /// construction. It is also a dictionary lookup rather than an overload resolution, which
        /// is what it costs per node of every compiled expression.
        /// </summary>
        [ConstantField]
        internal static readonly Dictionary<(string Name, int ArgCount, Type Type), MethodInfo> Definitions = new()
        {
            { ("Log", 2, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>((a, b) => Log(a, b)) },
            { ("Log", 2, typeof(double)), Def<double>((a, b) => Log(a, b)) },
            { ("Log", 2, typeof(float)), Def<float>((a, b) => Log(a, b)) },
            { ("Log", 2, typeof(long)), Def<long>((a, b) => Log(a, b)) },
            { ("Log", 2, typeof(int)), Def<int>((a, b) => Log(a, b)) },
            { ("Log", 2, typeof(BigInteger)), Def<BigInteger>((a, b) => Log(a, b)) },
            { ("Pow", 2, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>((a, b) => Pow(a, b)) },
            { ("Pow", 2, typeof(double)), Def<double>((a, b) => Pow(a, b)) },
            { ("Pow", 2, typeof(float)), Def<float>((a, b) => Pow(a, b)) },
            { ("Pow", 2, typeof(long)), Def<long>((a, b) => Pow(a, b)) },
            { ("Pow", 2, typeof(int)), Def<int>((a, b) => Pow(a, b)) },
            { ("Pow", 2, typeof(BigInteger)), Def<BigInteger>((a, b) => Pow(a, b)) },
            { ("Max", 2, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>((a, b) => Max(a, b)) },
            { ("Max", 2, typeof(double)), Def<double>((a, b) => Max(a, b)) },
            { ("Max", 2, typeof(float)), Def<float>((a, b) => Max(a, b)) },
            { ("Max", 2, typeof(long)), Def<long>((a, b) => Max(a, b)) },
            { ("Max", 2, typeof(int)), Def<int>((a, b) => Max(a, b)) },
            { ("Max", 2, typeof(BigInteger)), Def<BigInteger>((a, b) => Max(a, b)) },
            { ("Min", 2, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>((a, b) => Min(a, b)) },
            { ("Min", 2, typeof(double)), Def<double>((a, b) => Min(a, b)) },
            { ("Min", 2, typeof(float)), Def<float>((a, b) => Min(a, b)) },
            { ("Min", 2, typeof(long)), Def<long>((a, b) => Min(a, b)) },
            { ("Min", 2, typeof(int)), Def<int>((a, b) => Min(a, b)) },
            { ("Min", 2, typeof(BigInteger)), Def<BigInteger>((a, b) => Min(a, b)) },
            { ("Sin", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Sin(a)) },
            { ("Sin", 1, typeof(double)), Def<double>(a => Sin(a)) },
            { ("Sin", 1, typeof(float)), Def<float>(a => Sin(a)) },
            { ("Sin", 1, typeof(long)), Def<long>(a => Sin(a)) },
            { ("Sin", 1, typeof(int)), Def<int>(a => Sin(a)) },
            { ("Sin", 1, typeof(BigInteger)), Def<BigInteger>(a => Sin(a)) },
            { ("Cos", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Cos(a)) },
            { ("Cos", 1, typeof(double)), Def<double>(a => Cos(a)) },
            { ("Cos", 1, typeof(float)), Def<float>(a => Cos(a)) },
            { ("Cos", 1, typeof(long)), Def<long>(a => Cos(a)) },
            { ("Cos", 1, typeof(int)), Def<int>(a => Cos(a)) },
            { ("Cos", 1, typeof(BigInteger)), Def<BigInteger>(a => Cos(a)) },
            { ("Tan", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Tan(a)) },
            { ("Tan", 1, typeof(double)), Def<double>(a => Tan(a)) },
            { ("Tan", 1, typeof(float)), Def<float>(a => Tan(a)) },
            { ("Tan", 1, typeof(long)), Def<long>(a => Tan(a)) },
            { ("Tan", 1, typeof(int)), Def<int>(a => Tan(a)) },
            { ("Tan", 1, typeof(BigInteger)), Def<BigInteger>(a => Tan(a)) },
            { ("Asin", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Asin(a)) },
            { ("Asin", 1, typeof(double)), Def<double>(a => Asin(a)) },
            { ("Asin", 1, typeof(float)), Def<float>(a => Asin(a)) },
            { ("Asin", 1, typeof(long)), Def<long>(a => Asin(a)) },
            { ("Asin", 1, typeof(int)), Def<int>(a => Asin(a)) },
            { ("Asin", 1, typeof(BigInteger)), Def<BigInteger>(a => Asin(a)) },
            { ("Acos", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Acos(a)) },
            { ("Acos", 1, typeof(double)), Def<double>(a => Acos(a)) },
            { ("Acos", 1, typeof(float)), Def<float>(a => Acos(a)) },
            { ("Acos", 1, typeof(long)), Def<long>(a => Acos(a)) },
            { ("Acos", 1, typeof(int)), Def<int>(a => Acos(a)) },
            { ("Acos", 1, typeof(BigInteger)), Def<BigInteger>(a => Acos(a)) },
            { ("Atan", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Atan(a)) },
            { ("Atan", 1, typeof(double)), Def<double>(a => Atan(a)) },
            { ("Atan", 1, typeof(float)), Def<float>(a => Atan(a)) },
            { ("Atan", 1, typeof(long)), Def<long>(a => Atan(a)) },
            { ("Atan", 1, typeof(int)), Def<int>(a => Atan(a)) },
            { ("Atan", 1, typeof(BigInteger)), Def<BigInteger>(a => Atan(a)) },
            { ("Cot", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Cot(a)) },
            { ("Cot", 1, typeof(double)), Def<double>(a => Cot(a)) },
            { ("Cot", 1, typeof(float)), Def<float>(a => Cot(a)) },
            { ("Cot", 1, typeof(long)), Def<long>(a => Cot(a)) },
            { ("Cot", 1, typeof(int)), Def<int>(a => Cot(a)) },
            { ("Cot", 1, typeof(BigInteger)), Def<BigInteger>(a => Cot(a)) },
            { ("Sec", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Sec(a)) },
            { ("Sec", 1, typeof(double)), Def<double>(a => Sec(a)) },
            { ("Sec", 1, typeof(float)), Def<float>(a => Sec(a)) },
            { ("Sec", 1, typeof(long)), Def<long>(a => Sec(a)) },
            { ("Sec", 1, typeof(int)), Def<int>(a => Sec(a)) },
            { ("Sec", 1, typeof(BigInteger)), Def<BigInteger>(a => Sec(a)) },
            { ("Csc", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Csc(a)) },
            { ("Csc", 1, typeof(double)), Def<double>(a => Csc(a)) },
            { ("Csc", 1, typeof(float)), Def<float>(a => Csc(a)) },
            { ("Csc", 1, typeof(long)), Def<long>(a => Csc(a)) },
            { ("Csc", 1, typeof(int)), Def<int>(a => Csc(a)) },
            { ("Csc", 1, typeof(BigInteger)), Def<BigInteger>(a => Csc(a)) },
            { ("Acot", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Acot(a)) },
            { ("Acot", 1, typeof(double)), Def<double>(a => Acot(a)) },
            { ("Acot", 1, typeof(float)), Def<float>(a => Acot(a)) },
            { ("Acot", 1, typeof(long)), Def<long>(a => Acot(a)) },
            { ("Acot", 1, typeof(int)), Def<int>(a => Acot(a)) },
            { ("Acot", 1, typeof(BigInteger)), Def<BigInteger>(a => Acot(a)) },
            { ("Asec", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Asec(a)) },
            { ("Asec", 1, typeof(double)), Def<double>(a => Asec(a)) },
            { ("Asec", 1, typeof(float)), Def<float>(a => Asec(a)) },
            { ("Asec", 1, typeof(long)), Def<long>(a => Asec(a)) },
            { ("Asec", 1, typeof(int)), Def<int>(a => Asec(a)) },
            { ("Asec", 1, typeof(BigInteger)), Def<BigInteger>(a => Asec(a)) },
            { ("Acsc", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Acsc(a)) },
            { ("Acsc", 1, typeof(double)), Def<double>(a => Acsc(a)) },
            { ("Acsc", 1, typeof(float)), Def<float>(a => Acsc(a)) },
            { ("Acsc", 1, typeof(long)), Def<long>(a => Acsc(a)) },
            { ("Acsc", 1, typeof(int)), Def<int>(a => Acsc(a)) },
            { ("Acsc", 1, typeof(BigInteger)), Def<BigInteger>(a => Acsc(a)) },
            { ("Abs", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Abs(a)) },
            { ("Abs", 1, typeof(double)), Def<double>(a => Abs(a)) },
            { ("Abs", 1, typeof(float)), Def<float>(a => Abs(a)) },
            { ("Abs", 1, typeof(long)), Def<long>(a => Abs(a)) },
            { ("Abs", 1, typeof(int)), Def<int>(a => Abs(a)) },
            { ("Abs", 1, typeof(BigInteger)), Def<BigInteger>(a => Abs(a)) },
            { ("Sgn", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Sgn(a)) },
            { ("Sgn", 1, typeof(double)), Def<double>(a => Sgn(a)) },
            { ("Sgn", 1, typeof(float)), Def<float>(a => Sgn(a)) },
            { ("Sgn", 1, typeof(long)), Def<long>(a => Sgn(a)) },
            { ("Sgn", 1, typeof(int)), Def<int>(a => Sgn(a)) },
            { ("Sgn", 1, typeof(BigInteger)), Def<BigInteger>(a => Sgn(a)) },
            { ("Floor", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Floor(a)) },
            { ("Floor", 1, typeof(double)), Def<double>(a => Floor(a)) },
            { ("Floor", 1, typeof(float)), Def<float>(a => Floor(a)) },
            { ("Floor", 1, typeof(long)), Def<long>(a => Floor(a)) },
            { ("Floor", 1, typeof(int)), Def<int>(a => Floor(a)) },
            { ("Floor", 1, typeof(BigInteger)), Def<BigInteger>(a => Floor(a)) },
            { ("Ceil", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Ceil(a)) },
            { ("Ceil", 1, typeof(double)), Def<double>(a => Ceil(a)) },
            { ("Ceil", 1, typeof(float)), Def<float>(a => Ceil(a)) },
            { ("Ceil", 1, typeof(long)), Def<long>(a => Ceil(a)) },
            { ("Ceil", 1, typeof(int)), Def<int>(a => Ceil(a)) },
            { ("Ceil", 1, typeof(BigInteger)), Def<BigInteger>(a => Ceil(a)) },
            { ("Round", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Round(a)) },
            { ("Round", 1, typeof(double)), Def<double>(a => Round(a)) },
            { ("Round", 1, typeof(float)), Def<float>(a => Round(a)) },
            { ("Round", 1, typeof(long)), Def<long>(a => Round(a)) },
            { ("Round", 1, typeof(int)), Def<int>(a => Round(a)) },
            { ("Round", 1, typeof(BigInteger)), Def<BigInteger>(a => Round(a)) },
            { ("Phi", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Phi(a)) },
            { ("Phi", 1, typeof(double)), Def<double>(a => Phi(a)) },
            { ("Phi", 1, typeof(float)), Def<float>(a => Phi(a)) },
            { ("Phi", 1, typeof(long)), Def<long>(a => Phi(a)) },
            { ("Phi", 1, typeof(int)), Def<int>(a => Phi(a)) },
            { ("Phi", 1, typeof(BigInteger)), Def<BigInteger>(a => Phi(a)) },
            { ("Erf", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Erf(a)) },
            { ("Erf", 1, typeof(double)), Def<double>(a => Erf(a)) },
            { ("Erfc", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Erfc(a)) },
            { ("Erfc", 1, typeof(double)), Def<double>(a => Erfc(a)) },
            { ("Erfi", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Erfi(a)) },
            { ("Erfi", 1, typeof(double)), Def<double>(a => Erfi(a)) },
            { ("Ei", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Ei(a)) },
            { ("Ei", 1, typeof(double)), Def<double>(a => Ei(a)) },
            { ("Li", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Li(a)) },
            { ("Li", 1, typeof(double)), Def<double>(a => Li(a)) },
            { ("Si", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Si(a)) },
            { ("Si", 1, typeof(double)), Def<double>(a => Si(a)) },
            { ("Ci", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Ci(a)) },
            { ("Ci", 1, typeof(double)), Def<double>(a => Ci(a)) },
            { ("Shi", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Shi(a)) },
            { ("Shi", 1, typeof(double)), Def<double>(a => Shi(a)) },
            { ("Chi", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Chi(a)) },
            { ("Chi", 1, typeof(double)), Def<double>(a => Chi(a)) },
            { ("Factorial", 1, typeof(System.Numerics.Complex)), Def<System.Numerics.Complex>(a => Factorial(a)) },
            { ("Factorial", 1, typeof(double)), Def<double>(a => Factorial(a)) },
        };

        // An expression tree is how C# spells "the MethodInfo of this method" without a string:
        // the compiler emits ldtoken for the call it contains, which both picks the overload at
        // compile time and roots the method for the trimmer.
        private static MethodInfo Def<T>(Expression<Func<T, T>> e)
            => ((MethodCallExpression)e.Body).Method;

        private static MethodInfo Def<T>(Expression<Func<T, T, T>> e)
            => ((MethodCallExpression)e.Body).Method;
    }
}
