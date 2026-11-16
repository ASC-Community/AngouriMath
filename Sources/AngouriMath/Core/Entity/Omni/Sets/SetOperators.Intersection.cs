//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Core.Sets
{
    internal static partial class SetOperators
    {
        /// <summary>
        /// The members of a family of roots <paramref name="family"/> that are in a set of reals
        /// <paramref name="real"/> -- the reals or an interval with numbers for its ends -- as a
        /// finite set; <see langword="null"/> where the family is not of the shape read here or a
        /// membership is not decided, and the intersection stays as written.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each member is read as <c>c + k n</c> in the index <c>n</c>, with numbers <c>c</c> and
        /// <c>k</c>, <c>k</c> not 0. Its imaginary part is <c>Im(c) + n Im(k)</c>. Where <c>k</c> is not
        /// real that is 0 at one <c>n</c> only, <c>n0 = -Im(c) / Im(k)</c>, so the family has one real
        /// member where <c>n0</c> is a whole number in the index set and none otherwise: every
        /// exponential's family over the complex numbers, <c>ln(2) + 2 pi i n</c>, is real at
        /// <c>n = 0</c> alone, which makes <c>e ^ x = 2 and x in RR</c> the root <c>ln(2)</c>. Where
        /// <c>k</c> is real every member is real if <c>Im(c) = 0</c> and none is otherwise, and the
        /// members inside a bounded interval are the <c>n</c> between its ends mapped back, at most
        /// <see cref="MostRealMembers"/> of them: <c>sin(x) = 1/2</c> on <c>[0; 2 pi]</c> is
        /// <c>{ pi/6, 5 pi/6 }</c>. Over an unbounded set there are infinitely many, and the
        /// intersection stays.
        /// </para>
        /// <para>
        /// <c>n0</c> is a ratio of the imaginary parts as evaluated, taken as whole within
        /// <c>1e-30</c>; the member at it is then put in <paramref name="real"/> as an exact
        /// expression, which is what decides it.
        /// https://github.com/asc-community/AngouriMath/issues/2319
        /// </para>
        /// <para>
        /// A member with a condition, as an equation with a denominator has, is read by its
        /// expression, and the index is put into the whole member: <c>2 pi n provided not 2 pi n = 0</c>
        /// has no member at <c>n = 0</c>, so <c>sin(x)/x = 0</c> on <c>[-10; 10]</c> is
        /// <c>{ -3 pi, -2 pi, -pi, pi, 2 pi, 3 pi }</c>. A condition not decided at an index leaves
        /// the intersection as written. https://github.com/asc-community/AngouriMath/issues/2502
        /// </para>
        /// </remarks>
        internal static Set? RealMembers(IndexedUnionf family, Set real)
        {
            if (family.Var is not Variable n || family.Over is not Set over || family.Body is not FiniteSet body)
                return null;
            Real? from = null, to = null;
            switch (real)
            {
                case SpecialSet.Reals:
                    break;
                case Interval { Left: var left, Right: var right }
                    when left.Evaled is Real lower && right.Evaled is Real upper:
                    (from, to) = (lower, upper);
                    break;
                default:
                    return null;
            }
            var found = new FiniteSetBuilder();
            foreach (var member in body)
            {
                // A member with a condition is read by its expression; the condition is decided
                // below, when the index is put into the whole member.
                var expression = member;
                while (expression is Providedf(var inner, _))
                    expression = inner;
                if (Functions.Algebra.AnalyticalSolving.SolutionParameters.NumericLine(expression, n) is not var (k, c))
                    return null;
                var indices = new List<EInteger>();
                if (!k.ImaginaryPart.IsZero)
                {
                    var at = (-c.ImaginaryPart.EDecimal).Divide(k.ImaginaryPart.EDecimal, MathS.Settings.DecimalPrecisionContext.Value);
                    var whole = at.RoundToIntegerNoRoundedFlag(EContext.Unlimited.WithRounding(ERounding.HalfEven));
                    if (at.Subtract(whole).Abs().CompareTo(EDecimal.Create(1, -30)) < 0)
                        indices.Add(whole.ToEInteger());
                }
                else if (c.ImaginaryPart.IsZero)
                {
                    if (from is not { EDecimal.IsFinite: true } || to is not { EDecimal.IsFinite: true })
                        return null;
                    var context = MathS.Settings.DecimalPrecisionContext.Value;
                    var atFrom = from.EDecimal.Subtract(c.RealPart.EDecimal).Divide(k.RealPart.EDecimal, context);
                    var atTo = to.EDecimal.Subtract(c.RealPart.EDecimal).Divide(k.RealPart.EDecimal, context);
                    var low = EDecimal.Min(atFrom, atTo).RoundToIntegerNoRoundedFlag(EContext.Unlimited.WithRounding(ERounding.Floor)).ToEInteger();
                    var high = EDecimal.Max(atFrom, atTo).RoundToIntegerNoRoundedFlag(EContext.Unlimited.WithRounding(ERounding.Ceiling)).ToEInteger();
                    if (high.Subtract(low).CompareTo(EInteger.FromInt32(MostRealMembers)) > 0)
                        return null;
                    for (var index = low; index.CompareTo(high) <= 0; index += EInteger.One)
                        indices.Add(index);
                }
                foreach (var index in indices)
                {
                    if (!over.TryContains(Integer.Create(index), out var inOver))
                        return null;
                    if (!inOver)
                        continue;
                    var value = member.Substitute(n, Integer.Create(index)).InnerSimplified;
                    // The member's condition fails at this index, so there is no member here.
                    if (value.IsNaN)
                        continue;
                    // The condition is not decided at this index.
                    if (value is Providedf)
                        return null;
                    if (!real.TryContains(value, out var inReal))
                        return null;
                    if (inReal)
                        found.Add(value);
                }
            }
            return found.ToFiniteSet();
        }

        /// <summary>The most members of a family with a real step read inside a bounded interval.</summary>
        private const int MostRealMembers = 64;

        /// <summary>Whether <paramref name="set"/> is a union with a family of roots among its pieces.</summary>
        internal static bool HasAFamily(Set set)
            => set is IndexedUnionf || set is Unionf(Set left, Set right) && (HasAFamily(left) || HasAFamily(right));

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
