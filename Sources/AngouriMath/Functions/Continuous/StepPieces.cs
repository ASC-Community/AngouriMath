//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath.Numerics;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra
{
    /// <summary>
    /// An expression with floors and ceilings of <c>x</c> in it, written over a bounded range as the piecewise it is
    /// there: each floor and ceiling replaced by its value on each piece, the pieces' ends exact, and each end owned by
    /// the piece whose value it has. <c>floor(x^2)</c> over <c>[0, 2]</c> is <c>0</c> on <c>[0, 1)</c>, <c>1</c> on
    /// <c>[1, sqrt(2))</c>, <c>2</c> on <c>[sqrt(2), sqrt(3))</c>, <c>3</c> on <c>[sqrt(3), 2)</c> and <c>4</c> at 2.
    /// https://github.com/asc-community/AngouriMath/issues/1807
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ends are where a step's argument <c>g</c> is a whole number: the roots of <c>g(x) = k</c> for each whole
    /// <c>k</c> in <c>g</c>'s range there, as the solver gives them, each checked to be one exactly. What makes the
    /// answer a partition rather than a list of the roots the solver happened to find is the proof that between two
    /// consecutive ends <c>g</c> reaches no whole number: by bisection, each part settled by intervals that hold every
    /// value there (see <see cref="IntervalEvaluation.Over"/>) -- <c>g</c> inside one <c>(k, k + 1)</c>, or <c>g</c>
    /// strictly monotone with no whole number between its values at the part's ends, or, next to an end where
    /// <c>g = k</c>, the first derivative of <c>g</c> that does not vanish there of one sign throughout, so that by
    /// Taylor's theorem <c>g - k</c> has one sign beside the end. A root the solver missed is a part none of these
    /// settles, and the whole is declined: <see langword="null"/>, not an answer with a piece missing.
    /// </para>
    /// <para>
    /// The last criterion is for the ends where <c>g</c> only touches a whole number, which are most of them for a
    /// sine: <c>floor(sin(pi x (4 - x)/2))</c> over <c>[0, 4]</c> reaches 1, -1 and 0 at the turning points of its
    /// argument, where <c>g' = 0</c> and no monotone part has the end in it.
    /// </para>
    /// </remarks>
    internal static class StepPieces
    {
        /// <summary>The most whole numbers one argument may pass through over the range.</summary>
        private const int MostWholeNumbers = 256;
        /// <summary>How far either way a solution's integer parameter is tried.</summary>
        private const int ParameterWindow = 64;
        /// <summary>The most intervals evaluated to settle one gap.</summary>
        private const int MostEvaluationsPerGap = 4096;
        /// <summary>The highest derivative asked at an end where the argument touches a whole number.</summary>
        private const int HighestDerivative = 4;

        internal static Piecewise? Of(Entity expr, Variable x, Entity from, Entity to)
        {
            if (from.Evaled is not Real lower || !lower.IsFinite || to.Evaled is not Real upper || !upper.IsFinite || lower >= upper)
                return null;
            var steps = expr.Nodes.Where(node => node is Floorf or Ceilf && node.ContainsNode(x)).Distinct().ToList();
            // Any other jump in x is one this does not read, and a step inside a step's argument is a jump in it.
            if (expr.Nodes.Any(node => node is Piecewise or Signumf or Iversonf or Providedf && node.ContainsNode(x)))
                return null;
            var arguments = new List<Entity>();
            foreach (var step in steps)
            {
                var argument = ArgumentOf(step);
                if (argument.Nodes.Any(node => node is Floorf or Ceilf) || argument.Vars.Any(variable => variable != x))
                    return null;
                if (!arguments.Contains(argument))
                    arguments.Add(argument);
            }

            // The ends: the range's own, and every point where an argument is a whole number.
            var range = new Interval(AsADouble(lower.EDecimal).Low, AsADouble(upper.EDecimal).High);
            var ends = new List<(Entity At, EDecimal Value)> { (from, lower.EDecimal), (to, upper.EDecimal) };
            var wholeAt = new List<Dictionary<Entity, EInteger>>();
            foreach (var argument in arguments)
            {
                if (WholeNumbersReached(argument, x, lower, upper, range) is not { } reached)
                    return null;
                wholeAt.Add(reached.ToDictionary(root => root.At, root => root.Whole));
                ends.AddRange(reached.Select(root => (root.At, root.Value)));
            }
            if (Ordered(ends) is not { } ordered)
                return null;

            // Each argument's value at each end and on each gap between two.
            var points = new Value[arguments.Count, ordered.Count];
            var gaps = new EInteger[arguments.Count, ordered.Count - 1];
            for (var a = 0; a < arguments.Count; a++)
            {
                var argument = arguments[a];
                var derivatives = new Derivatives(argument, x);
                for (var i = 0; i < ordered.Count; i++)
                    if (AtAnEnd(argument, x, ordered[i], wholeAt[a]) is { } value)
                        points[a, i] = value;
                    else
                        return null;
                for (var i = 0; i + 1 < ordered.Count; i++)
                    if (OnAGap(argument, x, derivatives, (ordered[i], points[a, i]), (ordered[i + 1], points[a, i + 1])) is { } between)
                        gaps[a, i] = between;
                    else
                        return null;
            }

            // The expression on each end and gap in turn, and the runs of one expression joined into one piece.
            var regions = new List<Entity>();
            for (var i = 0; i < ordered.Count; i++)
            {
                regions.Add(Resolved(expr, steps, arguments, step => points[arguments.IndexOf(ArgumentOf(step)), i].Of(step)));
                if (i + 1 < ordered.Count)
                    regions.Add(Resolved(expr, steps, arguments, step => Value.Between(gaps[arguments.IndexOf(ArgumentOf(step)), i]).Of(step)));
            }
            var cases = new List<Providedf>();
            for (var start = 0; start < regions.Count;)
            {
                var end = start;
                while (end + 1 < regions.Count && regions[end + 1] == regions[start])
                    end++;
                // Region 2i is the end i, region 2i + 1 the gap after it.
                var (left, leftClosed) = (ordered[start / 2].At, start % 2 == 0);
                var (right, rightClosed) = end % 2 == 0 ? (ordered[end / 2].At, true) : (ordered[end / 2 + 1].At, false);
                Entity condition = start == end && start % 2 == 0
                    ? MathS.Equality(x, left)
                    : MathS.Sets.ElementInSet(x, MathS.Interval(left, leftClosed, right, rightClosed));
                cases.Add(new Providedf(regions[start], condition));
                start = end + 1;
            }
            return new Piecewise(cases);
        }

        private static Entity ArgumentOf(Entity step) => step switch
        {
            Floorf(var argument) => argument,
            Ceilf(var argument) => argument,
            _ => throw new Core.Exceptions.AngouriBugException($"{step} is not a step")
        };

        private static Entity Resolved(Entity expr, List<Entity> steps, List<Entity> arguments, System.Func<Entity, Entity> valueOf)
        {
            var resolved = expr;
            foreach (var step in steps)
                resolved = resolved.Substitute(step, valueOf(step));
            return resolved.InnerSimplified;
        }

        /// <summary>
        /// An argument's value at a point: a whole number exactly, or strictly between <c>Floor</c> and <c>Floor + 1</c>.
        /// </summary>
        private readonly struct Value
        {
            internal readonly EInteger Floor;
            internal readonly bool IsWhole;
            private Value(EInteger floor, bool isWhole) => (Floor, IsWhole) = (floor, isWhole);
            internal static Value Whole(EInteger value) => new(value, true);
            internal static Value Between(EInteger floor) => new(floor, false);
            internal Entity Of(Entity step) => Integer.Create(step is Ceilf && !IsWhole ? Floor.Add(EInteger.One) : Floor);
        }

        /// <summary>
        /// The points of the range where <paramref name="argument"/> is a whole number, with that number; null where
        /// the argument is not real and finite over the range, passes too many whole numbers, or the solver gives
        /// something other than a finite set for one of them.
        /// </summary>
        private static List<(Entity At, EDecimal Value, EInteger Whole)>? WholeNumbersReached(Entity argument, Variable x, Real lower, Real upper, Interval range)
        {
            if (IntervalEvaluation.Over(argument, x, range) is not { IsReal: true } over)
                return null;
            var least = EInteger.FromInt64((long)System.Math.Floor(over.Re.Low));
            var most = EInteger.FromInt64((long)System.Math.Ceiling(over.Re.High));
            if (most.Subtract(least).CompareTo(EInteger.FromInt32(MostWholeNumbers)) > 0)
                return null;
            var found = new List<(Entity At, EDecimal Value, EInteger Whole)>();
            for (var k = least; k.CompareTo(most) <= 0; k = k.Add(EInteger.One))
            {
                var whole = Integer.Create(k);
                // With a family's parameter free, to be tried at the whole numbers near the range below.
                if (EquationSolver.Solve(argument - whole, x) is not Set.FiniteSet solutions)
                    return null;
                foreach (var solution in solutions)
                {
                    var parameters = solution.Vars.Where(variable => variable != x && !argument.Vars.Contains(variable)).Distinct().ToList();
                    if (parameters.Count > 1)
                        return null;
                    var candidates = parameters.Count == 0
                        ? new[] { solution }
                        : Enumerable.Range(-ParameterWindow, 2 * ParameterWindow + 1).Select(n => solution.Substitute(parameters[0], n));
                    foreach (var candidate in candidates)
                    {
                        // The intervals first, which turn most candidates away for the cost of a few doubles: one
                        // outside the range, or off the real line. An interval that cannot tell -- the root of a
                        // discriminant that is exactly zero, at a double root -- leaves it to the decimals.
                        if (IntervalEvaluation.Of(candidate) is { } enclosure
                            && (enclosure.IsReal ? enclosure.Re.High < range.Low || enclosure.Re.Low > range.High : !enclosure.Im.ContainsZero))
                            continue;
                        if (candidate.Evaled is not Real { IsFinite: true } at || at < lower || at > upper)
                            continue;
                        var root = Bare(candidate.Simplify());
                        // A root exactly, or none: its end is owned by the value there.
                        if (!TreeAnalyzer.IsZero(Bare((argument.Substitute(x, root) - whole).Simplify())))
                            return null;
                        // Two solutions can be one root: sin(u) = 0 is u = 2 pi n and u = pi + 2 pi n.
                        if (!found.Any(known => known.At == root))
                            found.Add((root, at.EDecimal, k));
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// The ends in increasing order, each once: two that are one point by their values are one point exactly, or
        /// the order is not known and this is null.
        /// </summary>
        private static List<(Entity At, EDecimal Value)>? Ordered(List<(Entity At, EDecimal Value)> ends)
        {
            var sorted = ends.OrderBy(end => end.Value).ToList();
            var ordered = new List<(Entity At, EDecimal Value)>();
            foreach (var end in sorted)
            {
                if (ordered.Count > 0 && ordered[^1] is var last && AlikeAt(last.Value, end.Value))
                {
                    if (last.At == end.At)
                        continue;
                    if (!TreeAnalyzer.IsZero(Bare((last.At - end.At).Simplify())))
                        return null;
                    if (end.At.Complexity < last.At.Complexity)
                        ordered[^1] = end;
                    continue;
                }
                ordered.Add(end);
            }
            return ordered;

            static bool AlikeAt(EDecimal a, EDecimal b)
                => a.Subtract(b).Abs().CompareTo(EDecimal.FromString("1E-40").Multiply(EDecimal.Max(EDecimal.One, a.Abs()))) <= 0;
        }

        /// <summary>
        /// <paramref name="argument"/>'s value at an end: the whole number it was found to be there, or one its
        /// interval at the point holds strictly between two, or a whole number the simplification shows it to be.
        /// </summary>
        private static Value? AtAnEnd(Entity argument, Variable x, (Entity At, EDecimal Value) end, Dictionary<Entity, EInteger> wholeAt)
        {
            if (wholeAt.TryGetValue(end.At, out var known))
                return Value.Whole(known);
            var there = argument.Substitute(x, end.At);
            if (IntervalEvaluation.Of(there) is not { IsReal: true } enclosure)
                return null;
            var floor = System.Math.Floor(enclosure.Re.Low);
            if (enclosure.Re.Low > floor && enclosure.Re.High < floor + 1)
                return Value.Between(EInteger.FromInt64((long)floor));
            // An end found for another argument, or the range's own, where this one may be whole too -- or so near a
            // whole number that the intervals cannot tell, and neither can this.
            if (there.Evaled is not Real { IsFinite: true } value)
                return null;
            var nearest = value.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.HalfEven)).ToEInteger();
            return TreeAnalyzer.IsZero(Bare((there - Integer.Create(nearest)).Simplify())) ? Value.Whole(nearest) : null;
        }

        /// <summary>The derivatives of an argument, taken as they are asked for.</summary>
        private sealed class Derivatives
        {
            private readonly List<Entity> taken;
            private readonly Variable x;
            private readonly Dictionary<(int Order, Entity At), bool> vanishing = new();
            internal Derivatives(Entity argument, Variable x) => (taken, this.x) = (new List<Entity> { argument }, x);

            internal Entity this[int order]
            {
                get
                {
                    while (taken.Count <= order)
                        taken.Add(Bare(taken[^1].Differentiate(x)).InnerSimplified);
                    return taken[order];
                }
            }

            /// <summary>Whether the derivative of this order is exactly zero at the point, by simplification.</summary>
            internal bool VanishesAt(int order, Entity at)
            {
                if (!vanishing.TryGetValue((order, at), out var vanishes))
                    vanishing[(order, at)] = vanishes = TreeAnalyzer.IsZero(Bare(this[order].Substitute(x, at).Simplify()));
                return vanishes;
            }
        }

        /// <summary>
        /// The whole number <c>j</c> with <paramref name="argument"/> strictly between <c>j</c> and <c>j + 1</c> on the
        /// whole open gap between two consecutive ends, or null where the bisection does not settle every part of it.
        /// </summary>
        private static EInteger? OnAGap(Entity argument, Variable x, Derivatives derivatives,
            ((Entity At, EDecimal Value) End, Value Value) left, ((Entity At, EDecimal Value) End, Value Value) right)
        {
            if (IntervalEvaluation.Of(left.End.At) is not { IsReal: true } leftAt || IntervalEvaluation.Of(right.End.At) is not { IsReal: true } rightAt)
                return null;
            var budget = MostEvaluationsPerGap;
            EInteger? settled = null;
            var pending = new Stack<(Side Left, Side Right)>();
            pending.Push((Side.AnEnd(leftAt.Re, left.Value, left.End.At), Side.AnEnd(rightAt.Re, right.Value, right.End.At)));
            while (pending.Count > 0)
            {
                var (l, r) = pending.Pop();
                if ((budget -= 4) < 0 || !(l.Range.High < r.Range.Low))
                    return null;
                if (Settle(l, r) is { } j)
                {
                    if (settled is { } before && !before.Equals(j))
                        return null;
                    settled = j;
                    continue;
                }
                var middle = l.Range.High / 2 + r.Range.Low / 2;
                if (!(l.Range.High < middle && middle < r.Range.Low)
                    || IntervalEvaluation.Over(argument, x, Interval.Exactly(middle)) is not { IsReal: true } atTheMiddle)
                    return null;
                var floor = System.Math.Floor(atTheMiddle.Re.Low);
                // A part that meets a whole number at a split point is not settled by splitting it there.
                if (!(atTheMiddle.Re.Low > floor && atTheMiddle.Re.High < floor + 1))
                    return null;
                var inside = Side.Inside(middle, Value.Between(EInteger.FromInt64((long)floor)));
                pending.Push((inside, r));
                pending.Push((l, inside));
            }
            return settled;

            EInteger? Settle(Side l, Side r)
            {
                var over = new Interval(l.Range.Low, r.Range.High);
                if (IntervalEvaluation.Over(argument, x, over) is not { IsReal: true } values)
                    return null;
                // Inside one (j, j + 1) throughout.
                var floor = System.Math.Floor(values.Re.Low);
                if (values.Re.Low > floor && values.Re.High < floor + 1)
                    return EInteger.FromInt64((long)floor);
                // Strictly monotone, with no whole number strictly between its values at the two sides.
                if (IntervalEvaluation.Over(derivatives[1], x, over) is { IsReal: true } slope && !slope.Re.ContainsZero
                    && Between(l.Value, r.Value) is { } monotone)
                    return monotone;
                // Beside an end where it is a whole number k, the first derivative that does not vanish there of one
                // sign throughout: g - k then has one sign beside the end, by Taylor's theorem.
                foreach (var (end, beside, onTheRight) in new[] { (l, r, true), (r, l, false) })
                {
                    if (end.At is not { } at || !end.Value.IsWhole || beside.At is not null)
                        continue;
                    for (var order = 1; order <= HighestDerivative; order++)
                    {
                        if (IntervalEvaluation.Over(derivatives[order], x, over) is { IsReal: true } derivative && !derivative.Re.ContainsZero)
                        {
                            var above = derivative.Re.Low > 0 == (onTheRight || order % 2 == 0);
                            var k = end.Value.Floor;
                            // And no other whole number reached on the part.
                            if (above ? values.Re.High < (double)k.ToInt64Checked() + 1 : values.Re.Low > (double)k.ToInt64Checked() - 1)
                                return above ? k : k.Subtract(EInteger.One);
                            break;
                        }
                        if (!derivatives.VanishesAt(order, at))
                            break;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// The whole number <c>j</c> with every value strictly between the two in <c>(j, j + 1)</c>, for a function
        /// strictly monotone between two points whose values are those; null where a whole number is strictly between.
        /// </summary>
        private static EInteger? Between(Value a, Value b)
        {
            if (a.IsWhole && b.IsWhole)
            {
                var difference = a.Floor.Subtract(b.Floor).Abs();
                return difference.Equals(EInteger.One) ? EInteger.Min(a.Floor, b.Floor) : null;
            }
            if (a.IsWhole || b.IsWhole)
            {
                var (whole, other) = a.IsWhole ? (a, b) : (b, a);
                return other.Floor.Equals(whole.Floor) || other.Floor.Equals(whole.Floor.Subtract(EInteger.One)) ? other.Floor : null;
            }
            return a.Floor.Equals(b.Floor) ? a.Floor : null;
        }

        /// <summary>
        /// One side of a part being settled: an end of the gap, with its exact point and value, or a split point inside
        /// it, a double, with the argument's value there.
        /// </summary>
        private readonly struct Side
        {
            internal readonly Interval Range;
            internal readonly Value Value;
            internal readonly Entity? At;
            private Side(Interval range, Value value, Entity? at) => (Range, Value, At) = (range, value, at);
            internal static Side AnEnd(Interval range, Value value, Entity at) => new(range, value, at);
            internal static Side Inside(double point, Value value) => new(Interval.Exactly(point), value, null);
        }

        private static Entity Bare(Entity expr) => expr is Providedf(var inner, _) ? inner : expr;

        /// <summary>
        /// The double a bound is, exactly, where it is one -- 0, a half, a whole number -- and the interval of one ulp
        /// either way otherwise. Widened, the range of <c>sqrt(x)</c> from 0 held a negative and had no value.
        /// </summary>
        private static Interval AsADouble(EDecimal value)
        {
            var nearest = value.ToDouble();
            return EDecimal.FromDouble(nearest).CompareTo(value) == 0 ? Interval.Exactly(nearest) : Interval.Around(nearest);
        }
    }
}
