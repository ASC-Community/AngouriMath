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
    /// f floor(g), f ceil(g) and f round(g), where f tends to 0 and g is real, have the limit of f g: the step is within 1
    /// of its argument, so the two products differ by f times something bounded. x floor(2/x) at 0 was left unevaluated,
    /// each side stopping at 0 * floor(+-oo), where it is 2. Each value here was checked at x = +-7/3 * 10^-k for k from 4
    /// to 8. No issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AStepOfAnUnboundedArgumentTimesAVanishingFactorTest
    {
        [Theory]
        [InlineData("x * floor(2/x)", "0", "2")]
        [InlineData("floor(1/x) * sin(x)", "0", "1")]
        [InlineData("x^2 * floor(1/x^2)", "0", "1")]
        [InlineData("x * ceil(1/x)", "0", "1")]
        [InlineData("x * round(1/x)", "0", "1")]
        [InlineData("x * ceil(-2/x)", "0", "-2")]
        [InlineData("-3 * x * floor(1/x)", "0", "-3")]
        [InlineData("x^2 * floor(1/x)", "0", "0")]
        [InlineData("(x - 1) * floor(1/(x - 1))", "1", "1")]
        [InlineData("x * floor(2/x) + 3", "0", "5")]
        [InlineData("floor(x) / x", "+oo", "1")]
        public void FromEachSideAndBoth(string input, string destination, string expected)
        {
            var expr = input.ToEntity();
            Assert.Equal(expected.ToEntity(), expr.Limit("x", destination, ApproachFrom.Left).InnerSimplified);
            Assert.Equal(expected.ToEntity(), expr.Limit("x", destination, ApproachFrom.Right).InnerSimplified);
            Assert.Equal(expected.ToEntity(), expr.Limit("x", destination).InnerSimplified);
        }

        // Where f g has no limit, neither has f floor(g): the sides diverge in different directions, so there is none.
        [Theory]
        [InlineData("x * floor(1/x^2)")]
        [InlineData("sqrt(abs(x)) * floor(1/x)")]
        public void SidesThatDivergeApart(string input)
        {
            var expr = input.ToEntity();
            Assert.Equal(Entity.Number.Real.NegativeInfinity, expr.Limit("x", 0, ApproachFrom.Left));
            Assert.Equal(Entity.Number.Real.PositiveInfinity, expr.Limit("x", 0, ApproachFrom.Right));
            Assert.Equal(MathS.NaN, expr.Limit("x", 0));
        }

        // x floor(sin(1/x)/x) is sin(1/x) up to a term that vanishes, and has no limit. The rule takes only a value from
        // the limit of f g, so it leaves this unevaluated rather than claiming anything.
        [Fact]
        public void AnOscillatingProductIsNotAnswered()
            => Assert.IsType<Entity.Limitf>("x * floor(sin(1/x)/x)".ToEntity().Limit("x", 0));
    }
}
