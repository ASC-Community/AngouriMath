//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// <c>acsch(c x)</c> and <c>asech(c x)</c> with a symbolic <c>c</c>: the parser spells them
    /// <c>ln(1/(c x) + sqrt(1/(c x)^2 ± 1))</c>, and three things stood between that and an
    /// answer -- <c>(c x)^2</c> read as a power of something that is not a polynomial, the
    /// root of <c>(1 ± c^2 x^2)/(c^2 x^2)</c> not written apart because <c>c^2</c> was not a
    /// number, and <c>|c|</c> differentiated to <c>derivative(|c|, x)</c> by parts -- where the
    /// same rows with <c>c = 2</c> were answered. Rubi's 7.5.1 and 7.6.1.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class InverseHyperbolicSecantIntegralTest
    {
        /// <summary>Where <c>asech(3 x / 2)</c> is real, <c>0 &lt; c x &lt;= 1</c>, and <c>acsch</c> too.</summary>
        private static readonly double[] Points = { 0.15, 0.25, 0.4, 0.55, 0.62 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            // A condition on a parameter, `c^2 > 0`, holds at the c the rows are checked at.
            var bare = integral.Substitute("C", 0).Substitute("c", "3/2".ToEntity());
            var derivative = bare.Differentiate("x");
            var original = integrand.ToEntity().Substitute("c", "3/2".ToEntity());
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        [Theory]
        [InlineData("x*acsch(c*x)")]
        [InlineData("x^2*acsch(c*x)")]
        [InlineData("x^2*asech(c*x)")]
        [InlineData("x/sqrt(1 + 1/(c*x)^2)")]
        public void AnInverseHyperbolicSecantOrCosecantWithASymbolicCoefficient(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// Rubi's 7.5.1 with a symbolic linear or quadratic below the bar: <c>(a + b asech(c x))/(d + e x)^2</c>
        /// ran for ten minutes without an answer. By parts leaves <c>1/(x^2 (d + e x) sqrt(1/(c x)^2 - 1))</c>,
        /// which was handed on as a product to the power -1 that no radical rule reads, its
        /// <c>(c x)^2</c> distributed one level down past the rule that writes the root apart,
        /// and, once written apart with <c>|x|</c> for <c>x &gt; 0</c>, declined for having no
        /// parity to extend by -- <c>sgn(x)</c> stands in for it now. Pinned with <c>a = 1/3</c>,
        /// <c>b = 1/2</c>, <c>c = 3/2</c>, <c>d = 2/3</c>, <c>e = 5/4</c>.
        /// </summary>
        [Theory]
        [InlineData("(a+b*asech(c*x))/(d+pe*x)^2")]
        [InlineData("(a+b*asech(c*x))/x^7")]
        [InlineData("x*(a+b*asech(c*x))/(d+pe*x^2)^2")]
        [InlineData("e^(2*asech(a*x))/x^2")]
        // Powers of the secant, by two and three rounds of parts: the second round asks for
        // `x^2/sqrt(1/(a x)^2 - 1)` two levels down, and its answer with `sgn(x)` in front,
        // where the extension by parity wrote it in |x| and the remainder was a search in |x|.
        [InlineData("x^3*asech(a*x)^2")]
        [InlineData("asech(a*x)^2/x^2")]
        [InlineData("asech(a*x)^3/x^2")]
        [InlineData("(a+b*asech(c*x))^2/x^4")]
        [InlineData("x^3*(a+b*acsch(c*x))^2")]
        public void ARationalFunctionWithSymbolicCoefficientsBesideTheSecant(string integrand) => DifferentiatesBackPinned(integrand);

        /// <summary>
        /// Rubi's 7.5.1 and 7.6.1 with the inverse below the bar. Under <c>c x = csch(u)</c>,
        /// <c>1/(x^2 (a + b acsch(c x)))</c> is <c>-c cosh(u)/(a + b u)</c>, which the hyperbolic
        /// sine and cosine integrals answer, and under <c>c x = sech(u)</c> the secant's are
        /// the same; the substitution that undoes an inverse hyperbolic function read the
        /// logarithms of the other three and not of these two. An exponential of one below the
        /// bar is a polynomial in <c>e^u</c> under the same substitution.
        /// </summary>
        [Theory]
        [InlineData("1/(x^4*(a+b*asech(c*x)))")]
        [InlineData("1/(x^2*(a+b*acsch(c*x)))")]
        [InlineData("1/(x^2*asech(c*x))")]
        [InlineData("1/(x^2*acsch(c*x))")]
        [InlineData("1/(x^2*(a+b*asech(c*x))^2)")]
        [InlineData("1/(x^3*(a+b*acsch(c*x))^2)")]
        [InlineData("1/(x^4*(a+b*asech(c*x))^3)")]
        [InlineData("1/(e^asech(a*x)*x^5)")]
        public void AnInverseHyperbolicSecantOrCosecantBelowTheBar(string integrand) => DifferentiatesBackPinned(integrand);

        /// <summary>Pinned with <c>a = 1/3</c>, <c>b = 1/2</c>, <c>c = 3/2</c>, <c>d = 2/3</c>, <c>e = 5/4</c>.</summary>
        private static void DifferentiatesBackPinned(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("C", 0).Substitute("a", "1/3".ToEntity()).Substitute("b", "1/2".ToEntity())
                .Substitute("c", "3/2".ToEntity()).Substitute("d", "2/3".ToEntity()).Substitute("pe", "5/4".ToEntity());
            var derivative = Pinned(integral).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            var compared = 0;
            // Where asech(a x) with a = 1/3 is real too, 0 < x <= 3, and asech(3x/2) real, x <= 2/3.
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        [Fact]
        public void TheAnswerNeedsNoAssumptionOnTheParameter()
        {
            // x^(-2) is positive for a real x, so the root of 1 + 1/(c x)^2 is the root of
            // x^2 + 1/c^2 over |x| whatever c is: the answer holds for a c off the real line, on
            // both sides of 0, and says nothing of c but that it is not zero.
            var integral = "x/sqrt(1 + 1/(c*x)^2)".ToEntity().Integrate("x");
            Assert.DoesNotContain(integral.Nodes, node => node is Providedf(_, var predicate) && predicate.Nodes.Any(inner => inner == "c^2 > 0".ToEntity()));
            var c = "0.7 + 0.4i".ToEntity();
            var derivative = integral.Substitute("C", 0).Differentiate("x").Substitute("c", c);
            var integrand = "x/sqrt(1 + 1/(c*x)^2)".ToEntity().Substitute("c", c);
            foreach (var at in new[] { 0.5, 1.3, -0.8, -1.7 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = integrand.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart) < 1e-9,
                    $"d/dx of the antiderivative is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
