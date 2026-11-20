//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The conjugate of a number was rebuilt from its parts as decimals, so a ratio that no downcast
    /// reads back came out a decimal: <c>conj(1/12345678901)</c> was a hundred-digit decimal and
    /// <c>conj(10^-30 x)</c> was <c>1E-30 * conjugate(x)</c>, which left
    /// <c>-i + 2 * 10^-30 x + 3 conj(10^-30 x) = 0</c> unsolved. Found while solving generated
    /// equations with <c>x</c> written as <c>10^-30 x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheConjugateOfAnExactNumberIsExactTest
    {
        [Theory]
        [InlineData("conj(1/12345678901)", "1/12345678901")]
        [InlineData("conj(10^(-30))", "1/1000000000000000000000000000000")]
        [InlineData("conj(10^(-30) x)", "1/1000000000000000000000000000000 * conj(x)")]
        [InlineData("conj(1/3)", "1/3")]
        [InlineData("conj(1/12345678901 + 2i)", "1/12345678901 + -2i")]
        public void TheConjugateKeepsTheRatio(string expression, string expected)
            => Assert.Equal(expected.ToEntity().InnerSimplified, expression.ToEntity().InnerSimplified);

        [Fact]
        public void AnEquationInTheConjugateOfASmallMultipleIsSolved()
        {
            var answer = Assert.IsType<Set.FiniteSet>("-i + 2 * 10^(-30) x + 3 * conj(10^(-30) x) = 0".ToEntity().Solve("x"));
            var root = Assert.Single(answer);
            Assert.Equal("-1000000000000000000000000000000 * i".ToEntity().Evaled, root.Evaled);
        }
    }
}
