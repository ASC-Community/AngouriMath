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
    /// A polynomial in <c>x^2</c> over <c>x^(2k)</c> times a biquadratic with a symbol in it, a power
    /// of <c>x</c> on both sides cancelled first, split in <c>w = x^2</c>: the terms in
    /// <c>w^(-j)</c> are the expansion of the remainder over the biquadratic at <c>w = 0</c>, and
    /// what is left is the biquadratic's own. A half-odd power of the cotangent beside one of
    /// <c>a + b tan(x)</c> comes to one under <c>u = tan(x)</c> and the root of the quotient
    /// <c>t = sqrt(u/(a + b u))</c>; split in <c>t</c>, the answer to the last row ran to a million
    /// characters. Rubi's 4.3.2.1 and 4.3.3.1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// The cotangent's rows are real where <c>tan(x)</c> and <c>a + b tan(x)</c> are positive,
    /// which the points are. The pinned answer is differentiated as it is, since simplifying
    /// answers of this shape costs minutes.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class EvenQuotientOverAPowerOfXAndABiquadraticIntegralTest
    {
        [Theory]
        [InlineData("1/(x^2*(a + b*x^2 + c*x^4))")]
        [InlineData("(1 + x^10)/(x^2*(a + b*x^2 + c*x^4))")]
        [InlineData("2*a*(1 - b*x^2)^4*x/(x^5*(1 - 2*b*x^2 + (a^2 + b^2)*x^4))")]
        [InlineData("cot(x)^(5/2)/(a + b*tan(x))^(3/2)")]
        [InlineData("cot(x)^(9/2)*sqrt(a + b*tan(x))*(A + B*tan(x))")]
        [InlineData("cot(x)^(13/2)*(a + b*tan(x))^(5/2)*(A + B*tan(x))")]
        public void SplitInTheSquare(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var text = integral.Stringize();
            Assert.DoesNotContain("integral(", text);
            Assert.True(text.Length < 5000, $"{text.Length} characters of answer for {integrand}");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 2.9)
                .Substitute("A", 0.4).Substitute("B", 1.1);
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
