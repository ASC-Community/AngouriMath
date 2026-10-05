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
    /// <c>(c/a)^d * a^e = c^d * a^(e - d)</c> splits the power of the quotient, which holds for a
    /// whole <c>d</c>, and for an odd root over a positive <c>c</c>, and moves the branch
    /// otherwise: <c>(1/(-2))^(1/2)</c> is <c>0.707i</c> and <c>(-2)^(-1/2)</c> is
    /// <c>-0.707i</c>. It was applied for every numeric <c>d</c>, so <c>sqrt(1/x) * sqrt(x)</c>
    /// came back as 1 where it is -1, and <c>sec(x)^(3/2) cos(x)^(3/2)</c>, the constant an
    /// antiderivative carries where the cosine is negative, as 1 where it is -1.
    /// https://github.com/asc-community/AngouriMath/issues/1734
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ReciprocalPowerBranchTest
    {
        /// <summary>
        /// Value, not shape, compared as complex numbers: several of these are not real at the
        /// point, and the simplified form was off by a sign or a phase there.
        /// </summary>
        private static void AssertSameValueAt(string expr, string variable, string point)
        {
            var original = expr.ToEntity();
            var simplified = original.Simplify();
            Entity.Number.Complex At(Entity e)
            {
                var substituted = e.Substitute(variable, point.ToEntity());
                while (substituted is Entity.Providedf(var inner, _)) substituted = inner;
                return substituted.EvalNumerical();
            }
            var (want, got) = (At(original), At(simplified));
            Assert.True((want - got).Abs().EDecimal.ToDouble() < 1e-9,
                $"{expr} is {want} at {variable} = {point}, and its simplified form {simplified} is {got}");
        }

        /// <summary>The negative side, where every one of these was wrong.</summary>
        [Theory]
        [InlineData("sqrt(x) * sqrt(1/x)", "x", "-2")]
        [InlineData("sqrt(1/x) * x", "x", "-63/100")]
        [InlineData("sqrt(1/x) * x ^ (3/2)", "x", "-63/100")]
        [InlineData("(1/x) ^ (3/2) * x ^ (3/2)", "x", "-63/100")]
        [InlineData("(2/x) ^ (1/2) * x", "x", "-3")]
        [InlineData("sec(x) ^ (3/2) * cos(x) ^ (3/2)", "x", "2")]
        [InlineData("sec(x) ^ (1/2) * cos(x) ^ (1/2)", "x", "2")]
        public void ARewriteThatMovesTheBranchIsNotMade(string expr, string variable, string point) =>
            AssertSameValueAt(expr, variable, point);

        /// <summary>
        /// What the rule still does, because there it is true: a whole power of the quotient,
        /// and an odd root, which takes the real root of a negative base.
        /// </summary>
        [Theory]
        [InlineData("(1/x) ^ 2 * x ^ 3", "x")]
        [InlineData("(2/x) ^ 2 * x", "4 / x")]
        [InlineData("(1/x) ^ (1/3) * x ^ (1/3)", "1")]
        public void ARewriteThatHoldsIsStillMade(string expr, string expected)
        {
            var simplified = expr.ToEntity().Simplify();
            while (simplified is Entity.Providedf(var inner, _)) simplified = inner;
            Assert.Equal(expected.ToEntity().Simplify(), simplified);
            AssertSameValueAt(expr, "x", "-63/100");
        }
    }
}
