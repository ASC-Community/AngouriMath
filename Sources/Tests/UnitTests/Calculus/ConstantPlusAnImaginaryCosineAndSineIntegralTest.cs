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
    /// <c>a + b cos(x) ± i b sin(x)</c> is <c>a + b e^(±i x)</c>, and with the cosine and sine above
    /// it written as exponentials too the integrand is rational in <c>e^(i x)</c>. Rubi's 4.7.7.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>Compared as complex numbers, the integrand being complex.</remarks>
    [Trait("Area", "Calculus")]
    public sealed class ConstantPlusAnImaginaryCosineAndSineIntegralTest
    {
        [Theory]
        [InlineData("(k + q*cos(x))/(a + b*cos(x) + i*b*sin(x))")]
        [InlineData("(k + q*sin(x))/(a + b*cos(x) - i*b*sin(x))")]
        [InlineData("(k + p*cos(x) + q*sin(x))/(a + b*cos(x) + i*b*sin(x))")]
        [InlineData("cos(x)/(a + b*cos(x) + i*b*sin(x))")]
        public void IsRationalInTheExponential(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.9)
                .Substitute("k", 1.1).Substitute("p", 0.4).Substitute("q", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -2.3, -1.1, -0.4, 0.4, 1.1, 2.3 })
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
