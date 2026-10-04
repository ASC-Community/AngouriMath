//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A definite integral whose integrand can jump is split at the jumps rather than taken
    /// through an antiderivative that holds the case or the step constant across them. Question
    /// I.2 of https://github.com/asc-community/AngouriMath/issues/1212, and the review of #1215.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class BreakpointIntegrationTest
    {
        // ---- a piecewise: finitely many breakpoints ---------------------------------------------

        // The tent map over [0, 1] used to answer 1: the antiderivative of the first case,
        // x^2, evaluated at both ends.
        [Theory]
        [InlineData("integral(piecewise(2x provided x <= 1/2, 2 - 2x), x, 0, 1)", "1/2")]
        [InlineData("integral(piecewise(x provided x < 1, x^2), x, 0, 2)", "17/6")]
        [InlineData("integral(piecewise(1 provided x < 0, 2 provided x < 1, 3), x, -1, 2)", "6")]
        [InlineData("integral(piecewise(x provided x <= 1/2, 1 - x), x, 1/4, 3/4)", "3/16")]
        [InlineData("integral(piecewise(x provided x < 1, x^2), x, 3, 4)", "37/3")]
        [InlineData("integral(piecewise(1 provided x > 0 and x < 1, 0), x, -2, 2)", "1")]
        [InlineData("integral(piecewise(sin(x) provided x < pi, 0), x, 0, 2pi)", "2")]
        public void APiecewiseIsIntegratedCaseByCase(string integral, string expected)
            => Assert.Equal(expected.ToEntity(), integral.ToEntity().Simplify());

        // A condition against a symbol, or a symbolic bound, is declined rather than answered
        // with a piecewise that still mentions the integration variable.
        [Theory]
        [InlineData("integral(piecewise(x provided x < a, 0), x, 0, 2)")]
        [InlineData("integral(piecewise(2x provided x <= 1/2, 2 - 2x), x, 0, t)")]
        public void ASymbolicBreakpointOrBoundIsLeftAsWritten(string integral)
            => Assert.IsType<Integralf>(integral.ToEntity().Simplify());

        // A condition on the variable is a one-case piecewise whose other case is undefined:
        // integrated where it holds, and declined where the range reaches outside it.
        [Fact]
        public void AConditionOnTheVariableIsIntegratedWhereItHolds()
        {
            Assert.Equal("1/2".ToEntity(), "integral(x provided x > 0, x, 0, 1)".ToEntity().Simplify());
            Assert.IsType<Integralf>("integral(x provided x > 0, x, -1, 1)".ToEntity().Simplify());
        }

        // A piecewise whose conditions do not mention the variable is a constant here.
        [Fact]
        public void APiecewiseInAnotherSymbolIsAConstant()
        {
            var value = "integral(piecewise(1 provided a > 0, 2), x, 0, 3)".ToEntity().Simplify();
            Assert.IsNotType<Integralf>(value);
            Assert.Equal("3".ToEntity(), value.Substitute("a", 1).Simplify());
            Assert.Equal("6".ToEntity(), value.Substitute("a", -1).Simplify());
        }

        // ---- a floor or a ceiling: infinitely many, evenly spaced ------------------------------

        [Fact]
        public void TheOrientationWeekIntegralIsHalfOfEMinusOne()
        {
            var value = "integral((x - floor(x)) / floor(x)!, x, 1, +oo)".ToEntity().Simplify();
            Assert.IsNotType<Integralf>(value);
            Assert.Equal("(e - 1) / 2".ToEntity().Simplify(), value);
        }

        [Theory]
        [InlineData("integral(floor(x), x, 0, 5)", "10")]
        [InlineData("integral(x - floor(x), x, 0, 3)", "3/2")]
        [InlineData("integral((x - floor(x))^2, x, 0, 4)", "4/3")]
        [InlineData("integral(floor(x) * x, x, 0, 3)", "13/2")]
        [InlineData("integral(floor(x) / (floor(x) + 1)!, x, 0, +oo)", "1")]
        public void AnIntegralOverWholeBoundsIsSummedOverUnitIntervals(string integral, string expected)
            => Assert.Equal(expected.ToEntity(), integral.ToEntity().Simplify());

        [Theory]
        [InlineData("integral(floor(x), x, 1/2, 2)", "1")]
        [InlineData("integral(x - floor(x), x, 0, 5/2)", "9/8")]
        [InlineData("integral(floor(x), x, 1/4, 3/4)", "0")]
        [InlineData("integral(x - floor(x), x, 3/2, 7/4)", "5/32")]
        [InlineData("integral(ceil(x), x, 0, 3)", "6")]
        [InlineData("integral(ceil(x) - x, x, 1/2, 2)", "5/8")]
        public void ABoundThatIsNotWholeContributesItsOwnPiece(string integral, string expected)
            => Assert.Equal(expected.ToEntity(), integral.ToEntity().Simplify());

        // A symbolic bound is declined rather than integrated as if the step were constant,
        // which used to answer (n - floor(n))^3 / 3 for this one; with the bound a number the
        // same integral splits.
        [Fact]
        public void ASymbolicBoundIsLeftAsWritten()
        {
            var value = "integral((x - floor(x))^2, x, 0, n)".ToEntity().Simplify();
            Assert.IsType<Integralf>(value);
            Assert.Equal("2".ToEntity(), value.Substitute("n", 6).Simplify());
        }

        // The split leaves a summation, and whatever answers that answers the integral. The first
        // is a geometric series; the second is sum(n^n, n, 1, +oo), whose terms do not tend to
        // zero. This line used to assert the integral stayed as written, with a comment saying it
        // would move to the value the day a divergence test existed. See DivergentSeriesTest.
        [Fact]
        public void TheSumTheSplitLeavesIsWhatAnswersTheIntegral()
        {
            Assert.Equal("2".ToEntity(), "integral(2^(-floor(x)), x, 0, +oo)".ToEntity().Simplify());
            Assert.Equal(Number.Real.PositiveInfinity,
                "integral(floor(x)^floor(x), x, 1, +oo)".ToEntity().Simplify());
        }

        // Not split and not integrated either: a floor of something other than the variable
        // is not a break this reads, and an integrand the unit interval cannot integrate stays.
        [Theory]
        [InlineData("integral(floor(x^2), x, 0, 2)")]
        [InlineData("integral(floor(x) * e^(x^3) , x, 0, 2)")]
        public void WhatCannotBeSplitIsLeftAsWritten(string integral)
            => Assert.IsType<Integralf>(integral.ToEntity().Simplify());

        // ---- what does not break is unaffected --------------------------------------------------

        [Theory]
        [InlineData("integral((2x^2+x+1)/(x^3+x^2+x+1), x, 3/4, 4/3)", "ln(16/9)")]
        [InlineData("integral(sgn(x), x, -1, 2)", "1")]
        [InlineData("integral(abs(x - 1), x, 0, 3)", "5/2")]
        public void AnIntegrandWithoutABreakIsUnaffected(string integral, string expected)
            => Assert.Equal(expected.ToEntity(), integral.ToEntity().Simplify());
    }
}
