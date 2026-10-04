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
    /// The integrals that answer in the exponential and logarithmic integrals: an exponential of a
    /// linear over a power of a linear, the Gaussian's odd negative moments, and a power of x over
    /// a power of a logarithm.
    /// https://github.com/asc-community/AngouriMath/issues/1501
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialIntegralIntegrationTest
    {
        private static void DifferentiatesBack(string integrand, double[] points, params (string Name, string Value)[] pins)
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
        }

        /// <summary>Either side of 0 and of every pole below.</summary>
        private static readonly double[] AroundZero = { -1.7, 0.35, 0.9, 1.45 };

        /// <summary>Past 1, where every logarithm below is real and positive.</summary>
        private static readonly double[] PastOne = { 1.3, 1.9, 2.6, 3.4 };

        private static readonly (string, string)[] Pins =
            { ("a", "1/3"), ("b", "2/3"), ("c", "5/4"), ("d", "1/2"), ("k", "2"), ("m", "1/2"), ("n", "3"), ("F", "2") };

        /// <summary>
        /// Under <c>u = c + d x</c>, each term is <c>u^m e^(k u)</c>, and <c>m = -1</c> is <c>Ei(k u)</c>.
        /// Rubi's 2.3, <c>F^(c (a + b x))/(d + e x)^n</c>.
        /// </summary>
        [Theory]
        [InlineData("e^x/x")]
        [InlineData("e^(2*x + 1)/(3*x + 4)")]
        [InlineData("e^x/x^2")]
        [InlineData("x*e^x/(x + 1)")]
        [InlineData("2^x/x^3")]
        [InlineData("(1 + x)^2*e^(-x)/x^2")]
        [InlineData("F^(c*(a + b*x))/(d + k*x)^2")]
        public void AnExponentialOfALinearOverAPowerOfALinear(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// Beside a sine or a cosine of a linear, the sine and cosine are written as exponentials,
        /// and each term is the exponential's above with a complex rate: <c>e^(2x) sin(x)/x</c> is
        /// <c>(Ei((2 + i) x) - Ei((2 - i) x))/(2i)</c>, two conjugate terms whose sum is real.
        /// </summary>
        [Theory]
        [InlineData("e^(2*x)*sin(x)/x")]
        [InlineData("e^x*cos(x)/(1 + x)")]
        [InlineData("x*e^x*sin(2*x)/(x - 1)")]
        public void AnExponentialTimesASineOrCosineOverALinear(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// A power of <c>x</c> times a function of a logarithm of a monomial, under
        /// <c>t = ln(c x^n)</c>, with <c>x^(m + 1)</c> written as <c>K e^((m + 1) t/n)</c> and
        /// <c>K = x^(m + 1) (c x^n)^(-(m + 1)/n)</c> locally constant. With an even <c>n</c> the
        /// integrand is real at negative <c>x</c> as well, and the answer is checked there too,
        /// where <c>ln(c x^n)</c> is not <c>ln(c) + n ln(x)</c>.
        /// </summary>
        [Theory]
        [InlineData("x*sin(ln(x))/ln(x)")]
        [InlineData("Si(d*(a + b*ln(c*x^n)))")]
        [InlineData("x*Ci(d*(a + b*ln(c*x^n)))")]
        [InlineData("(k*x)^m*Ei(d*(a + b*ln(c*x^n)))")]
        public void APowerTimesAFunctionOfALogarithmOfAMonomial(string integrand)
            => DifferentiatesBack(integrand, new[] { -2.1, -0.7, 0.6, 1.7 },
                ("a", "2/5"), ("b", "13/10"), ("c", "7/10"), ("d", "19/10"), ("k", "11/10"), ("m", "1/2"), ("n", "2"));

        /// <summary>
        /// Over several linears, the quotient is split into partial fractions over them first, and
        /// each term is the question above: <c>e^x/(x (x + 1))</c> is <c>Ei(x) - Ei(x + 1)/e</c>. The
        /// last row is what by parts leaves of <c>Ei(a + b x)/x^2</c>.
        /// </summary>
        [Theory]
        [InlineData("e^x/(x*(x + 1))")]
        [InlineData("e^x/(x*(x - a))")]
        [InlineData("e^x/(x^2*(x + 1))")]
        [InlineData("x^3*e^(2*x)/((x + 1)*(x - 2))")]
        [InlineData("e^(a + b*x)/((a + b*x)*x)")]
        public void AnExponentialOfALinearOverSeveralLinears(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// Under a substitution, what is left in x is a power of the candidate: under
        /// <c>u = sqrt(1 - a x)/sqrt(1 + a x)</c>, <c>F^(k u)/(1 - a^2 x^2)</c> is
        /// <c>-F^(k u)/(a u)</c>, and under <c>u = c/(a + b x)</c>, <c>F^u</c> is
        /// <c>-c F^u/(b u^2)</c>; and beside a quadratic <c>u</c>, what is left of
        /// <c>F^(1/u) u'/u^2</c> is <c>1/u^2</c> written otherwise. Rubi's 2.3. Every point is where
        /// <c>|a x| &lt; 1</c>, off <c>a + b x = 0</c>, and the integrand is real.
        /// </summary>
        [Theory]
        [InlineData("F^(sqrt(1 - a*x)/sqrt(1 + a*x))/(1 - a^2*x^2)")]
        [InlineData("F^(2*sqrt(1 - a*x)/sqrt(1 + a*x))/(1 - a^2*x^2)")]
        [InlineData("F^(3*sqrt(1 - a*x)/sqrt(1 + a*x))/(1 - a^2*x^2)")]
        [InlineData("F^(c/(a + b*x))")]
        [InlineData("F^(1/(a + b*x + c*x^2))*(b + 2*c*x)/(a + b*x + c*x^2)^2")]
        public void WhatIsLeftInXIsAPowerOfTheCandidate(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// The Gaussian's odd negative moments end at <c>int e^(A x^2)/x = Ei(A x^2)/2</c>, and
        /// <c>x e^(-1/x^2)</c> is one of them under <c>u = 1/x</c>.
        /// </summary>
        [Theory]
        [InlineData("e^(x^2)/x")]
        [InlineData("e^(-x^2)/x^3")]
        [InlineData("x*e^(-1/x^2)")]
        [InlineData("x^3*e^(-1/x^2)")]
        public void AnOddNegativeMomentOfTheGaussian(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        /// <summary>
        /// Under <c>t = A + B ln(c x^r)</c>, <c>x^p (A + B L)^(-k)</c> is a constant times
        /// <c>e^((p + 1) t/(B r)) t^(-k)</c>, and a power of x inside, <c>d + e x^m</c>, is the same
        /// question under <c>u = x^m</c>. Rubi's 3.1.2, 3.3 and 3.4.
        /// </summary>
        [Theory]
        [InlineData("1/ln(x)")]
        [InlineData("x/ln(x)")]
        [InlineData("x^2/ln(x)^2")]
        [InlineData("x^m/(a + b*ln(c*x^n))")]
        [InlineData("1/(a + b*ln(c*x^n))^2")]
        [InlineData("(d + k*x)^2/ln(c*(d + k*x)^n)")]
        [InlineData("(1 + x)/ln(x)")]
        [InlineData("x^2/ln(c*(d + k*x^3)^n)")]
        [InlineData("x^8/ln(c*(d + k*x^3)^n)^2")]
        [InlineData("(k*x)^m*x/ln(b*x)")]
        public void APowerOverAPowerOfALogarithm(string integrand)
            => DifferentiatesBack(integrand, PastOne, Pins);

        /// <summary>
        /// The non-elementary neighbours the exponential ansatz declines, which <c>Ei</c> answers:
        /// <c>e^(e^x)</c> under <c>u = e^x</c>, and <c>e^x ln(x)</c> by parts.
        /// </summary>
        [Theory]
        [InlineData("e^(e^x)")]
        [InlineData("e^x/(1 + x)")]
        public void AnExponentialIntegralTheAnsatzDeclines(string integrand)
            => DifferentiatesBack(integrand, AroundZero, Pins);

        [Fact]
        public void TheExponentialTimesTheLogarithm()
            => DifferentiatesBack("e^x*ln(x)", PastOne, Pins);

        /// <summary>
        /// Where the exponential integrals cancel, the antiderivative is elementary, and is left to
        /// the rules that write it shortest.
        /// </summary>
        [Fact]
        public void WhereTheExponentialIntegralsCancelTheAnswerIsElementary()
            => Assert.Equal("e ^ x / (1 + x) + C", "e^x*x/(1 + x)^2".ToEntity().Integrate("x").Stringize());

        /// <summary><c>int 1/ln(x) dx</c> is the logarithmic integral itself, since <c>Ei(ln y) = li(y)</c>.</summary>
        [Fact]
        public void TheReciprocalOfTheLogarithmIsTheLogarithmicIntegral()
        {
            Assert.Equal(MathS.Li("x") + "C", "1/ln(x)".ToEntity().Integrate("x"));
            Assert.Equal(MathS.Li("y"), MathS.Ei(MathS.Ln("y")).InnerSimplified);
        }
    }
}
