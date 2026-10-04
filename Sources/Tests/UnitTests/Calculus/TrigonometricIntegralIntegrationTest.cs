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

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The integrals that answer in the sine and cosine integrals: sines and cosines of a linear
    /// over a power of a linear.
    /// https://github.com/asc-community/AngouriMath/issues/1501
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class TrigonometricIntegralIntegrationTest
    {
        private static Entity DifferentiatesBack(string integrand, double[] points, params (string Name, string Value)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => pins.Aggregate(e, (current, pin) => current.Substitute(pin.Name, pin.Value.ToEntity()));
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9, $"d/dx of {integral} is {got} at x = {at}, where {integrand} is {want}");
            }
            return integral;
        }

        /// <summary>Either side of 0 and of every pole below.</summary>
        private static readonly double[] AroundZero = { -1.7, 0.35, 0.9, 1.45 };

        private static readonly (string, string)[] Pins =
            { ("a", "1/3"), ("b", "2/3"), ("c", "5/4"), ("d", "1/2"), ("k", "2") };

        /// <summary>
        /// Under <c>u = c + d x</c>, each term is <c>u^m sin(q u)</c> or <c>u^m cos(q u)</c>, and
        /// <c>m = -1</c> is <c>Si(q u)</c> or <c>Ci(q u)</c>, by parts below that. Rubi's 4.1.10,
        /// <c>(c + d x)^m sin(a + b x)</c> with <c>m</c> negative.
        /// </summary>
        [Theory]
        [InlineData("sin(x)/x")]
        [InlineData("cos(x)/x")]
        [InlineData("sin(2*x + 1)/(3*x + 4)")]
        [InlineData("cos(3*x - 1)/(1 - x)")]
        [InlineData("sin(x)/x^2")]
        [InlineData("cos(x)/x^3")]
        [InlineData("x*sin(x)/(x + 1)^2")]
        [InlineData("(1 + x^2)*cos(x)/x^2")]
        [InlineData("x^(-2)*sin(2*x)")]
        [InlineData("sin(-2*x)/x")]
        [InlineData("sin(a + b*x)/(c + d*x)")]
        [InlineData("cos(a + b*x)/(c + k*x)^2")]
        public void ASineOrACosineOfALinearOverAPowerOfALinear(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// A power of a sine or a cosine, or a product of both, is a sum of sines and cosines of
        /// multiples of the argument, and an even power leaves a constant term, whose integral is
        /// a logarithm: <c>int sin(x)^2/x = ln(x)/2 - Ci(2 x)/2</c>.
        /// </summary>
        [Theory]
        [InlineData("sin(x)^2/x")]
        [InlineData("cos(x)^2/x")]
        [InlineData("sin(x)*cos(x)/x")]
        [InlineData("sin(x)^3/x^2")]
        [InlineData("sin(2*x + 1)^2*cos(2*x + 1)/(x + 1)")]
        public void APowerOfASineOrACosineOverAPowerOfALinear(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// Two or more linears below the bar are split into partial fractions over them, and
        /// each term is the one-linear question. Rubi's 4.1.11, <c>sin(c + d x)/(x^m (a + b x)^n)</c>.
        /// </summary>
        [Theory]
        [InlineData("sin(x)/(x*(x + 1))")]
        [InlineData("cos(2*x + 1)/((x - 1)*(x + 2)^2)")]
        [InlineData("sin(c + d*x)/(x^2*(a + b*x))")]
        [InlineData("x^3*sin(x)^2/((x + 1)*(x + 3))")]
        [InlineData("x*sin(x)/((c + d*x)*(x - 2))")]
        [InlineData("x*cos(a + b*x)/((c + d*x)*(x + 1))")]
        public void OverSeveralLinears(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// A quadratic below the bar is its leading coefficient times the linears of its two
        /// roots, complex where its discriminant is negative, and the sine and cosine integrals of
        /// the two conjugate arguments add up to a real answer. Rubi's 4.1.11 and 4.7.7,
        /// <c>sin(c + d x)/(a + b x^2)</c> and <c>sin(a + b x)/(c + d x + e x^2)</c>, and a binomial past
        /// it, <c>a + b x^3</c> or <c>a + b x^4</c>, the linears of its roots. A numerator past the degree
        /// is divided by the denominator as it is written, since the remainder of a division by the
        /// linears of symbolic roots was left in their powers.
        /// </summary>
        [Theory]
        [InlineData("sin(x)/(1 + x^2)")]
        [InlineData("cos(x)/(x^2 - 4)")]
        [InlineData("x*sin(2*x)/(x^2 + x + 1)")]
        [InlineData("sin(c + d*x)/(a + b*x^2)")]
        [InlineData("cos(x)/(x*(1 + x^2))")]
        [InlineData("sin(x)/(1 + x^2)^2")]
        [InlineData("x^2*sin(c + d*x)/(a + b*x^2)")]
        [InlineData("x^3*cos(c + d*x)/(a + b*x^2)")]
        [InlineData("sin(c + d*x)/(a + b*x^3)")]
        [InlineData("x^3*sin(c + d*x)/(a + b*x^3)")]
        [InlineData("sin(c + d*x)/(a + b*x^3)^2")]
        [InlineData("sin(x)/(1 + x^4)")]
        [InlineData("x*sin(x)/(1 + x^3)")]
        public void OverAQuadratic(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// An argument <c>a + b x^r</c> beside a power of <c>x</c> is the same question under
        /// <c>u = x^r</c>, for a whole, a fractional, a negative and a symbolic <c>r</c>. Rubi's
        /// 4.1.12, <c>(e x)^m (a + b sin(c + d x^n))^p</c>.
        /// </summary>
        [Theory]
        [InlineData("sin(x^2)/x")]
        [InlineData("(a + b*sin(c + d*x^2))^2/x^3")]
        [InlineData("sin(sqrt(x))/x")]
        [InlineData("sin(a + b/x)")]
        [InlineData("x^2*sin(a + b/x)^2")]
        [InlineData("x^(-1 - k)*sin(a + b*x^k)")]
        public void UnderAPowerOfTheVariable(string integrand)
            => DifferentiatesBack(integrand, new[] { 0.35, 0.9, 1.45, 2.3 }, Pins);

        /// <summary>
        /// An inverse sine, cosine or tangent below the bar is undone by the substitution
        /// <c>x = sin(u)</c> and its kin, after which the integrand is a polynomial in the sine and
        /// cosine over a power of <c>a + b u</c>. Rubi's 5.1.2 and 5.3.4,
        /// <c>x^m/(a + b arcsin(c x))^n</c> and <c>x^m/((c + a^2 c x^2)^2 arctan(a x))</c>.
        /// </summary>
        [Theory]
        [InlineData("1/asin(x)")]
        [InlineData("x/asin(c*x)")]
        [InlineData("x^2/(a + b*asin(c*x))")]
        [InlineData("1/acos(x)")]
        [InlineData("sqrt(1 - x^2)/asin(x)")]
        [InlineData("x/((c + d^2*c*x^2)^2*atan(d*x))")]
        [InlineData("1/((c + d^2*c*x^2)^3*atan(d*x))")]
        public void ThroughAnInverseTrigonometricFunction(string integrand)
            => DifferentiatesBack(integrand, new[] { 0.15, 0.3, 0.5, 0.7 }, Pins);

        [Theory]
        [InlineData("sin(x)/x", "Si(x)")]
        [InlineData("cos(x)/x", "Ci(x)")]
        [InlineData("sin(2*x)/x", "Si(2 * x)")]
        [InlineData("sin(x)*cos(x)/x", "1/2 * Si(2 * x)")]
        public void TheDefinitionsAreTheAnswers(string integrand, string expected)
            => Assert.Equal(expected.ToEntity() + "C".ToEntity(), integrand.ToEntity().Integrate("x"));

        /// <summary>
        /// Where the cosine integrals of two terms cancel, the antiderivative is elementary, and
        /// simplified it is written without them: <c>(x cos(x) - sin(x))/x^2</c> is the derivative
        /// of <c>sin(x)/x</c>.
        /// </summary>
        [Fact]
        public void AnElementaryAntiderivativeKeepsNoSineOrCosineIntegral()
        {
            var integral = DifferentiatesBack("(x*cos(x) - sin(x))/x^2", AroundZero).Simplify().Stringize();
            Assert.DoesNotContain("Si(", integral);
            Assert.DoesNotContain("Ci(", integral);
        }
    }
}
