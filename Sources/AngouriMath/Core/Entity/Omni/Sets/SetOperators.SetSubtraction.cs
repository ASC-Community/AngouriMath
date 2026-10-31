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
        internal static Set SetSubtractSetAndFiniteSet(Set set, FiniteSet finite)
        {
            if (set is FiniteSet another && FiniteSet.TryFullSubtract(another, finite, out var result))
                return result;
            var fsb = new FiniteSetBuilder(finite);
            foreach (var el in finite)
                if (set.TryContains(el, out var contains) && !contains)
                    fsb.Remove(el);
            return fsb.IsEmpty ? set : set.SetSubtract(fsb.ToFiniteSet());
        }

        private static bool Implies(this bool assumption, bool conclusion)
            => !assumption || conclusion;

        /// <summary>
        /// What is left of <paramref name="A"/> with <paramref name="B"/> taken out: the part of
        /// <paramref name="A"/> below <paramref name="B"/> and the part above it, each an
        /// interval, a point or nothing.
        /// </summary>
        /// <remarks>
        /// Each part ends where <paramref name="B"/> begins, and holds that end where
        /// <paramref name="B"/> does not: <c>[0; 10] \ (2; 3)</c> is <c>[0; 2] \/ [3; 10]</c>,
        /// and <c>[0; 10] \ (0; 3)</c> is <c>{ 0 } \/ [3; 10]</c>. One interval from an end of
        /// <paramref name="A"/> to the far end of <paramref name="B"/> was answered for the first,
        /// <c>[0; 3]</c>, the end of the cut was read from the other end of
        /// <paramref name="B"/>, and an end of <paramref name="A"/> that <paramref name="B"/> left
        /// was lost. The ends are compared by their values, as the intersection compares them,
        /// and kept as written. Where an end has no value the difference is left as it is, unless
        /// the ends are the same and <paramref name="B"/> holds every end <paramref name="A"/>
        /// holds; then it holds all of <paramref name="A"/>. Where <paramref name="A"/> holds an
        /// end that <paramref name="B"/> does not, what is left depends on the order of the ends:
        /// <c>[a; b] \ (a; b)</c> is <c>{ a, b }</c> where <c>a &lt; b</c> and nothing where
        /// <c>a &gt; b</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2290
        /// </remarks>
        internal static Set SetSubtractIntervalAndInterval(Interval A, Interval B)
        {
            if (A == B)
                return Empty;
            if (A.Left == B.Left && A.Right == B.Right && A.LeftClosed.Implies(B.LeftClosed) && A.RightClosed.Implies(B.RightClosed))
                return Empty;
            if (A.Left.Evaled is not Real { IsNaN: false } aLeft ||
                A.Right.Evaled is not Real { IsNaN: false } aRight ||
                B.Left.Evaled is not Real { IsNaN: false } bLeft ||
                B.Right.Evaled is not Real { IsNaN: false } bRight)
                return A.SetSubtract(B);
            if (aLeft > aRight || aLeft == aRight && !(A.LeftClosed && A.RightClosed))
                return Empty;
            if (bLeft > bRight || bLeft == bRight && !(B.LeftClosed && B.RightClosed))
                return A;
            // Below B: from A's left end to whichever comes first, A's right end or B's left end.
            var (belowEnd, belowEndValue, belowEndClosed) =
                aRight < bLeft ? (A.Right, aRight, A.RightClosed) :
                bLeft < aRight ? (B.Left, bLeft, !B.LeftClosed) :
                (A.Right, aRight, A.RightClosed && !B.LeftClosed);
            // Above B: from whichever comes last, A's left end or B's right end, to A's right end.
            var (aboveStart, aboveStartValue, aboveStartClosed) =
                aLeft > bRight ? (A.Left, aLeft, A.LeftClosed) :
                bRight > aLeft ? (B.Right, bRight, !B.RightClosed) :
                (A.Left, aLeft, A.LeftClosed && !B.RightClosed);
            var below = Part(A.Left, aLeft, A.LeftClosed, belowEnd, belowEndValue, belowEndClosed);
            var above = Part(aboveStart, aboveStartValue, aboveStartClosed, A.Right, aRight, A.RightClosed);
            return (below, above) switch
            {
                (null, null) => Empty,
                ({ } low, null) => low,
                (null, { } high) => high,
                (FiniteSet { Count: 1 } low, FiniteSet { Count: 1 } high) => new FiniteSet(low.First(), high.First()),
                ({ } low, { } high) => low.Unite(high),
            };

            static Set? Part(Entity left, Real leftValue, bool leftClosed, Entity right, Real rightValue, bool rightClosed)
                => leftValue < rightValue ? new Interval(left, leftClosed, right, rightClosed)
                 : leftValue == rightValue && leftClosed && rightClosed ? new FiniteSet(left)
                 : null;
        }

        internal static Set SetSubtractCSetAndCSet(ConditionalSet intLeft, ConditionalSet intRight)
        {
            (intLeft, intRight) = MergeToOneVariable(intLeft, intRight);
            return new ConditionalSet(intLeft.Var, (intLeft.Predicate & !intRight.Predicate).InnerSimplified);
        }
    }
}
