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
    /// A rational function of <c>tan(z)</c> beside a power of <c>S = a ± i a tan(z)</c>, integrated
    /// in <c>S</c>, where <c>dz = c dS/(S (S - 2a))</c>: the factors are <c>S</c>, <c>S - 2a</c> and
    /// <c>S - a</c>, with no imaginary root among them. Beside <c>q - i q tan(z)</c>, a linear in
    /// <c>S</c>, the sum under a power that is not whole, a fraction or a symbol, is the variable.
    /// Rubi's 4.3.3.1 and 4.3.2.1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>Compared as complex numbers, the integrand being complex.</remarks>
    [Trait("Area", "Calculus")]
    public sealed class RationalInTheTangentBesideAnImaginarySumIntegralTest
    {
        [Theory]
        [InlineData("cot(c + d*x)^3*(k + q*tan(c + d*x))/(a + i*a*tan(c + d*x))")]
        [InlineData("cot(c + d*x)*(k + q*tan(c + d*x))/(a + i*a*tan(c + d*x))^2")]
        [InlineData("tan(c + d*x)^2*(k + q*tan(c + d*x))/sqrt(a + i*a*tan(c + d*x))")]
        [InlineData("(k + q*tan(c + d*x))/(a - i*a*tan(c + d*x))^(3/2)")]
        [InlineData("cot(c + d*x)^2*(k + q*tan(c + d*x))/(a + i*a*tan(c + d*x))^4")]
        [InlineData("(a + i*a*tan(c + d*x))/(q - i*q*tan(c + d*x))^(3/2)")]
        [InlineData("sqrt(q - i*q*tan(c + d*x))/(a + i*a*tan(c + d*x))")]
        [InlineData("(a + i*a*tan(c + d*x))^2/sqrt(q - i*q*tan(c + d*x))")]
        // A symbol for the power of one sum, a whole power of the other.
        [InlineData("(a + i*a*tan(c + d*x))^m*(q - i*q*tan(c + d*x))^4")]
        [InlineData("(a + i*a*tan(c + d*x))^m*(q - i*q*tan(c + d*x))")]
        [InlineData("(a + i*a*tan(c + d*x))^2*(k + q*tan(c + d*x))*(q - i*q*tan(c + d*x))^n")]
        public void IsIntegratedInTheSum(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("c", 0.4).Substitute("d", 1.1)
                .Substitute("k", 1.1).Substitute("q", 0.6).Substitute("m", 1.7).Substitute("n", 0.6);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.1, -0.6, 0.3, 0.9 })
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
