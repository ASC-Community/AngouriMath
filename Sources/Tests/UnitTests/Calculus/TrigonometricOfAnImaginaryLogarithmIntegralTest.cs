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
    /// A trigonometric function of <c>a + b ln(u)</c> with an imaginary number for <c>b</c>, in
    /// exponentials: <c>e^(i (a + b ln(u)))</c> is <c>e^(i a)</c> times a real power of
    /// <c>u</c>. The closed form for a power of the variable times a sine or cosine of a logarithm
    /// divides by zero on exactly these, Rubi's 4.7.5. The integrands are complex, and compared as
    /// complex numbers.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class TrigonometricOfAnImaginaryLogarithmIntegralTest
    {
        [Theory]
        [InlineData("sin(a + ln(c*x^2)*sqrt(-1/4))")]
        [InlineData("tan(a + i*ln(x))")]
        [InlineData("cot(a + i*ln(x))/x^2")]
        [InlineData("1/cos(a - 2*i*ln(c*x))^(3/2)")]
        [InlineData("x^2*cos(a + i*ln(x))^2")]
        public void InExponentials(string integrand) => DifferentiatesBack(integrand, integrand.ToEntity().Integrate("x"));

        /// <summary>
        /// Beside its own derivative the logarithm is left to the substitution:
        /// <c>tan(a + i ln(x))/x</c> is <c>tan(a + i u)</c> in <c>u = ln(x)</c>, answered in a
        /// closed form, where the exponentials would answer it as a piecewise in <c>e^(i a)</c>.
        /// </summary>
        [Theory]
        [InlineData("tan(a + i*ln(x))/x")]
        [InlineData("cot(a + i*ln(x))/x")]
        public void BesideItsOwnDerivativeByTheSubstitution(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("piecewise", integral.Stringize());
            DifferentiatesBack(integrand, integral);
        }

        private static void DifferentiatesBack(string integrand, Entity integral)
        {
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 0.4).Substitute("c", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.8, 1.5, 2.4 })
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
