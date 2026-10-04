//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The long division reads its divisor's degree off the divisor expanded, and a coefficient
    /// with a symbol in it can be zero as a value without being zero as written:
    /// <c>(-(a - a u^2) + -(u^2 + 1) a)^3</c> is <c>-8 a^3</c>, and expanded it leaves
    /// <c>-3 a^3 + 3 a^3</c> at <c>u^6</c>. The division took that for the leading coefficient
    /// and divided by it, and the quotient was NaN.
    /// https://github.com/asc-community/AngouriMath/issues/1665
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class LongDivisionLeadingCoefficientTest
    {
        [Fact]
        public void ACoefficientThatIsZeroAsAValueIsNoDegree()
        {
            var u = MathS.Var("u");
            var dividend = "u^4 (a - a u^2)".ToEntity();
            // As the substitution writes it: with `- (u^2 + 1) a` the expansion collects to `-8 a^3`.
            var divisor = "(-(a - a u^2) + -(u^2 + 1) a)^3".ToEntity();
            var divided = Functions.TreeAnalyzer.PolynomialLongDivision(dividend, divisor, genericCase: true, inTermsOf: u);
            Assert.NotNull(divided);
            var (quotient, remainder) = divided!.Value;
            Assert.DoesNotContain("NaN", quotient.Stringize());
            Assert.DoesNotContain("NaN", remainder.Stringize());
            foreach (var (a, at) in new[] { (1.37, 0.4), (-0.83, 1.9), (2.71, -1.3) })
            {
                Entity Pinned(Entity e) => e.Substitute("a", a).Substitute(u, at);
                var got = Pinned(quotient + remainder).EvalNumerical();
                var want = Pinned(dividend / divisor).EvalNumerical();
                Assert.True(((got - want).Abs() as Entity.Number.Real)!.EDecimal.ToDouble() < 1e-9 * System.Math.Max(1, (want.Abs() as Entity.Number.Real)!.EDecimal.ToDouble()),
                    $"the quotient and remainder are {got} at a = {a}, u = {at}, where the division is {want}");
            }
        }

        [Theory]
        [InlineData("-3 * a^3 + 3 * a^3")]
        [InlineData("-(2 * 1/c * c - 1) + 1")]
        [InlineData("-(c/c - 1) * c")]
        public void IsZeroAsAValue(string coefficient)
            => Assert.True(Functions.PartialFractions.IsZeroAsAValue(coefficient.ToEntity()));

        [Theory]
        [InlineData("-3 * a^3 + 2 * a^3")]
        [InlineData("2 * 1/c * c")]
        [InlineData("a - b")]
        public void IsNotZeroAsAValue(string coefficient)
            => Assert.False(Functions.PartialFractions.IsZeroAsAValue(coefficient.ToEntity()));
    }
}
