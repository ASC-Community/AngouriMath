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
    /// The binomial differential <c>x^m (a + b x^n)^(p/q)</c> with symbols for <c>a</c> and
    /// <c>b</c>, with a power of <c>x</c> that is not whole, or with a power of a multiple of
    /// <c>x</c> in place of the power of <c>x</c>. Chebyshev's theorem is about the exponents,
    /// and each of these was declined.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back with <c>a = 2.3</c>, <c>b = 0.7</c> and the given
    /// <c>c</c>, at points where the integrand is real -- on both sides of zero wherever it is
    /// real on both, an odd root of a negative being real here.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class SymbolicBinomialDifferentialTest
    {
        private static void DifferentiatesBack(string integrand, double c, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.7).Substitute("c", c);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)want.ImaginaryPart) < 1e-12, $"the integrand at x = {at} is {want}, not real");
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// A whole power of <c>x</c> beside a root of a binomial with symbols in it, in the third
        /// case: <c>x^2/(a + b x^4)^(3/4)</c> is <c>(m + 1)/n + p/q = 0</c>.
        /// </summary>
        [Theory]
        [InlineData("x^2/(a + b*x^4)^(3/4)")]
        [InlineData("(a + b*x^4)^(1/4)/x^2")]
        [InlineData("x^6*(a + b*x^4)^(1/4)")]
        public void SymbolsInTheBinomial(string integrand)
            => DifferentiatesBack(integrand, 1.3, new[] { -1.4, -0.6, 0.5, 1.2 });

        /// <summary>
        /// A power of <c>x</c> that is not whole: <c>x^(7/3) (a + b x^2)^(1/3)</c> is the third
        /// case, <c>(m + 1)/n + p/q = 2</c>.
        /// </summary>
        [Theory]
        [InlineData("x^(7/3)*(a + b*x^2)^(1/3)")]
        [InlineData("x^(1/3)*(a + b*x^2)^(1/3)")]
        public void APowerOfXThatIsNotWhole(string integrand)
            => DifferentiatesBack(integrand, 1.3, new[] { -1.4, -0.6, 0.3, 0.7, 1.9 });

        /// <summary>
        /// A power of a multiple of <c>x</c>, read as the power of <c>x</c> times
        /// <c>(c x)^r/x^r</c>, which stands outside: checked for a positive <c>c</c> on the
        /// positive side, and for a negative one on the negative side, where <c>c x</c> is
        /// positive again.
        /// </summary>
        [Theory]
        [InlineData("(c*x)^(7/3)/(a + b*x^2)^(2/3)", 1.3, new[] { 0.3, 0.7, 1.2, 1.6 })]
        [InlineData("(c*x)^(7/3)/(a + b*x^2)^(2/3)", -1.3, new[] { -1.6, -1.2, -0.7, -0.3 })]
        [InlineData("(c*x)^(5/2)/(a - b*x^2)^(3/4)", 1.3, new[] { 0.3, 0.7, 1.2, 1.6 })]
        [InlineData("(c*x)^(5/2)/(a - b*x^2)^(3/4)", -1.3, new[] { -1.6, -1.2, -0.7, -0.3 })]
        [InlineData("(a - b*x^2)^(1/4)/(c*x)^(15/2)", 1.3, new[] { 0.3, 0.7, 1.2, 1.6 })]
        [InlineData("(a - b*x^2)^(1/4)/(c*x)^(15/2)", -1.3, new[] { -1.6, -1.2, -0.7, -0.3 })]
        public void APowerOfAMultipleOfX(string integrand, double c, double[] points)
            => DifferentiatesBack(integrand, c, points);
    }
}
