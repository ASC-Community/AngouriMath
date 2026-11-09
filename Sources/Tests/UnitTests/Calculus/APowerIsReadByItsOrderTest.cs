//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A power g^a to an exponent that is not an integer, of a g tending to 0, had no reading against a logarithm unless
    /// g was x itself: x^pi ln(x) at 0 from the right was 0 and sin(x)^pi ln(x), and even (2x)^pi ln(x), were left
    /// unevaluated, while ln(x) / (2x)^(-pi) took fifteen seconds to be. As a factor, g^a is x^(k a) times
    /// (g / x^k)^a, where k is the power of x that g behaves like, and the second part tends to a positive number that
    /// can be taken out of the limit. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class APowerIsReadByItsOrderTest
    {
        [Theory]
        [InlineData("sin(x)^pi * ln(x)", "0")]
        [InlineData("(2x)^pi * ln(x)", "0")]
        [InlineData("sqrt(sin(x)) * ln(x)", "0")]
        [InlineData("tan(x)^pi * ln(x)", "0")]
        [InlineData("(1 - cos(x))^pi * ln(x)", "0")]
        [InlineData("(x + x^2)^pi * ln(x)", "0")]
        [InlineData("ln(x) / (2x)^(-pi)", "0")]
        [InlineData("sin(sin(x)^pi * ln(x))", "0")]
        [InlineData("sin(x)^pi / x^pi", "1")]
        public void AtZeroFromTheRight(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", 0, ApproachFrom.Right));

        [Fact]
        public void AtAnInfinity()
            => Assert.Equal("1".ToEntity(), "(x^2 + 1)^(1/pi) / x^(2/pi)".ToEntity().Limit("x", "+oo", ApproachFrom.BothSides));

        // Only a factor is taken out: in a sum the part taken out is the whole of the answer.
        [Fact]
        public void APowerInASumIsLeftAlone()
            => Assert.Equal("0".ToEntity(), "sin(x)^pi - x^pi".ToEntity().Limit("x", 0, ApproachFrom.Right));

        [Fact]
        public void APowerOfAGrowingBaseIsInTheDivisor()
        {
            Assert.Equal("+oo".ToEntity(), "sqrt(arctan(1)) / (1/2 / x)^sinh(pi)".ToEntity().Limit("x", "+oo", ApproachFrom.BothSides));
            Assert.Equal("-oo".ToEntity(), "x / (1/2) / (x + 1)^pi".ToEntity().Limit("x", -1, ApproachFrom.Right));
        }

        // A real power of a base tending to -oo is complex and stays unevaluated. It is not read by its order, which
        // took 6.8 s to decline the first of these and 17.6 s the second, where master declines both in 0.3 s.
        [Theory]
        [InlineData("log(x^pi, pi^2)^arctan(tan(2))")]
        [InlineData("log(x, 2)^(1 - pi) / (log(1/2, x) - pi)")]
        public void APowerOfANegativeBaseIsDeclined(string input)
            => Assert.IsType<Entity.Limitf>(input.ToEntity().Limit("x", 1, ApproachFrom.Left));
    }
}
