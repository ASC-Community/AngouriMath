//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The power rule wrote a constant exponent as its value, so an exponent that is not rational became a decimal:
    /// x^pi differentiated to 3.14159... * x^2.14159..., and l'Hopital's rule answered (x^pi - 1) / (x - 1) at 1 with
    /// that decimal instead of pi. Nothing reads such a decimal as pi again, and x^pi / log(x, 2) - 2 at 0 from the
    /// right was left unevaluated for it. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ADerivativeKeepsAnIrrationalExponentExactTest
    {
        [Theory]
        [InlineData("x^pi", "pi * x^(pi - 1)")]
        [InlineData("x^sqrt(2)", "sqrt(2) * x^(sqrt(2) - 1)")]
        [InlineData("x^ln(2)", "ln(2) * x^(ln(2) - 1)")]
        [InlineData("sin(x)^pi", "pi * sin(x)^(pi - 1) * cos(x)")]
        public void TheExponentIsWrittenAsItIs(string input, string expected)
        {
            var derivative = input.ToEntity().Differentiate("x");
            Assert.DoesNotContain(derivative.Nodes.OfType<Real>(), real => real is not Rational);
            foreach (var at in new[] { 0.3, 0.7, 1.9 })
                Assert.Equal(Value(expected.ToEntity(), at), Value(derivative, at), 9);
        }

        // A rational exponent is still its value.
        [Fact]
        public void ARationalExponentIsItsValue()
            => Assert.Equal("3 * x ^ 2".ToEntity(), "x^(2 + 1)".ToEntity().Differentiate("x").InnerSimplified);

        [Theory]
        [InlineData("(x^pi - 1) / (x - 1)", "pi")]
        [InlineData("(x^sqrt(2) - 1) / ln(x)", "sqrt(2)")]
        [InlineData("(x^ln(2) - 1) / (x - 1)", "ln(2)")]
        public void LHopitalsRuleAnswersTheExponent(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", 1, ApproachFrom.BothSides));

        private static double Value(Entity expr, double at)
            => (double)((Real)expr.Substitute("x", at).EvalNumerical()).RealPart;
    }
}
