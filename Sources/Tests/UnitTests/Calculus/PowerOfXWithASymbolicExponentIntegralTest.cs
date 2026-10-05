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
    /// A function of <c>x^n</c> for a symbolic <c>n</c> beside a power of <c>x</c>,
    /// <c>x^m R(x^n)</c> with <c>(m + 1)/n</c> rational, under <c>u = x^(n/d)</c>: Rubi's 1.2.3.2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 1.3</c>, <c>b = 0.7</c>, <c>c = 0.4</c> and
    /// <c>n = 1.7</c>, at positive <c>x</c>, where a symbolic power of it is real.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfXWithASymbolicExponentIntegralTest
    {
        [Theory]
        [InlineData("x^(-1 + 4*n)/(a + b*x^n + c*x^(2*n))")]
        [InlineData("x^(-1 + n)/(b*x^n + c*x^(2*n))")]
        [InlineData("1/(x*(a + b*x^n + c*x^(2*n)))")]
        // Under u = x^(n/2).
        [InlineData("x^(-1 + n/2)/(a + b*x^n + c*x^(2*n))")]
        [InlineData("sqrt(a + b*x^n + c*x^(2*n))/x")]
        // A perfect square, which stays one in u: its coefficients are not named apart.
        [InlineData("1/(x*(a^2 + 2*a*b*x^n + b^2*x^(2*n))^(3/2))")]
        public void IsIntegratedInAPowerOfX(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// <c>coth(a + b ln(c x^n))^3/x</c> is written through the exponential form as a rational
        /// function of <c>x^(2 n b)</c> with <c>(e^a)^2 (c^b)^2</c> in it, and its integral in
        /// <c>u</c> is asked with that constant named: carried through the partial fractions as
        /// written it was past half a minute, where named it is under one. A regression makes
        /// this crawl rather than fail.
        /// </summary>
        [Theory]
        [InlineData("coth(a + b*ln(c*x^n))^3/x")]
        [InlineData("tanh(a + b*ln(c*x^n))^3/x")]
        public void ACompoundConstantIsNamedInThePower(string integrand) => DifferentiatesBack(integrand);

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 0.4).Substitute("n", 1.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.8, 1.4, 2.1 })
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
