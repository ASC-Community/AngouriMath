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
    /// A product of powers of the trigonometric functions of one argument, some not whole, which
    /// is <c>sin^M cos^N</c> times a constant on every interval where both are continuous, with
    /// <c>M + N</c> even, the tangent's form <c>tan^M (1 + tan^2)^(-(M + N)/2)</c>, or with
    /// <c>M</c> or <c>N</c> an odd whole number, the plain product. Compared as complex numbers
    /// on both sides of the zeros, where the integrands leave the reals and the constant is what
    /// keeps the answer right. Rubi's 4.1.0 to 4.6.0.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ProductOfTrigonometricPowersIntegralTest
    {
        [Theory]
        [InlineData("sqrt(c*sin(x))/sqrt(d*cos(x))")]
        [InlineData("(d*csc(x))^(3/2)*sqrt(c*sec(x))")]
        [InlineData("(a*sin(x))^(5/2)*sqrt(b*sec(x))")]
        [InlineData("(b*tan(x)^3)^(3/2)")]
        [InlineData("sqrt(c*sin(a + b*x))/(d*cos(a + b*x))^(9/2)")]
        [InlineData("cos(x)^(7/2)/sin(x)^(7/2)")]
        [InlineData("(d*sec(x))^(5/2)*sqrt(b*tan(x))")]
        [InlineData("sqrt(b*tan(x))/(a*sin(x))^(3/2)")]
        public void AsPowersOfTheSineAndTheCosine(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 0.4).Substitute("b", 1.1).Substitute("c", 1.3).Substitute("d", 0.8);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -2.6, -1.1, -0.4, 0.3, 0.8, 1.2, 2.0 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 6, $"only {compared} points could be compared for {integrand}");
        }
    }
}
