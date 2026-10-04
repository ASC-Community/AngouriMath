//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A power of <c>1 - t^2</c> or <c>1 + t^2</c> beside a power of a quadratic with symbols in
    /// it, which is what the half-angle tangent makes of a power of the secant, the cosine or the
    /// cosecant over <c>(a + b sin(x))^n</c>. Part of
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The split over symbolic linear factors reads the linears it finds written, and
    /// <c>1 - t^2</c> is written as a quadratic, so these went to the Hermite reduction, whose one
    /// linear solve answered <c>1/((1 - t^2)^3 (a t^2 + 2 b t + a)^2)</c> with coefficients over a
    /// polynomial of degree 48 in <c>a</c> and <c>b</c> and did not return in 20 s. A rational
    /// factor that splits is now written in its linears beside a symbolic one.
    /// </para>
    /// <para>
    /// And the constant taken out of <c>(a t^2 + 2 b t + a)^2</c> left the monic quadratic as
    /// <c>a/a + 2 b/a t + t^2</c>, whose first coefficient the chain read as <c>a^0</c>, and the
    /// split declined it: <c>(1 + t^2)^2/((1 - t^2)^2 (a t^2 + 2 b t + a)^2)</c> took 14 s through
    /// the substitution search.
    /// </para>
    /// <para>
    /// And the split's numerator over a quadratic block is computed modulo that quadratic from
    /// the other factors' product, which was written out as a polynomial and read back; a
    /// coefficient like <c>(4 b^2 - 2 a^2)/a^2</c> expanded to an <c>a^0</c> the reader did not
    /// read, and every quadratic block beside a symbolic square was declined. It is reduced from
    /// the coefficients now, and quadratics alone are split so too where a symbolic one is
    /// repeated.
    /// </para>
    /// <para>
    /// Every answer is waited for with a bound, since the failure was not answering, and then
    /// differentiated back with the symbols pinned.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class RationalFactorsBesideASymbolicQuadraticTest
    {
        private static readonly double[] Points = { 0.3, 0.6, 1.2, 2.0, 2.7 };

        private static void AnswersAndDifferentiatesBack(string integrand)
        {
            var integrating = Task.Run(() => integrand.ToEntity().Integrate("x"));
            Assert.True(integrating.Wait(TimeSpan.FromSeconds(60)), $"{integrand} was not answered within a minute");
            var integral = integrating.Result;
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Substitute("a", 1.3).Substitute("b", 0.7).Differentiate("x");
            var original = integrand.ToEntity().Substitute("a", 1.3).Substitute("b", 0.7);
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
                Assert.True(difference / scale < 1e-8,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} points were comparable for {integrand}");
        }

        /// <summary>
        /// The rational functions the half-angle tangent hands on, in the variable itself. The first
        /// and the third did not return within a minute; the second took 14 s, which the bound here
        /// does not pin -- it is the answer that is checked.
        /// </summary>
        [Theory]
        [InlineData("1/((1-x^2)^3*(a*x^2+2*b*x+a)^2)")]
        [InlineData("(1+x^2)^2/((1-x^2)^2*(a*x^2+2*b*x+a)^2)")]
        [InlineData("2*(1+x^2)^5/((1-x^2)^4*(a*x^2+2*b*x+a)^2)")]
        public void OverAPowerOfOneLessTheSquare(string integrand) => AnswersAndDifferentiatesBack(integrand);

        /// <summary>
        /// A power of <c>1 + t^2</c> beside the square of a symbolic quadratic, with no linear
        /// factor at all, which the split now takes by residues alone. It did not return within
        /// 20 s.
        /// </summary>
        [Fact]
        public void OverAPowerOfOnePlusTheSquare() => AnswersAndDifferentiatesBack("2*(1-x^2)^5/((1+x^2)^4*(a-a*x^2+2*b*x)^2)");

        /// <summary>
        /// A cubic over the cube of the quadratic, which the table divides a power at a time: with
        /// a symbol leading, the remainder came back as the unexpanded difference
        /// <c>4x(1 + x^2) - (4x - 8b/a)(x^2 + 2b/a x + 1)</c>, a cubic to the check that it is
        /// linear, and under a condition on <c>a</c> once expanded, and the rule declined. It did
        /// not return within 20 s.
        /// </summary>
        [Fact]
        public void ACubicOverTheCubeOfTheQuadratic() => AnswersAndDifferentiatesBack("4*x*(1+x^2)/(a*x^2+2*b*x+a)^3");

        /// <summary>
        /// The trigonometric integrands these come from, each of which ran out a 20 s budget:
        /// Rubi's 4.1.1.2 rows 493 and 489, 4.1.2.2 row 1447 and 4.1.2.1 row 242.
        /// </summary>
        [Theory]
        [InlineData("sec(x)^4/(a+b*sin(x))^2")]
        [InlineData("cos(x)^6/(a+b*sin(x))^2")]
        [InlineData("cos(x)^4*csc(x)/(a+b*sin(x))^2")]
        [InlineData("sin(x)/(a+b*sin(x))^3")]
        public void OverAPowerOfASumWithTheSine(string integrand) => AnswersAndDifferentiatesBack(integrand);

        /// <summary>
        /// Neighbours the half-angle tangent answered already, through the same constant taken
        /// out of the same kind of quadratic, which stay answered.
        /// </summary>
        [Theory]
        [InlineData("tan(x)^4/(a+b*csc(x))")]
        [InlineData("sin(x)^2/(a+b*cos(x))")]
        public void TheNeighboursStayAnswered(string integrand) => AnswersAndDifferentiatesBack(integrand);

        /// <summary>
        /// The same integrand with `1 + x^2` written either way round. The Hermite reduction's
        /// system took its rows in the order its dictionaries met the powers, which is the
        /// spelling's, and with `(1 + x^2)^3` below the bar the solution came out in coefficients
        /// of the thirty-sixth degree in the symbols that nothing cancelled: forty seconds of
        /// declining their logarithmic part before the answer, where `(x^2 + 1)^3` took one.
        /// </summary>
        [Theory]
        [InlineData("x/((1+x^2)^3*(2*a*x+b*(x^2+1)))")]
        [InlineData("x/((x^2+1)^3*(2*a*x+b*(x^2+1)))")]
        [InlineData("4*x*(1-x^2)^2/((1+x^2)^3*(2*a*x+b*(1+x^2)))")]
        public void EitherSpellingOfOnePlusTheSquare(string integrand) => AnswersAndDifferentiatesBack(integrand);
    }
}
