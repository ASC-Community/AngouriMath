//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A rational function of the tangent of a linear argument with symbols for its
    /// coefficients, Rubi's 4.3.3.1 and 4.3.4.2. The substitution search reaches the tangent
    /// through <c>u = g + h x</c>, and under a linear candidate the quotient by its derivative is
    /// the integrand with its argument renamed over a constant: there is nothing in it to
    /// simplify, and simplifying it with nine symbols in it was most of the time each of these
    /// took.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with the symbols pinned after the integral is taken, at
    /// points where the tangent and every factor below the bar stay away from their poles and
    /// zeros.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class LinearArgumentSubstitutionTest
    {
        private static readonly double[] Points = { 0.2, 0.5, 0.8 };

        [Theory]
        [InlineData("(a + b*tan(g + h*x))*(A + B*tan(g + h*x) + K*tan(g + h*x)^2)/(c + d*tan(g + h*x))^2")]
        [InlineData("(a + b*tan(g + h*x))*(A + B*tan(g + h*x))/(c + d*tan(g + h*x))^2")]
        [InlineData("(A + B*tan(g + h*x) + K*tan(g + h*x)^2)/((a + b*tan(g + h*x))*(c + d*tan(g + h*x)))")]
        public void ARationalFunctionOfTheTangentOfALinear(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 2.1).Substitute("d", 0.9)
                .Substitute("g", 0.3).Substitute("h", 1.1).Substitute("A", 0.5).Substitute("B", 1.7).Substitute("K", 0.4);
            var derivative = Pin(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var point in Points)
            {
                var expected = original.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                var actual = derivative.Substitute("x", point).EvalNumerical().RealPart.EDecimal.ToDouble();
                Assert.True(Math.Abs(expected - actual) < 1e-8 * Math.Max(1, Math.Abs(expected)),
                    $"d/dx of the antiderivative of {integrand} is {actual} at x = {point}, where the integrand is {expected}");
            }
        }
    }
}
