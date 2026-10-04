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
    /// An even root of a quotient with an odd power below the bar, written apart: below the bar
    /// the power's phase counts against those above it, so <c>sqrt((1 + x)/x^3)</c>, real for
    /// <c>x &lt; -1</c> as well, is not <c>sqrt(1 + x) x^(-3/2)</c> there, and was integrated as
    /// though it were -- right for <c>x &gt; 0</c> and wrong for every <c>x &lt; -1</c>. Each row is
    /// checked at the points where the integrand is real, on both sides.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class RootOfAQuotientOnBothSidesIntegralTest
    {
        [Theory]
        [InlineData("sqrt((1 + x)/x^3)")]
        [InlineData("((1 + x)/x^3)^(3/2)")]
        [InlineData("sqrt(x/(1 + x)^3)")]
        [InlineData("sqrt((2 + x)/(1 + x)^3)")]
        [InlineData("x^2*sqrt(x/(1 + x)^3)")]
        public void RightOnBothSides(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var below = 0;
            var above = 0;
            foreach (var at in new[] { -2.7, -2.3, -1.9, -1.3, 0.31, 0.83, 1.77 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || Math.Abs((double)want.ImaginaryPart) > 1e-12)
                    continue;
                if (at < 0) below++; else above++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(below >= 2 && above >= 3, $"only {below} points below zero and {above} above it could be compared for {integrand}");
        }
    }
}
