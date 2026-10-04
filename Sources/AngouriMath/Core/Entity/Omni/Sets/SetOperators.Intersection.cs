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
        /// The members of <paramref name="finite"/> that are in <paramref name="set"/>, and the
        /// intersection left as written for those whose membership is not decided.
        /// </summary>
        /// <remarks>
        /// An undecided member is in the intersection only where it is in the set, so it stays
        /// intersected with it: <c>{ y } /\ { 2 }</c> is <c>{ 2 }</c> where <c>y = 2</c> and empty
        /// elsewhere, and was answered <c>{ y }</c> -- which made <c>x = y and x = 2</c> solve to
        /// <c>{ y }</c>. https://github.com/asc-community/AngouriMath/issues/1680
        /// </remarks>
        internal static Set IntersectFiniteSetAndSet(FiniteSet finite, Set set)
        {
            var fsb = new FiniteSetBuilder();
            var amb = new FiniteSetBuilder();
            foreach (var elem in finite)
            {
                if (!set.TryContains(elem, out var contains))
                    amb.Add(elem);
                else if (contains)
                    fsb.Add(elem);
            }
            if (amb.IsEmpty)
                return fsb.ToFiniteSet();
            var undecided = new Intersectionf(amb.ToFiniteSet(), set);
            return fsb.IsEmpty ? undecided : fsb.ToFiniteSet().Unite(undecided);
        }

        internal static Set IntersectIntervalAndInterval(Interval A, Interval B)
        {
            if (A.Left == B.Left && A.Right == B.Right)
                return new Interval(A.Left, A.LeftClosed && B.LeftClosed, A.Right, A.RightClosed && B.RightClosed);
            // Compared by what the endpoints are worth, not by whether they are written as
            // bare numbers. (sqrt(33) - 3) / 6 is a Divf and never a Real, so an interval
            // written with one was given up on -- which is
            // https://github.com/asc-community/AngouriMath/issues/415. The bounds of the result
            // are taken from the original expressions, so the answer keeps them exact
            // rather than turning them into a hundred decimal places.
            if (A.Left.Evaled is not Real aLeft ||
                A.Right.Evaled is not Real aRight ||
                B.Left.Evaled is not Real bLeft ||
                B.Right.Evaled is not Real bRight)
                return Rays(A, B) ?? Rays(B, A) ?? A.Intersect(B);
            if (aLeft == bRight)
                return A.LeftClosed && B.RightClosed ? new FiniteSet(A.Left) : Empty;
            if (bLeft == aRight)
                return A.RightClosed && B.LeftClosed ? new FiniteSet(B.Left) : Empty;
            // An interval whose ends are reversed holds nothing, and one closed at a single
            // point holds that point, so that is what it meets the other in. Returning the other
            // interval is what the union does, and is right there: (3; 1) /\ [0; 5] was [0; 5].
            if (aLeft >= aRight)
                return aLeft == aRight && A.LeftClosed && A.RightClosed ? IntersectFiniteSetAndSet(new FiniteSet(A.Left), B) : Empty;
            if (bLeft >= bRight)
                return bLeft == bRight && B.LeftClosed && B.RightClosed ? IntersectFiniteSetAndSet(new FiniteSet(B.Left), A) : Empty;
            if (aLeft > bRight)
                return Empty;
            if (bLeft > aRight)
                return Empty;
            var (left, leftClosed) =
               aLeft == bLeft ?
               (A.Left, A.LeftClosed && B.LeftClosed) :
               (bLeft < aLeft ? (A.Left, A.LeftClosed) : (B.Left, B.LeftClosed));
            var (right, rightClosed) =
                aRight == bRight ?
                (A.Right, A.RightClosed && B.RightClosed) :
                (bRight > aRight ? (A.Right, A.RightClosed) : (B.Right, B.RightClosed));
            return new Interval(left, leftClosed, right, rightClosed);
        }

        /// <summary>
        /// A ray up from <c>a</c> and a ray down to <c>b</c> meet in the interval from <c>a</c> to
        /// <c>b</c>, whatever the two are, which holds nothing where <c>a</c> is not below <c>b</c>:
        /// <c>(x; +oo) /\ (-oo; x)</c> is nothing, and is what <c>x &lt; y and y &lt; x</c> is
        /// solved to. <see langword="null"/> where the two are not such rays.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// </summary>
        private static Set? Rays(Interval up, Interval down)
        {
            if (up.Right != Real.PositiveInfinity || down.Left != Real.NegativeInfinity)
                return null;
            if (up.Left == down.Right)
                return up.LeftClosed && down.RightClosed ? new FiniteSet(up.Left) : Empty;
            return new Interval(up.Left, up.LeftClosed, down.Right, down.RightClosed);
        }

        internal static Set IntersectCSetAndCSet(ConditionalSet intLeft, ConditionalSet intRight)
        {
            (intLeft, intRight) = MergeToOneVariable(intLeft, intRight);
            // Two residue classes meet in one class or in nothing, by the Chinese remainder
            // theorem. https://github.com/asc-community/AngouriMath/issues/1409
            if (Functions.ResidueClasses.Read(intLeft) is var (x, a, m) && Functions.ResidueClasses.Read(intRight) is var (_, b, n))
                return Functions.ResidueClasses.Meet(x, a, m, b, n);
            return new ConditionalSet(intLeft.Var, (intLeft.Predicate & intRight.Predicate).InnerSimplified);
        }

        /// <summary>A residue class cut down to an interval is a finite set, where it is short enough to list.</summary>
        internal static Set? IntersectCSetAndInterval(ConditionalSet cset, Interval interval)
            => Functions.ResidueClasses.Read(cset) is var (x, residue, modulus)
                ? Functions.ResidueClasses.Within(x, residue, modulus, interval)
                : null;
    }
}
