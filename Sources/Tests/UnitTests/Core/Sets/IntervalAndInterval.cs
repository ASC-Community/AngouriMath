//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;
using static AngouriMath.MathS.Sets;

namespace AngouriMath.Tests.Core.Sets
{
    [Trait("Area", "Core")]
    public sealed class IntervalAndInterval
    {
        private readonly Set A = Interval(2, true, 5, true);
        private readonly Set B = Interval(2, true, 3, true);
        private readonly Set C = Interval(5, true, 7, true);
        private readonly Set C1 = Interval(5, false, 7, true);
        private readonly Set D = Interval(2, true, 3, false);
        private readonly Set E = Interval(3, false, 4, false);
        private readonly Set F = Interval(3, true, 6, true);
        private readonly Set F1 = Interval(3, false, 6, false);

        private void TestInt(Entity act, Interval expected)
        {
            var inter = Assert.IsType<Interval>(act.InnerSimplified);
            Assert.Equal(expected, inter);
        }

        private void TestArb(Entity actual, Entity expected)
        {
            Assert.Equal(expected, actual.InnerSimplified);
        }

        private void CannotSimplify(Entity act)
            => Assert.Equal(act, act.InnerSimplified);

        [Fact] public void Union1() => TestInt(A.Unite(B), Interval(2, true, 5, true));
        [Fact] public void Union2() => TestInt(B.Unite(A), Interval(2, true, 5, true));
        [Fact] public void Union3() => TestInt(A.Unite(C1), Interval(2, true, 7, true));
        [Fact] public void Union4() => TestInt(C1.Unite(A), Interval(2, true, 7, true));
        [Fact] public void Union5() => CannotSimplify(E.Unite(D));
        [Fact] public void Union6() => CannotSimplify(D.Unite(E));
        [Fact] public void Union7() =>  TestInt(A.Unite(A), (Interval)A);
        [Fact] public void Union8() =>  TestInt(B.Unite(B), (Interval)B);
        [Fact] public void Union9() =>  TestInt(C.Unite(C), (Interval)C);
        [Fact] public void Union10() => TestInt(D.Unite(D), (Interval)D);

        [Fact] public void Intersection1() => TestInt(A.Intersect(B), Interval(2, true, 3, true));
        [Fact] public void Intersection2() => TestInt(B.Intersect(A), Interval(2, true, 3, true));
        [Fact] public void Intersection3() => TestArb(A.Intersect(C), new FiniteSet(5));
        [Fact] public void Intersection4() => TestArb(C.Intersect(A), new FiniteSet(5));
        [Fact] public void Intersection5() => TestInt(A.Intersect(A), (Interval)A);
        [Fact] public void Intersection6() => TestInt(B.Intersect(B), (Interval)B);
        [Fact] public void Intersection7() => TestArb(E.Intersect(D), Set.Empty);
        [Fact] public void Intersection8() => TestArb(D.Intersect(E), Set.Empty);
        [Fact] public void Intersection9() => TestArb(E.Intersect(B), Set.Empty);
        [Fact] public void Intersection10() => TestArb(B.Intersect(E), Set.Empty);
        [Fact] public void Intersection11() => TestInt(A.Intersect(F), Interval(3, 5));
        [Fact] public void Intersection12() => TestInt(F.Intersect(A), Interval(3, 5));
        [Fact] public void Intersection13() => TestInt(A.Intersect(F1), Interval(3, false, 5, true));
        [Fact] public void Intersection14() => TestInt(F1.Intersect(A), Interval(3, false, 5, true));

        /// <summary>
        /// A ray up from <c>a</c> and a ray down to <c>b</c> meet in the interval from one to the
        /// other whatever the two are, and where they are one name, in that name or in nothing:
        /// what <c>x &lt; y and y &lt; x</c> is solved to.
        /// </summary>
        [Theory]
        [InlineData("(a; +oo) /\\ (-oo; b)", "(a; b)")]
        [InlineData("(-oo; b] /\\ [a; +oo)", "[a; b]")]
        [InlineData("(x; +oo) /\\ (-oo; x)", "{}")]
        [InlineData("[x; +oo) /\\ (-oo; x]", "{x}")]
        [InlineData("[x; +oo) /\\ (-oo; x)", "{}")]
        public void TwoRaysMeetWhateverTheirEnds(string intersection, string expected)
            => TestArb(MathS.FromString(intersection), MathS.FromString(expected).InnerSimplified);

        [Fact] public void Subtraction1() => TestInt(A.SetSubtract(B), Interval(3, false, 5, true));
        [Fact] public void Subtraction2() => TestArb(B.SetSubtract(A), Set.Empty);
        [Fact] public void Subtraction3() => TestInt(E.SetSubtract(D), (Interval)E);
        [Fact] public void Subtraction4() => TestInt(D.SetSubtract(E), (Interval)D);
        [Fact] public void Subtraction5() => TestInt(A.SetSubtract(F), Interval(2, true, 3, false));
        [Fact] public void Subtraction6() => TestInt(F.SetSubtract(A), Interval(5, false, 6, true));

        /// <summary>
        /// What is left of an interval with another taken out is the part below it and the part
        /// above it, and each part holds the end the second one leaves. One interval from an end
        /// of the first to the far end of the second used to be answered, with its ends read from
        /// the wrong side. https://github.com/asc-community/AngouriMath/issues/2290
        /// </summary>
        [Theory]
        [InlineData("[0; 10] \\ (2; 3)", "[0; 2] \\/ [3; 10]")]
        [InlineData("[0; 10] \\ [2; 3]", "[0; 2) \\/ (3; 10]")]
        [InlineData("(0; 10) \\ (2; 3)", "(0; 2] \\/ [3; 10)")]
        [InlineData("[0; 10] \\ [-1; 3)", "[3; 10]")]
        [InlineData("[0; 10] \\ (-1; 3]", "(3; 10]")]
        [InlineData("[0; 10] \\ (5; 12]", "[0; 5]")]
        [InlineData("[0; 10] \\ [5; 12)", "[0; 5)")]
        [InlineData("[0; 10] \\ (0; 3)", "{ 0 } \\/ [3; 10]")]
        [InlineData("[0; 10] \\ (7; 10)", "[0; 7] \\/ { 10 }")]
        [InlineData("[0; 10] \\ (0; 10)", "{ 0, 10 }")]
        [InlineData("[0; 10] \\ [0; 10)", "{ 10 }")]
        [InlineData("[0; 10] \\ [5; 5]", "[0; 10] \\ { 5 }")]
        [InlineData("[0; 10] \\ (5; 5)", "[0; 10]")]
        [InlineData("[0; sqrt(2)] \\ [1; 2]", "[0; 1)")]
        [InlineData("(-oo; +oo) \\ [0; 1)", "(-oo; 0) \\/ [1; +oo)")]
        public void ADifferenceKeepsThePartsOnEitherSide(string difference, string expected)
            => Assert.Equal(MathS.FromString(expected).Simplify(), MathS.FromString(difference).Simplify());

        /// <summary>
        /// The difference holds a point exactly where the first interval does and the second does
        /// not, over every order of the four ends, with each end open or closed, and with either
        /// interval reversed or at a single point. Two intervals that each have more than one point
        /// are subtracted to a set without a difference in it; one that has a point or none is a
        /// listed set by then, and the difference may stay.
        /// https://github.com/asc-community/AngouriMath/issues/2290
        /// </summary>
        [Fact]
        public void ADifferenceHoldsWhatTheFirstHoldsAndTheSecondDoesNot()
        {
            var points = Enumerable.Range(-1, 9).Select(k => (Entity)Entity.Number.Rational.Create(k, 2)).ToArray();
            var failures = new List<string>();
            foreach (var aLeft in Enumerable.Range(0, 4))
            foreach (var aRight in Enumerable.Range(0, 4))
            foreach (var bLeft in Enumerable.Range(0, 4))
            foreach (var bRight in Enumerable.Range(0, 4))
            foreach (var flags in Enumerable.Range(0, 16))
            {
                var a = Interval(aLeft, (flags & 1) != 0, aRight, (flags & 2) != 0);
                var b = Interval(bLeft, (flags & 4) != 0, bRight, (flags & 8) != 0);
                var difference = a.SetSubtract(b).InnerSimplified;
                if (difference is not Set left || left is SetMinusf && aLeft < aRight && bLeft < bRight)
                {
                    failures.Add($"{a} \\ {b} is not decided: {difference}");
                    continue;
                }
                foreach (var point in points)
                {
                    var expected = Holds(a, point) && !Holds(b, point);
                    if (!left.TryContains(point, out var contains) || contains != expected)
                        failures.Add($"{point} in {a} \\ {b} = {left}: expected {expected}");
                }
            }
            Assert.Empty(failures);

            static bool Holds(Interval interval, Entity point)
            {
                var value = (Entity.Number.Real)point.EvalNumerical();
                var (left, right) = ((Entity.Number.Real)interval.Left.EvalNumerical(), (Entity.Number.Real)interval.Right.EvalNumerical());
                return (left < value || interval.LeftClosed && left == value) && (value < right || interval.RightClosed && value == right);
            }
        }

        /// <summary>
        /// Ends that have no value say whether anything is left only when they are the same and
        /// the second interval holds every end the first holds. <c>[a; b] \ (a; b)</c> is
        /// <c>{ a, b }</c> where <c>a &lt; b</c> and nothing where <c>a &gt; b</c>, so it stays as
        /// it is, and it used to be answered <c>[a; b]</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2290
        /// </summary>
        [Theory]
        [InlineData("[a; b] \\ (a; b)")]
        [InlineData("[a; b] \\ [a; b)")]
        [InlineData("(a; b] \\ (a; b)")]
        public void ADifferenceOfSymbolicIntervalsWithAnEndLeftStays(string difference)
            => Assert.IsType<SetMinusf>(MathS.FromString(difference).InnerSimplified);

        [Theory]
        [InlineData("(a; b) \\ [a; b]")]
        [InlineData("[a; b) \\ [a; b]")]
        [InlineData("(a; b] \\ (a; b]")]
        public void ADifferenceOfSymbolicIntervalsThatCoversIsEmpty(string difference)
            => Assert.Equal(Set.Empty, MathS.FromString(difference).InnerSimplified);

        /// <summary>
        /// An interval whose ends are reversed holds nothing: it meets nothing, and adds nothing to
        /// a union. The meeting used to be the other interval.
        /// </summary>
        [Theory]
        [InlineData("(3; 1) /\\ [0; 5]", "{}")]
        [InlineData("[0; 5] /\\ (3; 1)", "{}")]
        [InlineData("(3; 1) \\/ [0; 5]", "[0; 5]")]
        public void AReversedIntervalHoldsNothing(string expression, string expected)
            => TestArb(MathS.FromString(expression), MathS.FromString(expected).InnerSimplified);
    }
}
