//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using static AngouriMath.Entity;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Numerics
{
    /// <summary>
    /// A closed interval of doubles, <c>[Low, High]</c>, that holds a real value: every operation
    /// rounds its bounds outward, so the exact value of what was evaluated is inside.
    /// </summary>
    internal readonly struct Interval
    {
        internal readonly double Low;
        internal readonly double High;

        internal Interval(double low, double high)
        {
            Low = low;
            High = high;
        }

        /// <summary>The one value, exactly.</summary>
        internal static Interval Exactly(double value) => new(value, value);

        /// <summary>A value known to within a unit in the last place either way.</summary>
        internal static Interval Around(double value) => new(Down(value), Up(value));

        internal bool IsFinite => IsFiniteDouble(Low) && IsFiniteDouble(High);
        internal bool ContainsZero => Low <= 0 && High >= 0;
        internal double Magnitude => Math.Max(Math.Abs(Low), Math.Abs(High));
        /// <summary>The distance of the interval from zero: zero where it contains zero.</summary>
        internal double Mignitude => ContainsZero ? 0 : Math.Min(Math.Abs(Low), Math.Abs(High));
        internal double Middle => Low / 2 + High / 2;

        /// <summary>Exactly zero, which arithmetic keeps exact: the imaginary part of a real value stays zero.</summary>
        internal bool IsZero => Low == 0 && High == 0;

        public static Interval operator +(Interval a, Interval b)
            => a.IsZero ? b : b.IsZero ? a : new(Down(a.Low + b.Low), Up(a.High + b.High));
        public static Interval operator -(Interval a, Interval b)
            => b.IsZero ? a : a.IsZero ? -b : new(Down(a.Low - b.High), Up(a.High - b.Low));
        public static Interval operator -(Interval a) => new(-a.High, -a.Low);

        /// <summary>
        /// The product, exactly zero where either factor is exactly zero, whatever the other --
        /// as the decimal evaluation has it, whose simplification takes <c>0 x</c> to <c>0</c>
        /// first. An answer's derivative can carry a term whose coefficient is exactly zero at
        /// the pinned values beside a factor with no value there, and reading the term as having
        /// none turned correct answers away: Rubi 6.6.7's <c>1/(a + b csch(c + d x)^2)</c>.
        /// </summary>
        public static Interval operator *(Interval a, Interval b)
        {
            if (a.IsZero || b.IsZero)
                return Exactly(0);
            var p1 = a.Low * b.Low;
            var p2 = a.Low * b.High;
            var p3 = a.High * b.Low;
            var p4 = a.High * b.High;
            return new(Down(Math.Min(Math.Min(p1, p2), Math.Min(p3, p4))), Up(Math.Max(Math.Max(p1, p2), Math.Max(p3, p4))));
        }

        /// <summary>The quotient, or an interval that is not finite where the divisor may be zero.</summary>
        public static Interval operator /(Interval a, Interval b)
        {
            if (a.IsZero && !b.ContainsZero)
                return Exactly(0);
            if (b.ContainsZero)
                return new(double.NegativeInfinity, double.PositiveInfinity);
            var q1 = a.Low / b.Low;
            var q2 = a.Low / b.High;
            var q3 = a.High / b.Low;
            var q4 = a.High / b.High;
            return new(Down(Math.Min(Math.Min(q1, q2), Math.Min(q3, q4))), Up(Math.Max(Math.Max(q1, q2), Math.Max(q3, q4))));
        }

        internal Interval Square()
        {
            if (ContainsZero)
                return new(0, Up(Math.Max(Low * Low, High * High)));
            var (small, large) = Low > 0 ? (Low, High) : (-High, -Low);
            return new(Down(small * small), Up(large * large));
        }

        internal Interval Exp() => new(Wider(Math.Exp(Low), -1), Wider(Math.Exp(High), 1));

        /// <summary>The natural logarithm of a positive interval; not finite otherwise.</summary>
        internal Interval Log()
            => Low > 0 ? new(Wider(Math.Log(Low), -1), Wider(Math.Log(High), 1)) : new(double.NaN, double.NaN);

        internal Interval Sqrt()
            => Low >= 0 ? new(Math.Max(0, Wider(Math.Sqrt(Low), -1)), Wider(Math.Sqrt(High), 1)) : new(double.NaN, double.NaN);

        // The sine and cosine have a derivative no larger than one, so over an interval of
        // half-width r they are within r of their value at the middle.
        internal Interval Sin() => AroundTheMiddle(Math.Sin, 1);
        internal Interval Cos() => AroundTheMiddle(Math.Cos, 1);

        internal Interval Atan() => new(Wider(Math.Atan(Low), -1), Wider(Math.Atan(High), 1));

        internal Interval Asin()
            => Low >= -1 && High <= 1 ? new(Wider(Math.Asin(Low), -1), Wider(Math.Asin(High), 1)) : new(double.NaN, double.NaN);

        internal Interval Acos()
            => Low >= -1 && High <= 1 ? new(Wider(Math.Acos(High), -1), Wider(Math.Acos(Low), 1)) : new(double.NaN, double.NaN);

        internal Interval Abs()
            => ContainsZero ? new(0, Magnitude) : Low > 0 ? this : -this;

        private Interval AroundTheMiddle(Func<double, double> function, double slope)
        {
            var middle = Middle;
            var value = function(middle);
            var reach = Up(Math.Max(Up(High - middle), Up(middle - Low)) * slope);
            // The function's error is relative to its value, so the value is widened before the
            // reach is added: widened after, the slack is a few units of the sum, which is far
            // smaller than a unit of the value where the reach nearly cancels it.
            return new(Math.Max(-1, Down(Wider(value, -1) - reach)), Math.Min(1, Up(Wider(value, 1) + reach)));
        }

        /// <summary>
        /// A library function's result moved four units in the last place outward, and a little
        /// more near zero: the platform's functions are within one of the true value, and the
        /// slack keeps the claim true where they are not quite.
        /// </summary>
        internal static double Wider(double value, int direction)
        {
            for (var i = 0; i < 4; i++)
                value = direction > 0 ? Up(value) : Down(value);
            return value + direction * 1e-300;
        }

        internal static bool IsFiniteDouble(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        internal static double Up(double value)
        {
            if (double.IsNaN(value) || double.IsPositiveInfinity(value))
                return value;
            if (value == 0)
                return double.Epsilon;
            var bits = BitConverter.DoubleToInt64Bits(value);
            return BitConverter.Int64BitsToDouble(value > 0 ? bits + 1 : bits - 1);
        }

        internal static double Down(double value) => -Up(-value);
    }

    /// <summary>A rectangle of two <see cref="Interval"/>s holding a complex value.</summary>
    internal readonly struct ComplexInterval
    {
        internal readonly Interval Re;
        internal readonly Interval Im;

        internal ComplexInterval(Interval re, Interval im)
        {
            Re = re;
            Im = im;
        }

        internal static ComplexInterval Real(Interval re) => new(re, Interval.Exactly(0));

        internal bool IsFinite => Re.IsFinite && Im.IsFinite;
        internal bool IsReal => Im.Low == 0 && Im.High == 0;
        internal double Magnitude => Math.Sqrt(Re.Magnitude * Re.Magnitude + Im.Magnitude * Im.Magnitude);

        public static ComplexInterval operator +(ComplexInterval a, ComplexInterval b) => new(a.Re + b.Re, a.Im + b.Im);
        public static ComplexInterval operator -(ComplexInterval a, ComplexInterval b) => new(a.Re - b.Re, a.Im - b.Im);
        public static ComplexInterval operator -(ComplexInterval a) => new(-a.Re, -a.Im);

        public static ComplexInterval operator *(ComplexInterval a, ComplexInterval b)
            => a.IsReal && b.IsReal ? Real(a.Re * b.Re) : new(a.Re * b.Re - a.Im * b.Im, a.Re * b.Im + a.Im * b.Re);

        public static ComplexInterval operator /(ComplexInterval a, ComplexInterval b)
        {
            if (b.IsReal)
                return new(a.Re / b.Re, a.IsReal ? Interval.Exactly(0) : a.Im / b.Re);
            var denominator = b.Re.Square() + b.Im.Square();
            return new((a.Re * b.Re + a.Im * b.Im) / denominator, (a.Im * b.Re - a.Re * b.Im) / denominator);
        }

        internal ComplexInterval Exp()
        {
            var modulus = Re.Exp();
            return IsReal ? Real(modulus) : new(modulus * Im.Cos(), modulus * Im.Sin());
        }

        /// <summary>
        /// The principal logarithm, where the rectangle neither holds zero nor straddles the
        /// negative real axis, the cut; not finite otherwise. Off the cut the argument is
        /// continuous on the rectangle and takes its extremes at the corners.
        /// </summary>
        internal ComplexInterval Log()
        {
            if (IsReal && Re.Low > 0)
                return Real(Re.Log());
            // Exactly on the negative real axis the principal value is ln|x| + i pi, the cut's
            // upper side, as the decimal evaluation has it: sqrt(-3) is i sqrt(3). Only a
            // rectangle that straddles the cut is undecided.
            if (IsReal && Re.High < 0)
                return new((-Re).Log(), Interval.Around(Math.PI));
            if (Re.High <= 0 && Im.ContainsZero || Re.ContainsZero && Im.ContainsZero)
                return new(new(double.NaN, double.NaN), new(double.NaN, double.NaN));
            var modulusSquared = Re.Square() + Im.Square();
            var logModulus = modulusSquared.Log();
            var a1 = Math.Atan2(Im.Low, Re.Low);
            var a2 = Math.Atan2(Im.Low, Re.High);
            var a3 = Math.Atan2(Im.High, Re.Low);
            var a4 = Math.Atan2(Im.High, Re.High);
            var argument = new Interval(
                Interval.Wider(Math.Min(Math.Min(a1, a2), Math.Min(a3, a4)), -1),
                Interval.Wider(Math.Max(Math.Max(a1, a2), Math.Max(a3, a4)), 1));
            return new(new(logModulus.Low / 2, logModulus.High / 2), argument);
        }

        /// <summary>A whole power by repeated squaring, a negative one as the reciprocal.</summary>
        internal ComplexInterval Pow(long exponent)
        {
            if (exponent < 0)
                return Real(Interval.Exactly(1)) / Pow(-exponent);
            var result = Real(Interval.Exactly(1));
            var power = this;
            while (exponent > 0)
            {
                if ((exponent & 1) == 1)
                    result *= power;
                exponent >>= 1;
                if (exponent > 0)
                    power *= power;
            }
            return result;
        }

        /// <summary>The principal power, <c>exp(w log z)</c>; not finite where the logarithm is not.</summary>
        internal ComplexInterval Pow(ComplexInterval exponent)
        {
            // A real square root of a real that is not negative, without the logarithm's detour.
            if (IsReal && Re.Low >= 0 && exponent.IsReal && exponent.Re.Low == 0.5 && exponent.Re.High == 0.5)
                return Real(Re.Sqrt());
            if (IsReal && Re.Low > 0 && exponent.IsReal)
                return Real((exponent.Re * Re.Log()).Exp());
            return (exponent * Log()).Exp();
        }

        internal ComplexInterval Sin()
        {
            if (IsReal)
                return Real(Re.Sin());
            // sin(a + b i) = sin a cosh b + i cos a sinh b
            var (cosh, sinh) = CoshSinh(Im);
            return new(Re.Sin() * cosh, Re.Cos() * sinh);
        }

        internal ComplexInterval Cos()
        {
            if (IsReal)
                return Real(Re.Cos());
            // cos(a + b i) = cos a cosh b - i sin a sinh b
            var (cosh, sinh) = CoshSinh(Im);
            return new(Re.Cos() * cosh, -(Re.Sin() * sinh));
        }

        private static (Interval Cosh, Interval Sinh) CoshSinh(Interval b)
        {
            var up = b.Exp();
            var down = (-b).Exp();
            var half = Interval.Exactly(0.5);
            return ((up + down) * half, (up - down) * half);
        }
    }

    /// <summary>
    /// A number an expression stands for, as a <see cref="ComplexInterval"/> that holds it: in
    /// double arithmetic rounded outward, with no settings read -- neither the precision of the
    /// decimal evaluation nor the downcasting -- so that a question needing a dozen digits is
    /// answered in a dozen and gets the same answer however the caller's settings are set. The
    /// pilot of #1019's item 10, evaluation to a requested accuracy, for the checks inside the
    /// library; <see cref="Entity.EvalNumerical"/> stays as it is beside it.
    /// </summary>
    /// <remarks>
    /// An interval is an answer with its error attached, so a comparison has three outcomes, not
    /// two: the values agree to the accuracy asked, they differ by more, or the intervals are too
    /// wide to tell -- where cancellation has eaten the digits, or a node is not one this reads,
    /// and the caller asks the decimal evaluation instead.
    /// https://github.com/asc-community/AngouriMath/issues/1019
    /// </remarks>
    internal static class IntervalEvaluation
    {
        /// <summary>
        /// The interval <paramref name="expr"/> evaluates to, or null where it holds a variable, a
        /// node this does not read, or a value that is not finite -- a pole, a logarithm of zero,
        /// a point on a branch cut.
        /// </summary>
        internal static ComplexInterval? Of(Entity expr)
        {
            var value = Evaluate(expr);
            return value is { IsFinite: true } finite ? finite : null;
        }

        /// <summary>
        /// Whether the two agree to <paramref name="relativeTolerance"/> of the larger's size (or
        /// of one, where both are smaller): true where the whole of their difference is within
        /// it, false where the whole of it is beyond, null where the intervals cannot tell.
        /// </summary>
        internal static bool? Agree(ComplexInterval left, ComplexInterval right, double relativeTolerance)
        {
            var difference = left - right;
            var scale = Math.Max(1.0, Math.Max(left.Magnitude, right.Magnitude));
            var allowed = relativeTolerance * scale;
            var largest = Math.Sqrt(difference.Re.Magnitude * difference.Re.Magnitude + difference.Im.Magnitude * difference.Im.Magnitude);
            if (largest <= allowed)
                return true;
            var smallest = Math.Sqrt(difference.Re.Mignitude * difference.Re.Mignitude + difference.Im.Mignitude * difference.Im.Mignitude);
            if (smallest > allowed)
                return false;
            return null;
        }

        [AngouriMath.Core.ConstantField] private static readonly ComplexInterval Undefined = new(new(double.NaN, double.NaN), new(double.NaN, double.NaN));

        private static ComplexInterval Evaluate(Entity expr)
        {
            switch (expr)
            {
                case Number.Complex number:
                    return FromNumber(number);
                case Variable variable when variable == MathS.pi:
                    return ComplexInterval.Real(Interval.Around(Math.PI));
                case Variable variable when variable == MathS.e:
                    return ComplexInterval.Real(Interval.Around(Math.E));
                case Sumf(var left, var right):
                    return Evaluate(left) + Evaluate(right);
                case Minusf(var left, var right):
                    return Evaluate(left) - Evaluate(right);
                case Mulf(var left, var right):
                    return Evaluate(left) * Evaluate(right);
                case Divf(var left, var right):
                    return Evaluate(left) / Evaluate(right);
                // In the range of an int, so that the reciprocal's negation cannot overflow.
                case Powf(var @base, Number.Integer power) when power.EInteger.CanFitInInt32():
                    return Evaluate(@base).Pow(power.EInteger.ToInt32Checked());
                case Powf(var @base, var exponent):
                    if (@base == MathS.e)
                        return Evaluate(exponent).Exp();
                    return Evaluate(@base).Pow(Evaluate(exponent));
                case Sinf(var argument):
                    return Evaluate(argument).Sin();
                case Cosf(var argument):
                    return Evaluate(argument).Cos();
                case Tanf(var argument):
                {
                    var inner = Evaluate(argument);
                    return inner.Sin() / inner.Cos();
                }
                case Cotanf(var argument):
                {
                    var inner = Evaluate(argument);
                    return inner.Cos() / inner.Sin();
                }
                case Secantf(var argument):
                    return ComplexInterval.Real(Interval.Exactly(1)) / Evaluate(argument).Cos();
                case Cosecantf(var argument):
                    return ComplexInterval.Real(Interval.Exactly(1)) / Evaluate(argument).Sin();
                case Logf(var @base, var antilogarithm):
                    return @base == MathS.e ? Evaluate(antilogarithm).Log() : Evaluate(antilogarithm).Log() / Evaluate(@base).Log();
                case Arctanf(var argument):
                    return RealOnly(Evaluate(argument), static x => x.Atan());
                case Arcsinf(var argument):
                    return RealOnly(Evaluate(argument), static x => x.Asin());
                case Arccosf(var argument):
                    return RealOnly(Evaluate(argument), static x => x.Acos());
                // As the decimal evaluation defines them: arctan(1/x), with pi/2 at zero, where
                // the function jumps; arccos(1/x); arcsin(1/x).
                case Arccotanf(var argument):
                    return RealOnly(Evaluate(argument), static x => x.IsZero ? Interval.Around(Math.PI / 2) : (Interval.Exactly(1) / x).Atan());
                case Arcsecantf(var argument):
                    return RealOnly(Evaluate(argument), static x => (Interval.Exactly(1) / x).Acos());
                case Arccosecantf(var argument):
                    return RealOnly(Evaluate(argument), static x => (Interval.Exactly(1) / x).Asin());
                // The special functions, from their double-precision routines.
                // https://github.com/asc-community/AngouriMath/issues/1607
                case Erff(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Erf, static z => TwoOverSqrtPi * (-(z * z)).Exp(), static _ => false, static _ => true);
                case Erfcf(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Erfc, static z => TwoOverSqrtPi * (-(z * z)).Exp(), static _ => false, static _ => true);
                case Erfif(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Erfi, static z => TwoOverSqrtPi * (z * z).Exp(), static _ => false, static _ => true);
                case Eif(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Ei, static z => z.Exp() / z, static z => StraddlesTheAxisLeftOf(z, 0), static _ => true);
                case Lif(var argument):
                    // Its cut is where the logarithm's is, and where Ei's is under it: 0 < x < 1.
                    return Special(Evaluate(argument), SpecialFunctions.Li, static z => One / z.Log(), static z => StraddlesTheAxisLeftOf(z, 1), static x => x.Low >= 0);
                case Sif(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Si, static z => z.Sin() / z, static _ => false, static _ => true);
                case Cif(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Ci, static z => z.Cos() / z, static z => StraddlesTheAxisLeftOf(z, 0), static x => x.Low > 0);
                case Shif(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Shi, static z => Sinh(z) / z, static _ => false, static _ => true);
                case Chif(var argument):
                    return Special(Evaluate(argument), SpecialFunctions.Chi, static z => Cosh(z) / z, static z => StraddlesTheAxisLeftOf(z, 0), static x => x.Low > 0);
                case Absf(var argument):
                {
                    var inner = Evaluate(argument);
                    return inner.IsReal ? ComplexInterval.Real(inner.Re.Abs()) : ComplexInterval.Real((inner.Re.Square() + inner.Im.Square()).Sqrt());
                }
                case Signumf(var argument):
                {
                    var inner = Evaluate(argument);
                    if (!inner.IsReal || inner.Re.ContainsZero || !inner.Re.IsFinite)
                        return Undefined;
                    return ComplexInterval.Real(Interval.Exactly(inner.Re.Low > 0 ? 1 : -1));
                }
                case Providedf(var inner, var predicate):
                    // The value where the condition holds, none where it fails, as the decimal
                    // evaluation has it; a condition the intervals cannot settle leaves the value
                    // unsettled too.
                    return Decide(predicate) switch
                    {
                        true => Evaluate(inner),
                        false => Undefined,
                        null => Undefined,
                    };
                case Piecewise piecewise:
                    // The first case whose condition holds; one that cannot be settled before it
                    // leaves the whole unsettled.
                    foreach (var @case in piecewise.Cases)
                        switch (Decide(@case.Predicate))
                        {
                            case true:
                                return Evaluate(@case.Expression);
                            case null:
                                return Undefined;
                        }
                    return Undefined;
                case Iversonf(var statement):
                    // 1 where the statement holds and 0 where it does not; one the intervals
                    // cannot settle leaves the value unsettled, as a piecewise does.
                    return Decide(statement) switch
                    {
                        true => ComplexInterval.Real(Interval.Exactly(1)),
                        false => ComplexInterval.Real(Interval.Exactly(0)),
                        null => Undefined,
                    };
                default:
                    return Undefined;
            }
        }

        /// <summary>
        /// A condition at the point the expression was evaluated at: true, false, or null where
        /// the intervals cannot tell -- an equality between two values their intervals do not
        /// separate, a comparison of values that are not real, a node this does not read.
        /// </summary>
        private static bool? Decide(Entity predicate)
        {
            switch (predicate)
            {
                case Entity.Boolean value:
                    return value == Entity.Boolean.True;
                case Notf(var argument):
                    return !Decide(argument);
                case Andf(var left, var right):
                {
                    var l = Decide(left);
                    if (l == false) return false;
                    var r = Decide(right);
                    if (r == false) return false;
                    return l == true && r == true ? true : null;
                }
                case Orf(var left, var right):
                {
                    var l = Decide(left);
                    if (l == true) return true;
                    var r = Decide(right);
                    if (r == true) return true;
                    return l == false && r == false ? false : null;
                }
                case Xorf(var left, var right):
                    return Decide(left) is { } first && Decide(right) is { } second ? first != second : null;
                case Impliesf(var assumption, var conclusion):
                {
                    var a = Decide(assumption);
                    if (a == false) return true;
                    var c = Decide(conclusion);
                    if (c == true) return true;
                    return a == true && c == false ? false : null;
                }
                case Equalsf(var left, var right):
                {
                    var difference = Evaluate(left) - Evaluate(right);
                    if (!difference.IsFinite)
                        return null;
                    if (difference.Re.Mignitude > 0 || difference.Im.Mignitude > 0)
                        return false;
                    return difference.Re.IsZero && difference.Im.IsZero ? true : null;
                }
                // Real where the imaginary part is exactly zero, which real arithmetic keeps it;
                // not real where it is certainly not zero. An answer's conditions ask this of its
                // constants -- `-4 - 4 a^2 in RR` -- and legacy answers it.
                case Set.Inf(var element, Set.SpecialSet.Reals):
                {
                    var value = Evaluate(element);
                    if (!value.IsFinite)
                        return null;
                    return value.IsReal ? true : value.Im.Mignitude > 0 ? false : null;
                }
                case Set.Inf(var element, Set.SpecialSet.Complexes):
                    return Evaluate(element).IsFinite ? true : null;
                case Greaterf(var left, var right):
                    return Compare(left, right) is { } greater ? greater > 0 : null;
                case GreaterOrEqualf(var left, var right):
                    return Compare(left, right, orEqual: true) is { } atLeast ? atLeast >= 0 : null;
                case Lessf(var left, var right):
                    return Compare(right, left) is { } less ? less > 0 : null;
                case LessOrEqualf(var left, var right):
                    return Compare(right, left, orEqual: true) is { } atMost ? atMost >= 0 : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The sign of <c>left - right</c>, both real: 1 or -1 where the intervals are apart, 0
        /// where both are the same exact value (asked only for a comparison that allows it), and
        /// null otherwise.
        /// </summary>
        private static int? Compare(Entity left, Entity right, bool orEqual = false)
        {
            var l = Evaluate(left);
            var r = Evaluate(right);
            if (!l.IsFinite || !r.IsFinite || !l.IsReal || !r.IsReal)
                return null;
            if (l.Re.Low > r.Re.High)
                return 1;
            if (l.Re.High < r.Re.Low)
                return -1;
            if (orEqual && l.Re.Low == l.Re.High && r.Re.Low == r.Re.High && l.Re.Low == r.Re.Low)
                return 0;
            return null;
        }

        [AngouriMath.Core.ConstantField] private static readonly ComplexInterval One = ComplexInterval.Real(Interval.Exactly(1));
        [AngouriMath.Core.ConstantField] private static readonly ComplexInterval TwoOverSqrtPi = ComplexInterval.Real(Interval.Around(2 / Math.Sqrt(Math.PI)));

        private static ComplexInterval Sinh(ComplexInterval z) => (z.Exp() - (-z).Exp()) * ComplexInterval.Real(Interval.Exactly(0.5));
        private static ComplexInterval Cosh(ComplexInterval z) => (z.Exp() + (-z).Exp()) * ComplexInterval.Real(Interval.Exactly(0.5));

        /// <summary>Whether the rectangle crosses the real axis left of <paramref name="end"/>, where a cut runs.</summary>
        private static bool StraddlesTheAxisLeftOf(ComplexInterval z, double end)
            => !z.IsReal && z.Im.ContainsZero && z.Re.Low < end;

        /// <summary>
        /// A special function over <paramref name="argument"/>. The value is the double-precision
        /// routine's at the rectangle's middle. It is widened by the routine's error, and by the most
        /// the function can move across the rectangle: the largest its derivative is there, which
        /// these intervals bound, times the rectangle's half diagonal. The value is real where the
        /// argument is real and <paramref name="realOn"/> says the function is real there.
        /// </summary>
        /// <remarks>
        /// The routines agree with the interpreter's kernels to within <c>3e-13</c> of the value,
        /// measured on some ten thousand points rather than proved. They are allowed <c>1e-10</c> of
        /// the value, and <c>1e-10</c> more for a value near a zero, where a relative error means
        /// nothing. A rectangle that straddles a cut, where the function jumps, has no enclosure
        /// this way. It is left undecided, and the caller asks the decimal evaluation.
        /// </remarks>
        private static ComplexInterval Special(ComplexInterval argument, Func<NumericsComplex, NumericsComplex> function,
            Func<ComplexInterval, ComplexInterval> derivative, Func<ComplexInterval, bool> straddlesACut, Func<Interval, bool> realOn)
        {
            if (!argument.IsFinite || straddlesACut(argument))
                return Undefined;
            var middle = new NumericsComplex(argument.Re.Middle, argument.Im.Middle);
            var value = function(middle);
            if (!Interval.IsFiniteDouble(value.Real) || !Interval.IsFiniteDouble(value.Imaginary))
                return Undefined;
            var reach = Math.Max(argument.Re.High - middle.Real, middle.Real - argument.Re.Low);
            var imReach = Math.Max(argument.Im.High - middle.Imaginary, middle.Imaginary - argument.Im.Low);
            var radius = Interval.Up(Math.Sqrt(reach * reach + imReach * imReach));
            var spread = 0.0;
            if (radius > 0)
            {
                var slope = derivative(argument);
                if (!slope.IsFinite)
                    return Undefined;
                spread = Interval.Up(Interval.Up(slope.Magnitude) * radius);
            }
            var error = Interval.Up(1e-10 * (Math.Sqrt(value.Real * value.Real + value.Imaginary * value.Imaginary) + 1) + spread);
            var re = new Interval(Interval.Down(value.Real - error), Interval.Up(value.Real + error));
            if (argument.IsReal && realOn(argument.Re))
                return ComplexInterval.Real(re);
            return new(re, new Interval(Interval.Down(value.Imaginary - error), Interval.Up(value.Imaginary + error)));
        }

        /// <summary>A function read on real arguments only, in this pilot.</summary>
        private static ComplexInterval RealOnly(ComplexInterval argument, Func<Interval, Interval> function)
            => argument.IsReal && argument.Re.IsFinite ? ComplexInterval.Real(function(argument.Re)) : Undefined;

        private static ComplexInterval FromNumber(Number.Complex number)
        {
            if (number is Number.Rational rational)
                return ComplexInterval.Real(FromRational(rational.ERational));
            var re = FromDecimal(number.RealPart.EDecimal);
            var im = number.ImaginaryPart.EDecimal.IsZero ? Interval.Exactly(0) : FromDecimal(number.ImaginaryPart.EDecimal);
            return new(re, im);
        }

        /// <summary>
        /// A rational as an interval: the double it rounds to, exactly where that is the rational
        /// itself, and a unit in the last place either way otherwise -- from the exact value, not
        /// from the hundred-digit decimal that stands for a third.
        /// </summary>
        private static Interval FromRational(ERational value)
        {
            var converted = value.ToDouble();
            if (!Interval.IsFiniteDouble(converted))
                return new(double.NaN, double.NaN);
            return ERational.FromDouble(converted).CompareTo(value) == 0 ? Interval.Exactly(converted) : Interval.Around(converted);
        }

        /// <summary>The decimal as an interval, the same way.</summary>
        private static Interval FromDecimal(EDecimal value)
        {
            if (!value.IsFinite)
                return new(double.NaN, double.NaN);
            var converted = value.ToDouble();
            if (!Interval.IsFiniteDouble(converted))
                return new(double.NaN, double.NaN);
            return EDecimal.FromDouble(converted).CompareTo(value) == 0 ? Interval.Exactly(converted) : Interval.Around(converted);
        }
    }
}
