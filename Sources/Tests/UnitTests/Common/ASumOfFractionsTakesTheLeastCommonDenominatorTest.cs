//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using AngouriMath.Functions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Two fractions added inside <c>Simplify</c> went over the product of their denominators,
    /// which squares a factor the two share, and every sum after it multiplied the denominators
    /// again before anything cancelled. <c>(1 - tanh(x)^2) / tanh(x)</c> written through
    /// exponentials took two seconds to simplify, nearly all of it in two sums whose denominators
    /// had grown to <c>(e^(2x) + 1)^6 (e^(2x) - 1)^3</c>. Where the denominators share a factor,
    /// the sum now goes over their least common multiple. Found while tracing a slow limit; no
    /// issue was filed.
    /// </summary>
    /// <remarks>
    /// Checked by value: the denominator is a constant multiple of the least common multiple, and
    /// the sum keeps its value. The spelling the simplifier gives either is not what is asked.
    /// </remarks>
    [Trait("Area", "Common")]
    public sealed class ASumOfFractionsTakesTheLeastCommonDenominatorTest
    {
        private static readonly double[] Points = { 0.37, 1.13, -0.8 };

        [Theory]
        // A factor of one denominator is the other.
        [InlineData("1", "(e^(2x) + 1)^2 * (e^(2x) - 1)", "x", "e^(2x) - 1", "(e^(2x) + 1)^2 * (e^(2x) - 1)")]
        // A factor shared under a power of a product.
        [InlineData("1", "((e^(2x) - 1) * (e^(2x) + 1))^2", "x", "(e^(2x) + 1)^2 * (e^(2x) - 1)", "(e^(2x) - 1)^2 * (e^(2x) + 1)^2")]
        // The same factor written as a sum with -1 in one and a difference in the other.
        [InlineData("x", "e^(2x) + -1", "1", "(e^(2x) - 1) * (e^(2x) + 1)^2", "(e^(2x) - 1) * (e^(2x) + 1)^2")]
        public void GoesOverTheLeastCommonMultiple(string leftNum, string leftDen, string rightNum, string rightDen, string multiple)
        {
            var (ln, ld, rn, rd) = (leftNum.ToEntity(), leftDen.ToEntity(), rightNum.ToEntity(), rightDen.ToEntity());
            var sum = Patterns.SumOfFractions(ln / ld + rn / rd, ln, ld, rn, rd);
            var denominator = Assert.IsType<Entity.Divf>(sum).Divisor;
            var ratio = denominator / multiple.ToEntity();
            var first = Value(ratio, Points[0]);
            foreach (var point in Points)
            {
                Assert.Equal(first, Value(ratio, point), 9);
                Assert.Equal(Value(ln / ld + rn / rd, point), Value(sum, point), 9);
            }
        }

        // Denominators that share nothing are still cross-multiplied, as before.
        [Fact]
        public void LeavesDenominatorsThatShareNothing()
        {
            var (ln, ld, rn, rd) = ("1".ToEntity(), "x + 1".ToEntity(), "x".ToEntity(), "x - 2".ToEntity());
            var sum = Patterns.SumOfFractions(ln / ld + rn / rd, ln, ld, rn, rd);
            Assert.Equal((rd * ld).Simplify(), Assert.IsType<Entity.Divf>(sum).Divisor);
        }

        private static double Value(Entity expr, double at)
            => (double)((Entity.Number.Real)expr.Substitute("x", at).EvalNumerical()).RealPart;
    }
}
