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
    /// A power that is not whole of <c>csc(x) - sin(x)</c> or <c>sec(x) - cos(x)</c>, which are
    /// <c>cos(x)^2/sin(x)</c> and <c>sin(x)^2/cos(x)</c>: written so, a product of powers of the
    /// sine and cosine, read through the tangent. Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Compared as complex numbers on both sides of the zeros of the sine and the cosine, where the
    /// integrands are complex on some of the intervals.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class DifferenceOfReciprocalFunctionsIntegralTest
    {
        [Theory]
        [InlineData("(csc(x) - sin(x))^(5/2)")]
        [InlineData("(csc(x) - sin(x))^(3/2)")]
        [InlineData("1/(csc(x) - sin(x))^(1/2)")]
        [InlineData("1/(csc(x) - sin(x))^(7/2)")]
        [InlineData("(sec(x) - cos(x))^(5/2)")]
        [InlineData("1/(sec(x) - cos(x))^(3/2)")]
        [InlineData("(sin(x) - csc(x))^(3/2)")]
        public void IsAProductOfPowersOfTheSineAndCosine(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in new[] { -2.3, -1.1, -0.4, 0.4, 1.1, 1.9, 2.6 })
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
