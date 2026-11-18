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
    /// An antiderivative is checked by differentiating it back at sampled points, to 1e-9 of the
    /// values' size, or of one where both are smaller. An integrand of size 1e-12 agreed with any
    /// derivative as small, so a rule that read the integrand's powers through an absolute tolerance
    /// as well, and took a wrong candidate, had its answer passed: <c>10^-12 x sqrt(sin(x)^2)</c>
    /// integrated to <c>x/352000000000 cos(x) (sin(x)^2)^(3/2) - ...</c>, whose derivative is not the
    /// integrand anywhere. The integrand is checked at its own size now. Found by integrating Rubi's
    /// problems as written and times 10^-12, where 11 of 402 were wrong; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASmallIntegrandIsCheckedAtItsOwnSizeIntegralTest
    {
        [Theory]
        [InlineData("x * sqrt(sin(x)^2)", "10^(-12)")]
        [InlineData("x * sqrt(cos(x)^2)", "10^(-12)")]
        [InlineData("x * sqrt(sin(x)^2)", "10^(-20)")]
        [InlineData("x * sqrt(sin(x)^2)", "1")]
        public void TheDerivativeIsTheIntegrand(string integrand, string scale)
        {
            var scaled = scale.ToEntity() * integrand.ToEntity();
            var integral = scaled.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x") / scale.ToEntity();
            var original = integrand.ToEntity();
            foreach (var at in new[] { 0.7, 1.3, 2.1, 4.0 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {scale} {integrand}, over {scale}, is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
