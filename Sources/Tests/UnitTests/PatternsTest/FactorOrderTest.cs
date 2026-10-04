//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.PatternsTest
{
    /// <summary>
    /// A product is written the way it is by hand: the number, then the letters and their powers,
    /// then the functions, so <c>y sin(x)</c> and <c>x e^x</c>. Algebrite's issue 29, from #180.
    /// https://github.com/asc-community/AngouriMath/issues/1628
    /// </summary>
    [Trait("Area", "PatternsTest")]
    public sealed class FactorOrderTest
    {
        [Theory]
        [InlineData("sin(x) * y", "y * sin(x)")]
        [InlineData("sin(x) * b", "b * sin(x)")]
        [InlineData("y * 2 * sin(x)", "2 * y * sin(x)")]
        [InlineData("log(x) * z", "z * log(10, x)")]
        [InlineData("sin(x) * x ^ 2", "x ^ 2 * sin(x)")]
        [InlineData("a * log(x) + y * sin(x)", "y * sin(x) + a * log(10, x)")]
        public void LettersComeBeforeFunctions(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Simplify().Stringize());

        /// <summary>
        /// An exponential is a function, whether its base is <c>e</c> or a number; a letter raised
        /// to a letter is a power of that letter.
        /// </summary>
        [Theory]
        [InlineData("e ^ x * x", "x * e ^ x")]
        [InlineData("2 ^ n * n", "n * 2 ^ n")]
        [InlineData("x ^ n * a", "a * x ^ n")]
        public void AnExponentialIsAFunctionAndAPowerOfALetterIsALetter(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Simplify().Stringize());

        /// <summary>
        /// Only the answer is put in this order. The rules that combine factors still meet them in
        /// the order the search sorts by, where <c>2 cos(x) sin(x)</c> are neighbours: sorting the
        /// letters first inside the search put <c>x</c> between them, and this stopped being
        /// <c>x sin(2x)</c>.
        /// </summary>
        [Theory]
        [InlineData("2 * x * sin(x) * cos(x)", "x * sin(2 * x)")]
        [InlineData("x * (x - 1)!", "x!")]
        public void TheRulesStillMeetTheFactorsTheyCombine(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Simplify().Stringize());

        /// <summary>The order is the tree's, so the printed form parses back to the same product.</summary>
        [Theory]
        [InlineData("sin(x) * y")]
        [InlineData("e ^ x * x * 3")]
        public void ThePrintedFormReadsBackAsTheSameProduct(string input)
        {
            var simplified = input.ToEntity().Simplify();
            Assert.Equal(simplified, simplified.Stringize().ToEntity());
        }
    }
}
