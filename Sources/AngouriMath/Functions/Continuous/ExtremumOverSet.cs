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
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions
{
    /// <summary>
    /// The largest or smallest value an expression takes over a set, and the points where it
    /// takes it: <c>max(f(t), t in S)</c>, <c>min</c>, <c>argmax</c>, <c>argmin</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Over a finite set of numbers the expression is evaluated at every element and the values
    /// compared, which is the definition. Over a closed interval with numeric ends the extremum is
    /// attained at an endpoint or at a stationary point inside, so the candidates are the closed
    /// endpoints and the zeros of the derivative in the open interval, which the solver is asked
    /// for; a periodic family of zeros -- <c>pi/6 + 2 pi n</c> -- is read as a line in its
    /// parameter and the whole numbers that land inside the interval are enumerated. An open
    /// endpoint is not a candidate, since a value there is not attained: <c>max(x, x in [0; 1))</c>
    /// has no maximum, and is left as written rather than answered 1.
    /// </para>
    /// <para>
    /// Two guards, because a wrong maximum is worse than none. The expression has to be one
    /// that is smooth on the reals -- sums, products, non-negative whole powers, sines, cosines,
    /// and exponentials with a positive base -- so that every interior extremum is a zero of the
    /// derivative; <c>abs(x)</c> has its minimum where its derivative is undefined, and
    /// <c>1/x</c> has no maximum on <c>[-1; 1]</c> at all. And the candidates' best value is
    /// checked against the expression sampled along the interval: the solver's list of zeros is
    /// not guaranteed complete, and a sample that beats the best candidate means one was missed,
    /// on which the question is left as written. A sample can miss a narrow peak, so this is a
    /// guard and not a proof; what it rules out is the wrong answer the incomplete list would
    /// otherwise give with confidence.
    /// </para>
    /// <para>
    /// The value is returned symbolically -- the expression at the best point, simplified -- and
    /// the points as the candidates themselves, so <c>max(sin(t)^3 cos(t), t in [0; pi/2])</c>
    /// is <c>3 sqrt(3) / 16</c>. Question I.6 of
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1212">#1212</a>.
    /// </para>
    /// </remarks>
    internal static class ExtremumOverSet
    {
        /// <summary>How many points along an interval the best candidate is checked against.</summary>
        private const int Samples = 256;

        /// <summary>How many members of a periodic family are enumerated before declining.</summary>
        private const int MaxFamilyMembers = 4096;

        /// <summary>The largest (or smallest) value, or <see langword="null"/> where it is not settled.</summary>
        internal static Entity? Value(Entity expression, Entity var, Entity over, bool largest)
            => Find(expression, var, over, largest) is (var value, _) ? value : null;

        /// <summary>The set of points where the largest (or smallest) value is taken, or <see langword="null"/>.</summary>
        internal static Entity? Points(Entity expression, Entity var, Entity over, bool largest)
            => Find(expression, var, over, largest) is (_, var points) ? new FiniteSet(points.ToArray()) : null;

        private static (Entity value, List<Entity> points)? Find(Entity expression, Entity var, Entity over, bool largest)
        {
            if (var is not Variable x)
                return null;
            var domain = over.InnerSimplified;
            // The least member of a set of whole numbers bounded below, or the greatest of one
            // bounded above, where the expression is the variable itself: min(PP) is 2,
            // min(x, x in PP and x > 14) the next prime, 17, and max(x, x in PP and x < 14) the
            // previous, 13. https://github.com/asc-community/AngouriMath/issues/1450
            if (expression == x && domain is Set bounded && (largest ? GreatestWholeMember(bounded) : LeastWholeMember(bounded)) is { } end)
                return (end, new List<Entity> { end });
            List<Entity>? candidates;
            (Real from, Real to)? interval = null;
            switch (domain)
            {
                // Elements that are numbers -- pi/2 among them -- are distinct exactly when
                // unequal, so the set is what it says; with a symbol in it, it is not yet.
                case FiniteSet finite when finite.All(static element => element.Evaled is Real { IsFinite: true }):
                    candidates = finite.Elements.ToList();
                    break;
                case Interval bounds when bounds.Left.Evaled is Real { IsFinite: true } left && bounds.Right.Evaled is Real { IsFinite: true } right:
                    candidates = OverAnInterval(expression, x, bounds, left, right);
                    interval = (left, right);
                    break;
                default:
                    return null;
            }
            if (candidates is null || candidates.Count == 0)
                return null;

            var valued = new List<(Entity point, Real value)>();
            Real? best = null;
            foreach (var candidate in candidates)
            {
                if (expression.Substitute(x, candidate).Evaled is not Real value || !value.IsFinite || value.IsNaN)
                    return null;
                valued.Add((candidate, value));
                if (best is null || (largest ? value > best : value < best))
                    best = value;
            }
            if (interval is var (a, b) && !SamplesAgree(expression, x, a, b, best!, largest))
                return null;

            var points = valued.Where(p => p.value == best).Select(p => p.point.InnerSimplified).ToList();
            var at = expression.Substitute(x, points[0]).InnerSimplified;
            return (at, points);
        }

        /// <summary>
        /// The least member of <c>PP</c>, <c>ZZ+</c>, <c>ZZ*</c>, of one of them cut by an
        /// interval or a ray, or of <c>{ x in S : x &gt; a }</c> and its kin over one of them;
        /// <see langword="null"/> for anything else, and where a search for the next prime is
        /// not taken to its end.
        /// </summary>
        private static Integer? LeastWholeMember(Set set)
        {
            switch (set)
            {
                case SpecialSet special:
                    return special.ToDomain() switch
                    {
                        Core.Domain.Prime => Integer.Create(2),
                        Core.Domain.PositiveInteger => Integer.One,
                        Core.Domain.NonNegativeInteger => Integer.Zero,
                        _ => null,
                    };
                case Intersectionf(Set left, Set right):
                    {
                        var (whole, cut) = (left, right) switch
                        {
                            (SpecialSet s, Interval i) => (s, i),
                            (Interval i, SpecialSet s) => (s, i),
                            _ => (null, null),
                        };
                        if (whole is null || cut is null || LeastWholeMember(whole) is not { } floor)
                            return null;
                        if (cut.Left.Evaled is not Real from)
                            return null;
                        var start = from.IsFinite ? from.EDecimal.RoundToExponent(EInteger.Zero, ERounding.Ceiling).ToEInteger() : floor.EInteger;
                        if (from.IsFinite && !cut.LeftClosed && from.EDecimal.CompareTo(EDecimal.FromEInteger(start)) == 0)
                            start += 1;
                        if (start.CompareTo(floor.EInteger) < 0)
                            start = floor.EInteger;
                        var first = whole.ToDomain() == Core.Domain.Prime ? Primes.NextPrime(start) : start;
                        if (first is null)
                            return null;
                        // Within the cut on the right, or the set is empty and has no least member.
                        if (cut.Right.Evaled is Real to && to.IsFinite)
                        {
                            var order = EDecimal.FromEInteger(first).CompareTo(to.EDecimal);
                            if (order > 0 || order == 0 && !cut.RightClosed)
                                return null;
                        }
                        return Integer.Create(first);
                    }
                case ConditionalSet { DeclaredMembership: var (declared, rest), Var: Variable y } when declared.InnerSimplified is Set over:
                    return BoundsAsInterval(rest, y) is { } bounded ? LeastWholeMember(new Intersectionf(over, bounded)) : FirstThatHolds(over, y, rest);
                default:
                    return null;
            }
        }

        /// <summary>
        /// The least member of the whole numbers <paramref name="over"/> at which
        /// <paramref name="condition"/> evaluates to <c>True</c>, found by trying them in order from
        /// the least up: every member before it was tried and evaluated to <c>False</c>, so it is the
        /// least. Sullivan and Mackey's Prob 8.9.3,
        /// <c>min(n, n in { n in ZZ+ : n binomial(n - 1, 2) &gt;= 14 })</c>, is 5. <see langword="null"/>
        /// where a member's condition does not evaluate to a truth value, or none of the first
        /// <see cref="MaxMembersSearched"/> holds. https://github.com/asc-community/AngouriMath/issues/1409
        /// </summary>
        private static Integer? FirstThatHolds(Set over, Variable y, Entity condition)
        {
            if (LeastWholeMember(over) is not { } least || over is not SpecialSet special)
                return null;
            var prime = special.ToDomain() == Core.Domain.Prime;
            EInteger? member = least.EInteger;
            for (var tried = 0; tried < MaxMembersSearched && member is { } current; tried++)
            {
                switch (condition.Substitute(y, Integer.Create(current)).Evaled)
                {
                    case Entity.Boolean(true):
                        return Integer.Create(current);
                    case Entity.Boolean(false):
                        member = prime ? Primes.NextPrime(current + 1) : current + 1;
                        continue;
                    default:
                        return null;
                }
            }
            return null;
        }

        /// <summary>How many members <see cref="FirstThatHolds"/> tries before declining.</summary>
        private const int MaxMembersSearched = 4096;

        /// <summary>
        /// A conjunction of bounds on <paramref name="y"/> -- <c>y &gt; a</c>, <c>y &gt;= a</c>,
        /// <c>a &lt; y</c>, <c>y &lt; b</c> and their kin -- as the interval they cut, or
        /// <see langword="null"/> where a conjunct is anything else.
        /// </summary>
        private static Interval? BoundsAsInterval(Entity bounds, Variable y)
        {
            Entity left = Real.NegativeInfinity;
            Entity right = Real.PositiveInfinity;
            bool leftClosed = false, rightClosed = false;
            foreach (var bound in Conjuncts(bounds))
            {
                var (lower, lowerClosed, upper, upperClosed) = bound switch
                {
                    Greaterf(var l, var r) when l == y && !r.ContainsNode(y) => (r, false, (Entity?)null, false),
                    GreaterOrEqualf(var l, var r) when l == y && !r.ContainsNode(y) => (r, true, null, false),
                    Lessf(var l, var r) when r == y && !l.ContainsNode(y) => (l, false, null, false),
                    LessOrEqualf(var l, var r) when r == y && !l.ContainsNode(y) => (l, true, null, false),
                    Lessf(var l, var r) when l == y && !r.ContainsNode(y) => (null, false, r, false),
                    LessOrEqualf(var l, var r) when l == y && !r.ContainsNode(y) => (null, false, r, true),
                    Greaterf(var l, var r) when r == y && !l.ContainsNode(y) => (null, false, l, false),
                    GreaterOrEqualf(var l, var r) when r == y && !l.ContainsNode(y) => (null, false, l, true),
                    _ => ((Entity?)null, false, (Entity?)null, false),
                };
                if (lower is null && upper is null)
                    return null;
                // Two bounds on one side: the tighter wins where both are numbers, and the set
                // is left as written otherwise.
                if (lower is not null)
                {
                    if (left is Real a && lower.Evaled is Real b)
                    {
                        var order = b.EDecimal.CompareTo(a.EDecimal);
                        if (order > 0 || order == 0 && !lowerClosed) { left = lower; leftClosed = lowerClosed && (order > 0 || leftClosed); }
                    }
                    else if (left.Evaled is Real { IsFinite: false }) { left = lower; leftClosed = lowerClosed; }
                    else return null;
                }
                if (upper is not null)
                {
                    if (right is Real a && upper.Evaled is Real b)
                    {
                        var order = b.EDecimal.CompareTo(a.EDecimal);
                        if (order < 0 || order == 0 && !upperClosed) { right = upper; rightClosed = upperClosed && (order < 0 || rightClosed); }
                    }
                    else if (right.Evaled is Real { IsFinite: false }) { right = upper; rightClosed = upperClosed; }
                    else return null;
                }
            }
            return new Interval(left, leftClosed, right, rightClosed);
        }

        private static IEnumerable<Entity> Conjuncts(Entity statement)
            => statement is Andf(var l, var r) ? Conjuncts(l).Concat(Conjuncts(r)) : new[] { statement };

        /// <summary>
        /// The range of <c>min(expr, range)</c> and <c>max</c>: <c>x in S</c>, or a conjunction with
        /// one <c>x in S</c> among its conjuncts, the rest being the condition of
        /// <c>{ x in S : rest }</c>. https://github.com/asc-community/AngouriMath/issues/1450
        /// </summary>
        internal static (Variable var, Entity over)? AsRange(Entity range)
        {
            if (range is Inf(Variable x, var set))
                return (x, set);
            if (range is not Andf)
                return null;
            var conjuncts = Conjuncts(range).ToList();
            var declared = conjuncts.Where(c => c is Inf { Element: Variable }).ToList();
            if (declared.Count != 1)
                return null;
            var membership = (Inf)declared[0];
            var rest = conjuncts.Where(c => !ReferenceEquals(c, membership)).Aggregate((a, b) => a & b);
            return ((Variable)membership.Element, new ConditionalSet(membership, rest));
        }

        /// <summary>
        /// The greatest member of <c>PP</c>, <c>ZZ+</c>, <c>ZZ*</c> or <c>ZZ</c> cut by an interval
        /// with a finite right end, or of <c>{ x in S : x &lt; a }</c> and its kin over one of
        /// them: the last whole number at or before the end, or the last prime, searched down.
        /// <see langword="null"/> where the set has no greatest member, or none at all.
        /// </summary>
        private static Integer? GreatestWholeMember(Set set)
        {
            switch (set)
            {
                case Intersectionf(Set left, Set right):
                    {
                        var (whole, cut) = (left, right) switch
                        {
                            (SpecialSet s, Interval i) => (s, i),
                            (Interval i, SpecialSet s) => (s, i),
                            _ => (null, null),
                        };
                        if (whole is null || cut is null || whole.ToDomain() is not (Core.Domain.Prime or Core.Domain.PositiveInteger or Core.Domain.NonNegativeInteger or Core.Domain.Integer))
                            return null;
                        if (cut.Right.Evaled is not Real to || !to.IsFinite)
                            return null;
                        var end = to.EDecimal.RoundToExponent(EInteger.Zero, ERounding.Floor).ToEInteger();
                        if (!cut.RightClosed && to.EDecimal.CompareTo(EDecimal.FromEInteger(end)) == 0)
                            end -= 1;
                        // Not below the set's own floor, where there is one.
                        var floor = whole.ToDomain() switch
                        {
                            Core.Domain.Prime => EInteger.FromInt32(2),
                            Core.Domain.PositiveInteger => EInteger.One,
                            Core.Domain.NonNegativeInteger => EInteger.Zero,
                            _ => (EInteger?)null,
                        };
                        if (floor is { } lowest && end.CompareTo(lowest) < 0)
                            return null;
                        EInteger? last = end;
                        if (whole.ToDomain() == Core.Domain.Prime)
                        {
                            last = null;
                            for (var candidate = end; candidate.CompareTo(EInteger.FromInt32(2)) >= 0 && (end - candidate).CompareTo(EInteger.FromInt32(100000)) <= 0; candidate -= 1)
                                if (Primes.IsPrime(candidate) is { } decided)
                                {
                                    if (decided) { last = candidate; break; }
                                }
                                else
                                    return null;
                        }
                        if (last is null)
                            return null;
                        // And within the cut on the left, or the set is empty.
                        if (cut.Left.Evaled is Real from && from.IsFinite)
                        {
                            var order = EDecimal.FromEInteger(last).CompareTo(from.EDecimal);
                            if (order < 0 || order == 0 && !cut.LeftClosed)
                                return null;
                        }
                        return Integer.Create(last);
                    }
                case ConditionalSet { DeclaredMembership: var (declared, rest), Var: Variable y } when declared.InnerSimplified is Set over:
                    return BoundsAsInterval(rest, y) is { } bounded ? GreatestWholeMember(new Intersectionf(over, bounded)) : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// The closed endpoints and the stationary points inside, or <see langword="null"/> where
        /// the expression is not one whose extrema are all stationary, or the solver does not
        /// give a finite list.
        /// </summary>
        private static List<Entity>? OverAnInterval(Entity expression, Variable x, Interval bounds, Real left, Real right)
        {
            var order = left.EDecimal.CompareTo(right.EDecimal);
            if (order > 0)
                return null;
            if (order == 0)
                return bounds.LeftClosed && bounds.RightClosed ? new List<Entity> { bounds.Left } : null;
            if (!IsSmoothOn(expression, x))
                return null;

            // The ends as written, so that pi/2 stays pi/2 in the answer.
            var candidates = new List<Entity>();
            if (bounds.LeftClosed)
                candidates.Add(bounds.Left);
            if (bounds.RightClosed)
                candidates.Add(bounds.Right);
            var interior = new Interval(bounds.Left, false, bounds.Right, false);

            if (Algebra.AnalyticalSolving.StatementSolver.Solved(expression.Differentiate(x).Simplify().Equalizes(Integer.Zero), x) is not FiniteSet zeros)
                return null;
            foreach (var zero in zeros.Elements)
            {
                // Vars, not FreeVariables: the solver's parameter is n_1, and pi, e and i are
                // constants that FreeVariables would miscount as parameters of the family.
                var parameters = zero.Vars.Where(v => v != x).ToList();
                switch (parameters.Count)
                {
                    case 0:
                        if (Inside(zero, interior))
                            candidates.Add(zero);
                        break;
                    case 1:
                        if (!AddTheFamily(zero, parameters[0], interior, left, right, candidates))
                            return null;
                        break;
                    default:
                        return null;
                }
            }
            return candidates;
        }

        private static bool Inside(Entity point, Interval interior)
            => new Inf(point, interior).Evaled is Entity.Boolean { Value: true };

        /// <summary>
        /// A zero of the form <c>c + p n</c> for a whole <c>n</c>: the members that land inside
        /// the interval are added. Anything else in the parameter declines.
        /// </summary>
        private static bool AddTheFamily(Entity family, Variable parameter, Interval interior, Real left, Real right, List<Entity> candidates)
        {
            // A family with an imaginary step, ln(2) / ln(2) + 2 pi i n / ln(2) for a zero of
            // 2^x ln(2) - 1 over the complex numbers, is real at one member at most: where the
            // imaginary parts cancel. https://github.com/asc-community/AngouriMath/issues/2319
            if (AtComplex(0) is { } c0 && AtComplex(1) is { } c01 && (c01 - c0) is var step
                && Math.Abs(step.Imaginary) > 1e-9 && Math.Abs(step.Real) < 1e-12 * Math.Max(1, Math.Abs(step.Imaginary)))
            {
                var at = Math.Round(-c0.Imaginary / step.Imaginary);
                if (Math.Abs(c0.Imaginary + at * step.Imaginary) > 1e-9 * Math.Max(1, Math.Abs(step.Imaginary)) || Math.Abs(at) > MaxFamilyMembers)
                    return true;
                var real = family.Substitute(parameter, Integer.Create((long)at)).InnerSimplified;
                if (Inside(real, interior))
                    candidates.Add(real);
                return true;
            }
            System.Numerics.Complex? AtComplex(int n)
                => family.Substitute(parameter, Integer.Create(n)).Evaled is Complex { IsFinite: true } value
                    ? new System.Numerics.Complex(value.RealPart.EDecimal.ToDouble(), value.ImaginaryPart.EDecimal.ToDouble())
                    : null;
            // Read numerically at 0, 1 and 2: the family is c + p n exactly when the steps agree,
            // whatever shape the solver wrote it in.
            if (At(0) is not { } c || At(1) is not { } c1 || At(2) is not { } c2)
                return false;
            var p = c1 - c;
            if (p == 0 || Math.Abs((c2 - c1) - p) > 1e-9 * Math.Max(1, Math.Abs(p)))
                return false;
            // a < c + p n < b bounds n between (a - c) / p and (b - c) / p, in either order.
            var first = (left.EDecimal.ToDouble() - c) / p;
            var last = (right.EDecimal.ToDouble() - c) / p;

            double? At(int n)
                => family.Substitute(parameter, Integer.Create(n)).Evaled is Real { IsFinite: true } value
                    ? value.EDecimal.ToDouble()
                    : null;
            var lowest = Math.Floor(Math.Min(first, last)) - 1;
            var highest = Math.Ceiling(Math.Max(first, last)) + 1;
            if (highest - lowest > MaxFamilyMembers)
                return false;
            for (var n = (long)lowest; n <= (long)highest; n++)
            {
                var member = family.Substitute(parameter, Integer.Create(n)).InnerSimplified;
                if (Inside(member, interior))
                    candidates.Add(member);
            }
            return true;
        }

        /// <summary>
        /// Whether the expression, sampled along the interval, never beats the best candidate.
        /// A sample that does means a stationary point the solver did not list.
        /// </summary>
        private static bool SamplesAgree(Entity expression, Variable x, Real left, Real right, Real best, bool largest)
        {
            var a = left.EDecimal.ToDouble();
            var b = right.EDecimal.ToDouble();
            var bestValue = best.EDecimal.ToDouble();
            var tolerance = 1e-9 * Math.Max(1, Math.Abs(bestValue));
            for (var k = 0; k <= Samples; k++)
            {
                var t = a + (b - a) * k / Samples;
                if (expression.Substitute(x, Real.Create(EDecimal.FromDouble(t))).Evaled is not Real sample || !sample.IsFinite || sample.IsNaN)
                    return false;
                var v = sample.EDecimal.ToDouble();
                if (largest ? v > bestValue + tolerance : v < bestValue - tolerance)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Whether every extremum of the expression on the reals is a zero of its derivative:
        /// built from sums, products, non-negative whole powers, sines and cosines, exponentials
        /// with a positive base, and divisions by something free of the variable.
        /// </summary>
        private static bool IsSmoothOn(Entity expression, Variable x)
        {
            if (!expression.ContainsNode(x))
                return true;
            return expression switch
            {
                Variable => true,
                Sumf(var left, var right) => IsSmoothOn(left, x) && IsSmoothOn(right, x),
                Minusf(var left, var right) => IsSmoothOn(left, x) && IsSmoothOn(right, x),
                Mulf(var left, var right) => IsSmoothOn(left, x) && IsSmoothOn(right, x),
                Divf(var left, var right) => IsSmoothOn(left, x) && !right.ContainsNode(x),
                Powf(var @base, Integer { IsNegative: false }) => IsSmoothOn(@base, x),
                Powf(var @base, var exponent) when !@base.ContainsNode(x) && @base.Evaled is Real value && value > Integer.Zero
                    => IsSmoothOn(exponent, x),
                Sinf(var argument) => IsSmoothOn(argument, x),
                Cosf(var argument) => IsSmoothOn(argument, x),
                _ => false
            };
        }
    }
}
