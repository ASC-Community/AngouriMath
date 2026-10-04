//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Sets
{
    /// <summary>
    /// A function monotone on an interval maps it to the interval between the images of its
    /// ends, the openness travelling with each end and an infinite image never attained; a
    /// product of two intervals is the interval between the least and the greatest product of
    /// ends. <a href="https://github.com/asc-community/AngouriMath/issues/322">#322</a>
    /// </summary>
    [Trait("Area", "Sets")]
    public sealed class IntervalImageTest
    {
        [Theory]
        [InlineData("ln((0; 1))", "(-oo; 0)")]
        [InlineData("ln([0; 1))", "(-oo; 0)")]
        [InlineData("ln([1; e])", "[0; 1]")]
        [InlineData("log(1/2, (0; 1])", "[0; +oo)")]
        [InlineData("e^[0; 1]", "[1; e]")]
        [InlineData("2^(-oo; 0)", "(0; 1)")]
        [InlineData("(1/2)^[0; 2]", "[1/4; 1]")]
        [InlineData("[1; 2]^2", "[1; 4]")]
        [InlineData("(-2; 1]^2", "[0; 4)")]
        [InlineData("[-1; 1]^2", "[0; 1]")]
        [InlineData("(-3; -1)^2", "(1; 9)")]
        [InlineData("[-2; 3)^3", "[-8; 27)")]
        [InlineData("[1; 2]^(-1)", "[1/2; 1]")]
        [InlineData("(0; 1]^(-1)", "[1; +oo)")]
        [InlineData("1 / (0; 1]", "[1; +oo)")]
        [InlineData("2 / [1; 2]", "[1; 2]")]
        [InlineData("-3 / (1; 2]", "(-3; -3/2]")]
        [InlineData("[0; 4]^(1/2)", "[0; 2]")]
        [InlineData("sqrt((1; 9))", "(1; 3)")]
        [InlineData("abs((-1; 2])", "[0; 2]")]
        [InlineData("abs([-3; 1))", "[0; 3]")]
        [InlineData("abs((-2; 2))", "[0; 2)")]
        [InlineData("abs([1; 5))", "[1; 5)")]
        [InlineData("abs((-5; -1])", "[1; 5)")]
        [InlineData("arctan([0; 1])", "[0; pi/4]")]
        [InlineData("arctan((-oo; +oo))", "(-1/2 * pi; 1/2 * pi)")]
        [InlineData("arcsin([-1; 1])", "[-1/2 * pi; 1/2 * pi]")]
        [InlineData("arccos([0; 1])", "[0; pi/2]")]
        [InlineData("(1; 2) * (3; 4)", "(3; 8)")]
        [InlineData("[1; 2] * [-1; 1]", "[-2; 2]")]
        [InlineData("[-2; 3] * [-1; 4]", "[-8; 12]")]
        [InlineData("(1; 2) / (3; 4)", "(1/4; 2/3)")]
        [InlineData("[1; 2] * [3; 4] + 1", "[4; 9]")]
        public void TheImageOfAnInterval(string expression, string image)
            => Assert.Equal(image.ToEntity(), expression.ToEntity().Simplify());

        [Theory]
        // Not monotone on the interval, not real on it, zero inside a divisor, a symbolic end
        // or factor: nothing is guessed.
        [InlineData("ln((-1; 1))")]
        [InlineData("(-1; 4)^(1/2)")]
        [InlineData("[-8; 8]^(1/3)")]
        [InlineData("1 / [-1; 1]")]
        [InlineData("[1; 2] / [-1; 1]")]
        [InlineData("arcsin([0; 2])")]
        [InlineData("sin([0; 1])")]
        [InlineData("k * (0; 1)")]
        [InlineData("[a; b]^2")]
        public void LeftAsWritten(string expression)
            => Assert.Equal(expression.ToEntity(), expression.ToEntity().Simplify());

        /// <summary>
        /// A reciprocal power is the quotient it means, whatever the interval: the shape a
        /// negative exponent always took, now with an answer where zero is outside.
        /// </summary>
        [Fact]
        public void AReciprocalPowerOfAnIntervalAroundZeroIsTheQuotient()
            => Assert.Equal("1 / [-1; 1]".ToEntity(), "[-1; 1]^(-1)".ToEntity().Simplify());

        [Fact]
        public void AMemberOfTheImageIsAMember()
            => Assert.Equal(Entity.Boolean.True, "0.5 in (1; 2) / (3; 4)".ToEntity().Simplify());
    }
}
