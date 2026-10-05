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
    /// Powers of two linears whose exponents sum to a whole number below <c>-2</c>, beside a
    /// polynomial of degree at most two less than its negative: under <c>t = L1/L2</c> a power of
    /// <c>t</c> beside a polynomial, so the antiderivative is the two powers times a polynomial.
    /// Rubi's <c>(a + b x)^m (c + d x)^n</c> files.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class TwoLinearPowersIntegralTest
    {
        [Theory]
        [InlineData("(a + b*x)^m*(c + d*x)^(-3 - m)")]
        [InlineData("x^(n - 4)/(a + b*x)^n")]
        [InlineData("(a + b*x)^m*(c + d*x)^(-5 - m)*(p + q*x)^3")]
        [InlineData("(a + b*x)*(c + d*x)^(n - 4)/(p + q*x)^n")]
        [InlineData("(a + b*x)^m*(c + d*x)^(-4 - m)*(p + q*x)*(g + h*x)")]
        [InlineData("(a + b*x)^(5/2)/(c + d*x)^(11/2)")]
        public void UnderTheQuotientOfTheLinears(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.9).Substitute("c", 0.7)
                .Substitute("d", 1.1).Substitute("g", 0.8).Substitute("h", 0.6).Substitute("m", 0.37)
                .Substitute("n", 1.21).Substitute("p", 0.4).Substitute("q", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.4, -1.5, 0.3, 0.8, 1.5, 2.4 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// One linear twice is not two: `x + a/b` beside `a c + b c x` has a cross term
        /// `a/b (b c) - a c` that is zero with symbols in it, and the formula divided by it, for an
        /// answer with no value anywhere. Rubi's 1.2.1.3:2459.
        /// https://github.com/asc-community/AngouriMath/issues/1793
        /// </summary>
        [Theory]
        [InlineData("(a*c + b*c*x)^(-3 - 2*p)*(f + g*x)*(a^2 + 2*a*b*x + b^2*x^2)^p")]
        [InlineData("(x + a/b)^(2*p)*(a*c + b*c*x)^(-3 - 2*p)*(f + g*x)")]
        public void OneLinearTwiceHasAValue(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 1.1)
                .Substitute("f", 0.3).Substitute("g", 1.7).Substitute("p", 0.29);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.8, 1.5, 2.4 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.False(got.IsNaN, $"the antiderivative of {integrand} has no derivative at x = {at}: {integral}");
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart) < 1e-9 * Math.Max(1.0, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
