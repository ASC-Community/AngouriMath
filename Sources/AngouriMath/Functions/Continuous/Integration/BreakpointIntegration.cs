//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra
{
    /// <summary>
    /// A definite integral whose integrand can jump inside the range, split at the jumps. A
    /// piecewise breaks where a case's condition changes truth; <c>floor(x)</c> and
    /// <c>ceil(x)</c> break at every whole number. Between two breakpoints the piecewise is one
    /// of its cases and the step is one constant, so the pieces integrate through an
    /// antiderivative and add.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>F(b) - F(a)</c> is the integral only where <c>F</c> is continuous on <c>[a, b]</c>, and
    /// an antiderivative found with the piecewise's cases or the floor taken as constants is
    /// continuous between two jumps and not across one. Taken across one it answered
    /// <c>integral(x - floor(x), x, 0, 3)</c> with <c>0</c> and the tent map's integral over
    /// <c>[0, 1]</c> with <c>1</c>, where they are <c>3/2</c> and <c>1/2</c>; with a symbolic
    /// bound it answered <c>(n - floor(n))^3 / 3</c>, and with a piecewise it handed back a
    /// piecewise that still mentioned the integration variable. None of that happens here: an
    /// integrand with a break never goes through an antiderivative between two bounds.
    /// </para>
    /// <para>
    /// <b>Finitely many breakpoints</b> -- a piecewise whose conditions compare the variable with
    /// numbers -- are collected, the range is cut at the ones inside it, and on each piece the
    /// piecewise is replaced by the case that holds at the piece's midpoint, which is exact
    /// because the condition has no other place to change. A condition that compares the
    /// variable with something symbolic, or a bound that is symbolic, is declined: the pieces
    /// depend on where the jumps fall, and answering across them is what this exists to stop.
    /// A piecewise whose conditions do not mention the variable is a constant here and goes
    /// through the antiderivative as one.
    /// </para>
    /// <para>
    /// <b>Infinitely many, evenly spaced</b> -- a floor or a ceiling of the variable -- are the
    /// same split written as a sum: on <c>[n, n + 1)</c> the floor is <c>n</c>, the ceiling
    /// <c>n + 1</c>, <c>x</c> is <c>n + t</c> with <c>t</c> over <c>[0, 1)</c>, and the integral
    /// over whole bounds is a sum over <c>n</c> of an integral over <c>t</c> with no step in it,
    /// which the summation's closed forms answer; <c>+oo</c> is allowed as the upper bound. A
    /// numeric bound that is not whole contributes the piece up to the nearest whole number,
    /// on which the step is one constant. The fractional part <c>x - floor(x)</c> is substituted
    /// as a unit, since written out it arrives as <c>n + t - n</c>, and a term subtracted from
    /// itself simplifies to a conditional zero rather than to nothing
    /// (<a href="https://github.com/asc-community/AngouriMath/issues/1174">#1174</a>).
    /// </para>
    /// <para>
    /// <b>Where something in the variable is a whole number</b> -- a floor or a ceiling of
    /// <c>x^2</c>, of <c>sin(pi x/2)</c> -- the breaks are those points, which
    /// <see cref="StepPieces"/> finds and proves to be all of them over a range with numeric
    /// bounds; each piece is then integrated with its step a constant.
    /// </para>
    /// <para>
    /// Offered only where every piece then resolves; an integral rewritten as an unevaluated sum
    /// of unevaluated integrals is not an answer. Question I.2 of
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1212">#1212</a>, and the
    /// review of <a href="https://github.com/asc-community/AngouriMath/pull/1215">#1215</a>,
    /// which asked for the general mechanism rather than the floor's special case.
    /// </para>
    /// </remarks>
    internal static class BreakpointIntegration
    {
        /// <summary>
        /// Whether the integrand can jump in <paramref name="x"/>: a piecewise whose conditions
        /// mention it, or a floor or ceiling of something in it.
        /// </summary>
        internal static bool HasABreak(Entity expr, Variable x)
            => expr.Nodes.Any(node => IsABreak(node, x));

        private static bool IsABreak(Entity node, Variable x)
            => node switch
            {
                Floorf(var argument) => argument.ContainsNode(x),
                Ceilf(var argument) => argument.ContainsNode(x),
                Piecewise piecewise => piecewise.Cases.Any(c => c.Predicate.ContainsNode(x)),
                // A condition on the variable is a one-case piecewise whose other case is undefined.
                Providedf provided => provided.Predicate.ContainsNode(x),
                _ => false,
            };

        internal static Entity? Split(Entity expr, Variable x, Entity from, Entity to)
        {
            if (expr.Nodes.Any(node => node is Piecewise or Providedf && IsABreak(node, x)))
                return OverCaseBoundaries(expr, x, from, to);
            if (expr.Nodes.Any(node => node is Floorf(var f) && f != x && f.ContainsNode(x) || node is Ceilf(var c) && c != x && c.ContainsNode(x)))
                return OverStepPieces(expr, x, from, to);
            if (HasAStep(expr, x))
                return OverUnitIntervals(expr, x, from, to);
            return null;
        }

        // ---- the modulus of something in the variable: split where it changes sign ---------------

        /// <summary>
        /// The integral of an integrand holding <c>abs(u)</c> of something in <paramref name="x"/>, split where each
        /// <c>u</c> is zero or breaks, with <c>abs(u)</c> on each piece written as <c>u</c> or <c>-u</c> by its sign at the
        /// piece's midpoint; or <see langword="null"/> where a bound is not a number, a <c>u</c> holds another variable
        /// or another modulus, its zeros cannot all be listed, or a piece does not integrate.
        /// </summary>
        /// <remarks>
        /// Asked only where the integrand has no antiderivative the integral can be taken through, so an integral answered
        /// that way, <c>abs(x - 1)</c> through <c>sgn(x - 1) (x - 1)^2 / 2</c>, is answered as before. The integrator has
        /// none for <c>sqrt(abs(x))</c>, <c>exp(-abs(x))</c> or <c>abs(x^2 - 1)</c>, and their integrals were left
        /// unevaluated, where on each side of a zero of <c>u</c> they are integrals of elementary functions. Between two
        /// listed points <c>u</c> is continuous and not zero, so it keeps the sign it has at the midpoint. A piece that
        /// diverges is an infinity, which the sum keeps: <c>1/abs(x)</c> over <c>[-1, 1]</c> is <c>+oo</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2605
        /// </remarks>
        internal static Entity? AcrossTheSignsOfAbs(Entity expr, Variable x, Entity from, Entity to)
        {
            var arguments = expr.Nodes.OfType<Absf>().Select(abs => abs.Argument).Where(u => u.ContainsNode(x)).Distinct().ToList();
            if (arguments.Count == 0)
                return null;
            if (from.Evaled is not Real lower || lower.IsNaN || to.Evaled is not Real upper || upper.IsNaN || lower == upper)
                return null;
            if (upper < lower)
                return AcrossTheSignsOfAbs(expr, x, to, from) is { } reversed ? (-reversed).InnerSimplified : null;
            var breaks = new List<(Entity At, Real Value)>();
            foreach (var argument in arguments)
            {
                if (argument.Vars.Any(variable => variable != x) || argument.Nodes.Any(node => node is Absf))
                    return null;
                // Where u is zero, and where it is not continuous, which is the other place a sign can change.
                if (!AntiderivativeBreaks.ZerosOf(argument, x, lower, upper, breaks)
                    || AntiderivativeBreaks.Inside(argument, x, lower, upper) is not { } poles)
                    return null;
                breaks.AddRange(poles);
            }
            var points = new List<(Entity At, Real Value)> { (from, lower) };
            foreach (var point in breaks.Where(point => lower < point.Value && point.Value < upper).OrderBy(point => point.Value.EDecimal))
                if (point.Value != points[points.Count - 1].Value)
                    points.Add(point);
            points.Add((to, upper));

            Entity total = Integer.Zero;
            for (var i = 0; i + 1 < points.Count; i++)
            {
                var (start, end) = (points[i], points[i + 1]);
                Entity midpoint = (start.Value.IsFinite, end.Value.IsFinite) switch
                {
                    (true, true) => (start.Value + end.Value) / Integer.Create(2),
                    (true, false) => start.Value + Integer.One,
                    (false, true) => end.Value - Integer.One,
                    _ => Integer.Zero
                };
                var failed = false;
                var resolved = expr.Replace(node =>
                {
                    if (node is not Absf(var argument) || !argument.ContainsNode(x))
                        return node;
                    if (argument.Substitute(x, midpoint).Evaled is not Real { IsFinite: true } sign || sign.IsZero)
                    {
                        failed = true;
                        return node;
                    }
                    return sign.IsNegative ? -argument : argument;
                });
                if (failed)
                    return null;
                var piece = resolved.Integrate(x, start.At, end.At);
                if (piece.Nodes.Any(node => node is Integralf))
                    return null;
                // A real integrand diverging on a piece diverges to a real infinity. The antiderivative of -1/x on
                // [-1, 0] is -ln(x), which is complex there, and the difference came out +oo + i pi, an imaginary
                // part that only the logarithm's branch put in.
                if (piece.Evaled is Complex { RealPart.EDecimal: var real, ImaginaryPart.EDecimal: var imaginary }
                    && real.IsInfinity() && imaginary.IsFinite
                    && resolved.Substitute(x, midpoint).Evaled is Real)
                    piece = real.IsPositiveInfinity() ? Real.PositiveInfinity : Real.NegativeInfinity;
                total += piece;
            }
            return total.InnerSimplified;
        }

        // ---- a floor or a ceiling of something in the variable: the pieces StepPieces proves -------

        /// <summary>
        /// The integral as the sum of the integrals of the pieces <see cref="StepPieces"/> writes the integrand in over
        /// the range, each with its steps replaced by their values there, so with no jump in it: <c>floor(x^2)</c> from 0
        /// to 2 is <c>(sqrt(2) - 1) + 2 (sqrt(3) - sqrt(2)) + 3 (2 - sqrt(3))</c>, which is <c>5 - sqrt(2) - sqrt(3)</c>.
        /// A piece that is a point adds nothing. Where the pieces are not proved complete, or a piece does not integrate,
        /// the integral is left as written. https://github.com/asc-community/AngouriMath/issues/1807
        /// </summary>
        private static Entity? OverStepPieces(Entity expr, Variable x, Entity from, Entity to)
        {
            if (from.Evaled is not Real lower || !lower.IsFinite || to.Evaled is not Real upper || !upper.IsFinite)
                return null;
            if (lower == upper)
                return Integer.Zero;
            if (upper < lower)
                return OverStepPieces(expr, x, to, from) is { } reversed ? (-reversed).InnerSimplified : null;
            if (StepPieces.Of(expr, x, from, to) is not { } pieces)
                return null;
            Entity total = Integer.Zero;
            foreach (var @case in pieces.Cases)
            {
                if (@case.Predicate is not Set.Inf(_, Set.Interval interval))
                    continue;
                var piece = @case.Expression.Integrate(x, interval.Left, interval.Right);
                if (piece.Nodes.Any(node => node is Integralf))
                    return null;
                total += piece;
            }
            return total.InnerSimplified;
        }

        // ---- finitely many breakpoints: a piecewise --------------------------------------------

        private static Entity? OverCaseBoundaries(Entity expr, Variable x, Entity from, Entity to)
        {
            if (from.Evaled is not Real lower || !lower.IsFinite || to.Evaled is not Real upper || !upper.IsFinite)
                return null;
            if (upper < lower)
                return null;

            var thresholds = new List<Real>();
            foreach (var node in expr.Nodes)
                switch (node)
                {
                    case Piecewise piecewise:
                        foreach (var @case in piecewise.Cases)
                            if (!CollectThresholds(@case.Predicate, x, thresholds))
                                return null;
                        break;
                    case Providedf provided:
                        if (!CollectThresholds(provided.Predicate, x, thresholds))
                            return null;
                        break;
                }

            var points = new List<Real> { lower };
            foreach (var threshold in thresholds.Where(t => lower < t && t < upper).OrderBy(t => t.EDecimal))
                if (!points.Contains(threshold))
                    points.Add(threshold);
            if (upper > lower)
                points.Add(upper);

            Entity total = Integer.Zero;
            for (var i = 0; i + 1 < points.Count; i++)
            {
                var (start, end) = (points[i], points[i + 1]);
                if (((Entity)start + end).Evaled is not Real twice)
                    return null;
                var midpoint = (Real)(twice / Integer.Create(2)).Evaled;
                var resolved = ResolvedAt(expr, x, midpoint);
                if (resolved is null)
                    return null;
                var piece = resolved.Integrate(x, start, end);
                if (piece is Integralf)
                    return null;
                total += piece;
            }
            return total.InnerSimplified;
        }

        /// <summary>
        /// The values of <paramref name="x"/> at which <paramref name="predicate"/> can change
        /// truth, added to <paramref name="thresholds"/>; <see langword="false"/> where it
        /// compares <paramref name="x"/> with something that is not a number, or is a shape
        /// this does not read.
        /// </summary>
        private static bool CollectThresholds(Entity predicate, Variable x, List<Real> thresholds)
        {
            if (!predicate.ContainsNode(x))
                return true;
            bool Both(Entity left, Entity right)
                => CollectThresholds(left, x, thresholds) && CollectThresholds(right, x, thresholds);
            switch (predicate)
            {
                case Andf(var a, var b):
                    return Both(a, b);
                case Orf(var a, var b):
                    return Both(a, b);
                case Impliesf(var a, var b):
                    return Both(a, b);
                case Xorf(var a, var b):
                    return Both(a, b);
                case Notf(var operand):
                    return CollectThresholds(operand, x, thresholds);
                case ComparisonSign comparison when comparison.DirectChildren.Count == 2:
                {
                    var (left, right) = (comparison.DirectChildren[0], comparison.DirectChildren[1]);
                    var other = left == x ? right : right == x ? left : null;
                    if (other is null || other.ContainsNode(x))
                        return false;
                    if (other.Evaled is not Real value || !value.IsFinite)
                        return false;
                    thresholds.Add(value);
                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>
        /// <paramref name="expr"/> with every piecewise replaced by the case that holds at
        /// <paramref name="at"/>, or <see langword="null"/> where a case's truth there cannot be
        /// decided or no case holds.
        /// </summary>
        private static Entity? ResolvedAt(Entity expr, Variable x, Real at)
        {
            var failed = false;
            var resolved = expr.Replace(node =>
            {
                if (failed)
                    return node;
                if (node is Providedf provided && provided.Predicate.ContainsNode(x))
                {
                    // Where the condition fails the integrand has no value, and neither has
                    // the integral.
                    if (provided.Predicate.Substitute(x, at).Evaled is Entity.Boolean { Value: true })
                        return provided.Expression;
                    failed = true;
                    return node;
                }
                if (node is not Piecewise piecewise)
                    return node;
                foreach (var @case in piecewise.Cases)
                {
                    if (@case.Predicate.Substitute(x, at).Evaled is not Entity.Boolean holds)
                    {
                        failed = true;
                        return node;
                    }
                    if (holds.Value)
                        return @case.Expression;
                }
                failed = true;
                return node;
            });
            return failed ? null : resolved;
        }

        // ---- infinitely many, evenly spaced: a floor or a ceiling ------------------------------

        private static bool HasAStep(Entity expr, Variable x)
            => expr.Nodes.Any(node => node is Floorf(var f) && f == x || node is Ceilf(var c) && c == x);

        private static Entity? OverUnitIntervals(Entity expr, Variable x, Entity from, Entity to)
        {
            if (from.Evaled is not Real lower || !lower.IsFinite)
                return null;
            var unbounded = to.Evaled is Real { IsFinite: false, IsNaN: false, IsNegative: false };
            if (!unbounded && (to.Evaled is not Real || !((Real)to.Evaled).IsFinite))
                return null;
            var upper = unbounded ? null : (Real)to.Evaled;
            if (upper is not null && upper < lower)
                return null;

            // The step is one constant from the lower bound up to the next whole number, and
            // one constant from the last whole number up to the upper bound.
            var firstWhole = Ceiling(lower);
            Entity total = Integer.Zero;
            if (upper is not null && Floor(upper) == Floor(lower) && !IsWhole(lower))
                // Both bounds inside one unit interval: a single piece with the step known.
                return OnAPiece(expr, x, from, to, Floor(lower)) is { } single ? single.InnerSimplified : null;
            if (!IsWhole(lower))
            {
                if (OnAPiece(expr, x, from, Integer.Create(firstWhole), Floor(lower)) is not { } head)
                    return null;
                total += head;
            }
            if (upper is not null && !IsWhole(upper))
            {
                var lastWhole = Floor(upper);
                if (OnAPiece(expr, x, Integer.Create(lastWhole), to, lastWhole) is not { } tail)
                    return null;
                total += tail;
            }

            var taken = expr.Vars.Append(x).ToList();
            var n = Variable.CreateTemp(taken);
            taken.Add(n);
            var t = Variable.CreateTemp(taken);
            var piece = expr
                .Substitute(x - new Floorf(x), t)
                .Substitute(new Floorf(x), n)
                .Substitute(new Ceilf(x), n + Integer.One)
                .Substitute(x, n + t);
            var overUnitInterval = new Integralf(piece, t, (Integer.Zero, Integer.One)).InnerSimplified;
            if (overUnitInterval is Integralf)
                return null;
            Entity lastInterval = unbounded ? to : Integer.Create(Floor(upper!).Subtract(EInteger.One));
            var sum = new Summationf(overUnitInterval, n, Integer.Create(firstWhole), lastInterval).InnerSimplified;
            if (sum is Summationf)
                return null;
            return (total + sum).InnerSimplified;
        }

        /// <summary>
        /// The integral from <paramref name="from"/> to <paramref name="to"/> on a piece where
        /// the floor is <paramref name="floorValue"/> throughout (and the ceiling one more,
        /// the ends aside), or <see langword="null"/> where that integral is not found.
        /// </summary>
        private static Entity? OnAPiece(Entity expr, Variable x, Entity from, Entity to, EInteger floorValue)
        {
            var known = expr
                .Substitute(new Floorf(x), Integer.Create(floorValue))
                .Substitute(new Ceilf(x), Integer.Create(floorValue.Add(EInteger.One)));
            var result = known.Integrate(x, from, to);
            return result is Integralf ? null : result;
        }

        private static bool IsWhole(Real value) => value.EDecimal.IsInteger();
        private static EInteger Floor(Real value) => value.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Floor)).ToEInteger();
        private static EInteger Ceiling(Real value) => value.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Ceiling)).ToEInteger();
    }
}
