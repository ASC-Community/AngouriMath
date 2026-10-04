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
    /// A polynomial over a binomial <c>a x^n + b</c>, <c>n >= 3</c>, decomposed at the
    /// <c>n</c>-th roots of <c>-b/a</c> in closed form.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>1/(x^3 + 1)</c> came out and <c>1/(x^3 + 2)</c> did not: the first factors over the
    /// rationals, the second does not, and nothing further was tried. The rule writes the
    /// residue at each root and pairs the conjugates, so every term is a logarithm and an
    /// arctangent and the answer is real wherever the sign of <c>-b/a</c> can be read.
    /// </para>
    /// <para>
    /// Every answer is differentiated back and compared to the integrand at sampled points;
    /// where a parameter is present it is pinned first. The exact cases — <c>x^3 + 1</c>,
    /// <c>x^4 + 1</c>, <c>x^6 + 1</c> — are here to show they still take the split over the
    /// rationals in front of this rule and keep the answer they had.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class BinomialDenominatorIntegralTest
    {
        private static readonly double[] Points = { 0.31, 0.77, 1.43, 2.19, -0.58 };

        private static void DifferentiatesBack(string integrand, params (string, double)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            Entity original = integrand.ToEntity();
            foreach (var (name, value) in pins)
            {
                derivative = derivative.Substitute(name, value);
                original = original.Substitute(name, value);
            }

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
                $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        /// <summary>
        /// The denominators that do not factor over the rationals, at each degree from three
        /// to six, with both signs of the constant.
        /// </summary>
        [Theory]
        [InlineData("1/(x^3 + 2)")]
        [InlineData("1/(x^3 - 2)")]
        [InlineData("1/(5*x^3 - 3)")]
        [InlineData("1/(x^4 + 3)")]
        [InlineData("1/(x^5 + 1)")]
        [InlineData("1/(x^5 + 32)")]
        [InlineData("1/(x^5 - 7)")]
        [InlineData("1/(x^6 + 2)")]
        [InlineData("1/(2 + x^6)")]
        public void AConstantOverABinomial(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A polynomial numerator, monomial by monomial: the residue carries the power, and the
        /// terms for each are summed.
        /// </summary>
        [Theory]
        [InlineData("x/(x^3 + 2)")]
        [InlineData("x^2/(x^5 + 1)")]
        [InlineData("(x^3 + x)/(x^5 + 1)")]
        [InlineData("(1 + 2*x + 3*x^2)/(x^4 + 5)")]
        [InlineData("x^7/(1 + x^12)")]
        public void APolynomialOverABinomial(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A symbol in the binomial. <c>x^3 + a</c> takes <c>rho = a^(1/3)</c> with the roots at
        /// the odd multiples, which is the real answer for <c>a &gt; 0</c>; <c>a x^3 - b</c> takes
        /// <c>(b/a)^(1/3)</c>, the real answer for <c>b/a &gt; 0</c>. Pinned to those.
        /// </summary>
        [Theory]
        [InlineData("1/(x^3 + a)")]
        [InlineData("1/(a*x^3 - b)")]
        [InlineData("1/(x^5 + a^5)")]
        [InlineData("1/(x^4 + a)")]
        [InlineData("x/(a*x^3 + b)")]
        public void ASymbolicBinomial(string integrand)
            => DifferentiatesBack(integrand, ("a", 1.7), ("b", 2.3));

        /// <summary>
        /// What factors over the rationals keeps the exact answer it had: the split in front of
        /// this rule takes it, and no root of unity appears.
        /// </summary>
        [Theory]
        [InlineData("1/(x^3 + 1)", "sqrt(3)")]
        [InlineData("1/(x^6 + 1)", "sqrt(3)")]
        [InlineData("1/(x^4 + 1)", "sqrt(2)")]
        public void AnExactFactorisationIsStillTakenFirst(string integrand, string expectedRadical)
        {
            var integral = integrand.ToEntity().Integrate("x").Stringize();
            Assert.DoesNotContain("integral(", integral);
            Assert.DoesNotContain("cos(", integral);
            Assert.Contains(expectedRadical, integral);
            DifferentiatesBack(integrand);
        }

        /// <summary>
        /// What is not this shape is not this rule's to claim: a trinomial with no rational root.
        /// It is answered by the sum over its roots that
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1285">#1285</a> is about,
        /// so the answer is that sum rather than this rule's cosines of roots of unity, and it
        /// differentiates back.
        /// </summary>
        [Theory]
        [InlineData("1/(x^3 + x + 1)")]
        public void OutsideTheShapeTheSumOverTheRootsAnswers(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.Contains(integral.Nodes, node => node is Entity.SumOverSetf);
            Assert.DoesNotContain("cos(", integral.Stringize());
            DifferentiatesBack(integrand);
        }

        /// <summary>
        /// A repeated binomial is the Hermite reduction's first, and what that leaves over the
        /// binomial itself is this rule's: <c>1/(x^3 + 2)^2</c> is <c>x/(6(x^3 + 2))</c> plus a
        /// third of <c>int 1/(x^3 + 2)</c>.
        /// </summary>
        [Theory]
        [InlineData("1/(x^3 + 2)^2")]
        [InlineData("x/(x^5 + 1)^2")]
        public void ARepeatedBinomialIsHermitesThenThis(string integrand) => DifferentiatesBack(integrand);

        [Theory]
        [InlineData("x^4/(x^3 + 2)")]
        [InlineData("x^5/(x^3 - 2)")]
        public void AnImproperFractionIsDividedOutFirst(string integrand) => DifferentiatesBack(integrand);
    }
}
