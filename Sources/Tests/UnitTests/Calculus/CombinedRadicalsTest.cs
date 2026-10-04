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
    /// Several square roots of polynomials in the variable written as one before the rules
    /// that read one radical look at the integrand: <c>sqrt(1 + x^2) sqrt(1 - x^2)</c> is
    /// <c>sqrt(1 - x^4)</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>x/(sqrt(1 + x^2) sqrt(1 - x^2))</c> had no antiderivative and <c>x/sqrt(1 - x^4)</c>
    /// is one substitution. It is what by parts leaves from <c>arcsin(x)/(1 + x^2)^(3/2)</c>,
    /// from <c>ln(x + sqrt(1 + x^2))/(1 - x^2)^(3/2)</c>, and from every other pairing of an
    /// inverse function's radical with the one in the power it stands over.
    /// </para>
    /// <para>
    /// <b>The identity is conditional.</b> <c>sqrt(P) sqrt(Q) = sqrt(PQ)</c> on the principal
    /// branch only where at most one of <c>P</c>, <c>Q</c> is negative; with both negative
    /// the two sides differ by sign. The rule decides that from the real roots of the bases
    /// and declines otherwise, and the decline is pinned here beside the answers. Every answer
    /// is differentiated back, including at points where one base <em>is</em> negative and
    /// the integrand is real all the same.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class CombinedRadicalsTest
    {
        private static void DifferentiatesBack(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            var original = integrand.ToEntity();
            var compared = 0;
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-8,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 3,
                $"only {compared} of {points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// A square root of a polynomial with a repeated factor, the factor taken out of the
        /// root with its sign: <c>sqrt(9 + 3x - 5x^2 + x^3)</c> is <c>sqrt((x - 3)^2 (x + 1))</c>,
        /// which is <c>sgn(x - 3) (x - 3) sqrt(x + 1)</c>, and the sign is a constant on each side
        /// of <c>3</c> that comes out in front of the integral. Timofeev's
        /// <c>1/sqrt(9 + 3x - 5x^2 + x^3)</c> and <c>1/(9 + 3x - 5x^2 + x^3)^(3/2)</c>, checked on
        /// both sides of the repeated root.
        /// </summary>
        [Theory]
        [InlineData("1/sqrt(9 + 3*x - 5*x^2 + x^3)")]
        [InlineData("1/(9 + 3*x - 5*x^2 + x^3)^(3/2)")]
        [InlineData("x/sqrt(x^4 + 2*x^3 + x^2)")]
        [InlineData("sqrt(x^3 + 2*x^2 + x)")]
        public void ARepeatedFactorComesOutOfTheRootWithItsSign(string integrand)
        {
            DifferentiatesBack(integrand, new[] { 0.3, 1.1, 2.4, 3.6, 5.2 });
            Assert.Contains("sgn(", integrand.ToEntity().Integrate("x").Stringize());
        }

        /// <summary>
        /// The power of x common to every term of a root of a sum with symbols in it:
        /// <c>sqrt(a x^2 + b x^5)</c> is <c>|x| sqrt(a + b x^3)</c>, and the sign of x comes out in
        /// front where the power it leaves is odd; <c>sqrt(b x^(2/3) + a x)</c> is
        /// <c>x^(1/3) sqrt(b + a x^(1/3))</c> times a factor that is constant where x is not zero.
        /// Checked with the symbols pinned after integrating, on both sides of 0 where x is
        /// under a whole power.
        /// </summary>
        [Theory]
        [InlineData("1/sqrt(a*x^2 + b*x^5)", new[] { -1.1, -0.6, 0.4, 1.2, 2.3 })]
        [InlineData("1/sqrt(x^2*(a + b*x^3))", new[] { -1.1, -0.6, 0.4, 1.2, 2.3 })]
        [InlineData("x/(a*x^2 + b*x^3)^(3/2)", new[] { -1.5, -0.6, 0.4, 1.2, 2.3 })]
        [InlineData("sqrt(a*x^4 + b*x^6)", new[] { -1.5, -0.6, 0.4, 1.2, 2.3 })]
        // A fractional power of x, and the factor that is constant in front: real where x is positive.
        [InlineData("1/(x*sqrt(b*x^(2/3) + a*x))", new[] { 0.3, 0.9, 1.7, 2.6 })]
        [InlineData("1/(x^2*(b*x^(1/2) + a*x)^(1/2))", new[] { 0.3, 0.9, 1.7, 2.6 })]
        public void APowerOfXComesOutOfARootWithSymbolsInIt(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Substitute("C", 0).Differentiate("x").Substitute("a", 1.3).Substitute("b", 0.7);
            var original = integrand.ToEntity().Substitute("a", 1.3).Substitute("b", 0.7);
            var compared = 0;
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-8,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} of {points.Length} points were comparable for {integrand}");
        }

        /// <summary>Inside <c>(-1, 1)</c>, where <c>sqrt(1 - x^2)</c> is real.</summary>
        private static readonly double[] InsideTheUnitInterval = { -0.7, -0.3, 0.2, 0.55, 0.85 };

        /// <summary>
        /// Two radicals above or below the bar, and the shapes by parts leaves them in:
        /// Charlwood's <c>arcsin(x)/(1 + x^2)^(3/2)</c> and the two logarithms.
        /// </summary>
        [Theory]
        [InlineData("x/(sqrt(1 + x^2)*sqrt(1 - x^2))")]
        [InlineData("x*sqrt(1 + x^2)*sqrt(1 - x^2)")]
        [InlineData("arcsin(x)/(1 + x^2)^(3/2)")]
        [InlineData("ln(x + sqrt(1 + x^2))/(1 - x^2)^(3/2)")]
        [InlineData("x*ln(x + sqrt(1 - x^2))/sqrt(1 - x^2)")]
        public void TwoRadicalsWrittenAsOne(string integrand) => DifferentiatesBack(integrand, InsideTheUnitInterval);

        /// <summary>
        /// <c>sqrt(x^2 - 1)</c> is negative inside the unit interval and <c>1 + x^2</c> never
        /// is, so the identity holds on the whole line; checked past <c>1</c>, where the
        /// integrand is real.
        /// </summary>
        [Theory]
        [InlineData("x/(sqrt(1 + x^2)*sqrt(x^2 - 1))")]
        [InlineData("ln(x + sqrt(x^2 - 1))/(1 + x^2)^(3/2)")]
        public void OneBaseMayBeNegative(string integrand) => DifferentiatesBack(integrand, new[] { 1.2, 1.6, 2.3, 3.1 });

        /// <summary>
        /// A root of a quotient of polynomials written as a quotient of roots, where that is
        /// exact: <c>sqrt(P/Q) = sqrt(P)/sqrt(Q)</c> unless <c>Q &lt; 0 &lt; P</c> somewhere on the
        /// reals. Charlwood's <c>arcsin(x/sqrt(1 - x^2))</c> by parts against one leaves
        /// <c>x (1 - x^2)^(-3/2)/sqrt((1 - 2x^2)/(1 - x^2))</c>, which no rule reads and which
        /// split is <c>x/((1 - x^2) sqrt(1 - 2x^2))</c>. Checked where the arcsine is real,
        /// <c>|x| &lt; 1/sqrt(2)</c>, on both sides of zero.
        /// </summary>
        [Theory]
        [InlineData("arcsin(x/sqrt(1 - x^2))")]
        [InlineData("x*(1 - x^2)^(-3/2)/sqrt((1 - 2*x^2)/(1 - x^2))")]
        public void ARootOfAQuotientIsSplit(string integrand) => DifferentiatesBack(integrand, new[] { -0.6, -0.3, 0.2, 0.45, 0.65 });

        /// <summary>
        /// <c>sqrt((1 + x)/(3 + 2x))</c> is not answered: <c>3 + 2x &lt; 0 &lt; 1 + x</c> nowhere, so
        /// the split is exact, but the two roots it leaves are both negative below
        /// <c>-3/2</c> and no rule takes them further; and <c>x/((1 - x^2) sqrt((1 - 2x^2)/(1 - x^2)))</c>
        /// splits and gathers to <c>x/(sqrt(1 - x^2) sqrt(1 - 2x^2))</c>, two roots both
        /// negative past <c>1</c>, where the integrand is real, so those are not combined
        /// either. Either verdict but a wrong answer.
        /// </summary>
        [Theory]
        [InlineData("sqrt((1 + x)/(3 + 2*x))")]
        [InlineData("sqrt((1 + x^2)/(1 - x^2))")]
        [InlineData("x/((1 - x^2)*sqrt((1 - 2*x^2)/(1 - x^2)))")]
        public void ARootOfAQuotientTheRulesBehindCannotUse(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;
            DifferentiatesBack(integrand, new[] { -0.7, -0.3, 0.2, 0.55, 0.85 });
        }

        /// <summary>
        /// Over the irreducible factors, where that cancels something: <c>sqrt(1 - x^4)</c> over
        /// <c>sqrt(1 - x^2)</c> -- what by parts leaves from Charlwood's <c>x^3 arcsin(x)/sqrt(1 - x^4)</c>
        /// -- is <c>sqrt(1 + x^2)</c>, and the two roots as written share nothing. The sign of
        /// the factorization goes into whichever factor lets the split be exact: <c>1 - x^4</c>
        /// is <c>-(x + 1)(x - 1)(x^2 + 1)</c>, and with the sign on <c>x + 1</c> two factors are
        /// negative inside the unit interval, where with it on <c>x - 1</c> at most one is
        /// anywhere.
        /// </summary>
        [Theory]
        [InlineData("x^3*arcsin(x)/sqrt(1 - x^4)")]
        [InlineData("sqrt(1 - x^4)/sqrt(1 - x^2)")]
        public void OverTheIrreducibleFactors(string integrand) => DifferentiatesBack(integrand, InsideTheUnitInterval);

        /// <summary>
        /// <c>sqrt(x^4 - 1)/sqrt(x^2 - 1)</c> does not split: <c>x^4 - 1</c> is <c>(x - 1)(x + 1)(x^2 + 1)</c>
        /// and below <c>-1</c> two of those are negative whichever carries the sign, where the
        /// integrand is real. Either verdict but a wrong answer, on both sides.
        /// </summary>
        [Theory]
        [InlineData("sqrt(x^4 - 1)/sqrt(x^2 - 1)")]
        [InlineData("x^3*arcsec(x)/sqrt(x^4 - 1)")]
        public void WhereNoPlacementOfTheSignSplitsExactly(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;
            DifferentiatesBack(integrand, new[] { -3.1, -2.2, -1.4, 1.3, 2.1, 3.4 });
        }

        /// <summary>
        /// A small power of a sum of radicals, as a factor, is written out first:
        /// Bondarenko's <c>1/(sqrt(1 - x) + sqrt(1 + x))^2</c> is <c>1/(2 + 2 sqrt(1 - x^2))</c>,
        /// which Euler's substitution answers.
        /// </summary>
        [Theory]
        [InlineData("1/(sqrt(1 - x) + sqrt(1 + x))^2")]
        [InlineData("(sqrt(1 - x) + sqrt(1 + x))^2")]
        public void APowerOfASumOfRadicalsIsWrittenOut(string integrand) => DifferentiatesBack(integrand, InsideTheUnitInterval);

        /// <summary>
        /// <c>sqrt(x - 1) sqrt(x - 2)</c> is <c>-sqrt((x - 1)(x - 2))</c> below <c>1</c>, where both
        /// bases are negative, so the two do not combine as they stand; they combine up to
        /// the sign <c>(1 + sgn(x - 1) + sgn(x - 2) - sgn(x - 1) sgn(x - 2))/2</c>, which is
        /// <c>-1</c> there and <c>1</c> elsewhere, exactly, and a constant between the roots
        /// that goes in front of the integral. Timofeev's
        /// <c>sqrt(x - 5) sqrt(x + 3)/((x - 1)(x^2 - 25))</c> had no antiderivative for it. The
        /// answer is differentiated back below <c>1</c> as well as above <c>2</c>, and between
        /// them, where the integrand is not real and the identity holds all the same.
        /// </summary>
        [Theory]
        [InlineData("sqrt(x - 1)*sqrt(x - 2)")]
        [InlineData("x/(sqrt(x - 1)*sqrt(x - 2))")]
        [InlineData("sqrt(x - 5)*sqrt(x + 3)/((x - 1)*(x^2 - 25))")]
        public void TwoNegativeBasesAreCombinedWithASign(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.Contains("sgn(", integral.Stringize());
            DifferentiatesBack(integrand, new[] { -6.5, -4.4, -1.5, -0.4, 0.3, 1.6, 2.4, 3.1, 4.7, 5.6, 7.2 });
        }

        /// <summary>
        /// The same two bases where the combining rule declines at the top: the rule for two
        /// linear radicals answers through the quotient of the two roots,
        /// <c>sqrt(x - 1)/sqrt(x - 2)</c>, and needs no sign, since a whole power of a quotient
        /// of principal roots is the quotient of the powers for every complex <c>x</c>.
        /// Differentiated back on both sides of the roots and between them.
        /// </summary>
        [Fact]
        public void AQuotientOfTwoSquareRootsNeedsNoSign()
        {
            var integral = "1/(sqrt(x - 1)*sqrt(x - 2))".ToEntity().Integrate("x");
            Assert.DoesNotContain("sgn(", integral.Stringize());
            DifferentiatesBack("1/(sqrt(x - 1)*sqrt(x - 2))", new[] { -6.5, -4.4, -1.5, -0.4, 0.3, 1.6, 2.4, 3.1, 4.7, 5.6, 7.2 });
        }

        /// <summary>
        /// A root above the bar over a root below it combines as the quotient it is where the
        /// product would not: <c>sqrt(x^4 - 1)/sqrt(x^2 - 1)</c> has two negative bases inside
        /// the unit interval, where <c>sqrt(P) sqrt(Q)</c> is <c>-sqrt(PQ)</c>, and is
        /// <c>sqrt(x^2 + 1)</c> there all the same, since the two <c>i</c>s cancel in a quotient.
        /// Checked inside the interval and outside it. And with a root below the bar alone
        /// negative -- <c>sqrt(x^2 + 1)/sqrt(x^2 - 1)</c> inside the interval, where
        /// <c>1/sqrt(Q)</c> is <c>-i/sqrt|Q|</c> and <c>sqrt(1/Q)</c> is <c>+i/sqrt|Q|</c> -- the
        /// identity fails, and the rule leaves it: either verdict but a wrong answer.
        /// </summary>
        [Theory]
        [InlineData("sqrt(x^4 - 1)/sqrt(x^2 - 1)")]
        [InlineData("x*sqrt(x^4 - 1)/sqrt(x^2 - 1)")]
        [InlineData("sqrt(x^4 - 1)/(x^2*sqrt(1 - 1/x^2))")]
        public void RootsCombinedAsAQuotient(string integrand)
            => DifferentiatesBack(integrand, new[] { -2.6, -1.5, -0.7, -0.3, 0.2, 0.55, 0.85, 1.4, 2.3 });

        [Theory]
        [InlineData("sqrt(x^2 + 1)/sqrt(x^2 - 1)")]
        [InlineData("sqrt(x^2 + 1)/(x*sqrt(x^2 - 1))")]
        public void ARootBelowTheBarAloneNegativeIsNotCombined(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;   // unanswered is a legitimate verdict; a wrong answer is not
            DifferentiatesBack(integrand, new[] { -2.6, -1.5, -0.7, -0.3, 0.2, 0.55, 0.85, 1.4, 2.3 });
        }

        /// <summary>
        /// The quotient identity at a point inside the unit interval, where both bases are
        /// negative: <c>sqrt(-15/16)/sqrt(-3/4)</c> is <c>sqrt(5/4)</c>, which is
        /// <c>sqrt((x^4 - 1)/(x^2 - 1))</c> there; and <c>1/sqrt(-3/4)</c> is
        /// <c>-sqrt(1/(-3/4))</c>, which is why a root below the bar alone negative is not.
        /// </summary>
        [Fact]
        public void TheQuotientIdentityHoldsWithTwoNegativeBasesAndFailsWithOneBelow()
        {
            var left = "sqrt(x^4 - 1)/sqrt(x^2 - 1)".ToEntity().Substitute("x", 0.5).EvalNumerical();
            var right = "sqrt((x^4 - 1)/(x^2 - 1))".ToEntity().Substitute("x", 0.5).EvalNumerical();
            Assert.True(Math.Abs((double)(left - right).RealPart) + Math.Abs((double)(left - right).ImaginaryPart) < 1e-12, $"{left} against {right}");
            var below = "1/sqrt(x^2 - 1)".ToEntity().Substitute("x", 0.5).EvalNumerical();
            var underOneRoot = "sqrt(1/(x^2 - 1))".ToEntity().Substitute("x", 0.5).EvalNumerical();
            Assert.True(Math.Abs((double)(below + underOneRoot).ImaginaryPart) < 1e-12, $"{below} against {underOneRoot}");
        }

        /// <summary>
        /// The identity itself, at a point where both bases are negative, so that the
        /// condition the rule checks is pinned as a fact and not as a convention:
        /// <c>sqrt(-1) sqrt(-2)</c> is <c>-sqrt(2)</c> and <c>sqrt(2)</c> is not.
        /// </summary>
        [Fact]
        public void TheIdentityFailsWithTwoNegativeBases()
        {
            var left = "sqrt(x - 1)*sqrt(x - 2)".ToEntity().Substitute("x", 0).EvalNumerical();
            var right = "sqrt((x - 1)*(x - 2))".ToEntity().Substitute("x", 0).EvalNumerical();
            Assert.True(Math.Abs((double)(left + right).RealPart) < 1e-12, $"{left} against {right}");
            Assert.True(Math.Abs((double)left.RealPart + Math.Sqrt(2)) < 1e-12, $"{left}");
        }

        /// <summary>
        /// A constant multiple of <c>D'/D</c> for a sum <c>D</c> below the bar holding a root,
        /// decided at sampled points and answered as that multiple of <c>ln(D)</c>. Hearn's
        /// <c>x((x^2 - 1) sqrt(x^2 - 4) + (x^2 - 4) sqrt(x^2 - 1))/((x^2 - 1)(x^2 - 4)(1 + sqrt(x^2 - 4) + sqrt(x^2 - 1)))</c>
        /// is the derivative of its denominator over it, and the substitution that reads
        /// <c>u = D</c> was declined for the quotient by <c>D'</c> it could not simplify, a
        /// search past the budget. The answer is checked against the integrand before it is
        /// returned, so a quotient that is not constant -- the second row, one root's
        /// derivative missing -- goes the way it went.
        /// </summary>
        [Theory]
        [InlineData("x*(-sqrt(-4+x^2)+x^2*sqrt(-4+x^2)-4*sqrt(-1+x^2)+x^2*sqrt(-1+x^2))/((4-5*x^2+x^4)*(1+sqrt(-4+x^2)+sqrt(-1+x^2)))", "ln(1 + sqrt(-4 + x ^ 2) + sqrt(-1 + x ^ 2)) + C")]
        [InlineData("3*(1/(2*sqrt(x)) + 1)/(sqrt(x) + x)", "3 * ln(sqrt(x) + x) + C")]
        public void ADerivativeOfARadicalSumOverIt(string integrand, string expected)
        {
            Assert.Equal(expected.ToEntity(), integrand.ToEntity().Integrate("x"));
            DifferentiatesBack(integrand, new[] { 2.3, 3.1, 4.7, 6.2 });
        }

        [Fact]
        public void AQuotientThatIsNotConstantIsNotALogarithm()
        {
            var integral = "x/(sqrt(x) + x + 1)".ToEntity().Integrate("x");
            Assert.DoesNotContain("ln(sqrt(x) + x + 1)", integral.Stringize());
            DifferentiatesBack("x/(sqrt(x) + x + 1)", new[] { 0.3, 1.1, 2.3, 4.7 });
        }
    }
}
