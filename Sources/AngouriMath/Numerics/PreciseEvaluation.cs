//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Numerics
{
    /// <summary>
    /// An interval of decimals at a working precision the evaluation carries: every operation
    /// rounds its lower bound down and its upper bound up in that many digits, so the exact value
    /// is inside. <see cref="Interval"/>'s second tier, for where doubles cannot tell.
    /// </summary>
    internal readonly struct PreciseInterval
    {
        internal readonly EDecimal Low;
        internal readonly EDecimal High;

        internal PreciseInterval(EDecimal low, EDecimal high)
        {
            Low = low;
            High = high;
        }

        internal static PreciseInterval Exactly(EDecimal value) => new(value, value);

        internal bool IsFinite => Low.IsFinite && High.IsFinite;
        internal bool IsZero => Low.IsZero && High.IsZero;
        internal bool ContainsZero => Low.Sign <= 0 && High.Sign >= 0;
        internal EDecimal Magnitude => EDecimal.Max(Low.Abs(), High.Abs());
        internal EDecimal Mignitude => ContainsZero ? EDecimal.Zero : EDecimal.Min(Low.Abs(), High.Abs());

        internal PreciseInterval Negate() => new(High.Negate(), Low.Negate());
    }

    /// <summary>A rectangle of two <see cref="PreciseInterval"/>s.</summary>
    internal readonly struct PreciseComplexInterval
    {
        internal readonly PreciseInterval Re;
        internal readonly PreciseInterval Im;

        internal PreciseComplexInterval(PreciseInterval re, PreciseInterval im)
        {
            Re = re;
            Im = im;
        }

        internal bool IsFinite => Re.IsFinite && Im.IsFinite;
        internal bool IsReal => Im.IsZero;
    }

    /// <summary>
    /// <see cref="IntervalEvaluation"/> at a requested number of significant digits, for the
    /// points where double intervals are too wide to tell -- the digits lost to cancellation --
    /// or land on a cut by a rounding. The precision is an argument, never a setting.
    /// </summary>
    internal sealed class PreciseEvaluation
    {
        private readonly int digits;
        private readonly EContext down;
        private readonly EContext up;
        private readonly EContext near;
        private readonly EDecimal unit;
        private readonly EDecimal fixedPointError;
        private readonly EDecimal least;
        private readonly PreciseComplexInterval undefined =
            new(new(EDecimal.NaN, EDecimal.NaN), new(EDecimal.NaN, EDecimal.NaN));

        // Values already worked out, by node: an expression substituted at several points shares
        // every subtree free of the point, and each of those is worked out once.
        private readonly Dictionary<Entity, PreciseComplexInterval> known = new(ByReference.Instance);

        // The roots a sum over roots is working through, each under a name of its own and as the
        // rectangle that encloses it.
        private Dictionary<Variable, PreciseComplexInterval>? bound;

        // One set of contexts per precision, for the life of the process: the library's constant
        // cache and its guard-digit contexts are keyed by the context instance, so contexts
        // built afresh for every evaluation had pi and the logarithm tables recomputed each time.
        [AngouriMath.Core.ConstantField]
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (EContext Down, EContext Up, EContext Near)> contexts = new();

        private PreciseEvaluation(int digits)
        {
            this.digits = digits;
            (down, up, near) = contexts.GetOrAdd(digits, static d => (
                EContext.ForPrecisionAndRounding(d, ERounding.Floor),
                EContext.ForPrecisionAndRounding(d, ERounding.Ceiling),
                EContext.ForPrecisionAndRounding(d + 10, ERounding.HalfEven)));
            unit = EDecimal.FromString("1E-" + (digits - 2));
            fixedPointError = EDecimal.FromString("1E-" + (digits + 5));
            least = EDecimal.FromString("1E-" + (digits + 30));
        }

        /// <summary>
        /// An evaluation in <paramref name="digits"/> digits that remembers what it has worked
        /// out, for evaluating several expressions that share subtrees.
        /// </summary>
        internal static PreciseEvaluation In(int digits) => new(digits);

        /// <summary>The interval <paramref name="expr"/> evaluates to in <paramref name="digits"/> digits, or null.</summary>
        internal static PreciseComplexInterval? Of(Entity expr, int digits) => In(digits).ValueOf(expr);

        /// <summary>The interval <paramref name="expr"/> evaluates to, or null.</summary>
        internal PreciseComplexInterval? ValueOf(Entity expr)
        {
            var value = Evaluate(expr);
            return value.IsFinite ? value : null;
        }

        private sealed class ByReference : IEqualityComparer<Entity>
        {
            [AngouriMath.Core.ConstantField] internal static readonly ByReference Instance = new();
            public bool Equals(Entity? x, Entity? y) => ReferenceEquals(x, y);
            public int GetHashCode(Entity obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        /// <summary>The three-way agreement of <see cref="IntervalEvaluation.Agree"/>, in decimals.</summary>
        internal static bool? Agree(PreciseComplexInterval left, PreciseComplexInterval right, double relativeTolerance)
        {
            var context = EContext.ForPrecision(30);
            var reDifference = new PreciseInterval(left.Re.Low.Subtract(right.Re.High, context), left.Re.High.Subtract(right.Re.Low, context));
            var imDifference = new PreciseInterval(left.Im.Low.Subtract(right.Im.High, context), left.Im.High.Subtract(right.Im.Low, context));
            var scale = EDecimal.Max(EDecimal.One, EDecimal.Max(
                EDecimal.Max(left.Re.Magnitude, left.Im.Magnitude), EDecimal.Max(right.Re.Magnitude, right.Im.Magnitude)));
            var allowed = scale.Multiply(EDecimal.FromDouble(relativeTolerance), context);
            var largest = EDecimal.Max(reDifference.Magnitude, imDifference.Magnitude);
            if (largest.CompareTo(allowed) <= 0)
                return true;
            var smallest = EDecimal.Max(reDifference.Mignitude, imDifference.Mignitude);
            if (smallest.CompareTo(allowed) > 0)
                return false;
            return null;
        }

        // ---- real intervals -------------------------------------------------------------

        private PreciseInterval Add(PreciseInterval a, PreciseInterval b)
            => a.IsZero ? b : b.IsZero ? a : new(a.Low.Add(b.Low, down), a.High.Add(b.High, up));

        private PreciseInterval Subtract(PreciseInterval a, PreciseInterval b) => Add(a, b.Negate());

        /// <summary>The product, exactly zero where either factor is, as <see cref="Interval"/>'s is.</summary>
        private PreciseInterval Multiply(PreciseInterval a, PreciseInterval b)
        {
            if (a.IsZero || b.IsZero)
                return PreciseInterval.Exactly(EDecimal.Zero);
            // Where neither straddles zero, the ends are products of ends: two multiplications.
            if (a.Low.Sign >= 0 && b.Low.Sign >= 0)
                return new(a.Low.Multiply(b.Low, down), a.High.Multiply(b.High, up));
            if (a.High.Sign <= 0 && b.High.Sign <= 0)
                return new(a.High.Multiply(b.High, down), a.Low.Multiply(b.Low, up));
            if (a.Low.Sign >= 0 && b.High.Sign <= 0)
                return new(a.High.Multiply(b.Low, down), a.Low.Multiply(b.High, up));
            if (a.High.Sign <= 0 && b.Low.Sign >= 0)
                return new(a.Low.Multiply(b.High, down), a.High.Multiply(b.Low, up));
            var lows = new[] { a.Low.Multiply(b.Low, down), a.Low.Multiply(b.High, down), a.High.Multiply(b.Low, down), a.High.Multiply(b.High, down) };
            var highs = new[] { a.Low.Multiply(b.Low, up), a.Low.Multiply(b.High, up), a.High.Multiply(b.Low, up), a.High.Multiply(b.High, up) };
            return new(Min(lows), Max(highs));
        }

        private PreciseInterval Divide(PreciseInterval a, PreciseInterval b)
        {
            if (!a.IsFinite || !b.IsFinite || b.ContainsZero)
                return new(EDecimal.NaN, EDecimal.NaN);
            if (a.IsZero)
                return PreciseInterval.Exactly(EDecimal.Zero);
            // The divisor is of one sign, so the ends are quotients of ends: two divisions.
            if (b.Low.Sign > 0)
                return a.Low.Sign >= 0 ? new(a.Low.Divide(b.High, down), a.High.Divide(b.Low, up))
                     : a.High.Sign <= 0 ? new(a.Low.Divide(b.Low, down), a.High.Divide(b.High, up))
                     : new(a.Low.Divide(b.Low, down), a.High.Divide(b.Low, up));
            return a.Low.Sign >= 0 ? new(a.High.Divide(b.High, down), a.Low.Divide(b.Low, up))
                 : a.High.Sign <= 0 ? new(a.High.Divide(b.Low, down), a.Low.Divide(b.High, up))
                 : new(a.High.Divide(b.High, down), a.Low.Divide(b.High, up));
        }

        private PreciseInterval Square(PreciseInterval a)
        {
            if (a.ContainsZero)
                return new(EDecimal.Zero, EDecimal.Max(a.Low.Multiply(a.Low, up), a.High.Multiply(a.High, up)));
            var small = a.Mignitude;
            var large = a.Magnitude;
            return new(small.Multiply(small, down), large.Multiply(large, up));
        }

        /// <summary>
        /// A function's value over the interval, from its value at the middle, computed with ten
        /// digits to spare, and a bound on its slope there: the value moves no further than the
        /// slope times the half-width. The function's own error is a unit or two in the last place
        /// of the working precision, relative to the value -- and, for the functions this library
        /// computes in fixed point, <paramref name="absoluteError"/> besides, which is what is
        /// left of it where the value is small.
        /// </summary>
        private PreciseInterval AroundTheMiddle(PreciseInterval a, Func<EDecimal, EDecimal> function, EDecimal slope, EDecimal absoluteError)
        {
            var middle = a.Low.Add(a.High, near).Divide(EDecimal.FromInt32(2), near);
            var halfWidth = EDecimal.Max(a.High.Subtract(middle, up), middle.Subtract(a.Low, up));
            var value = function(middle);
            if (!value.IsFinite)
                return new(EDecimal.NaN, EDecimal.NaN);
            var reach = slope.Multiply(halfWidth, up).Add(value.Abs().Multiply(unit, up), up)
                .Add(EDecimal.Max(absoluteError, least), up);
            return new(value.Subtract(reach, down), value.Add(reach, up));
        }

        /// <summary>
        /// A monotone function's value over the interval: its values at the two ends, computed
        /// with ten digits to spare, each widened by the function's own error -- a unit or two in
        /// the last place of the working precision, relative to the value, and, for a function
        /// computed in fixed point, <paramref name="absoluteError"/> besides.
        /// </summary>
        private PreciseInterval AtTheEnds(PreciseInterval a, Func<EDecimal, EDecimal> function, bool increasing, EDecimal absoluteError)
        {
            var atLow = function(a.Low);
            var atHigh = a.Low.CompareTo(a.High) == 0 ? atLow : function(a.High);
            if (!atLow.IsFinite || !atHigh.IsFinite)
                return new(EDecimal.NaN, EDecimal.NaN);
            var (low, high) = increasing ? (atLow, atHigh) : (atHigh, atLow);
            var floor = EDecimal.Max(absoluteError, least);
            return new(low.Subtract(low.Abs().Multiply(unit, up).Add(floor, up), down),
                       high.Add(high.Abs().Multiply(unit, up).Add(floor, up), up));
        }

        // The exponential and the logarithm are this library's fixed-point series -- a few
        // microseconds, where PeterO's are hundreds -- within a unit of the value, the
        // logarithm's error absolute near one. The square root is PeterO's. The sine, the cosine
        // and the inverse functions are the library's too, in fixed point, and their error is
        // absolute: the sine's grows with the argument, whose reduction by 2 pi uses pi to the
        // same fixed point.

        private PreciseInterval Exp(PreciseInterval a)
            => AtTheEnds(a, x => x.Exponential(near), increasing: true, EDecimal.Zero);

        private PreciseInterval Log(PreciseInterval a)
            => a.Low.Sign > 0 ? AtTheEnds(a, x => x.NaturalLogarithm(near), increasing: true, fixedPointError) : new(EDecimal.NaN, EDecimal.NaN);

        private PreciseInterval Sqrt(PreciseInterval a)
            => a.Low.Sign >= 0 ? AtTheEnds(a, x => x.Sqrt(near), increasing: true, EDecimal.Zero) : new(EDecimal.NaN, EDecimal.NaN);

        private PreciseInterval Sin(PreciseInterval a) => AroundTheMiddle(a, x => x.Sin(near), EDecimal.One, ReducedArgumentError(a));
        private PreciseInterval Cos(PreciseInterval a) => AroundTheMiddle(a, x => x.Cos(near), EDecimal.One, ReducedArgumentError(a));
        private PreciseInterval Atan(PreciseInterval a) => AtTheEnds(a, x => x.Arctan(near), increasing: true, fixedPointError);

        private EDecimal ReducedArgumentError(PreciseInterval a)
            => fixedPointError.Multiply(EDecimal.Max(EDecimal.One, a.Magnitude), up);

        private PreciseInterval Asin(PreciseInterval a)
            => WithinTheUnitInterval(a) ? AtTheEnds(a, x => x.Arcsin(near), increasing: true, fixedPointError) : new(EDecimal.NaN, EDecimal.NaN);

        private PreciseInterval Acos(PreciseInterval a)
            => WithinTheUnitInterval(a) ? AtTheEnds(a, x => x.Acos(near), increasing: false, fixedPointError) : new(EDecimal.NaN, EDecimal.NaN);

        private static bool WithinTheUnitInterval(PreciseInterval a)
            => a.Low.CompareTo(EDecimal.FromInt32(-1)) >= 0 && a.High.CompareTo(EDecimal.One) <= 0;

        private static EDecimal Min(EDecimal[] values)
        {
            var result = values[0];
            foreach (var value in values)
                if (value.IsNaN() || value.CompareTo(result) < 0) result = value;
            return result;
        }

        private static EDecimal Max(EDecimal[] values)
        {
            var result = values[0];
            foreach (var value in values)
                if (value.IsNaN() || value.CompareTo(result) > 0) result = value;
            return result;
        }

        // ---- complex rectangles ---------------------------------------------------------

        private PreciseComplexInterval Real(PreciseInterval re) => new(re, PreciseInterval.Exactly(EDecimal.Zero));

        private PreciseComplexInterval Add(PreciseComplexInterval a, PreciseComplexInterval b) => new(Add(a.Re, b.Re), Add(a.Im, b.Im));
        private PreciseComplexInterval Subtract(PreciseComplexInterval a, PreciseComplexInterval b) => new(Subtract(a.Re, b.Re), Subtract(a.Im, b.Im));

        private PreciseComplexInterval Multiply(PreciseComplexInterval a, PreciseComplexInterval b)
            => a.IsReal && b.IsReal ? Real(Multiply(a.Re, b.Re))
             : new(Subtract(Multiply(a.Re, b.Re), Multiply(a.Im, b.Im)), Add(Multiply(a.Re, b.Im), Multiply(a.Im, b.Re)));

        private PreciseComplexInterval Divide(PreciseComplexInterval a, PreciseComplexInterval b)
        {
            if (b.IsReal)
                return new(Divide(a.Re, b.Re), a.IsReal ? PreciseInterval.Exactly(EDecimal.Zero) : Divide(a.Im, b.Re));
            var denominator = Add(Square(b.Re), Square(b.Im));
            return new(Divide(Add(Multiply(a.Re, b.Re), Multiply(a.Im, b.Im)), denominator),
                       Divide(Subtract(Multiply(a.Im, b.Re), Multiply(a.Re, b.Im)), denominator));
        }

        private PreciseComplexInterval Exp(PreciseComplexInterval a)
        {
            var modulus = Exp(a.Re);
            return a.IsReal ? Real(modulus) : new(Multiply(modulus, Cos(a.Im)), Multiply(modulus, Sin(a.Im)));
        }

        private PreciseComplexInterval Log(PreciseComplexInterval a)
        {
            if (a.IsReal && a.Re.Low.Sign > 0)
                return Real(Log(a.Re));
            if (a.IsReal && a.Re.High.Sign < 0)
                return new(Log(a.Re.Negate()), Pi());
            if (a.Re.High.Sign <= 0 && a.Im.ContainsZero || a.Re.ContainsZero && a.Im.ContainsZero)
                return undefined;
            var logModulus = Log(Add(Square(a.Re), Square(a.Im)));
            var half = EDecimal.FromString("0.5");
            var re = new PreciseInterval(logModulus.Low.Multiply(half, down), logModulus.High.Multiply(half, up));
            // The argument over a rectangle off the cut takes its extremes at the corners.
            var corners = new[]
            {
                a.Im.Low.Arctan2(a.Re.Low, near), a.Im.Low.Arctan2(a.Re.High, near),
                a.Im.High.Arctan2(a.Re.Low, near), a.Im.High.Arctan2(a.Re.High, near),
            };
            var low = Min(corners);
            var high = Max(corners);
            var slack = EDecimal.Max(low.Abs(), high.Abs()).Multiply(unit, up).Add(fixedPointError, up);
            return new(re, new(low.Subtract(slack, down), high.Add(slack, up)));
        }

        private PreciseComplexInterval Pow(PreciseComplexInterval a, long exponent)
        {
            if (exponent < 0)
                return Divide(Real(PreciseInterval.Exactly(EDecimal.One)), Pow(a, -exponent));
            var result = Real(PreciseInterval.Exactly(EDecimal.One));
            var power = a;
            while (exponent > 0)
            {
                if ((exponent & 1) == 1)
                    result = Multiply(result, power);
                exponent >>= 1;
                if (exponent > 0)
                    power = Multiply(power, power);
            }
            return result;
        }

        private PreciseComplexInterval Pow(PreciseComplexInterval a, PreciseComplexInterval exponent)
        {
            if (a.IsReal && a.Re.Low.Sign >= 0 && exponent.IsReal && exponent.Re.Low.CompareTo(EDecimal.FromString("0.5")) == 0
                && exponent.Re.High.CompareTo(EDecimal.FromString("0.5")) == 0)
                return Real(Sqrt(a.Re));
            if (a.IsReal && a.Re.Low.Sign > 0 && exponent.IsReal)
                return Real(Exp(Multiply(exponent.Re, Log(a.Re))));
            return Exp(Multiply(exponent, Log(a)));
        }

        private PreciseComplexInterval Sin(PreciseComplexInterval a)
        {
            if (a.IsReal)
                return Real(Sin(a.Re));
            var (cosh, sinh) = CoshSinh(a.Im);
            return new(Multiply(Sin(a.Re), cosh), Multiply(Cos(a.Re), sinh));
        }

        private PreciseComplexInterval Cos(PreciseComplexInterval a)
        {
            if (a.IsReal)
                return Real(Cos(a.Re));
            var (cosh, sinh) = CoshSinh(a.Im);
            return new(Multiply(Cos(a.Re), cosh), Multiply(Sin(a.Re), sinh).Negate());
        }

        private (PreciseInterval Cosh, PreciseInterval Sinh) CoshSinh(PreciseInterval b)
        {
            var expUp = Exp(b);
            var expDown = Exp(b.Negate());
            var half = PreciseInterval.Exactly(EDecimal.FromString("0.5"));
            return (Multiply(Add(expUp, expDown), half), Multiply(Subtract(expUp, expDown), half));
        }

        private PreciseInterval Pi()
        {
            var pi = EDecimal.FromString(
                "3.14159265358979323846264338327950288419716939937510582097494459230781640628620899862803482534211706798214808651328230664709384460955058223172535940812848111745028410270193852110555964462294895493038196");
            return new(pi.RoundToPrecision(down), pi.RoundToPrecision(up));
        }

        private PreciseInterval HalfPi()
        {
            var pi = Pi();
            var half = EDecimal.FromString("0.5");
            return new(pi.Low.Multiply(half, down), pi.High.Multiply(half, up));
        }

        private PreciseInterval E()
        {
            var e = EDecimal.FromString(
                "2.71828182845904523536028747135266249775724709369995957496696762772407663035354759457138217852516642742746639193200305992181741359662904357290033429526059563073813232862794349076323382988075319525101901");
            return new(e.RoundToPrecision(down), e.RoundToPrecision(up));
        }

        private PreciseComplexInterval Evaluate(Entity expr)
        {
            // Ahead of the values remembered by node: a name bound to one root now is bound to
            // another the next time round.
            if (bound is not null && expr is Variable name && bound.TryGetValue(name, out var root))
                return root;
            if (known.TryGetValue(expr, out var value))
                return value;
            value = EvaluateNode(expr);
            known[expr] = value;
            return value;
        }

        private PreciseComplexInterval EvaluateNode(Entity expr)
        {
            switch (expr)
            {
                case Number.Rational rational:
                {
                    var value = rational.ERational;
                    return Real(new(value.ToEDecimal(down), value.ToEDecimal(up)));
                }
                case Number.Complex number:
                {
                    var re = number.RealPart.EDecimal;
                    var im = number.ImaginaryPart.EDecimal;
                    return new(new(re.RoundToPrecision(down), re.RoundToPrecision(up)), new(im.RoundToPrecision(down), im.RoundToPrecision(up)));
                }
                case Variable variable when variable == MathS.pi:
                    return Real(Pi());
                case Variable variable when variable == MathS.e:
                    return Real(E());
                case Sumf(var left, var right):
                    return Evaluate(left) is { IsFinite: true } lSumf ? Add(lSumf, Evaluate(right)) : undefined;
                case Minusf(var left, var right):
                    return Evaluate(left) is { IsFinite: true } lMinusf ? Subtract(lMinusf, Evaluate(right)) : undefined;
                case Mulf(var left, var right):
                    return Multiply(Evaluate(left), Evaluate(right));
                case Divf(var left, var right):
                    return Evaluate(left) is { IsFinite: true } lDivf ? Divide(lDivf, Evaluate(right)) : undefined;
                case Powf(var @base, Number.Integer power) when power.EInteger.CanFitInInt32():
                    return Pow(Evaluate(@base), power.EInteger.ToInt32Checked());
                case Powf(var @base, var exponent):
                    return @base == MathS.e ? Exp(Evaluate(exponent)) : Pow(Evaluate(@base), Evaluate(exponent));
                case Sinf(var argument):
                    return Sin(Evaluate(argument));
                case Cosf(var argument):
                    return Cos(Evaluate(argument));
                case Tanf(var argument):
                {
                    var inner = Evaluate(argument);
                    return Divide(Sin(inner), Cos(inner));
                }
                case Cotanf(var argument):
                {
                    var inner = Evaluate(argument);
                    return Divide(Cos(inner), Sin(inner));
                }
                case Secantf(var argument):
                    return Divide(Real(PreciseInterval.Exactly(EDecimal.One)), Cos(Evaluate(argument)));
                case Cosecantf(var argument):
                    return Divide(Real(PreciseInterval.Exactly(EDecimal.One)), Sin(Evaluate(argument)));
                case Logf(var @base, var antilogarithm):
                    return @base == MathS.e ? Log(Evaluate(antilogarithm)) : Divide(Log(Evaluate(antilogarithm)), Log(Evaluate(@base)));
                case Arctanf(var argument):
                    return RealOnly(Evaluate(argument), Atan);
                case Arcsinf(var argument):
                    return RealOnly(Evaluate(argument), Asin);
                case Arccosf(var argument):
                    return RealOnly(Evaluate(argument), Acos);
                case Arccotanf(var argument):
                    return RealOnly(Evaluate(argument), x => x.IsZero ? HalfPi() : Atan(Divide(PreciseInterval.Exactly(EDecimal.One), x)));
                case Arcsecantf(var argument):
                    return RealOnly(Evaluate(argument), x => Acos(Divide(PreciseInterval.Exactly(EDecimal.One), x)));
                case Arccosecantf(var argument):
                    return RealOnly(Evaluate(argument), x => Asin(Divide(PreciseInterval.Exactly(EDecimal.One), x)));
                case Absf(var argument):
                {
                    var inner = Evaluate(argument);
                    if (!inner.IsReal)
                        return Real(Sqrt(Add(Square(inner.Re), Square(inner.Im))));
                    var re = inner.Re;
                    return Real(re.ContainsZero ? new(EDecimal.Zero, re.Magnitude) : re.Low.Sign > 0 ? re : re.Negate());
                }
                case Signumf(var argument):
                {
                    var inner = Evaluate(argument);
                    if (!inner.IsFinite || !inner.IsReal || inner.Re.ContainsZero)
                        return undefined;
                    return Real(PreciseInterval.Exactly(inner.Re.Low.Sign > 0 ? EDecimal.One : EDecimal.One.Negate()));
                }
                case Providedf(var inner, var predicate):
                    return Decide(predicate) == true ? Evaluate(inner) : undefined;
                case Piecewise piecewise:
                    foreach (var @case in piecewise.Cases)
                        switch (Decide(@case.Predicate))
                        {
                            case true:
                                return Evaluate(@case.Expression);
                            case null:
                                return undefined;
                        }
                    return undefined;
                // The Iverson bracket, 1 where its statement holds and 0 where it does not, and
                // undefined where the statement is not decided over the rectangle, as a piecewise is.
                case Iversonf(var statement):
                    return Decide(statement) switch
                    {
                        true => Real(PreciseInterval.Exactly(EDecimal.One)),
                        false => Real(PreciseInterval.Exactly(EDecimal.Zero)),
                        null => undefined,
                    };
                // The special functions, each through its derivative, which is elementary; Ei, Ci
                // and Chi jump across the real line left of 0, and li left of 1.
                case Erff(var argument):
                    return BySlope(Evaluate(argument), Number.Erf, z => Gaussian(z, negative: true), cutEnd: null);
                case Erfcf(var argument):
                    return BySlope(Evaluate(argument), Number.Erfc, z => Gaussian(z, negative: true), cutEnd: null);
                case Erfif(var argument):
                    return BySlope(Evaluate(argument), Number.Erfi, z => Gaussian(z, negative: false), cutEnd: null);
                case Eif(var argument):
                    return BySlope(Evaluate(argument), Number.Ei, z => Divide(Exp(z), z), cutEnd: EDecimal.Zero);
                case Lif(var argument):
                    return BySlope(Evaluate(argument), Number.Li, z => Divide(Real(PreciseInterval.Exactly(EDecimal.One)), Log(z)), cutEnd: EDecimal.One);
                case Sif(var argument):
                    return BySlope(Evaluate(argument), Number.Si, z => Divide(Sin(z), z), cutEnd: null);
                case Cif(var argument):
                    return BySlope(Evaluate(argument), Number.Ci, z => Divide(Cos(z), z), cutEnd: EDecimal.Zero);
                case Shif(var argument):
                    return BySlope(Evaluate(argument), Number.Shi, z => Divide(Half(Subtract(Exp(z), Exp(Negate(z)))), z), cutEnd: null);
                case Chif(var argument):
                    return BySlope(Evaluate(argument), Number.Chi, z => Divide(Half(Add(Exp(z), Exp(Negate(z)))), z), cutEnd: EDecimal.Zero);
                // A sum over the roots of a polynomial, the form Rothstein–Trager answers in: each
                // root enclosed, the summand worked out on its rectangle, and the terms added.
                // https://github.com/asc-community/AngouriMath/issues/1285
                case SumOverSetf(var summand, Variable name, Set.ConditionalSet { Var: Variable w, Predicate: Equalsf(var left, var right) }):
                    return OverTheRoots(summand, name, left - right, w);
                default:
                    return undefined;
            }
        }

        /// <summary>
        /// <paramref name="summand"/> added up over the roots of <paramref name="polynomial"/> in
        /// <paramref name="w"/>, taken by <paramref name="name"/> in turn; undefined where a root
        /// cannot be enclosed apart from the others.
        /// </summary>
        private PreciseComplexInterval OverTheRoots(Entity summand, Variable name, Entity polynomial, Variable w)
        {
            if (RootsEnclosed(polynomial, w) is not { } roots)
                return undefined;
            var total = Real(PreciseInterval.Exactly(EDecimal.Zero));
            foreach (var root in roots)
            {
                bound ??= new();
                var own = Variable.CreateTemp(summand.Vars.Concat(bound.Keys).Append(name));
                bound[own] = root;
                total = Add(total, Evaluate(summand.Substitute(name, own)));
            }
            return total;
        }

        /// <summary>
        /// A rectangle about each root of <paramref name="polynomial"/> in <paramref name="w"/>,
        /// holding that root and no other; null where the polynomial does not have rational
        /// coefficients, or the rectangles cannot be kept apart.
        /// </summary>
        /// <remarks>
        /// Durand–Kerner's approximations <c>z_k</c> at this precision, each enclosed by its
        /// Weierstrass correction <c>w_k = p(z_k)/(a prod_{j != k} (z_k - z_j))</c>, worked out in
        /// intervals. Every root lies in a disk about some <c>z_k - w_k</c> of radius
        /// <c>(n - 1)|w_k|</c>, and a disk apart from the others holds exactly one (Braess and
        /// Hadeler). That disk is inside the one of radius <c>n|w_k|</c> about <c>z_k</c>, whose
        /// square is the rectangle, so rectangles apart from each other hold one root each.
        /// </remarks>
        private List<PreciseComplexInterval>? RootsEnclosed(Entity polynomial, Variable w)
        {
            if (AngouriMath.Functions.SumOverSet.SquareFreeParts(polynomial, w) is not { } parts)
                return null;
            var enclosed = new List<PreciseComplexInterval>();
            foreach (var part in parts)
            {
                var p = part.Factor;
                var n = p.Degree;
                if (n < 1)
                    continue;
                if (AngouriMath.Functions.Algebra.NumericalSolving.DurandKerner.Roots(p, near) is not { } approximations
                    || approximations.Count != n)
                    return null;
                var points = new PreciseComplexInterval[n];
                for (var k = 0; k < n; k++)
                    points[k] = new(PreciseInterval.Exactly(approximations[k].RealPart.EDecimal),
                        PreciseInterval.Exactly(approximations[k].ImaginaryPart.EDecimal));
                var lead = Real(PreciseInterval.Exactly(EDecimal.FromEInteger(p[n])));
                var rectangles = new PreciseComplexInterval[n];
                for (var k = 0; k < n; k++)
                {
                    var value = lead;
                    for (var i = n - 1; i >= 0; i--)
                        value = Add(Multiply(value, points[k]), Real(PreciseInterval.Exactly(EDecimal.FromEInteger(p[i]))));
                    var product = lead;
                    for (var j = 0; j < n; j++)
                        if (j != k)
                            product = Multiply(product, Subtract(points[k], points[j]));
                    var correction = Divide(value, product);
                    if (!correction.IsFinite)
                        return null;
                    var radius = EDecimal.FromInt32(n).Multiply(correction.Re.Magnitude.Add(correction.Im.Magnitude, up), up);
                    var re = points[k].Re.Low;
                    var im = points[k].Im.Low;
                    rectangles[k] = new(new(re.Subtract(radius, down), re.Add(radius, up)), new(im.Subtract(radius, down), im.Add(radius, up)));
                }
                for (var j = 0; j < n; j++)
                    for (var k = j + 1; k < n; k++)
                        if (Overlap(rectangles[j], rectangles[k]))
                            return null;
                enclosed.AddRange(rectangles);
            }
            return enclosed;
        }

        private static bool Overlap(PreciseComplexInterval a, PreciseComplexInterval b)
            => a.Re.Low.CompareTo(b.Re.High) <= 0 && b.Re.Low.CompareTo(a.Re.High) <= 0
            && a.Im.Low.CompareTo(b.Im.High) <= 0 && b.Im.Low.CompareTo(a.Im.High) <= 0;

        /// <summary>
        /// A special function over the rectangle: its value at the middle, which the library
        /// works out with ten digits to spare, widened by the most the function can move within
        /// the rectangle, and by a unit in the last place of the working precision for its own
        /// error. The most it can move is a bound on the slope times the farthest the rectangle
        /// reaches from the middle, since along the segment from the middle to any point the
        /// function changes by the integral of its derivative there. The bound is the
        /// <paramref name="derivative"/>'s magnitude in intervals over the whole rectangle, and
        /// where that has no value -- a singularity inside -- neither has the function. Off the
        /// real line, a rectangle over the cut along the real line up to
        /// <paramref name="cutEnd"/> is undefined as well: the function jumps there, and the
        /// segment crosses the jump. On the real line itself there is no jump to cross, and the
        /// value is the one the function takes on the cut.
        /// </summary>
        private PreciseComplexInterval BySlope(PreciseComplexInterval a, Func<Number.Complex, Number.Complex> function,
            Func<PreciseComplexInterval, PreciseComplexInterval> derivative, EDecimal? cutEnd)
        {
            if (!a.IsFinite)
                return undefined;
            if (cutEnd is { } end && !a.IsReal && a.Im.ContainsZero && a.Re.Low.CompareTo(end) <= 0)
                return undefined;
            var two = EDecimal.FromInt32(2);
            var middleRe = a.Re.Low.Add(a.Re.High, near).Divide(two, near);
            var middleIm = a.Im.Low.Add(a.Im.High, near).Divide(two, near);
            var halfRe = EDecimal.Max(a.Re.High.Subtract(middleRe, up), middleRe.Subtract(a.Re.Low, up));
            var halfIm = EDecimal.Max(a.Im.High.Subtract(middleIm, up), middleIm.Subtract(a.Im.Low, up));
            var reach = halfRe.Multiply(halfRe, up).Add(halfIm.Multiply(halfIm, up), up).Sqrt(up);
            var moved = EDecimal.Zero;
            // A point needs no slope, and Si's derivative at 0 has none written.
            if (!reach.IsZero)
            {
                var slope = derivative(a);
                if (!slope.IsFinite)
                    return undefined;
                var steepest = slope.Re.Magnitude.Multiply(slope.Re.Magnitude, up)
                    .Add(slope.Im.Magnitude.Multiply(slope.Im.Magnitude, up), up).Sqrt(up);
                moved = steepest.Multiply(reach, up);
            }
            // The library's functions read the working precision from the settings; they are
            // given this evaluation's, and no downcasting, which would round the value.
            Number.Complex value;
            using (MathS.Settings.DecimalPrecisionContext.Set(near))
            using (MathS.Settings.DowncastingEnabled.Set(false))
                value = function(Number.Complex.Create(middleRe, middleIm));
            var (re, im) = (value.RealPart.EDecimal, value.ImaginaryPart.EDecimal);
            if (!re.IsFinite || !im.IsFinite)
                return undefined;
            var floor = moved.Add(EDecimal.Max(fixedPointError, least), up);
            var reReach = floor.Add(re.Abs().Multiply(unit, up), up);
            var imReach = floor.Add(im.Abs().Multiply(unit, up), up);
            // On the real line an imaginary part that is exactly zero at the middle is zero over
            // the interval, which has no singularity in it: the function is real along the line
            // there, or it would have jumped.
            var imaginary = a.IsReal && im.IsZero ? PreciseInterval.Exactly(EDecimal.Zero) : new(im.Subtract(imReach, down), im.Add(imReach, up));
            return new(new(re.Subtract(reReach, down), re.Add(reReach, up)), imaginary);
        }

        /// <summary>The error functions' derivative, <c>2/sqrt(pi) e^(-z^2)</c>, or <c>e^(z^2)</c> for <c>erfi</c>.</summary>
        private PreciseComplexInterval Gaussian(PreciseComplexInterval z, bool negative)
        {
            var square = Multiply(z, z);
            var twoOverRootPi = Divide(PreciseInterval.Exactly(EDecimal.FromInt32(2)), Sqrt(Pi()));
            return Multiply(Real(twoOverRootPi), Exp(negative ? Negate(square) : square));
        }

        private PreciseComplexInterval Negate(PreciseComplexInterval a) => new(a.Re.Negate(), a.Im.Negate());

        private PreciseComplexInterval Half(PreciseComplexInterval a)
        {
            var half = PreciseInterval.Exactly(EDecimal.FromString("0.5"));
            return new(Multiply(a.Re, half), Multiply(a.Im, half));
        }

        private PreciseComplexInterval RealOnly(PreciseComplexInterval argument, Func<PreciseInterval, PreciseInterval> function)
            => argument.IsReal && argument.Re.IsFinite ? Real(function(argument.Re)) : undefined;

        private bool? Decide(Entity predicate)
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
                    var difference = Subtract(Evaluate(left), Evaluate(right));
                    if (!difference.IsFinite)
                        return null;
                    if (difference.Re.Mignitude.Sign > 0 || difference.Im.Mignitude.Sign > 0)
                        return false;
                    return difference.Re.IsZero && difference.Im.IsZero ? true : null;
                }
                case Set.Inf(var element, Set.SpecialSet.Reals):
                {
                    var value = Evaluate(element);
                    if (!value.IsFinite)
                        return null;
                    return value.IsReal ? true : value.Im.Mignitude.Sign > 0 ? false : null;
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

        private int? Compare(Entity left, Entity right, bool orEqual = false)
        {
            var l = Evaluate(left);
            var r = Evaluate(right);
            if (!l.IsFinite || !r.IsFinite || !l.IsReal || !r.IsReal)
                return null;
            if (l.Re.Low.CompareTo(r.Re.High) > 0)
                return 1;
            if (l.Re.High.CompareTo(r.Re.Low) < 0)
                return -1;
            if (orEqual && l.Re.Low.CompareTo(l.Re.High) == 0 && r.Re.Low.CompareTo(r.Re.High) == 0 && l.Re.Low.CompareTo(r.Re.Low) == 0)
                return 0;
            return null;
        }
    }
}
