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
    /// A product of trigonometric powers whose powers of the sine and cosine cancel,
    /// <c>cos(x)^2 sec(x)^2</c>, is the constant wherever it is defined, and its integral the
    /// constant times x: it was declined, and with it <c>(1 + cos(x)^2) sec(x)^2</c>, Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class TrigonometricTimesItsReciprocalIntegralTest
    {
        [Theory]
        [InlineData("sin(x)*csc(x)")]
        [InlineData("cos(x)^2*sec(x)^2")]
        [InlineData("tan(x)*cot(x)")]
        [InlineData("(1 + cos(x)^2)*sec(x)^2")]
        [InlineData("csc(x)^2*(1 + sin(x)^2)")]
        public void IsTheConstantTimesX(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in new[] { -2.3, -1.1, -0.4, 0.4, 1.1, 2.3 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>The constant times x itself, not a function that is x only on one interval.</summary>
        [Theory]
        [InlineData("cos(x)^2*sec(x)^2")]
        [InlineData("tan(x)*cot(x)")]
        public void IsX(string integrand)
            => Assert.Equal(MathS.Var("x"), integrand.ToEntity().Integrate("x").Substitute("C", 0).InnerSimplified);
    }
}
