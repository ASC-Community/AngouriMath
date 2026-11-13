//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    internal static partial class Patterns
    {
        /// The real value of <paramref name="argument"/> where there is one to read. A rule
        /// that holds only on a principal branch may fire only where the branch can be
        /// decided, so a symbolic argument -- or one off the real line -- has to leave the
        /// node alone.
        internal static bool TryReadReal(Entity argument, out EDecimal value)
        {
            value = EDecimal.Zero;
            if (argument.Evaled is not Number.Real real || !real.EDecimal.IsFinite) return false;
            value = real.EDecimal;
            return true;
        }

        /// <summary>Whether the argument lies in <c>[-pi/2, pi/2]</c>, or in the open interval
        /// when <paramref name="closed"/> is false — the intervals <c>arcsin</c> and
        /// <c>arctan</c> answer in.</summary>
        /// <remarks>
        /// Tested by doubling the argument and comparing against pi rather than by halving pi,
        /// because addition of two <see cref="EDecimal"/>s is exact and division is not. The
        /// comparison is still only as good as the working precision, and an argument that
        /// close to an endpoint is treated as outside the interval — which declines to
        /// rewrite, the safe direction.
        /// </remarks>
        internal static bool WithinHalfPi(Entity argument, bool closed)
        {
            if (!TryReadReal(argument, out var value)) return false;
            var absolute = value.Abs();
            var comparison = absolute.Add(absolute).CompareTo(MathS.DecimalConst.pi);
            return closed ? comparison <= 0 : comparison < 0;
        }

        /// <summary>Whether the argument lies in the range <c>arccotan</c> answers in, which is
        /// <c>(-pi/2, pi/2]</c> without zero.</summary>
        /// <remarks>
        /// This library's <c>arccotan</c> is <c>arctan(1/x)</c> extended by
        /// <c>arccotan(0) = pi/2</c>, so its range is that half-open interval and **not** the
        /// <c>(0, pi)</c> that many texts use — <c>arccotan(-1)</c> is <c>-pi/4</c>. Zero is
        /// excluded because <c>cotan</c> has no value there, so the composition has none either
        /// and rewriting it to <c>x</c> would invent one.
        /// https://github.com/asc-community/AngouriMath/issues/887
        /// </remarks>
        internal static bool WithinArccotanRange(Entity argument)
        {
            if (!TryReadReal(argument, out var value) || value.IsZero) return false;
            var twice = value.Add(value);
            var pi = MathS.DecimalConst.pi;
            return twice.CompareTo(pi) <= 0 && twice.CompareTo(pi.Negate()) > 0;
        }

        /// <summary>
        /// <c>arctan(x) + arccotan(x)</c>, which is <c>pi/2</c> where <c>x</c> is a non-negative
        /// real and <c>-pi/2</c> where it is negative, or <see langword="null"/> where the sign
        /// cannot be read.
        /// </summary>
        /// <remarks>
        /// It follows from the range above: for positive <c>x</c> both terms are positive and sum
        /// to <c>pi/2</c>, and for negative <c>x</c> both are negative. <c>pi/2 * sgn(x)</c> is
        /// the closed form and is wrong at exactly one point — <c>x = 0</c>, where the sum is
        /// <c>pi/2</c> while <c>sgn</c> is <c>0</c> — so the sign is decided here rather than
        /// written into the answer. A symbolic argument has no decidable sign and is left alone.
        /// </remarks>
        internal static Entity? ArctanPlusArccotan(Entity argument)
        {
            var evaled = argument.Evaled;
            if (IsRealNegative(evaled)) return -MathS.pi / 2;
            if (IsRealPositive(evaled) || IsZero(evaled)) return MathS.pi / 2;
            return null;
        }

        /// <summary>Whether the argument lies in <c>[0, pi]</c>, or in the open interval when
        /// <paramref name="closed"/> is false — the interval <c>arccos</c> answers in.</summary>
        internal static bool WithinZeroAndPi(Entity argument, bool closed)
        {
            if (!TryReadReal(argument, out var value)) return false;
            if (value.IsNegative || (!closed && value.IsZero)) return false;
            var comparison = value.CompareTo(MathS.DecimalConst.pi);
            return closed ? comparison <= 0 : comparison < 0;
        }


        [AddressableRules]
        internal static Entity TrigonometricRules(Entity x) => x switch
        {
            // sin({}) * cos({}) = 1/2 * sin(2{})
            Mulf(Sinf(var any1), Cosf(var any1a)) when any1 == any1a => Rational.Create(1, 2) * new Sinf(2 * any1),
            Mulf(Cosf(var any1), Sinf(var any1a)) when any1 == any1a => Rational.Create(1, 2) * new Sinf(2 * any1),

            // arcsin(x) + arccos(x) = pi/2 wherever both are defined, and needs no assumption:
            // arccos(x) is pi/2 - arcsin(x) by definition, over the whole plane.
            Sumf(Arcsinf(var any1), Arccosf(var any1a)) when any1 == any1a => MathS.pi / 2,
            Sumf(Arccosf(var any1), Arcsinf(var any1a)) when any1 == any1a => MathS.pi / 2,

            // arctan(x) + arccotan(x) does *not* behave that way here, because arccotan is
            // arctan(1/x) with range (-pi/2, pi/2]: the sum is pi/2 for non-negative x and
            // -pi/2 for negative x. It was pi/2 unconditionally, which is a wrong answer at
            // every negative real -- https://github.com/asc-community/AngouriMath/issues/887
            Sumf(Arctanf(var any1), Arccotanf(var any1a)) when any1 == any1a
                && ArctanPlusArccotan(any1) is { } sum => sum,
            Sumf(Arccotanf(var any1), Arctanf(var any1a)) when any1 == any1a
                && ArctanPlusArccotan(any1) is { } sum => sum,

            // arctan(a) + arctan(b) = arctan((a + b)/(1 - ab)), which is the tangent
            // addition formula read backwards. It holds as written only while ab < 1: past
            // that the sum leaves the range arctan answers in and the identity is off by a
            // whole pi. Both arguments have to be numbers, so that ab < 1 is a question
            // with an answer -- arctan(1/2) + arctan(1/3) is pi/4, while
            // arctan(2) + arctan(3) is 3pi/4 and is left alone.
            Sumf(Arctanf(Real a), Arctanf(Real b)) when (a * b).Evaled is Real product && product < 1
                => MathS.Arctan(((a + b) / (1 - a * b)).InnerSimplified),

            // The two angles whose tangent is an irrational this can recognise. The ones
            // at +-1 are handled where arctan is simplified, since a candidate offered
            // here has to win on node count and pi/4 does not beat arctan(1).
            Arctanf(Powf(Integer(3), Rational(Integer(1), Integer(2)))) => MathS.pi / 3,
            Arctanf(Divf(Integer(1), Powf(Integer(3), Rational(Integer(1), Integer(2))))) => MathS.pi / 6,

            // sin(2u) csc(u) = 2 cos(u), which is the double angle over the single one.
            // A rule of its own because the two do not meet any other way: opening sin(2u)
            // up leaves 2 sin(u) cos(u) csc(u), whose sine and cosecant are no longer
            // adjacent in the product, and the rules that cancel those are pairwise. So
            // (sin(2t) csc(t))^2/4 - cos(2t) - sin(t)^2 stopped one step short of zero --
            // https://github.com/asc-community/AngouriMath/issues/557.
            // The condition is the cosecant's own and has to be carried: 2cos(u) is a
            // number where sin(u) is zero and sin(2u) csc(u) is not, so dropping it would
            // answer for a point the expression does not reach. It is the same condition
            // the cancellation of sin(u) csc(u) already comes back with.
            Mulf(Sinf(Mulf(Integer(2), var any1)), Cosecantf(var any1a)) when any1 == any1a
                => (2 * new Cosf(any1)).Provided(new Cosecantf(any1).DomainCondition),
            Mulf(Cosecantf(var any1a), Sinf(Mulf(Integer(2), var any1))) when any1 == any1a
                => (2 * new Cosf(any1)).Provided(new Cosecantf(any1).DomainCondition),

            // tan * cot = 1
            Mulf(Tanf(var any1), Cotanf(var any1a)) when any1 == any1a => 1,
            Mulf(Cotanf(var any1), Tanf(var any1a)) when any1 == any1a => 1,

            // arcfunc(func(x)) = x, but only where x already lies in the interval arcfunc
            // answers in. Outside it the composition folds back into that interval, so
            // arcsin(sin(3)) is pi - 3 and arccos(cos(4)) is 2*pi - 4. Applied
            // unconditionally this returned a wrong value at ordinary real points --
            // https://github.com/asc-community/AngouriMath/issues/884
            //
            // A symbolic argument is left as written, which is what SymPy and Mathematica
            // both do, and is a legitimate answer where returning x is not. Attaching the
            // interval as a condition instead would be a second wrong answer: the
            // composition is defined for every real x, so saying it is undefined outside the
            // interval trades a wrong value for a wrong domain.
            //
            // The two tangent-family intervals are open, because tan and cotan have no value
            // at the endpoints and the composition has none there either.
            Arcsinf(Sinf(var any1)) when WithinHalfPi(any1, closed: true) => any1,
            Arccosf(Cosf(var any1)) when WithinZeroAndPi(any1, closed: true) => any1,
            Arctanf(Tanf(var any1)) when WithinHalfPi(any1, closed: false) => any1,
            Arccotanf(Cotanf(var any1)) when WithinArccotanRange(any1) => any1,

            // func(arcfunc(x)) = x, and this direction needs no assumption: it composes the
            // *right* inverse, so sin(arcsin(z)) is z wherever arcsin(z) is defined at all.
            Sinf(Arcsinf(var any1)) => any1,
            Cosf(Arccosf(var any1)) => any1,
            Tanf(Arctanf(var any1)) => any1,
            Cotanf(Arccotanf(var any1)) => any1,

            // sin(:)^2 + cos(:)^2 = 1
            Sumf(Powf(Sinf(var any1), Integer(2)),
                 Powf(Cosf(var any1a), Integer(2))) when any1 == any1a => 1,
            Sumf(Powf(Cosf(var any1), Integer(2)),
                 Powf(Sinf(var any1a), Integer(2))) when any1 == any1a => 1,

            // The same identity solved for one square rather than for 1. Only this direction:
            // the two sides are interchangeable, and rewriting cos(:)^2 back as 1 - sin(:)^2
            // would undo this as fast as it fired.
            Minusf(Integer(1), Powf(Sinf(var any1), Integer(2))) => new Powf(new Cosf(any1), 2),
            Minusf(Integer(1), Powf(Cosf(var any1), Integer(2))) => new Powf(new Sinf(any1), 2),

            // The identity divided through by sin(:)^2 and by cos(:)^2. Knowing the one above
            // and not these made the answer depend on which of the three ways an expression
            // happened to be written -- https://github.com/asc-community/AngouriMath/issues/725.
            Sumf(Integer(1), Powf(Tanf(var any1), Integer(2))) => new Powf(new Secantf(any1), 2),
            Sumf(Powf(Tanf(var any1), Integer(2)), Integer(1)) => new Powf(new Secantf(any1), 2),
            Sumf(Integer(1), Powf(Cotanf(var any1), Integer(2))) => new Powf(new Cosecantf(any1), 2),
            Sumf(Powf(Cotanf(var any1), Integer(2)), Integer(1)) => new Powf(new Cosecantf(any1), 2),

            Minusf(Powf(Secantf(var any1), Integer(2)),
                   Powf(Tanf(var any1a), Integer(2))) when any1 == any1a => 1,
            Minusf(Powf(Cosecantf(var any1), Integer(2)),
                   Powf(Cotanf(var any1a), Integer(2))) when any1 == any1a => 1,

            Minusf(Powf(Sinf(var any1), Integer(2)), Powf(Cosf(var any1a), Integer(2))) when any1 == any1a =>
                -1 * (new Powf(new Cosf(any1), 2) - new Powf(new Sinf(any1), 2)),
            Minusf(Powf(Cosf(var any1), Integer(2)), Powf(Sinf(var any1a), Integer(2))) when any1 == any1a =>
                new Cosf(2 * any1),

            Divf(var any1, Secantf(var any2)) => any1 * any2.Cos(),
            Divf(var any1, Cosecantf(var any2)) => any1 * any2.Sin(),

            Mulf(Secantf(var any1), Cosf(var any1a)) when any1 == any1a => 1,
            Mulf(Cosf(var any1a), Secantf(var any1)) when any1 == any1a => 1,

            Mulf(Cosecantf(var any1), Sinf(var any1a)) when any1 == any1a => 1,
            Mulf(Sinf(var any1a), Cosecantf(var any1)) when any1 == any1a => 1,
            
            // TODO: add more secant/cosecant patterns

            Arcsinf(Divf(var number, var notNumber)) when number is Number && number != Integer.Zero && notNumber is not Number => new Arccosecantf(notNumber / number),
            Arccosf(Divf(var number, var notNumber)) when number is Number && number != Integer.Zero && notNumber is not Number => new Arcsecantf(notNumber / number),
            Arccosecantf(Divf(var number, var notNumber)) when number is Number && number != Integer.Zero && notNumber is not Number => new Arcsinf(notNumber / number),
            Arcsecantf(Divf(var number, var notNumber)) when number is Number && number != Integer.Zero && notNumber is not Number => new Arccosf(notNumber / number),

            _ => x
        };
        [AddressableRules]
        internal static Entity ExpandTrigonometricRules(Entity x) => x switch
        {
            Mulf(Rational(Integer(1), Integer(2)), Sinf(Mulf(Integer(2), var any1))) => new Sinf(any1) * new Cosf(any1),

            Cosf(Mulf(Integer(2), var any1)) =>
                new Powf(new Cosf(any1), Integer.Create(2)) - new Powf(new Sinf(any1), 2),

            _ => x
        };

        /// <summary>
        /// The largest whole multiplier worth opening up. The expansion of sin(n x) grows
        /// with n, and past a handful the result is longer than anything it buys.
        /// </summary>
        private const int MaxAngleMultiplier = 8;

        /// <summary>
        /// <c>sin(n x)</c> and <c>cos(n x)</c> written out in <c>sin(x)</c> and
        /// <c>cos(x)</c>, for a whole n.
        /// </summary>
        /// <remarks>
        /// The expansions are <see cref="TrigonometricAngleExpansion"/>'s, which were only
        /// ever reached for numeric angles that are multiples of pi. Nothing opened a
        /// symbolic <c>sin(2x)</c>, so <c>cos(2x) - (1 - 2sin(x)^2)</c> did not reduce to
        /// zero and neither did <c>(sin(2t)csc(t))^2/4 - cos(2t) - sin(t)^2</c>, which is
        /// https://github.com/asc-community/AngouriMath/issues/557.
        /// </remarks>
        [AddressableRules]
        internal static Entity ExpandMultipleAngleRules(Entity x) => x switch
        {
            Sinf(Mulf(Integer n, var inner)) when IsWorthExpanding(n) =>
                TrigonometricAngleExpansion.ExpandSineArgumentMultiplied(
                    new Sinf(inner), new Cosf(inner), n.EInteger.ToInt32Checked()),

            Cosf(Mulf(Integer n, var inner)) when IsWorthExpanding(n) =>
                TrigonometricAngleExpansion.ExpandCosineArgumentMultiplied(
                    new Sinf(inner), new Cosf(inner), n.EInteger.ToInt32Checked()),

            _ => x
        };

        /// <summary>Whether opening up sin(n x) or cos(n x) is worth the length it costs.</summary>
        /// <remarks>
        /// Internal rather than private so that the data form of this set asks the same
        /// question rather than repeating its bound, which is how two copies of a constant
        /// start disagreeing.
        /// </remarks>
        internal static bool IsWorthExpanding(Integer n)
            => n.EInteger.Abs() >= 2 && n.EInteger.Abs() <= MaxAngleMultiplier;

        [AddressableRules]
        internal static Entity CollapseTrigonometricFunctions(Entity x) => x switch
        {
            // sin / cos = tan
            Divf(Sinf(var any1), Cosf(var any1a)) when any1 == any1a => any1.Tan(),

            // cos / sin = cotan
            Divf(Cosf(var any1), Sinf(var any1a)) when any1 == any1a => any1.Cotan(),

            Divf(var any1, Sinf(var any2)) => any1 * any2.Cosec(),
            Divf(var any1, Cosf(var any2)) => any1 * any2.Sec(),
            _ => x
        };

        /// <summary>
        /// For this it is true that any trigonometric function is either sin or cos
        /// </summary>
        [AddressableRules]
        internal static Entity NormalTrigonometricForm(Entity x) => x switch
        {
            Tanf(var any1) => any1.Sin() / any1.Cos(),
            Cotanf(var any1) => any1.Cos() / any1.Sin(),
            Secantf(var any1) => 1 / any1.Cos(),
            Cosecantf(var any1) => 1 / any1.Sin(),
            _ => x
        };

        /// <summary>
        /// Here, we replace x with t which represents e^(ix).
        /// <list type="table">
        /// <item>sin(ax + b) = (t^a * e^(i*b) - t^(-a) * e^(-i*b)) / (2i)</item>
        /// <item>cos(ax + b) = (t^a * e^(i*b) + t^(-a) * e^(-i*b)) / 2</item>
        /// </list>
        /// </summary>
        internal static Func<Entity, Entity> TrigonometricToExponentialRules(Variable from, Variable to) => tree =>
        {
            // sin(ax + b) = (t^a * e^(i*b) - t^(-a) * e^(-i*b)) / (2i)
            Entity SinResult(Variable x, Entity a, Entity b) =>
                x == from
                ? MathS.Pow(to, a) * (MathS.Pow(MathS.e, b * MathS.i) / (2 * MathS.i)) - MathS.Pow(to, -a) * MathS.Pow(MathS.e, -b * MathS.i) / (2 * MathS.i)
                : tree;
            // cos(ax + b) = (t^a * e^(i*b) + t^(-a) * e^(-i*b)) / 2
            Entity CosResult(Variable x, Entity a, Entity b) =>
                x == from
                ? MathS.Pow(to, a) * (MathS.Pow(MathS.e, b * MathS.i) / 2) + MathS.Pow(to, -a) * MathS.Pow(MathS.e, -b * MathS.i) / 2
                : tree;
            // SolveLinear should also solve tan and cotan equations, but currently Polynomial solver cannot handle big powers
            // uncomment lines above when it will be fixed (TODO)
            // e.g. tan(ax + b) = -i + (2i)/(1 + e^(2i*b) t^(2a))
            return tree switch
            {
                Sinf(var arg) when TreeAnalyzer.TryGetPolyLinear(arg, from, out var a, out var b) =>
                    SinResult(from, a.InnerSimplified, b.InnerSimplified),

                Cosf(var arg) when TreeAnalyzer.TryGetPolyLinear(arg, from, out var a, out var b) =>
                    CosResult(from, a.InnerSimplified, b.InnerSimplified),

                _ => tree
            };
        };
    }
}
