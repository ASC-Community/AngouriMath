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
    /// A half-odd power of a constant over a polynomial whose sign changes, written apart with the
    /// factor that keeps it right on both sides of the polynomial's zeros: <c>(A/Q)^r</c> is
    /// <c>(A/Q)^r Q^r</c> times <c>Q^(-r)</c>, and for a positive number <c>A</c> that factor is
    /// <c>A^r sgn(Q)</c>. <c>sqrt(1/(1 - x^2))</c>, the arcsine's derivative where it is real, was
    /// declined.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 2.3</c>, <c>b = -0.7</c>, <c>c = 1.3</c>, so that
    /// <c>a + b x^2</c> changes sign as well, at points on both sides of every denominator's zeros;
    /// where the denominator is negative the integrand is imaginary, and the two are compared as
    /// complex numbers.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class HalfOddPowerOfAReciprocalIntegralTest
    {
        [Theory]
        [InlineData("sqrt(1/(1 - x^2))")]
        [InlineData("sqrt(1/(x^2 - 1))")]
        [InlineData("(1 - x^2)*sqrt(1/(2 - x^2))")]
        [InlineData("(1/(1 - x^2))^(3/2)")]
        [InlineData("(c/(a + b*x^2))^(3/2)")]
        [InlineData("x^2*(c/(a + b*x^2))^(3/2)")]
        public void IsWrittenApartWithItsSign(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", -0.7).Substitute("c", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.4, -1.6, -0.6, 0.6, 1.6, 2.4 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
