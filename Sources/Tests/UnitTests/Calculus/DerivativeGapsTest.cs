//
// Copyright (c) 2019-2022 Angouri.
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
    /// Two ways a derivative could come back unusable: as an unevaluated node, or with a
    /// condition on it that does not belong. Both were found by differentiating
    /// antiderivatives back and checking them at points.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class DerivativeGapsTest
    {
        [Fact]
        public void SignumHasADerivative() =>
            Assert.Equal("0 provided not x = 0".ToEntity(), "sgn(x)".ToEntity().Differentiate("x"));

        // A conditional expression is differentiated where its condition holds, and under
        // `x > 0` the roots of x are real, so the sign and the modulus of an expression in them
        // are differentiated as on the real line. The antiderivative of a square written in
        // x^(1/3) is `sgn(x^(1/3) + c) F(x) provided x > 0`, and could not be checked by
        // differentiating it back while that sign's derivative was left as written.
        [Theory]
        [InlineData("sgn(x^(1/3) + 2) provided x > 0", "1", "0")]
        [InlineData("abs(sqrt(x) - 1) provided x > 0", "4", "1/4")]
        [InlineData("abs(sqrt(x) - 1) provided x > 0", "1/4", "-1")]
        [InlineData("piecewise(abs(sqrt(x) - 1) provided x > 0, 0)", "1/4", "-1")]
        [InlineData("max(sqrt(x), 1) provided x > 0", "4", "1/4")]
        [InlineData("max(sqrt(x), 1) provided x > 0", "1/4", "0")]
        public void UnderItsConditionARootIsReal(string expression, string at, string derivative) =>
            Assert.Equal(derivative.ToEntity().Evaled, expression.ToEntity().Differentiate("x").Substitute("x", at.ToEntity()).Evaled);

        // For x < 0 the roots are complex and nothing makes their sign flat. `x >= 0` does not
        // help either: at 0 there is no interval of positive x around the point.
        [Theory]
        [InlineData("sgn(x^(1/3) + 2)")]
        [InlineData("abs(sqrt(x) - 1)")]
        [InlineData("sgn(x^(1/3) + 2) provided x >= 0")]
        public void WithoutAStrictConditionARootIsNotReadAsReal(string expression) =>
            Assert.Contains("derivative(", expression.ToEntity().Differentiate("x").Stringize());

        // The antiderivative of abs(x) is sgn(x) * x^2 / 2. Differentiating it back used to
        // produce derivative(sgn(x), x) and stop, so the answer could not be checked or
        // used numerically.
        [Theory]
        [InlineData("abs(x)")]
        [InlineData("abs(x) + x")]
        [InlineData("abs(x + 1)")]
        [InlineData("sgn(x) * x")]
        public void AntiderivativesOfAbsoluteValuesDifferentiateBack(string integrand)
        {
            var f = integrand.ToEntity();
            var derivative = f.Integrate("x").Substitute("C", 0).Differentiate("x");
            foreach (var point in new[] { 0.37, 1.41, 2.71 })
            {
                var expected = f.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.Equal(expected, actual, 8);
            }
        }

        // A constant exponent that happens to be written as a sum is still constant. Read
        // as non-constant it took the logarithmic rule, which needs a positive base, so
        // this came back undefined for x < 1.
        [Fact]
        public void ConstantExponentWrittenAsASumUsesThePowerRule() =>
            Assert.Equal("(x - 1) ^ 4".ToEntity(),
                "(x - 1) ^ (4 + 1) / (4 + 1)".ToEntity().Differentiate("x").Simplify());

        [Fact]
        public void AntiderivativeOfAPowerDifferentiatesBackEverywhere()
        {
            var f = "(x - 1) ^ 4".ToEntity();
            var derivative = f.Integrate("x").Substitute("C", 0).Differentiate("x");
            // 0.37 is below the base's root, which is where the spurious condition bit.
            foreach (var point in new[] { 0.37, 1.41, 2.71 })
            {
                var expected = f.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.Equal(expected, actual, 8);
            }
        }

        // A genuinely symbolic exponent must still take the logarithmic rule, condition
        // and all. The condition is `not x = 0` and was `x > 0` until the logarithm's
        // domain stopped being stated over the reals regardless of the reading: it comes
        // from `ln(x) * 0`, which is 0 everywhere ln(x) has a value and NaN at x = 0,
        // where -oo * 0 is indeterminate. The old condition also cost the answer at every
        // negative x -- at n = 3, x = -2.5 the derivative is 75/4 and used to be NaN.
        // https://github.com/asc-community/AngouriMath/issues/721
        [Theory]
        // The logarithmic rule's condition, `not x = 0`, is what the quotient by x says on its
        // own, and the simplified derivative no longer repeats it.
        // https://github.com/asc-community/AngouriMath/issues/1394
        [InlineData("x ^ n", "x ^ n * n / x")]
        [InlineData("2 ^ x", "ln(2) * 2 ^ x")]
        // Compared as printed text: what is under test is that the logarithmic rule and
        // its condition are still used, and the tree differs from the parsed expectation
        // only in how the condition nests.
        public void SymbolicExponentsAreUnaffected(string input, string expected) =>
            Assert.Equal(expected, input.ToEntity().Differentiate("x").Simplify().Stringize());
    }
}
