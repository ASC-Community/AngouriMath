//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Two rules rewrote a power in a way that is only true on part of the plane, and both
    /// changed the value for a negative argument: <c>(a^b)^c = a^(b*c)</c>, which needs a
    /// positive <c>a</c> or a whole <c>c</c>, and <c>(a*x)^c = a^c * x^c</c>, which needs a
    /// positive <c>a</c> or a whole <c>c</c>. Both were applied unconditionally, so
    /// <c>sqrt(x^2)</c> came back as <c>x</c> and <c>sqrt(-x)</c> as <c>i * sqrt(x)</c> --
    /// each the negation of the right answer where x is negative.
    /// https://github.com/asc-community/AngouriMath/issues/752
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class PowerOfPowerBranchTest
    {
        /// <summary>
        /// Value, not shape. The magnitude was right in every one of these and only the sign
        /// was wrong, so a test comparing printed forms could have passed throughout.
        /// </summary>
        private static void AssertSameValueAt(string expr, string point)
        {
            var original = expr.ToEntity();
            var simplified = original.Simplify();
            double At(Entity e)
            {
                var substituted = e.Substitute("x", point.ToEntity());
                while (substituted is Entity.Providedf(var inner, _)) substituted = inner;
                var value = substituted.EvalNumerical();
                Assert.True(System.Math.Abs(value.ImaginaryPart.EDecimal.ToDouble()) < 1e-9,
                    $"{expr} is not real at x = {point}");
                return value.RealPart.EDecimal.ToDouble();
            }
            Assert.Equal(At(original), At(simplified), 9);
        }

        /// <summary>
        /// The property, checked at a negative point, which is the only place any of this
        /// goes wrong. Every one of these was off by a sign before.
        /// </summary>
        [Theory]
        [InlineData("sqrt(x ^ 2)")]
        [InlineData("(x ^ 2) ^ (1/2)")]
        [InlineData("(x * x) ^ (1/2)")]
        [InlineData("(x ^ 2) ^ (3/2)")]
        [InlineData("(x ^ 2) ^ (1/2 - 1)")]
        [InlineData("(x ^ 2) ^ (1/2 - 2)")]
        [InlineData("sqrt(-x)")]
        [InlineData("sqrt(-1 * x)")]
        [InlineData("sqrt(x / -1)")]
        [InlineData("(-x) ^ (1/2)")]
        [InlineData("(-x) ^ (1/2 - 1)")]
        public void ARewriteThatMovesTheBranchIsNotMade(string expr) =>
            AssertSameValueAt(expr, "-63/100");

        /// <summary>
        /// What the rules still do, because there they are true. A whole outer exponent makes
        /// <c>(a^b)^c</c> sound whatever the sign of a -- it is <c>a^b</c> multiplied by
        /// itself c times -- and a positive constant may always be taken out from under a
        /// root.
        /// </summary>
        [Theory]
        [InlineData("(x ^ (1/2)) ^ 2", "x")]
        [InlineData("(x ^ 2) ^ 2", "x ^ 4")]
        [InlineData("(x ^ 2) ^ 3", "x ^ 6")]
        [InlineData("(-x) ^ 2", "x ^ 2")]
        [InlineData("(2 ^ 2) ^ (1/2)", "2")]
        [InlineData("(4 ^ (1/2)) ^ (1/2)", "sqrt(2)")]
        public void ARewriteThatHoldsIsStillMade(string expr, string expected) =>
            Assert.Equal(Entity.Number.Integer.Create(0),
                (expr.ToEntity().Simplify() - expected.ToEntity()).Simplify());

        // A positive base is unaffected in either rule, which is where they came from.
        [Theory]
        [InlineData("(2 ^ x) ^ 3")]
        [InlineData("(3 * x) ^ 2")]
        [InlineData("sqrt(4 * x)")]
        [InlineData("(x ^ 3) ^ 2")]
        public void APositiveBaseKeepsItsValue(string expr) =>
            AssertSameValueAt(expr, "17/10");

        /// <summary>
        /// The property that found all of this, stated over the shapes it found them in:
        /// wherever the expression and its simplification are both real, they are the same
        /// number. <c>Sources/Tests/Harnesses/SimpSweep</c> asks it of ten thousand generated expressions and
        /// went from 30 disagreements to 0 with these two rules narrowed.
        /// </summary>
        [Fact]
        public void NoPowerRewriteChangesTheValueAtANegativePoint()
        {
            string[] shapes =
            {
                "sqrt({0})", "({0}) ^ (1/2)", "({0}) ^ (3/2)", "({0}) ^ 2", "({0}) ^ 3",
                "1 / sqrt({0})", "sqrt(sqrt({0}))",
            };
            string[] insides = { "x ^ 2", "x * x", "-x", "-1 * x", "x / -1", "x ^ 4", "2 * x ^ 2" };
            foreach (var shape in shapes)
                foreach (var inside in insides)
                {
                    var expr = string.Format(shape, inside);
                    var original = expr.ToEntity();
                    var simplified = original.Simplify();
                    var before = original.Substitute("x", "-63/100".ToEntity()).EvalNumerical();
                    var after = simplified.Substitute("x", "-63/100".ToEntity()).EvalNumerical();
                    Assert.Equal(before.RealPart.EDecimal.ToDouble(),
                                 after.RealPart.EDecimal.ToDouble(), 9);
                    Assert.Equal(before.ImaginaryPart.EDecimal.ToDouble(),
                                 after.ImaginaryPart.EDecimal.ToDouble(), 9);
                }
        }
    }
}
