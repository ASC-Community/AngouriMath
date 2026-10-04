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
    /// The binomial differential <c>x^m (a + b x^n)^(p/q)</c>, in the two of Chebyshev's three
    /// cases that are not a whole power: where the substitution leaves a polynomial or a
    /// rational function of <c>u</c>, and the third case taken to those through <c>x = 1/y</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>x^2 sqrt(1 + x^3)</c> came out and <c>x^5 sqrt(1 + x^3)</c> did not, under the same
    /// substitution. <c>u = (a + b x^n)^(1/q)</c> leaves a monomial in <c>u</c> for the first —
    /// which the general substitution finds, because <c>x^(n-1)</c> is <c>du</c> up to a constant
    /// — and a polynomial for the second, which it does not.
    /// </para>
    /// <para>
    /// The condition is <c>s = (m + 1)/n</c> being a whole number of at least one. Checked by
    /// differentiating back and comparing at points.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class BinomialDifferentialTest
    {
        private static readonly double[] Points = { 0.21, 0.35, 0.52, 0.71, 0.83 };

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

        /// <summary>
        /// <c>s = 2</c> and above, where the substitution leaves a polynomial rather than a
        /// monomial. These are the ones that were declined.
        /// </summary>
        [Theory]
        [InlineData("x^5*sqrt(1 + x^3)")]
        [InlineData("x^5*(1 + x^3)^(2/3)")]
        [InlineData("x^5/(1 + x^3)^(1/3)")]
        [InlineData("x^3*(1 + x^2)^(1/3)")]
        [InlineData("x^7*(1 + x^4)^(1/2)")]
        [InlineData("x^8*sqrt(1 + x^3)")]
        public void WhereTheSubstitutionLeavesAPolynomial(string integrand)
            => DifferentiatesBack(integrand);

        /// <summary>
        /// <c>s = 1</c>, the monomial case, which the general substitution already answered. It
        /// reaches this rule first now, so the answers have to be right from here too.
        /// </summary>
        [Theory]
        [InlineData("x^2*sqrt(1 + x^3)")]
        [InlineData("x^2*(1 + x^3)^(1/3)")]
        [InlineData("x^2/(1 + x^3)^(1/3)")]
        [InlineData("x*sqrt(1 + x^2)")]
        [InlineData("x*(1 + x^2)^(1/3)")]
        [InlineData("x^3/sqrt(1 + x^4)")]
        [InlineData("x^2*(4 - x^3)^(1/3)")]
        public void TheMonomialCaseIsUnchangedInSubstance(string integrand)
            => DifferentiatesBack(integrand);

        /// <summary>
        /// A rational factor in front and a negative coefficient inside, both of which the read
        /// carries rather than declining over.
        /// </summary>
        [Theory]
        [InlineData("3*x^5*sqrt(1 + x^3)")]
        [InlineData("x^5*sqrt(2 - 3*x^3)/2")]
        [InlineData("x^5*(1 + 2*x^3)^(1/3)")]
        public void AFactorAndACoefficient(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// <c>s = (m + 1)/n</c> whole and at most zero, where the substitution leaves a rational
        /// function of <c>u</c> rather than a polynomial: <c>sqrt(1 + x^3)/x</c> is
        /// <c>(2/3) int u^2/(u^2 - 1) du</c>. Charlwood's, Bronstein's and four of Welz's.
        /// </summary>
        [Theory]
        [InlineData("sqrt(1 + x^3)/x")]
        [InlineData("1/(x*sqrt(1 - x^3))")]
        [InlineData("sqrt(1 + x^4)/(x*(1 + x^4))")]
        [InlineData("(1 - x^3)^(1/3)/x")]
        [InlineData("(1 - x^3)^(2/3)/x")]
        [InlineData("1/(x*(1 - x^2)^(1/3))")]
        [InlineData("1/(x*(1 + x^3)^(1/3))")]
        [InlineData("sqrt(1 + x^2)/x^3")]
        public void WhereTheSubstitutionLeavesARationalFunction(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// Chebyshev's third case, <c>s + p/q</c> whole, taken to the second by <c>x = 1/y</c>:
        /// <c>x^6 (3 + 4x^4)^(1/4)</c> and <c>(x^3 - 1)/(2 + x^3)^(1/3)</c> are Timofeev's, and the
        /// second is a sum whose terms are each this shape.
        /// </summary>
        [Theory]
        [InlineData("x^6*(3 + 4*x^4)^(1/4)")]
        [InlineData("x^3/(2 + x^3)^(1/3)")]
        [InlineData("1/(2 + x^3)^(1/3)")]
        [InlineData("(x^3 - 1)/(2 + x^3)^(1/3)")]
        [InlineData("x^2*(1 + x^2)^(-3/2)")]
        public void TheThirdCaseThroughTheReciprocal(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The third case's <c>u</c> has <c>b + a/x^n</c> for its <c>q</c>-th power, and written
        /// as <c>(b + a/x^n)^(1/q)</c> it is that root for a positive <c>x</c>, and for a negative
        /// one only where <c>q</c> is odd: <c>1/(1 + x^4)^(5/4)</c> came out as
        /// <c>1/(1 + 1/x^4)^(1/4)</c>, which is even, and its derivative was the integrand's
        /// negative for every negative <c>x</c>. Written as <c>(a + b x^n)^(1/q)/x^(n/q)</c> it is
        /// right on both sides of zero under either root, and each of these is real on both.
        /// </summary>
        [Theory]
        [InlineData("1/(1 + x^4)^(5/4)")]
        [InlineData("x^2/(1 + x^4)^(3/4)")]
        [InlineData("x^6*(3 + 4*x^4)^(1/4)")]
        [InlineData("x^3/(2 + x^3)^(1/3)")]
        [InlineData("1/(2 + x^3)^(1/3)")]
        [InlineData("(x^3 - 1)/(2 + x^3)^(1/3)")]
        public void TheThirdCaseOnBothSidesOfZero(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in new[] { -1.1, -0.7, -0.35, 0.35, 0.83, 1.4 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)want.ImaginaryPart) < 1e-12, $"the integrand at x = {at} is {want}, not real");
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// Outside all three of Chebyshev's cases there is — and this is the part worth knowing
        /// before anyone goes looking — no elementary antiderivative at all, which he proved.
        /// These must be declined or answered by another rule, never wrong.
        /// </summary>
        [Theory]
        [InlineData("sqrt(1 + x^3)")]
        [InlineData("x*sqrt(1 + x^3)")]
        [InlineData("x^3*sqrt(1 + x^3)")]
        public void OutsideTheFirstCase(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;   // unanswered is a legitimate verdict; a wrong answer is not
            DifferentiatesBack(integrand);
        }

        /// <summary>
        /// What the read must refuse: a whole exponent on the bracket, which is a polynomial and
        /// wants expanding; a linear bracket, which the linear radical rule answers more shortly;
        /// and a quadratic one, which the trigonometric substitution answers more shortly still.
        /// </summary>
        [Theory]
        [InlineData("x^2*(1 + x^3)^2")]
        [InlineData("x*sqrt(1 + x)")]
        [InlineData("sqrt(1 + 2*x)")]
        [InlineData("x^3*sqrt(1 + x^2)")]
        [InlineData("1/(1 + x^2)^(3/2)")]
        public void TheNeighboursAreUntouched(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A fractional power of a quotient whose denominator is positive for every real
        /// <c>x</c> is written as the two powers it is, and only then is a binomial seen:
        /// Timofeev's <c>((2 + x^2)/x^2)^(7/9)/(2 + x^2)^(3/2)</c> is
        /// <c>x^(-14/9) (2 + x^2)^(-13/18)</c> so written, under <c>u = x^(1/9)</c>. The power
        /// of <c>x^2</c> taken out is a power of <c>|x|</c>, so the answer for <c>x &gt; 0</c> is
        /// extended by parity -- the integrand is even and the antiderivative odd -- and both
        /// sides are checked.
        /// </summary>
        [Theory]
        [InlineData("((2 + x^2)/x^2)^(7/9)/(2 + x^2)^(3/2)")]
        [InlineData("((1 + x^2)/x^2)^(1/4)/(1 + x^2)^(3/2)")]
        public void APowerOfAQuotientIsWrittenApart(string integrand)
        {
            DifferentiatesBack(integrand);
            var derivative = integrand.ToEntity().Integrate("x").Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in new[] { -0.83, -0.35 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart) < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// Where the root of the quotient is not real the integrand can still be, and there an
        /// answer from the root written apart must not stand. <c>e^atanh(x) sqrt(1 - x)</c> is
        /// <c>sqrt((1 + x)/(1 - x)) sqrt(1 - x)</c>, and written apart as <c>sqrt(1 + x)</c> it
        /// is the integrand only for <c>x &lt; 1</c>. Past <c>x = 1</c> it is the product of two
        /// imaginary factors, real and of the other sign, and the answer was wrong there. Now the
        /// answer says it holds where the root is real, and claims nothing past it.
        /// https://github.com/asc-community/AngouriMath/issues/1664
        /// </summary>
        [Theory]
        [InlineData("e^atanh(x)*(1-x)^(1/2)")]
        [InlineData("e^atanh(x)/(1-x)^(1/2)")]
        [InlineData("((1+x)/(1-x))^(1/2)*(1-x)^(1/2)")]
        public void AnAnswerFromARootWrittenApartHoldsWhereItIsGiven(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in new[] { 0.29, -0.61 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            foreach (var at in new[] { 1.43, 3.17 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)want.ImaginaryPart) < 1e-12, $"the integrand at x = {at} is {want}, not real");
                var got = derivative.Substitute("x", at).Evaled;
                Assert.True(got.IsNaN || got is Entity.Number.Complex number
                    && Math.Abs((double)(number - want).RealPart) < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// An even power on either side of the bar comes out of the root as well, since it is
        /// not negative: <c>sqrt(sin(x)/cos(x)^5)</c> is <c>sqrt(sin(x)/cos(x))/cos(x)^2</c> and
        /// <c>sqrt(sin(x)^5/cos(x))</c> is <c>sin(x)^2 sqrt(sin(x)/cos(x))</c>, exactly, and the
        /// quotient set free is the tangent -- Timofeev's, binomials under <c>sqrt(tan(x))</c>
        /// from there. <c>tan(x)^2</c> under the root of <c>sqrt(tan(x) tan(2x))</c> is
        /// <c>|tan(x)|</c>, and the answer for a positive tangent is extended by parity, so it
        /// is checked on both signs. What is neither even nor positive stays under the root.
        /// </summary>
        [Theory]
        [InlineData("sqrt(sin(x)/cos(x)^5)", new[] { 0.21, 0.52, 0.83, 1.1, 1.4 })]
        [InlineData("sqrt(sin(x)^5/cos(x))", new[] { 0.21, 0.52, 0.83, 1.1, 1.4 })]
        [InlineData("sqrt(tan(x)*tan(2*x))", new[] { -0.7, -0.35, 0.21, 0.52, 0.7 })]
        public void AnEvenPowerComesOutOfTheRootOnEitherSide(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart) < 1e-8 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// A rational function of <c>x^n</c> beside <c>(c + d x^n)^(k - 1/n)</c>, rationalised by
        /// <c>u = x/(c + d x^n)^(1/n)</c>: <c>u^n</c> is <c>x^n/(c + d x^n)</c>, and
        /// <c>dx/(c + d x^n)^(1/n)</c> is <c>(c + d x^n) du/c</c>. Timofeev's
        /// <c>1/((1 + x^4)(2 + x^4)^(1/4))</c> is <c>1/(1 + u^4)</c> that way, and Welz's
        /// <c>1/((1 - x^3)(a + b x^3)^(1/3))</c> is <c>1/(1 - (a + b) u^3)</c> with the symbols
        /// still in it. Checked on both signs of <c>x</c>, with the symbols pinned.
        /// </summary>
        [Theory]
        [InlineData("1/((1 + x^4)*(2 + x^4)^(1/4))", new[] { -1.7, -0.6, 0.3, 0.9, 2.2 })]
        [InlineData("(1 + x^4)^(3/4)/(2 + x^4)^2", new[] { -1.7, -0.6, 0.3, 0.9, 2.2 })]
        [InlineData("x^4/((1 + x^4)*(2 + x^4)^(1/4))", new[] { -1.7, -0.6, 0.3, 0.9, 2.2 })]
        [InlineData("1/((1 - x^3)*(a + b*x^3)^(1/3))", new[] { -1.7, -0.6, 0.3, 0.9, 2.2 })]
        public void ARationalFunctionOfThePowerBesideTheRootOfItsBinomial(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("a", 2).Substitute("b", 3);
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity().Substitute("a", 2).Substitute("b", 3);
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart) < 1e-8 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
