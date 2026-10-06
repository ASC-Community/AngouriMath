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
    /// A polynomial in <c>x^2</c> over a power of a linear in <c>x^2</c> and a biquadratic, split in
    /// <c>x^2</c> minus the linear's root, the powers of <c>1/(x^2 - r)</c> by their reduction to an
    /// arctangent. Under the tangent and <c>t = sqrt(c + d u)</c>, Rubi's
    /// <c>sqrt(c + d tan(x))/(a + b tan(x))^n</c> comes to one; split in <c>t</c>, the answer to the
    /// third row ran to 3.5 million characters. Rubi's 4.3.2.1 and 4.3.4.2.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Real where <c>tan(x)</c>, <c>a + b tan(x)</c> and <c>c + d tan(x)</c> are positive, which the
    /// points are. The pinned answer is differentiated as it is.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfALinearInTheSquareBesideABiquadraticIntegralTest
    {
        [Theory]
        [InlineData("x^2/((k + x^2)^2*(a + b*x^2 + c*x^4))")]
        [InlineData("sqrt(c + d*x)/((a + b*x)^3*(1 + x^2))")]
        [InlineData("sqrt(c + d*tan(x))*(A + B*tan(x) + M*tan(x)^2)/(a + b*tan(x))^3")]
        [InlineData("(c + d*tan(x))^(3/2)/(a + b*tan(x))^3")]
        public void SplitInTheSquareLessTheRoot(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length < 10000, $"{text.Length} characters of answer for {integrand}");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 1.1).Substitute("d", 0.6)
                .Substitute("k", 0.9).Substitute("A", 0.4).Substitute("B", 1.1).Substitute("M", 0.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.2, 0.5, 0.9, 1.2 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
