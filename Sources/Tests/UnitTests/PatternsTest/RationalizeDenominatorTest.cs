//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.PatternsTest
{
    /// <summary>
    /// A surd in a two-term denominator was left where it stood:
    /// <code>
    ///     (5 - sqrt(3)) / (5 + sqrt(3))   unchanged, where it is 14/11 - 5/11 * sqrt(3)
    /// </code>
    /// https://github.com/asc-community/AngouriMath/issues/205
    /// </summary>
    /// <remarks>
    /// The library already prefers a denominator without a surd -- <c>1/sqrt(2)</c> has
    /// always come back as <c>sqrt(2)/2</c> -- so this extends an existing preference to
    /// the binomial case rather than introducing one, which is the half of #205 that does
    /// not need a maintainer's ruling.
    /// </remarks>
    [Trait("Area", "PatternsTest")]
    public sealed class RationalizeDenominatorTest
    {
        private static bool IsSurd(Entity node)
            => node is Entity.Powf(_, Entity.Number.Rational and not Entity.Number.Integer);

        /// <summary>Every denominator anywhere in the tree.</summary>
        private static bool HasASurdDenominator(Entity expr)
            => expr.Nodes.OfType<Entity.Divf>().Any(div => div.Divisor.Nodes.Any(IsSurd));

        private static void AssertSameValue(string original, Entity simplified)
        {
            var before = original.ToEntity().EvalNumerical();
            var after = simplified.EvalNumerical();
            var expected = before.RealPart.EDecimal.ToDouble();
            var actual = after.RealPart.EDecimal.ToDouble();
            var scale = Math.Max(Math.Max(Math.Abs(expected), Math.Abs(actual)), 1e-12);
            Assert.True(Math.Abs(expected - actual) <= 1e-12 * scale,
                $"{original} simplified to {simplified.Stringize()}, which is {actual} rather than {expected}");
        }

        [Theory]
        [InlineData("(5 - sqrt(3)) / (5 + sqrt(3))")]
        [InlineData("1 / (5 + sqrt(3))")]
        [InlineData("1 / (sqrt(2) + sqrt(3))")]
        [InlineData("(1 + sqrt(2)) / (1 - sqrt(2))")]
        [InlineData("2 / (3 - sqrt(5))")]
        [InlineData("1 / (2 * sqrt(3) - 1)")]
        public void TheSurdLeavesTheDenominator(string expr)
        {
            var simplified = expr.ToEntity().Simplify();
            Assert.False(HasASurdDenominator(simplified),
                $"{expr} still has a surd in a denominator: {simplified.Stringize()}");
            AssertSameValue(expr, simplified);
        }

        /// <summary>
        /// The rule multiplies by the conjugate, which clears a square root and nothing
        /// else. A cube root is untouched by it -- <c>1 - 2^(2/3)</c> is no better than
        /// <c>1 + 2^(1/3)</c> -- so the rule must decline rather than churn, and the answer
        /// must still be the same number.
        /// </summary>
        [Theory]
        [InlineData("1 / (1 + 2 ^ (1/3))")]
        [InlineData("1 / (1 + 5 ^ (1/4))")]
        public void ARootItCannotClearIsLeftAlone(string expr)
            => AssertSameValue(expr, expr.ToEntity().Simplify());

        /// <summary>
        /// A symbolic denominator is out of scope: whether the conjugate is non-zero is not
        /// decidable, and multiplying by something that may be zero is how a value gets
        /// lost. These must keep their surd rather than be rewritten under a condition
        /// nobody asked for.
        /// </summary>
        [Theory]
        [InlineData("1 / (a + sqrt(3))")]
        [InlineData("1 / (x - sqrt(x))")]
        public void ASymbolicDenominatorIsLeftAlone(string expr)
        {
            var simplified = expr.ToEntity().Simplify();
            foreach (var point in new[] { 1.7, 2.3, 4.1 })
            {
                var before = expr.ToEntity().Substitute("a", point).Substitute("x", point).EvalNumerical();
                var after = simplified.Substitute("a", point).Substitute("x", point).EvalNumerical();
                Assert.True(Math.Abs(before.RealPart.EDecimal.ToDouble() - after.RealPart.EDecimal.ToDouble()) < 1e-9,
                    $"{expr} at {point}: {simplified.Stringize()} is a different number");
            }
        }

        /// <summary>
        /// The one case the library already handled, which must keep working -- it goes
        /// through a different route and this rule does not fire on it at all.
        /// </summary>
        [Fact]
        public void ASingleSurdDenominatorStillRationalizes()
            => Assert.Equal("sqrt(2) / 2".ToEntity().Simplify(), "1 / sqrt(2)".ToEntity().Simplify());
    }
}
