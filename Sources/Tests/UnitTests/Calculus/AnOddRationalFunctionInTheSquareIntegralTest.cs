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
    /// An odd rational function with symbols in it is integrated in <c>u = x^2</c> before the
    /// partial fractions, which in x took a minute or more. Rubi's 1.2.2.4 and 1.2.2.5.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AnOddRationalFunctionInTheSquareIntegralTest
    {
        [Theory]
        [InlineData("(A + B*x^2)/(x*(a + b*x^2 + c*x^4)^3)")]
        [InlineData("1/(x^5*(d + g*x^2)*(a + b*x^2 + c*x^4))")]
        [InlineData("(g*x + f*x^3)/(a + b*x^2 + c*x^4)^3")]
        public void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2).Substitute("b", 3).Substitute("c", 5)
                .Substitute("d", -1).Substitute("g", 7).Substitute("f", 1).Substitute("A", 4).Substitute("B", -2);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { -1.7, -0.6, 0.45, 1.3 })
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
