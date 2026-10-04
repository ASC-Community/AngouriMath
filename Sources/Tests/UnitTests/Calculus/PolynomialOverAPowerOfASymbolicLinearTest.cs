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
    /// A polynomial over a power of one linear with a symbol in it, written in powers of the
    /// linear at its root: <c>t^9/(a + b t)^8</c> is a polynomial and eight powers of
    /// <c>1/(a + b t)</c>. Divided out and decomposed with the symbols in it, that one took
    /// forty-six seconds, and <c>x^4/(a + b sqrt(x))^8</c>, which is it under <c>x = t^2</c>,
    /// a minute; the sizes here are the ones that were slow, so that a regression shows as a
    /// slow suite rather than as a clock in a test.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class PolynomialOverAPowerOfASymbolicLinearTest
    {
        [Theory]
        [InlineData("x^9/(a + b*x)^8")]
        [InlineData("x^6/(a + b*x)^5")]
        [InlineData("x^9/(a + x)^8")]
        [InlineData("(c + d*x)^3/(a + b*x)^5")]
        [InlineData("x^4/(a + b*sqrt(x))^8")]
        public void InPowersOfTheLinear(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.7).Substitute("c", 1.3).Substitute("d", 1.7);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -1.7, -0.9, 0.3, 0.8, 1.6, 2.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || Math.Abs((double)want.ImaginaryPart) > 1e-12)
                    continue;
                compared++;
                var got = derivative.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 3, $"only {compared} points could be compared for {integrand}");
        }
    }
}
