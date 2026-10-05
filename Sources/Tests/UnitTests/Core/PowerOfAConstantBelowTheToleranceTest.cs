//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A product, a quotient or a power of nonzero numbers is not zero however small it is.
    /// Evaluation rounds a value within <c>1e-50</c> of an integer onto it, and the exact zero
    /// that made of <c>1/pi^136</c> was taken as its value.
    /// https://github.com/asc-community/AngouriMath/issues/1769
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class PowerOfAConstantBelowTheToleranceTest
    {
        private static double Value(Entity expr) => (double)((Number.Complex)expr.EvalNumerical()).RealPart;

        // With a rational for each constant, both sides are exact numbers.
        private static Entity Exactly(Entity expr) => expr.Substitute(MathS.pi, 3).Substitute(MathS.e, 2).Evaled;

        [Theory]
        [InlineData("1 / pi ^ 136")]
        [InlineData("(pi ^ 136) ^ (-1)")]
        [InlineData("pi ^ (-136) * pi ^ (-136)")]
        [InlineData("e ^ (-160)")]
        [InlineData("pi ^ (-136) + pi ^ (-137)")]
        [InlineData("-pi ^ (-136) - e ^ (-160)")]
        public void ItIsNotRoundedToZero(string written)
        {
            var expr = written.ToEntity();
            var simplified = expr.InnerSimplified;
            Assert.NotEqual(Number.Integer.Zero, simplified);
            Assert.Equal(Exactly(expr), Exactly(simplified));
        }

        [Fact]
        public void NorIsAPowerOfARoot()
            => Assert.NotEqual(Number.Integer.Zero, "sqrt(2) ^ (-400)".ToEntity().InnerSimplified);

        // What the rounding is for, the residual of a cancellation, is still zero.
        [Theory]
        [InlineData("sqrt(2) ^ 2 - 2")]
        [InlineData("pi ^ (-136) - pi ^ (-136)")]
        public void ACancellationIsStillZero(string written)
            => Assert.Equal(Number.Integer.Zero, written.ToEntity().InnerSimplified);

        // The common denominator raises pi to the 136th power on the way, and the expression
        // simplified to zero.
        [Fact]
        public void SimplifyKeepsTheValueOfAQuotientByAPowerOfPi()
        {
            var expr = "x ^ n * ((1 - d ^ 2) / x - x) ^ 3 * (1 + x ^ 2 - d * x) / (x - d) / pi ^ 2".ToEntity();
            static Entity At(Entity e) => e.Substitute("x", 1.3).Substitute("d", 0.7).Substitute("n", 0.37);
            var expected = Value(At(expr));
            Assert.True(Math.Abs(expected + 0.24771026954308137) < 1e-12, $"the expression is {expected} there");
            Assert.True(Math.Abs(Value(At(expr.Simplify())) - expected) < 1e-12);
        }
    }
}
