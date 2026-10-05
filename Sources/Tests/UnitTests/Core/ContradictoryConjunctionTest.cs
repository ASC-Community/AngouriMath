//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A conjunction does not repeat a conjunct it already has, and one that contradicts another
    /// makes the whole conjunction <c>False</c>. What this is for: a piecewise combined with a
    /// piecewise joins every case's predicate to every other's, and two piecewises that split on
    /// the same three signs of one quantity must combine to three cases and not nine — left as
    /// nine, a sum of such piecewises had 3^12 cases and took 8 GB.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1414">#1414</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class ContradictoryConjunctionTest
    {
        [Theory]
        [InlineData("x > 0 and x > 0", "x > 0")]
        [InlineData("(x > 0 and y > 0) and x > 0", "x > 0 and y > 0")]
        [InlineData("x > 0 and (y > 0 and x > 0)", "x > 0 and y > 0")]
        [InlineData("(a = 0 and not b = 0) and (a = 0 and not b = 0)", "a = 0 and not b = 0")]
        public void ARepeatedConjunctIsWrittenOnce(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().InnerSimplified);

        [Theory]
        [InlineData("q = 0 and q > 0")]
        [InlineData("q > 0 and q = 0")]
        [InlineData("q = 0 and q < 0")]
        [InlineData("q = 0 and not q = 0")]
        [InlineData("not q = 0 and q = 0")]
        [InlineData("(4 * c / a^2 = 0 and not a = 0) and (4 * c / a^2 > 0 and not a = 0)")]
        [InlineData("x = 0 and (y > 0 and (z > 0 and x < 0))")]
        public void AnEqualityAgainstAStrictSignOfTheSameQuantityIsFalse(string input)
            => Assert.Equal(Boolean.False, input.ToEntity().InnerSimplified);

        // False everywhere, not only over the reals: at q = i the equality is False, and
        // `False and anything` is False.
        [Theory]
        [InlineData("q = 0 and q > 0")]
        [InlineData("q = 0 and q < 0")]
        public void ThatIsSoundOffTheRealLine(string input)
            => Assert.Equal(Boolean.False, input.ToEntity().Substitute("q", "i").Evaled);

        // Two order comparisons are NaN off the real line, so deciding them is Simplify's, under
        // `q in RR` (https://github.com/asc-community/AngouriMath/issues/876); evaluation leaves
        // them, and compatible signs or signs of different quantities are left as written too.
        [Theory]
        [InlineData("q > 0 and q < 0")]
        [InlineData("q > 0 and q <= 0")]
        [InlineData("q < 0 and q >= 0")]
        [InlineData("not (q > 0) and q > 0")]
        [InlineData("q > 0 and q >= 0")]
        [InlineData("q >= 0 and q <= 0")]
        [InlineData("q = 0 and q >= 0")]
        [InlineData("q > 0 and p < 0")]
        [InlineData("q > 0 and q > 1")]
        [InlineData("not q = 0 and q > 0")]
        public void NothingElseIsDecided(string input)
            => Assert.Equal(input.ToEntity(), input.ToEntity().InnerSimplified);

        private static int CaseCount(Entity piecewise)
            => Assert.IsType<Piecewise>(piecewise).Cases.Count();

        [Fact]
        public void TwoPiecewisesOnTheSameSplitCombineCaseByCase()
        {
            // The three matching pairs. A pair that pairs an equality against a sign is False and
            // gone. The two pairs of opposite strict signs, `q > 0 and q < 0` and `q < 0 and q > 0`,
            // are NaN rather than False off the real line, where they would stop the cases after
            // them -- but every case after them tests q too, so none of those is true there either,
            // and they go as well: three cases, not nine.
            // https://github.com/asc-community/AngouriMath/issues/718
            var sum = "piecewise(1 provided q = 0, 2 provided q > 0, 3 provided q < 0) + piecewise(10 provided q = 0, 20 provided q > 0, 30 provided q < 0)".ToEntity().InnerSimplified;
            Assert.Equal(3, CaseCount(sum));
            Assert.Equal(Number.Integer.Create(11), sum.Substitute("q", 0).Evaled);
            Assert.Equal(Number.Integer.Create(22), sum.Substitute("q", 1).Evaled);
            Assert.Equal(Number.Integer.Create(33), sum.Substitute("q", -1).Evaled);
            // Off the real line it has no value, as with every pair: no case is true there.
            Assert.IsNotType<Number.Integer>(sum.Substitute("q", "i").Evaled);

            // And a chain of them stays at three cases rather than growing by a factor of three each
            // time.
            var chain = sum;
            for (var i = 0; i < 6; i++)
                chain = (chain + "piecewise(1 provided q = 0, 2 provided q > 0, 3 provided q < 0)".ToEntity()).InnerSimplified;
            Assert.Equal(3, CaseCount(chain));
            Assert.Equal(Number.Integer.Create(22 + 12), chain.Substitute("q", 1).Evaled);
            Assert.Equal(Number.Integer.Create(33 + 18), chain.Substitute("q", -1).Evaled);
            Assert.Equal(Number.Integer.Create(11 + 6), chain.Substitute("q", 0).Evaled);
        }

        /// <summary>
        /// The arm an antiderivative owes a quadratic off the real line, <c>not q in RR or q &gt; 0</c>,
        /// against the arms on the sign of <c>q</c>: an equality asks <c>q</c> to be real, and a
        /// strict sign is NaN off the real line, so neither pairing is ever true, and a sum of
        /// such piecewises stays at three cases. The antiderivative of
        /// <c>2 d^3 t^2/((t^2 - c - i d)^3 (t^2 - c + i d))</c> kept 45.
        /// https://github.com/asc-community/AngouriMath/issues/1788
        /// </summary>
        [Fact]
        public void TheArmOffTheRealLineExcludesTheSignArms()
        {
            const string arms = "piecewise(1 provided not q in RR or q > 0, 2 provided q = 0, 3 provided q < 0)";
            var sum = (arms.ToEntity() + arms.ToEntity()).InnerSimplified;
            Assert.Equal(3, CaseCount(sum));
            Assert.Equal(Number.Integer.Create(2), sum.Substitute("q", 1).Evaled);
            Assert.Equal(Number.Integer.Create(4), sum.Substitute("q", 0).Evaled);
            Assert.Equal(Number.Integer.Create(6), sum.Substitute("q", -1).Evaled);
            Assert.Equal(Number.Integer.Create(2), sum.Substitute("q", "i").Evaled);

            var chain = sum;
            for (var i = 0; i < 4; i++)
                chain = (chain + arms.ToEntity()).InnerSimplified;
            Assert.Equal(3, CaseCount(chain));
            Assert.Equal(Number.Integer.Create(6), chain.Substitute("q", "1 + i").Evaled);
            Assert.Equal(Number.Integer.Create(18), chain.Substitute("q", -2).Evaled);
        }

        /// <summary>
        /// A quantity is read up to a constant factor: <c>f = 0</c> and <c>not 2 f = 0</c>
        /// contradict each other, which is how the integrator's case for a vanishing leading
        /// coefficient meets the cases that divide by it.
        /// </summary>
        [Fact]
        public void AConstantFactorIsTheSameQuantity()
        {
            var sum = "piecewise(1 provided f = 0, 2 provided not 2 * f = 0) + piecewise(10 provided 3 * f = 0, 20 provided not f = 0)".ToEntity().InnerSimplified;
            Assert.Equal(2, CaseCount(sum));
            Assert.Equal(Number.Integer.Create(11), sum.Substitute("f", 0).Evaled);
            Assert.Equal(Number.Integer.Create(22), sum.Substitute("f", 5).Evaled);
        }

        /// <summary>
        /// ...and two opposite strict signs stay where a later case does not test the quantity:
        /// off the real line that case can be true, and the undecided pair is what keeps it from
        /// being reached.
        /// </summary>
        [Fact]
        public void OppositeSignsStayWhereALaterCaseDoesNotTestTheQuantity()
        {
            var sum = "piecewise(1 provided q > 0, 2 provided q < 0, 3 provided p > 0) + piecewise(10 provided q < 0, 20 provided q > 0, 30 provided p > 0)".ToEntity().InnerSimplified;
            Assert.Contains(Assert.IsType<Piecewise>(sum).Cases, @case => @case.Predicate.ToString().Contains("q > 0") && @case.Predicate.ToString().Contains("q < 0"));
        }

        [Fact]
        public void TwoPiecewisesOnDifferentSplitsStillCombineEveryPair()
        {
            var sum = "piecewise(1 provided p > 0, 2 provided p <= 0) * piecewise(10 provided q > 0, 20 provided q <= 0)".ToEntity().InnerSimplified;
            Assert.Equal(4, CaseCount(sum));
            Assert.Equal(Number.Integer.Create(40), sum.Substitute("p", -1).Substitute("q", -1).Evaled);
            Assert.Equal(Number.Integer.Create(10), sum.Substitute("p", 1).Substitute("q", 1).Evaled);
        }
    }
}
