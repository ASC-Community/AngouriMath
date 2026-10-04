//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Core.Sets
{
    internal static partial class SetOperators
    {
        /// <summary>
        /// A union of numeric intervals and listed real numbers written as its disjoint pieces:
        /// the listed numbers first, in increasing order, then the intervals in increasing order,
        /// a point at an open end closing it and an empty interval dropped. <c>x^2 &gt;= x</c> is
        /// solved to <c>{ 0, 1 } \/ (-oo; 0) \/ (1; +oo)</c>, which is <c>(-oo; 0] \/ [1; +oo)</c>,
        /// and two ways of writing one set of numbers come out the same. <see langword="null"/>
        /// where a piece is anything else, or the union is written so already.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// </summary>
        internal static Set? CanonicalUnion(Set set)
        {
            var pieces = new List<(Entity Left, bool LeftClosed, Entity Right, bool RightClosed)>();
            bool Collect(Entity piece)
            {
                switch (piece)
                {
                    case Unionf(var left, var right):
                        return Collect(left) && Collect(right);
                    case Interval(var left, var leftClosed, var right, var rightClosed) when left.Evaled is Number.Real && right.Evaled is Number.Real:
                        var order = At(left).CompareTo(At(right));
                        if (order < 0 || order == 0 && leftClosed && rightClosed)
                            pieces.Add((left, leftClosed, right, rightClosed));
                        return true;
                    case FiniteSet listed when listed.All(static member => member.Evaled is Number.Real):
                        foreach (var member in listed)
                            pieces.Add((member, true, member, true));
                        return true;
                    default:
                        return false;
                }
            }
            if (!Collect(set))
                return null;
            // By the left end, a closed end before an open one at the same point.
            pieces.Sort(static (a, b) => At(a.Left).CompareTo(At(b.Left)) is var byLeft and not 0 ? byLeft : b.LeftClosed.CompareTo(a.LeftClosed));
            var merged = new List<(Entity Left, bool LeftClosed, Entity Right, bool RightClosed)>();
            foreach (var piece in pieces)
            {
                if (merged.Count == 0)
                {
                    merged.Add(piece);
                    continue;
                }
                var last = merged[^1];
                var gap = At(piece.Left).CompareTo(At(last.Right));
                if (gap > 0 || gap == 0 && !last.RightClosed && !piece.LeftClosed)
                {
                    merged.Add(piece);
                    continue;
                }
                var further = At(piece.Right).CompareTo(At(last.Right));
                merged[^1] = further > 0 ? (last.Left, last.LeftClosed, piece.Right, piece.RightClosed)
                    : further == 0 ? (last.Left, last.LeftClosed, last.Right, last.RightClosed || piece.RightClosed)
                    : last;
            }
            var points = merged.Where(static piece => At(piece.Left).CompareTo(At(piece.Right)) == 0).Select(static piece => piece.Left).ToList();
            var intervals = merged.Where(static piece => At(piece.Left).CompareTo(At(piece.Right)) != 0)
                .Select(static piece => (Set)new Interval(piece.Left, piece.LeftClosed, piece.Right, piece.RightClosed)).ToList();
            Set canonical = intervals.Count == 0 ? new FiniteSet(points)
                : (points.Count == 0 ? intervals : intervals.Prepend(new FiniteSet(points)))
                    .Aggregate(static (left, right) => new Unionf(left, right));
            return canonical == set ? null : canonical;
        }

        private static PeterO.Numbers.EDecimal At(Entity end) => ((Number.Real)end.Evaled).EDecimal;

        internal static Set UniteFiniteSetAndSet(FiniteSet finite, Set set)
        {
            if (set is FiniteSet another)
                return FiniteSet.Unite(finite, another);
            var sb = new FiniteSetBuilder();
            if (set is Interval(var left, var leftClosed, var right, var rightClosed) inter)
            {
                var newLeftClosed = leftClosed;
                var newRightClosed = rightClosed;
                foreach (var el in finite)
                    if (!set.TryContains(el, out var contains) || !contains)
                    {
                        if (el == left)
                            newLeftClosed = true;
                        else if (el == right)
                            newRightClosed = true;
                        else
                            sb.Add(el);
                    }
                set = inter.New(left, newLeftClosed, right, newRightClosed);
            }
            else
            {
                foreach (var el in finite)
                    if (!set.TryContains(el, out var contains) || !contains)
                        sb.Add(el);
            }
            return sb.IsEmpty ? set : sb.ToFiniteSet().Unite(set);
        }

        /// <summary>
        /// The union of two intervals: one interval where they overlap or touch, and as written
        /// where they do not, or where that cannot be told.
        /// </summary>
        /// <remarks>
        /// An empty interval adds nothing -- one whose left end is above its right, or at it with
        /// an end open -- so <c>[0; 1] \/ [1; 0]</c> is <c>[0; 1]</c>. Two intervals that touch are
        /// one only when neither is empty. Touching was tested first, so <c>[1; 0]</c>, touching
        /// <c>[0; 1]</c> at 0, was joined to it into <c>{ 1 }</c>, and <c>[a; b] \/ [b; a]</c> into
        /// <c>{ b }</c>. Ends that are not numbers do not say whether an interval is empty, unless
        /// the ends are one expression closed at both or one end is infinite, as in
        /// <c>(-oo; x] \/ [x; +oo)</c>; otherwise the union is left as written.
        /// https://github.com/asc-community/AngouriMath/issues/1634
        /// </remarks>
        internal static Set UniteIntervalAndInterval(Interval A, Interval B)
        {
            if (IsEmpty(A))
                return B;
            if (IsEmpty(B))
                return A;
            if (!IsNotEmpty(A) || !IsNotEmpty(B))
                return A.Unite(B);
            if (A.Left == B.Right && (A.LeftClosed || B.RightClosed))
                return new Interval(B.Left, B.LeftClosed, A.Right, A.RightClosed);
            if (A.Right == B.Left && (A.RightClosed || B.LeftClosed))
                return new Interval(A.Left, A.LeftClosed, B.Right, B.RightClosed);
            if (A.Left is not Real aLeft ||
                A.Right is not Real aRight ||
                B.Left is not Real bLeft ||
                B.Right is not Real bRight)
                return A.Unite(B);
            // Neither is empty, so ends that are equal are a point closed at both.
            if (aLeft == aRight)
                return UniteFiniteSetAndSet(new FiniteSet(aLeft), B);
            if (bLeft == bRight)
                return UniteFiniteSetAndSet(new FiniteSet(bLeft), A);
            if (aLeft == bRight && !A.LeftClosed && !B.RightClosed)
                return A.Unite(B);
            if (bLeft == aRight && !B.LeftClosed && !A.RightClosed)
                return A.Unite(B);
            if (aLeft > bRight)
                return A.Unite(B);
            if (bLeft > aRight)
                return A.Unite(B);
            if (aLeft < bLeft && bRight < aRight)
                return A;
            if (bLeft < aLeft && aRight < bRight)
                return B;
            var (left, leftClosed) = 
                aLeft == bLeft ? 
                (aLeft, A.LeftClosed || B.LeftClosed) : 
                (bLeft < aLeft ? (bLeft, B.LeftClosed) : (aLeft, A.LeftClosed));
            var (right, rightClosed) =
                aRight == bRight ?
                (aRight, A.RightClosed || B.RightClosed) :
                (bRight > aRight ? (bRight, B.RightClosed) : (aRight, A.RightClosed));
            return new Interval(left, leftClosed, right, rightClosed);
        }

        /// <summary>Known to be empty: numeric ends out of order, or at one point with an end open.</summary>
        private static bool IsEmpty(Interval interval)
            => interval.Left is Real left && interval.Right is Real right
               && (left > right || left == right && !(interval.LeftClosed && interval.RightClosed));

        /// <summary>
        /// Known not to be empty: numeric ends in order, the same end at both sides and closed, or
        /// an end at an infinity, beyond any end the other side can have.
        /// </summary>
        private static bool IsNotEmpty(Interval interval)
            => interval.Left is Real left && interval.Right is Real right
                ? left < right || left == right && interval.LeftClosed && interval.RightClosed
                : interval.Left == interval.Right
                    ? interval.LeftClosed && interval.RightClosed
                    : interval.Left == Real.NegativeInfinity || interval.Right == Real.PositiveInfinity;

        internal static Set UniteCSetAndCSet(ConditionalSet intLeft, ConditionalSet intRight)
        {
            (intLeft, intRight) = MergeToOneVariable(intLeft, intRight);
            return new ConditionalSet(intLeft.Var, (intLeft.Predicate | intRight.Predicate).InnerSimplified);
        }
    }
}
