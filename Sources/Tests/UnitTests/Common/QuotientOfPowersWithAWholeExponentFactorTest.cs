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
    /// <c>a^n / b^(c n) -> (a / b^c)^n</c> and its mirror gather an exponent as
    /// <c>a^n / b^n -> (a / b)^n</c> does, and hold on the same condition: a whole <c>n</c>, or
    /// positive real bases (https://github.com/asc-community/AngouriMath/issues/802). The pair
    /// had no guard, so <c>x^n / y^(2 n)</c> became <c>(x / y^2)^n</c>, which is <c>1</c> at
    /// <c>x = 1, y = -1, n = 1/2</c> where the quotient is <c>-1</c>.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class QuotientOfPowersWithAWholeExponentFactorTest
    {
        private static System.Numerics.Complex ValueAt(Entity expr, string x, string y, string n)
        {
            var value = expr.Substitute("x", x.ToEntity()).Substitute("y", y.ToEntity())
                .Substitute("n", n.ToEntity()).EvalNumerical();
            return new(value.RealPart.EDecimal.ToDouble(), value.ImaginaryPart.EDecimal.ToDouble());
        }

        [Theory]
        [InlineData("x ^ n / y ^ (2 * n)", "1", "-1", "1/2")]
        [InlineData("x ^ n / y ^ (2 * n)", "2", "-3", "1/3")]
        [InlineData("x ^ n / y ^ (3 * n)", "1 + i", "-1 + 2i", "1/2")]
        [InlineData("x ^ (2 * n) / y ^ n", "-1", "1", "1/2")]
        [InlineData("x ^ (2 * n) / y ^ n", "-2", "3", "3/4")]
        [InlineData("x ^ (3 * n) / y ^ n", "-1 + 2i", "1 - i", "1/2")]
        public void SimplifyingKeepsTheValueOffThePositiveReals(string expr, string x, string y, string n)
        {
            var original = expr.ToEntity();
            var simplified = original.Simplify();
            var before = ValueAt(original, x, y, n);
            var after = ValueAt(simplified, x, y, n);
            Assert.True((before - after).Magnitude < 1e-9,
                $"{expr} simplified to {simplified.Stringize()}: {before} became {after}");
        }

        // Where the bases are positive reals the identity holds, and the gathering is kept.
        [Theory]
        [InlineData("2 ^ n / 3 ^ (2 * n)", "(2/9) ^ n")]
        [InlineData("2 ^ (2 * n) / 3 ^ n", "(4/3) ^ n")]
        public void PositiveRealBasesStillGather(string expr, string gathered)
            => Assert.Equal(gathered.ToEntity(), expr.ToEntity().Simplify());
    }
}
