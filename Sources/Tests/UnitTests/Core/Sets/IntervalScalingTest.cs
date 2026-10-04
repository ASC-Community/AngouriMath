//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Sets
{
    /// <summary>
    /// Scaling an interval by a constant, which is
    /// <a href="https://github.com/asc-community/AngouriMath/issues/322">#322</a>'s remaining half.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Sumf</c> and <c>Minusf</c> had interval cases and <c>Mulf</c> and <c>Divf</c> had none, so
    /// <c>(0; 1) + 1</c> answered <c>(1; 2)</c> while <c>(0; 1) * 2</c> was left alone. The issue's
    /// body says it "will be possible once we implement quantifiers"; it needs none — scaling is
    /// monotone in the factor's sign and in nothing else.
    /// </para>
    /// <para>
    /// <b>A negative factor reflects the interval</b>, so the ends swap and their openness swaps
    /// with them, exactly as subtracting one does. That is the half a test on the printed form
    /// alone would not catch: swapping the ends and leaving the flags where they were gives the
    /// right width and the wrong bracket at both ends, so membership is asserted too.
    /// </para>
    /// </remarks>
    [Trait("Area", "Sets")]
    public sealed class IntervalScalingTest
    {
        [Theory]
        [InlineData("(0; 1) * 2", "(0; 2)")]
        [InlineData("2 * (0; 1)", "(0; 2)")]
        [InlineData("(0; 1] * 2", "(0; 2]")]
        [InlineData("[0; 1) * 2", "[0; 2)")]
        [InlineData("(0; 1) * 1/2", "(0; 1/2)")]
        [InlineData("(0; 1) * pi", "(0; pi)")]
        public void APositiveFactorCarriesBothEndsWhereTheyWere(string expression, string expected)
            => Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());

        /// <summary>
        /// The ends swap, and so does their openness: <c>(0; 1] * -2</c> is <c>[-2; 0)</c> — the
        /// included 1 becomes the included lower end, and the excluded 0 the excluded upper one.
        /// </summary>
        [Theory]
        [InlineData("(0; 1) * (-2)", "(-2; 0)")]
        [InlineData("(-2) * (0; 1)", "(-2; 0)")]
        [InlineData("(0; 1] * (-2)", "[-2; 0)")]
        [InlineData("[0; 1) * (-2)", "(-2; 0]")]
        public void ANegativeFactorReflectsIt(string expression, string expected)
            => Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());

        [Theory]
        [InlineData("(2; 4) / 2", "(1; 2)")]
        [InlineData("(2; 4) / (-2)", "(-2; -1)")]
        [InlineData("(0; 1] / (-2)", "[-1/2; 0)")]
        public void DividingByAConstantIsScalingByItsReciprocal(string expression, string expected)
            => Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());

        /// <summary>
        /// <b>Negating an interval is multiplying it, so this comes for free — and it is the case
        /// #1117 had to leave out.</b> <c>0 - x</c> is negated by an earlier arm, so
        /// <c>0 - [0; 1)</c> became <c>-[0; 1)</c> and stopped there; with <c>Mulf</c> answering,
        /// it reaches <c>(-1; 0]</c>.
        /// </summary>
        [Theory]
        [InlineData("-(0; 1)", "(-1; 0)")]
        [InlineData("-[0; 1)", "(-1; 0]")]
        [InlineData("0 - [0; 1)", "(-1; 0]")]
        [InlineData("0 - (0; 1]", "[-1; 0)")]
        public void NegatingAnIntervalReflectsItToo(string expression, string expected)
            => Assert.Equal(expected.ToEntity(), expression.ToEntity().Simplify());

        /// <summary>
        /// Membership, which is what the order and the brackets are for.
        /// </summary>
        [Theory]
        [InlineData("1 in ((0; 1) * 2)", true)]
        [InlineData("2 in ((0; 1) * 2)", false)]
        [InlineData("2 in ((0; 1] * 2)", true)]
        [InlineData("-1 in ((0; 1) * (-2))", true)]
        [InlineData("-2 in ((0; 1] * (-2))", true)]
        [InlineData("-2 in ((0; 1) * (-2))", false)]
        [InlineData("0 in ((0; 1] * (-2))", false)]
        [InlineData("1.5 in ((2; 4) / 2)", true)]
        [InlineData("3 in ((2; 4) / 2)", false)]
        public void MembershipIsWhatTheReflectionIsFor(string expression, bool expected)
            => Assert.Equal(
                expected ? Entity.Boolean.True : Entity.Boolean.False,
                expression.ToEntity().Simplify());

        /// <summary>
        /// <b>An unknown sign is answered by not answering.</b> <c>k * (0; 1)</c> is one interval
        /// when <c>k</c> is positive and the reflected one when it is negative; picking either
        /// would be choosing which, so it is left alone — which is what an unevaluated node means.
        /// </summary>
        [Theory]
        [InlineData("k * (0; 1)")]
        [InlineData("(0; 1) / k")]
        public void AnUnknownSignIsLeftAlone(string expression)
            => Assert.Equal(expression.ToEntity(), expression.ToEntity().Simplify());

        /// <summary>
        /// Two boundaries of this change, asserted so that they are written down rather than
        /// discovered.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>(0; 1) * 0</c> answers the number <c>0</c> rather than the set <c>{ 0 }</c>. That is
        /// older than this and untouched: the arm it comes from is over every <c>Entity</c>, not
        /// only intervals, and moving it would change matrices and sets alike.
        /// </para>
        /// <para>
        /// <c>2 / [-1; 1]</c> is left alone. A constant over an interval that straddles zero is
        /// two unbounded pieces rather than one interval, so there is no <c>Interval</c> to
        /// answer with. Where the interval does not straddle zero its reciprocal is an interval
        /// and the quotient is that interval scaled: <c>2 / (0; 1)</c> is <c>(2; +oo)</c>.
        /// https://github.com/asc-community/AngouriMath/issues/322
        /// </para>
        /// </remarks>
        [Fact]
        public void TheTwoBoundariesOfThis()
        {
            Assert.Equal("0".ToEntity(), "(0; 1) * 0".ToEntity().Simplify());
            Assert.Equal("2 / [-1; 1]".ToEntity(), "2 / [-1; 1]".ToEntity().Simplify());
            Assert.Equal("(2; +oo)".ToEntity(), "2 / (0; 1)".ToEntity().Simplify());
        }
    }
}
