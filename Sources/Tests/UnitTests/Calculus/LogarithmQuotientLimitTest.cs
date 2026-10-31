//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A quotient of logarithms is differentiated, as l'Hopital's rule has it, only where both logarithms diverge. Whether
    /// one does was read off the form of its argument's limit, and the limit of cos(1/x) is cos(1 / +oo), which has an
    /// infinity in it and is 1: ln(x) / ln(cos(x)) at 0+, -oo over a divisor vanishing from below, was taken for oo/oo and
    /// answered -oo, where it is +oo. Each value is checked against the expression near the destination.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2267">#2267</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class LogarithmQuotientLimitTest
    {
        [Theory]
        [InlineData("limitright(ln(x) / ln(cos(x)), x, 0)", "+oo")]
        [InlineData("limitright(ln(x) * (1 / ln(cos(x))), x, 0)", "+oo")]
        [InlineData("limitright(ln(x) * (-1 / ln(cos(x))), x, 0)", "-oo")]
        [InlineData("limitright(ln(x) / ln(cos(2x)), x, 0)", "+oo")]
        [InlineData("limitright(ln(sin(x)) / ln(cos(x)), x, 0)", "+oo")]
        [InlineData("limitright(ln(x) / ln(2 - cos(x)), x, 0)", "-oo")]
        [InlineData("limitright(ln(x) / ln(sec(x)), x, 0)", "-oo")]
        [InlineData("limitleft(ln(-x) / ln(cos(x)), x, 0)", "+oo")]
        [InlineData("limit(ln(x^2) * (1 / ln(cos(x))), x, 0)", "+oo")]
        [InlineData("limitright(ln(x) / ln(1 - x^2 / 2), x, 0)", "+oo")]
        [InlineData("limitright((-1 / x) / ln(cos(x)), x, 0)", "+oo")]
        [InlineData("limitright(ln(x) / (cos(x) - 1), x, 0)", "+oo")]
        public void ItIsTheLimit(string limit, string value)
            => Assert.Equal(value.ToEntity(), limit.ToEntity().Simplify());

        /// <summary>Where a quotient is not answered now, it was answered with the wrong sign: declining is honest.</summary>
        [Theory]
        [InlineData("limit(ln(x) / ln(cos(1 / x)), x, +oo)", "-oo")]
        [InlineData("limitright(log(cos(x), x), x, 0)", "+oo")]
        public void ItIsTheLimitOrNothing(string limit, string value)
        {
            var answer = limit.ToEntity().Simplify();
            Assert.True(answer is not Entity.Number || answer == value.ToEntity(), $"{limit} is {answer}, where it is {value}");
        }
    }
}
