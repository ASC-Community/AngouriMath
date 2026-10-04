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
    /// A polynomial times an odd half power of a quadratic whose coefficients are quotients or
    /// products of symbols. The reduction solves a linear system in those coefficients, and
    /// carried as written they came back in answers of 40,000 to 480,000 characters, after up to
    /// eight seconds; named for the solve and written back, the answers are a thousand. The length is
    /// what is checked, beside the derivative, since a time is not something a test can hold.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class CompoundCoefficientsOfAQuadraticIntegralTest
    {
        [Theory]
        [InlineData("sqrt(a + b*x + g^2*x^2/f^2)")]
        [InlineData("d + g*x + f*sqrt(a + b*x + g^2*x^2/f^2)")]
        [InlineData("x/sqrt(a + b*x + g^2*x^2/f^2)")]
        [InlineData("(a + b*x + c*x^2/f)^(3/2)")]
        [InlineData("x^2*sqrt(a*f + b*x + c*x^2/f)")]
        public void IsAnsweredShortly(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            var written = integral.Stringize();
            Assert.DoesNotContain("integral(", written);
            Assert.True(written.Length < 5000, $"the antiderivative of {integrand} is {written.Length} characters long");
            Entity Pinned(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 0.6)
                .Substitute("d", 1.9).Substitute("f", 1.1).Substitute("g", 0.8);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.8, 1.5, 2.4 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
