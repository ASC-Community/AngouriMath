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
    /// A piecewise case whose predicate evaluates to NaN -- an order comparison of a number off
    /// the real line, <c>i - 1 &lt; 0</c> -- is never taken, as a false one is not, and the cases
    /// after it are read as they would be. Kept, it made the whole piecewise NaN.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class UndefinedPiecewiseConditionTest
    {
        private static void Near(double expected, Entity value)
            => Assert.True(Math.Abs((double)((Number.Complex)value.EvalNumerical()).RealPart - expected) < 1e-12, $"{value} is not {expected}");

        [Fact]
        public void TheCaseIsSkippedAndTheRestKept()
        {
            var simplified = "piecewise(2 * x provided not x = 0, 3 * x ^ 2 provided i - 1 < 0)".ToEntity().InnerSimplified;
            Assert.False(simplified.Evaled.IsNaN);
            Near(0.6, simplified.Substitute("x", 0.3));
        }

        [Fact]
        public void AnUndefinedCaseBeforeADecidedOneIsPassedOver()
            => Assert.Equal(Number.Integer.Create(2), "piecewise(1 provided i < 0, 2 provided 1 > 0)".ToEntity().InnerSimplified);

        [Fact]
        public void WithNoCaseThatHoldsItIsStillNaN()
            => Assert.True("piecewise(1 provided i < 0, 2 provided i > 0)".ToEntity().InnerSimplified.Evaled.IsNaN);

        // How an antiderivative with an arm for each sign of a quantity off the real line is
        // differentiated back: the arm for the negative sign is undefined there, and the
        // derivative was NaN where it is that of the arm which holds.
        [Fact]
        public void TheDerivativeIsThatOfTheCaseThatHolds()
        {
            var derivative = "piecewise(x ^ 2 provided not x = 0, x ^ 3 provided -13/10 * i - 3/5 < 0)".ToEntity().Differentiate("x");
            Near(0.6, derivative.Substitute("x", 0.3));
        }
    }
}
