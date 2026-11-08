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
    /// A quotient whose divisor is a quotient was read as written. x^(-pi) / (ln(2) / ln(x)) at +oo is then 0/0, and
    /// l'Hopital's rule makes each step of it another 0 * oo with a higher power of ln(x), so it was left unevaluated
    /// where x^(-pi) ln(x) / ln(2), the same function, is 0. log(x, 2) is ln(2) / ln(x), so x^(-pi) / log(x, 2) was
    /// left unevaluated too. The reading as a quotient of products is taken only where it gives a value: x ln(x) at 0
    /// is 0 * (-oo) to it, and x / log(x, 2) from both sides of 0 stays 0. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AQuotientOverAQuotientTest
    {
        [Theory]
        [InlineData("x^(-pi) / (ln(2) / ln(x))", "+oo", ApproachFrom.BothSides, "0")]
        [InlineData("x^(-pi) / log(x, 2)", "+oo", ApproachFrom.BothSides, "0")]
        [InlineData("(1/x)^pi / log(1/x, 2)", "+oo", ApproachFrom.BothSides, "0")]
        [InlineData("arcsin(pi + x) / (1 / x)", "0", ApproachFrom.BothSides, "0")]
        [InlineData("tan(1/2) / log(x, -1) / tanh(sin(x))", "1", ApproachFrom.Left, "0")]
        [InlineData("tanh(exp(pi)) / log(log(-1, x), 2)", "-1", ApproachFrom.BothSides, "0")]
        // Read as written, and not as x ln(x) / ln(2), whose limit from both sides was NaN.
        [InlineData("x / log(x, 2)", "0", ApproachFrom.BothSides, "0")]
        [InlineData("x ^ log(pi + 1, x - 1)", "1", ApproachFrom.BothSides, "1")]
        public void IsReadAsAQuotientOfProducts(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", destination, side));

        // The divisor's own divisor vanishing does not make the quotient vanish: x / (1 / x) is x^2, +oo at +oo.
        [Fact]
        public void TheInnerDivisorIsAFactorOfTheDividend()
            => Assert.Equal("+oo".ToEntity(), "x / (1 / x)".ToEntity().Limit("x", "+oo", ApproachFrom.BothSides));
    }
}
