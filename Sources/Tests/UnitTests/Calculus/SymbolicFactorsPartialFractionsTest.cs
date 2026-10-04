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
    /// Partial fractions over a denominator <b>written</b> as a product of distinct linear and
    /// quadratic factors whose coefficients are symbols, by undetermined coefficients.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>1/((x + 3)(x^2 + 5))</c> came out and <c>1/((x + a)(x^2 + b))</c> did not: the split
    /// over the rationals cannot read a symbol, and no other split existed. The factors were
    /// already in hand and each is a shape the integrator answers on its own.
    /// </para>
    /// <para>
    /// The decomposition solves a linear system whose entries are the parameters, and a
    /// symbolic pivot is a judgement rather than a proof — so the identity is checked at
    /// sampled points with the symbols pinned before anything is returned, and these tests
    /// check the antiderivative the same way. Where a piece lands on the quadratic rule with an
    /// undecidable discriminant the answer is a piecewise, which is correct and is what the
    /// differentiate-back check sees through numerically.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class SymbolicFactorsPartialFractionsTest
    {
        private static readonly double[] Points = { 0.31, 0.77, 1.43, 2.19 };

        private static void DifferentiatesBack(string integrand, string variable, params (string, double)[] pins)
        {
            var integral = integrand.ToEntity().Integrate(variable);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate(variable);
            Entity original = integrand.ToEntity();
            foreach (var (name, value) in pins)
            {
                derivative = derivative.Substitute(name, value);
                original = original.Substitute(name, value);
            }

            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute(variable, at).EvalNumerical();
                var want = original.Substitute(variable, at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/d{variable} of the antiderivative of {integrand} is {got} at {variable} = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 3,
                $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// A linear factor beside a quadratic one, and two quadratics, with a symbol in each.
        /// </summary>
        [Theory]
        [InlineData("1/((x + a)*(x^2 + b))")]
        [InlineData("1/((x^2 + a)*(x + 1))")]
        [InlineData("1/((a + x^2)*(b + x^2))")]
        [InlineData("1/((x^2 + 4)*(x^2 + b))")]
        [InlineData("x/((a^2 + x^2)*(b^2 + x^2))")]
        [InlineData("(x + 1)/((x + a)*(x^2 + b))")]
        [InlineData("x^2/((x + a)*(x^2 + b))")]
        public void DistinctSymbolicFactors(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3));

        /// <summary>
        /// Three factors, so the system is larger than one pair and the elimination has to
        /// pivot past a symbol.
        /// </summary>
        [Theory]
        [InlineData("1/((x + a)*(x + b)*(x^2 + 1))")]
        [InlineData("x/((x + a)*(x^2 + b)*(x^2 + 2))")]
        public void ThreeFactors(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3));

        /// <summary>
        /// A parameter that multiplies the variable, and a factor with a constant in front of
        /// the product — Moses's <c>-B (A^2 + B^2) / ((1 + w^2)(B^2 - A^2 w^2))</c> from the
        /// Rubi suite, integrated in <c>w</c>.
        /// </summary>
        [Theory]
        [InlineData("1/((1 + w^2)*(B^2 - A^2*w^2))")]
        [InlineData("-B*(A^2 + B^2)/((1 + w^2)*(B^2 - A^2*w^2))")]
        public void AParameterOnTheVariable(string integrand)
            => DifferentiatesBack(integrand, "w", ("A", 0.6), ("B", 1.9));

        /// <summary>
        /// The numeric spellings, which the split over the rationals still takes first and
        /// which keep their exact answers.
        /// </summary>
        [Theory]
        [InlineData("1/((x + 3)*(x^2 + 5))")]
        [InlineData("1/((3 + x^2)*(5 + x^2))")]
        public void TheNumericSpellingsStillAnswer(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Stringize();
            Assert.DoesNotContain("piecewise", integral);
            DifferentiatesBack(integrand, "x");
        }

        /// <summary>
        /// A power of x beside a block of the third degree or more with a symbol in it, split at
        /// the power of x: <c>1/(x (x^3 + c))</c> was declined where <c>1/(x (x^3 + 2))</c> was
        /// answered. Rubi's <c>x^m (a + b x^n)^p</c>, by the hundred.
        /// </summary>
        [Theory]
        [InlineData("1/(x*(x^3 + c))")]
        [InlineData("1/(x^2*(x^3 + c))")]
        [InlineData("1/(x*(x^3 + c)^2)")]
        [InlineData("1/(x^4*(a + b*x^3))")]
        [InlineData("1/(x*(a + b*x^5))")]
        [InlineData("(1 + x)/(x^2*(x^3 + c))")]
        [InlineData("(c + d*x + e*x^2)/(x^2*(a + b*x^3))")]
        [InlineData("(c + d*x^3 + e*x^6)/(x^4*(a + b*x^3)^2)")]
        public void APowerOfXBesideASymbolicBlock(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("e", 0.6));

        /// <summary>
        /// And a power of another linear beside such a block, split at its root:
        /// <c>1/((x - c)(a + b x^3))</c> was declined, and so was the same over <c>g + h x</c>.
        /// </summary>
        [Theory]
        [InlineData("1/((x - c)*(a + b*x^3))")]
        [InlineData("1/((x - c)^2*(a + b*x^4))")]
        [InlineData("(1 + x)/((x + 2)^2*(a + b*x^3))")]
        [InlineData("1/((g + h*x)*(a + b*x^3))")]
        public void APowerOfALinearBesideASymbolicBlock(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("g", 1.1), ("h", 0.6));

        /// <summary>
        /// Blocks that are each a polynomial in one power of x past the second, split in
        /// <c>u = x^n</c>, where they are linear: <c>1/((a + b x^3)(c + d x^3))</c> was declined.
        /// With a power of x beside them as well, both splits in turn; and one block quadratic in
        /// <c>u</c> is split at its two roots.
        /// </summary>
        [Theory]
        [InlineData("1/((a + b*x^3)*(c + d*x^3))")]
        [InlineData("x/((a + b*x^3)*(c + d*x^3))")]
        [InlineData("x^4/((a + b*x^3)*(c + d*x^3))")]
        [InlineData("1/((a + b*x^4)*(c + d*x^4))")]
        [InlineData("1/(x^2*(a + b*x^3)*(c + d*x^3))")]
        // One block quadratic in x^n is two at its roots.
        [InlineData("1/(a + b*x^3 + c*x^6)")]
        [InlineData("x/(a + b*x^3 + c*x^6)")]
        [InlineData("(1 + x^4)/(1 + b*x^4 + x^8)")]
        // Two blocks in x^2, which the split over written factors reads only while both are quadratic.
        [InlineData("1/(x^4*(d + e*x^2)*(a + c*x^4))")]
        public void SymbolicBlocksInOnePowerOfX(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1));

        /// <summary>
        /// The same with the block's roots complex, <c>b^2 &lt; 4 a c</c> as pinned. The split at
        /// the roots is last, since a rule after it that branches on the sign of a root has no
        /// branch for a complex one: the third row is answered whole by <c>v = x^2</c>, and split
        /// first it had no value anywhere.
        /// </summary>
        [Theory]
        [InlineData("1/(a + b*x^3 + c*x^6)")]
        [InlineData("(1 + x^4)/(1 + b*x^4 + x^8)")]
        [InlineData("(d + e*x^4)/(x^3*(a + b*x^4 + c*x^8))")]
        public void OneBlockWhoseRootsAreComplex(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 0.9), ("c", 1.3), ("d", 1.1), ("e", 0.6));

        /// <summary>
        /// A quotient with x below a bar inside it is written over one bar first:
        /// <c>1/(a + b/x)</c> was declined where <c>x/(a x + b)</c> was answered, and so was the
        /// numeric <c>1/(2 + 3/x^2)^3</c>. The power of x the bars were cleared with is taken out
        /// of both sides again, since the rules after it read the two as coprime: Rubi's
        /// <c>1/((a + c/x^2 + b/x) x (d + e x)^2)</c> was answered wrongly without that.
        /// </summary>
        [Theory]
        [InlineData("1/(a + b/x)")]
        [InlineData("1/(a + b/x^2)^2")]
        [InlineData("1/(a + b/x^3)")]
        [InlineData("1/(2 + 3/x^2)^3")]
        [InlineData("1/((a + c/x^2 + b/x)*x*(d + e*x)^2)")]
        [InlineData("x^2/((a + c/x^2 + b/x)*(d + e*x)^2)")]
        public void AQuotientWithXBelowABarInsideIt(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("e", 0.6));

        /// <summary>
        /// A product of sums in a power of x whose exponent is a symbol, written out: each term is
        /// a power of x, answered at once, and no rule reads the product as a polynomial.
        /// </summary>
        [Theory]
        [InlineData("(a + b*x^n)*(c + d*x^n)^3")]
        [InlineData("(c + d*x^(n - 1))*(a + b*x^n)^3")]
        public void AProductOfSumsInASymbolicPowerOfX(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("n", 1.3));

        /// <summary>
        /// What the splits above leave whole: a numerator that x divides as well, and a quotient
        /// written over one bar that is not of polynomials. Split, each part would be the whole
        /// question asked again a level down, and the rules that answer these at the top would not
        /// be reached in the time they take there.
        /// </summary>
        [Theory]
        [InlineData("(p*x^3 + q*x^4 + r*x^5)/(x*(a + b*x^2 + c*x^4)^2)")]
        [InlineData("sec(c + d*x)^8/(a + i*a*tan(c + d*x))^4")]
        public void WhatTheSplitsLeaveWhole(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.81), ("b", 2.18), ("c", 2.55), ("d", 1.1), ("p", 0.7), ("q", 1.07), ("r", 1.44));

        /// <summary>
        /// A repeated <b>linear</b> factor is one block, <c>P/(a + b u)^k</c>, which the rule for
        /// a polynomial over a power of a linear reads; Welz's <c>1/(a + b e^(p x))^2</c> is
        /// this under <c>u = e^(p x)</c>.
        /// </summary>
        [Theory]
        [InlineData("1/(u*(a + b*u)^2)")]
        [InlineData("1/(u*(a + b*u)^3)")]
        [InlineData("1/((u + a)^2*(u^2 + b))")]
        [InlineData("1/(a + e^(p*u)*b)^2")]
        public void ARepeatedSymbolicLinearFactor(string integrand)
            => DifferentiatesBack(integrand, "u", ("a", 1.7), ("b", 2.3), ("p", 0.6));

        /// <summary>
        /// The rule the block above lands on: a polynomial over a power of a linear with a
        /// symbol in it, under <c>t = a + b x</c>. <c>1/(a + b x)^2</c> was answered before as
        /// a piecewise on a discriminant identically zero, through the quadratic rule; that
        /// still runs first and this is what the split hands on.
        /// </summary>
        [Theory]
        [InlineData("(c + d*x)/(a + b*x)^2")]
        [InlineData("x/(a + b*x)^2")]
        [InlineData("x^2/(a + b*x)^3")]
        [InlineData("(1 + x + x^2)/(a + b*x)^4")]
        public void APolynomialOverASymbolicLinearPower(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1));

        /// <summary>
        /// A repeated symbolic quadratic was declined: the coefficients of its decomposition
        /// are a linear system with <c>a</c> in every entry, whose zero test on entities is
        /// numeric. Solved over polynomials in <c>a</c>, fraction-free, the system is exact
        /// and the answer is a piecewise on the sign of <c>a</c> -- checked on both sides of it.
        /// </summary>
        [Theory]
        [InlineData("1/((x^2 + a)^2*(x + 1))", 1.7)]
        [InlineData("1/((x^2 + a)^2*(x + 1))", -0.7)]
        public void ARepeatedSymbolicQuadratic(string integrand, double a)
            => DifferentiatesBack(integrand, "x", ("a", a));

        /// <summary>
        /// Linear factors with symbols in their coefficients are decomposed by the derivatives
        /// at the roots, and the answer is a line: solved as a system, fraction-free,
        /// <c>1/((a + b x)(f + g x)^3)</c> came out twenty kilobytes long with
        /// <c>210 a^8 b^2 f^24 g^14</c> in every term, and its fourth power did not evaluate.
        /// </summary>
        [Theory]
        [InlineData("1/((a + b*x)*(f + g*x)^3)")]
        [InlineData("1/((a + b*x)*(f + g*x)^4)")]
        [InlineData("x^2/((a + b*x)*(c + d*x)*(f + g*x)^2)")]
        public void RepeatedSymbolicLinearFactorsAreDecomposedByDerivatives(string integrand)
        {
            DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("f", 3.1), ("g", 0.7));
            Assert.True(integrand.ToEntity().Integrate("x").Stringize().Length < 4000, "the answer is a page");
        }

        /// <summary>
        /// And beside a quadratic factor the linear blocks are still taken by the derivatives,
        /// with what they leave of the numerator over the quadratic alone: Rubi's
        /// <c>tan^4 (A + B tan)/(a + b tan)^4</c> is <c>u^4 (A + B u)/((a + b u)^4 (1 + u^2))</c>
        /// under the tangent, and solved as a system it came out in <c>a^63 b^10</c> and did
        /// not evaluate within the corpus's budget.
        /// </summary>
        [Theory]
        [InlineData("x^4*(A + B*x)/((a + b*x)^4*(1 + x^2))")]
        [InlineData("1/((a + b*x)^2*(1 + x^2))")]
        [InlineData("x/((a + b*x)*(x^2 + 2))")]
        [InlineData("x^3/((a + b*x)^2*(1 + x^2)*(4 + x^2))")]
        [InlineData("tan(x)^4*(A + B*tan(x))/(a + b*tan(x))^4")]
        public void SymbolicLinearBlocksBesideAQuadratic(string integrand)
        {
            DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("A", 0.4), ("B", 1.1));
            Assert.True(integrand.ToEntity().Integrate("x").Stringize().Length < 4000, "the answer is a page");
        }

        /// <summary>
        /// A polynomial over a power of a linear beside a radical is written in powers of the
        /// linear at its root, so that what the power divides goes over the radical alone:
        /// <c>(A + B x + C x^2 + D x^3)/((a + b x) sqrt(c + d x))</c> went through the
        /// substitution <c>u = sqrt(c + d x)</c> term by term, a cubic in <c>u^2 - c</c> over a
        /// symbolic quadratic each time, in a hundred kilobytes of piecewise that did not
        /// evaluate within the corpus's budget; reduced it is a quadratic over the root and
        /// one <c>p_0/((a + b x) sqrt(c + d x))</c>, under four kilobytes.
        /// </summary>
        [Theory]
        [InlineData("(A + B*x + F*x^2 + G*x^3)/((a + b*x)*sqrt(c + d*x))")]
        [InlineData("x^3/((a + b*x)*sqrt(c + d*x))")]
        [InlineData("(A + B*x + F*x^2)/((a + b*x)*(c + d*x)^(1/3))")]
        public void APolynomialOverALinearBesideARadicalIsReduced(string integrand)
        {
            DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("A", 0.5), ("B", 1.3), ("F", 0.7), ("G", 2.1));
            var length = integrand.ToEntity().Integrate("x").Stringize().Length;
            Assert.True(length < 4000, $"{length} characters of answer for {integrand}");
        }

        /// <summary>
        /// A repeated symbolic quadratic beside the linears: the numerators over its powers are
        /// the digits of the numerator over the other factors written in powers of the
        /// quadratic, computed in the ring modulo its power with the inverse by Newton's
        /// iteration. <c>1/((a + b x)^2 (c + d x)^(3/2))</c> is, under <c>u = sqrt(c + d x)</c>,
        /// <c>1/(u^2 (K + u^2)^2)</c> with a symbolic <c>K</c>, and went to the Hermite reduction
        /// for a hundred kilobytes in thirty seconds; it is eight kilobytes in half a second now.
        /// </summary>
        [Theory]
        [InlineData("1/((a + b*x)^2*(c + d*x)^(3/2))", 10000)]
        [InlineData("1/(x^2*(a + b*x^2)^2)", 4000)]
        [InlineData("1/(x^3*(a + b*x + c*x^2)^2)", 10000)]
        [InlineData("x/((a + b*x)*(c + x^2)^2)", 4000)]
        [InlineData("(A + B*x)/((a + b*x)*(1 + x^2)^3)", 4000)]
        public void ARepeatedSymbolicQuadraticBesideALinear(string integrand, int atMost)
        {
            DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("A", 0.5), ("B", 1.3));
            var length = integrand.ToEntity().Integrate("x").Stringize().Length;
            Assert.True(length < atMost, $"{length} characters of answer for {integrand}");
        }

        /// <summary>
        /// A coefficient of the decomposition is in lowest terms over the symbols, its
        /// rational content included. The polynomial gcd normalizes its divisor to whole
        /// coprime coefficients, so <c>-1024/(1024 a)</c> stayed as it was, and Newton's
        /// iteration for the inverse modulo <c>(1 + u^2)^6</c>, which squares its iterate every
        /// round, had integers of twelve hundred digits by the third round and spent its
        /// minute in their gcd: <c>tanh(x)^6/(a + a sech(x))</c> went from 650 ms to a timeout.
        /// And the values the elimination solves for, where symbols are among them, are put
        /// in lowest terms before the terms are built: <c>csch(x)^5/(a + b cosh(x))</c>, after
        /// the Hermite reduction under <c>u = e^x</c>, had two thousand nodes of <c>a</c> and
        /// <c>b</c> in every term, and the terms went round the chain and did not return.
        /// And a constant factor is not a symbolic coefficient: with the content of
        /// <c>a (1 + u^2) + 2 a u</c> out, the denominator is a rational one with <c>a</c> in
        /// front, whose <c>(1 + u)^2</c> the refactoring over the rationals reads, and the
        /// answer is a page shorter than the split's.
        /// </summary>
        [Theory]
        [InlineData("1/(u*(u^2 + 1)^6*(a*(u^2 + 1) + 2*a*u))", 1000)]
        [InlineData("u^5/((u^2 - 1)^5*(2*a*u + b*(u^2 + 1)))", 30000)]
        [InlineData("tanh(u)^6/(a + a*sech(u))", 8000)]
        [InlineData("csch(u)^5/(a + b*cosh(u))", 30000)]
        public void TheCoefficientsAreInLowestTerms(string integrand, int atMost)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var integral = integrand.ToEntity().Integrate("u");
            watch.Stop();
            Assert.True(watch.Elapsed < IntegrationDecline.Guard, $"{integrand} took {watch.Elapsed.TotalSeconds:F1} s");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var length = integral.Stringize().Length;
            Assert.True(length < atMost, $"{length} characters of answer for {integrand}");
            DifferentiatesBack(integrand, "u", ("a", 1.7), ("b", 2.3));
        }

        /// <summary>
        /// Two linear factors with one root are one factor: <c>(a + b x)(a + x b)^2</c> is
        /// how the rules that make a factor monic and gather its powers write it, and as
        /// two distinct factors the decomposition had no answer, its coefficients being
        /// values at a root of the other.
        /// </summary>
        [Theory]
        [InlineData("1/((a + b*x)*(a*g + b*g*x)^2)")]
        [InlineData("1/((c + d*x)*(a + b*x)*(a*g + b*g*x)^2)")]
        public void ProportionalLinearFactorsAreOneFactor(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("g", 0.7));

        /// <summary>
        /// Written factors that share a factor, or repeat one inside, are written over what they
        /// share, the greatest common divisors taken in every variable: Rubi's 1.2.1.2,
        /// <c>(d + e x)^m (a d e + (c d^2 + a e^2) x + c d e x^2)^p</c>, whose quadratic is
        /// <c>(d + e x)(a e + c d x)</c>, and the square <c>c d^2 + 2 c d e x + c e^2 x^2</c>
        /// beside a power of its linear, or with the numerator sharing it. Each was declined or
        /// ran past the corpus's budget.
        /// </summary>
        [Theory]
        [InlineData("1/((d + h*x)*(a*d*h + (c*d^2 + a*h^2)*x + c*d*h*x^2)^2)")]
        [InlineData("(d + h*x)^6/(a*d*h + (c*d^2 + a*h^2)*x + c*d*h*x^2)^4")]
        [InlineData("1/((d + h*x)^2*(c*d^2 + 2*c*d*h*x + c*h^2*x^2)^3)")]
        [InlineData("x^2/((a + b*x)*(a^2 + 2*a*b*x + b^2*x^2))")]
        public void FactorsThatShareOneAreWrittenOverIt(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1), ("h", 0.7));

        /// <summary>
        /// The negative of a square, <c>-(b x^2 - 1)^2</c>, is the square with its sign in front:
        /// a divisor is found up to a unit, and the part the split left was <c>-1</c>. Rubi's
        /// 1.1.2.2 <c>x^4/(a + b x^2)^(3/2)</c> is this under <c>t = x/sqrt(a + b x^2)</c>, and was
        /// answered with the wrong sign.
        /// </summary>
        [Theory]
        [InlineData("(2*x^2/b - 1/b^2)/(-b^2*x^4 + 2*b*x^2 - 1)")]
        [InlineData("x^4/(a + b*x^2)^(3/2)")]
        [InlineData("(c + d*x^3)^2/(a + b*x^3)^(4/3)")]
        public void TheNegativeOfASquareKeepsItsSign(string integrand)
            => DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("c", 0.4), ("d", 1.1));

        /// <summary>
        /// A written power of a linear is integrated as the power it is: expanded,
        /// <c>(f/g + x)^2</c> is a quadratic whose discriminant is zero in a spelling the
        /// quadratic rule did not read as zero, and <c>1/(f/g + x)^2</c> was a piecewise on it.
        /// </summary>
        [Theory]
        [InlineData("1/(f/g + x)^2")]
        [InlineData("1/(f/g + x)^3")]
        [InlineData("b/(a - b*f/g)^2/(f/g + x)^2")]
        public void AWrittenPowerOfALinearIsNotAQuadratic(string integrand)
        {
            DifferentiatesBack(integrand, "x", ("a", 1.7), ("b", 2.3), ("f", 3.1), ("g", 0.7));
            Assert.DoesNotContain("piecewise", integrand.ToEntity().Integrate("x").Stringize());
        }
    }
}
