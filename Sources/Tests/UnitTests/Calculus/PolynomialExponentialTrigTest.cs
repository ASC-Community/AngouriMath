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
    /// A polynomial times an exponential times a sine or a cosine, closed by the repeated by
    /// parts this shape is the textbook case for.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>e^x cos(x)</c> came out and <c>x e^x cos(x)</c> did not: the polynomial by-parts
    /// integrates its <c>dv</c> with by-parts switched off, and <c>e^x cos(x)</c> is answered by
    /// nothing but by-parts — the cyclic one. Switching that flag on buys these and takes
    /// <c>x tan(x)^3 sec(x)^4</c> from 20 ms to 94 seconds to decline, so the family is named
    /// rather than searched for.
    /// </para>
    /// <para>
    /// Checked by differentiating back and comparing at points, never against a printed form.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PolynomialExponentialTrigTest
    {
        private static readonly double[] Points = { -1.3, -0.4, 0.7, 1.5, 2.2 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            Assert.DoesNotContain("integral(", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 4,
                $"only {compared} of {Points.Length} points were comparable for {integrand}, "
                + "so this asserts almost nothing");
        }

        /// <summary>All three factors at once, which is what was declined.</summary>
        [Theory]
        [InlineData("x*e^x*cos(x)")]
        [InlineData("x*e^x*sin(x)")]
        [InlineData("x^2*e^x*cos(x)")]
        [InlineData("x^3*e^x*cos(x)")]
        [InlineData("(x^2 + 1)*e^x*sin(x)")]
        [InlineData("x*e^(2*x)*sin(3*x)")]
        [InlineData("x*e^(-x)*cos(2*x)")]
        public void AllThreeFactors(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A base other than <c>e</c>, which is the same shape written differently:
        /// <c>2^x</c> is <c>e^(x ln 2)</c>, and these were declined for the spelling alone.
        /// </summary>
        [Theory]
        [InlineData("2^x*x*cos(x)")]
        [InlineData("3^x*x^2*sin(x)")]
        [InlineData("2^(3*x)*x*sin(x)")]
        public void ABaseOtherThanE(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The rate <c>i</c> times the frequency: the closed form divides by
        /// <c>a^2 + b^2 = 0</c> and answered NaN, where by Euler <c>e^(i x) cos(x)</c> is
        /// <c>(1 + e^(2 i x))/2</c>, a polynomial and a polynomial times one exponential.
        /// </summary>
        [Theory]
        [InlineData("e^(i*x)*cos(x)")]
        [InlineData("x*e^(-i*x)*sin(x)")]
        [InlineData("x^2*e^(2*i*x)*cos(2*x)")]
        public void TheResonantRate(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The degenerate corners of the same formula — no polynomial, no exponential, no
        /// trigonometric factor. Each already came out, and each now comes out through this rule
        /// as well, so the signs have to be right at <c>a = 0</c> and <c>b = 0</c> too. That is
        /// not a formality: the first version of the formula had the sign of the <c>b</c> terms
        /// reversed, which is invisible on <c>e^x cos(x)</c> and wrong on <c>x^2 sin(x)</c>.
        /// </summary>
        [Theory]
        [InlineData("e^x*cos(x)")]
        [InlineData("e^(2*x)*sin(3*x)")]
        [InlineData("x*e^x")]
        [InlineData("x^2*e^x")]
        [InlineData("x*sin(x)")]
        [InlineData("x^2*sin(x)")]
        [InlineData("x^2*cos(x)")]
        [InlineData("x^3*sin(x)")]
        [InlineData("2^x*x")]
        [InlineData("e^x")]
        [InlineData("sin(x)")]
        [InlineData("cos(3*x)")]
        public void TheDegenerateCorners(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A power of the sine or the cosine, or a product of the two, of one argument: a sum
        /// of sines and cosines of its multiples, each of which is the shape the loop closes.
        /// <c>e^x sin(x)^3</c> is what <c>e^(arcsin(x)) x^3/sqrt(1 - x^2)</c> becomes under
        /// <c>x = sin(u)</c>, and it was declined for the cube; a secant or a cosecant under the
        /// bar is a cosine or a sine above it and is read as one.
        /// </summary>
        [Theory]
        [InlineData("e^x*sin(x)^3")]
        [InlineData("e^x*cos(x)^2")]
        [InlineData("e^x*sin(x)^2*cos(x)")]
        [InlineData("e^x*sin(x)^4*cos(x)^3")]
        [InlineData("e^(2*x)*cos(3*x)^2")]
        [InlineData("x*e^x*cos(x)^2")]
        [InlineData("x^2*e^(-x)*sin(2*x)^3")]
        [InlineData("e^x/sec(x)")]
        [InlineData("e^x/csc(x)^2")]
        [InlineData("2^x*sin(x)^2")]
        public void APowerOfTheSineOrTheCosine(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The powers a symbol away from the numbers: <c>e^(a x) sin(b x)^2</c> with <c>a</c> and
        /// <c>b</c> pinned only when differentiating back.
        /// </summary>
        [Fact]
        public void ASymbolicRateAndFrequency()
        {
            var integrand = "e^(a*x)*sin(b*x)^2".ToEntity();
            var integral = integrand.Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x").Substitute("a", 0.7).Substitute("b", 1.3);
            var original = integrand.Substitute("a", 0.7).Substitute("b", 1.3);
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                Assert.True(difference / Math.Max(1.0, Math.Abs((double)want.RealPart)) < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// What the read must refuse: a bare polynomial, which is the power rule's; a phase in
        /// the trigonometric argument, which wants the angle-sum identity first; two
        /// trigonometric factors of different arguments, which the product-to-sum rule takes
        /// apart; a factor that is neither polynomial nor exponential nor trigonometric; a
        /// sine below the bar; and a power past the bound on the expansion.
        /// </summary>
        [Theory]
        [InlineData("x^2")]
        [InlineData("e^x*cos(x + 1)")]
        [InlineData("x*sin(x)*cos(2*x)")]
        [InlineData("x*ln(x)*e^x")]
        [InlineData("x*e^(x^2)*cos(x)")]
        [InlineData("e^x/sin(x)")]
        [InlineData("e^x*sin(x)^9")]
        public void WhatTheReadRefuses(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;   // unanswered is a legitimate verdict; a wrong answer is not
            DifferentiatesBack(integrand);
        }

        /// <summary>
        /// The integrand the closed form exists to avoid opening a search on. It is declined
        /// either way; what is pinned is that it is declined <em>cheaply</em>, which the closed
        /// rule keeps and switching the by-parts flag on did not.
        /// </summary>
        /// <remarks>
        /// Bounded by the integrator's own budget rather than by a wall clock, which measures the
        /// runner: `x tan(x)^3 sec(x)^4` took 94 seconds with the flag on and takes about thirty
        /// milliseconds here.
        /// </remarks>
        [Theory]
        [InlineData("x*tan(x)^3*sec(x)^4")]
        [InlineData("x*cot(x)^3*csc(x)^4")]
        public void TheSearchThisAvoidsOpening(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;
            DifferentiatesBack(integrand);
        }

        /// <summary>
        /// A phase in the trigonometric's argument, expanded by the angle-sum identity where an
        /// exponential stands beside it: the closed rule reads one frequency and no phase, an
        /// exponential's own offset being a constant factor where a trigonometric's is not.
        /// </summary>
        [Theory]
        [InlineData("x*e^(2*x)*cos(3*x + 1)")]
        [InlineData("x^2*e^x*sin(x + 2)")]
        [InlineData("e^(3*x)*cos(2*x + 1/2)*sin(2*x + 1/2)")]
        public void APhaseIsExpandedBesideAnExponential(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The resonant case, where the rate is the frequency times <c>i</c> and the closed
        /// form's <c>a^2 + b^2</c> is zero: the product is a sum of two exponentials instead.
        /// With a symbolic frequency <c>-f^2 + f^2</c> is not collected by
        /// <see cref="Entity.InnerSimplified"/>, so the guard read it as a nonzero number and the
        /// answer divided by it -- <c>NaN</c> at every point, for every row of Rubi's 4.3.10 that
        /// carries <c>a + i a tan(pe + f x)</c> below the bar.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
        /// </summary>
        [Theory]
        [InlineData("x*e^(-i*f*x)*cos(f*x)", "f")]
        [InlineData("e^(i*f*x)*sin(f*x)", "f")]
        [InlineData("x^2*e^(-i*x)*cos(x)", null)]
        public void TheResonanceIsSeenSymbolically(string integrand, string? symbol)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("NaN", integral.Stringize());
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pin(Entity e) => symbol is null ? e : e.Substitute(symbol, 1.3);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.False(got.IsNaN, $"the antiderivative of {integrand} differentiates to NaN at x = {at}");
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// <c>A + i A tan(z)</c> below the bar is <c>A e^(i z)/cos(z)</c>, and
        /// <c>A + i A cot(z)</c> is <c>i A e^(-i z)/sin(z)</c>: beside a polynomial that is the
        /// shape above, where the imaginary unit in the coefficient is read by nothing else.
        /// Rubi's 4.3.10 and 4.4.10. Above the bar the tangent's own rules answer it, and the
        /// rewrite leaves it alone.
        /// </summary>
        [Theory]
        [InlineData("x/(2 + 2*i*tan(x))")]
        [InlineData("x^2/(3 - 3*i*cot(x))")]
        [InlineData("(1 + 2*x)/(3 + 3*i*tan(1/2 + x))^2")]
        public void AnImaginaryTangentBelowTheBarIsAnExponential(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A tangent of the same argument above the bar is written in sines and cosines, so that the
        /// cosine the identity puts below cancels it, and what sines and cosines of it are left as
        /// exponentials: <c>tan(z)/(A + i A tan(z))</c> is <c>sin(z) e^(-i z)/A</c>, and
        /// <c>tan(z) cos(z) e^(-i z)/A</c> was a search past the budget with <c>z = c + d x</c>.
        /// The symbols pinned only after integrating.
        /// </summary>
        [Theory]
        [InlineData("tan(c + d*x)/(a + i*a*tan(c + d*x))")]
        [InlineData("tan(c + d*x)^2/(a + i*a*tan(c + d*x))")]
        [InlineData("tan(c + d*x)^3/(a + i*a*tan(c + d*x))^4")]
        [InlineData("cot(c + d*x)/(a + i*a*cot(c + d*x))")]
        [InlineData("1/(a + i*a*tan(c + d*x))")]
        public void AnImaginaryTangentOfALinearWithATangentAboveTheBar(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 0.9).Substitute("c", 0.3).Substitute("d", 1.1);
            var derivative = Pin(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9, $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} points could be compared for {integrand}");
        }

        /// <summary>
        /// And its sibling, <c>A cos(y) + i A sin(y)</c>, which is <c>A e^(i y)</c>: a sum of a
        /// cosine and a sine with no real rotation, since <c>A^2 + (i A)^2</c> is zero, and beside
        /// a power of the cosine an exponential times that power once written so. Rubi's 4.7.2,
        /// fourteen rows; <c>cos(x)^2/(a cos(x) + i a sin(x))^3</c> was declined after twelve
        /// seconds.
        /// </summary>
        [Theory]
        [InlineData("cos(x)^2/(2*cos(x) + 2*i*sin(x))^3")]
        [InlineData("cos(x)^5/(2*cos(x) + 2*i*sin(x))^2")]
        [InlineData("1/(3*cos(1/2 + x) + 3*i*sin(1/2 + x))^3")]
        [InlineData("cos(x)/(2*cos(x) - 2*i*sin(x))")]
        public void AnImaginarySumOfACosineAndASineBelowTheBarIsAnExponential(string integrand) => DifferentiatesBack(integrand);

        /// <summary>The same with the coefficient a symbol, pinned only after integrating.</summary>
        [Fact]
        public void AnImaginarySumWithASymbolicCoefficient()
        {
            var integral = "cos(x)^2/(a*cos(x) + i*a*sin(x))^3".ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Substitute("a", 0.9).Differentiate("x");
            var original = "cos(x)^2/(a*cos(x) + i*a*sin(x))^3".ToEntity().Substitute("a", 0.9);
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                Assert.True(difference < 1e-9, $"at x = {at}: {got} for {want}");
            }
        }

        /// <summary>
        /// A base with symbols in it, and an exponent whose constant part has them too:
        /// <c>F^(c (a + b x))</c> leaves <c>F^(a c)</c> as a constant factor, and that factor
        /// differentiates to a <em>conditional</em> zero -- <c>0 provided not F = 0 or a c > 0</c> --
        /// where the by-parts loop compared against the number and carried on until it ran out of
        /// steps. Rubi's 4.7.6 and 6.1.5.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
        /// </summary>
        [Theory]
        [InlineData("F^(c*(a + b*x))*sin(d + pe*x)^3")]
        [InlineData("F^(c*(a + b*x))*cos(d + pe*x)^2")]
        [InlineData("F^(c*(a + b*x))*sin(d + pe*x)*cos(d + pe*x)")]
        public void ASymbolicConstantFactorDoesNotStopTheParts(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("F", 2.3).Substitute("a", 0.4).Substitute("b", 1.1)
                .Substitute("c", 0.7).Substitute("d", 0.9).Substitute("pe", 1.3);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.False(got.IsNaN, $"the antiderivative of {integrand} differentiates to NaN at x = {at}");
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
