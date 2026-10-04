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
    /// An integrand holding an inverse trigonometric function of the variable, integrated by
    /// the substitution that undoes it: <c>x = sin(u)</c> for <c>arcsin(x)</c>, and likewise for
    /// the cosine, the tangent and the secant.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>e^(arcsin(x)) x^3/sqrt(1 - x^2)</c> had no antiderivative. Under <c>x = sin(u)</c> it is
    /// <c>e^u sin(u)^3</c>, and <c>x arcsec(x)/sqrt(x^2 - 1)</c> is <c>u sec(u)^2</c> under
    /// <c>x = sec(u)</c>. The general substitution does not find these because what is left
    /// beside the substituted subtree is <c>x</c> itself, which only the inverse substitution
    /// removes -- the same reason the logarithm has one.
    /// </para>
    /// <para>
    /// The radical goes by construction: <c>sqrt(1 - x^2)</c> is <c>cos(u)</c> on the branch the
    /// arcsine is defined on, where the cosine is not negative. Every answer is differentiated
    /// back at points on that branch.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class InverseTrigonometricSubstitutionTest
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

        /// <summary>Inside <c>(-1, 1)</c>, where the arcsine, the arccosine and their radical are real.</summary>
        private static readonly double[] InsideTheUnitInterval = { -0.7, -0.3, 0.2, 0.55, 0.85 };

        /// <summary>Past <c>1</c>, where the arcsecant and <c>sqrt(x^2 - 1)</c> are real.</summary>
        private static readonly double[] PastOne = { 1.3, 1.8, 2.5, 3.7 };

        /// <summary>
        /// The sine and the cosine: Charlwood's <c>e^(arcsin(x)) x^3/sqrt(1 - x^2)</c>, which is
        /// <c>e^u sin(u)^3</c>, and its neighbours.
        /// </summary>
        [Theory]
        [InlineData("e^(arcsin(x))*x^3/sqrt(1 - x^2)")]
        [InlineData("e^(arcsin(x))*x/sqrt(1 - x^2)")]
        [InlineData("e^(arccos(x))*x^2/sqrt(1 - x^2)")]
        [InlineData("arcsin(x)/(1 + sqrt(1 - x^2))")]
        [InlineData("arcsin(x)^2*x/sqrt(1 - x^2)")]
        [InlineData("x^3*arcsin(x)/(1 - x^2)^(3/2)")]
        public void TheSineAndTheCosine(string integrand) => DifferentiatesBack(integrand, InsideTheUnitInterval);

        /// <summary>
        /// The radicand an inverse sine or cosine holds without writing it: <c>arcsin(u)'</c> is
        /// <c>u'/sqrt(1 - u^2)</c>, so beside <c>arcsin(2x)</c> the factors <c>(3 + 6x)(5 - 10x)</c>,
        /// whose product is <c>15 (1 - 4x^2)</c>, are that radicand written as two, and
        /// <c>(5 - 20x^2)^(3/2)</c> a multiple of it written as one. Rubi's 5.1.4 and 5.2.4; the
        /// points are inside <c>|2x| &lt; 1</c>, where every integrand here is real.
        /// </summary>
        [Theory]
        [InlineData("sqrt(3 + 6*x)*(5 - 10*x)^(3/2)*(1 + arcsin(2*x))")]
        [InlineData("x*(1 + arcsin(2*x))^2/(sqrt(3 + 6*x)*sqrt(5 - 10*x))")]
        [InlineData("(3 + 6*x)^(3/2)*(1 + arcsin(2*x))/(5 - 10*x)^(3/2)")]
        [InlineData("(5 - 20*x^2)^(3/2)*(1 + arcsin(2*x))")]
        [InlineData("sqrt(1 + 2*x)*sqrt(1 - 2*x)*(1 + arccos(2*x))")]
        public void TheRadicandOfAnInverseSineOrCosine(string integrand)
            => DifferentiatesBack(integrand, new[] { -0.4, -0.1, 0.2, 0.35, 0.45 });

        /// <summary>
        /// The tangent, whose radical is <c>sqrt(1 + x^2)</c> and becomes the secant.
        /// </summary>
        [Theory]
        [InlineData("e^(arctan(x))/(1 + x^2)^(3/2)")]
        [InlineData("arctan(x)/(1 + x^2)^(3/2)")]
        public void TheTangent(string integrand) => DifferentiatesBack(integrand, new[] { -1.6, -0.4, 0.3, 1.1, 2.4 });

        /// <summary>
        /// The secant: Charlwood's <c>x arcsec(x)/sqrt(x^2 - 1)</c>, which is <c>u sec(u)^2</c>.
        /// </summary>
        [Theory]
        [InlineData("x*arcsec(x)/sqrt(x^2 - 1)")]
        [InlineData("x*arcsec(x)/sqrt(x^2 - 1)^3")]
        public void TheSecant(string integrand) => DifferentiatesBack(integrand, PastOne);

        /// <summary>
        /// The radical in both of its spellings: <c>(1 - x^2)^(3/2)</c> and <c>sqrt(1 - x^2)^3</c>
        /// are one function on the principal branch, and the second was declined -- by this rule
        /// for the spelling, and then by a by-parts search that did not return.
        /// </summary>
        [Theory]
        [InlineData("arcsin(x)/sqrt(1 - x^2)^3", "arcsin(x)/(1 - x^2)^(3/2)")]
        [InlineData("arctan(x)/sqrt(1 + x^2)^3", "arctan(x)/(1 + x^2)^(3/2)")]
        public void BothSpellingsOfTheRadical(string nested, string flat)
        {
            DifferentiatesBack(nested, new[] { -0.7, -0.3, 0.2, 0.55 });
            DifferentiatesBack(flat, new[] { -0.7, -0.3, 0.2, 0.55 });
        }

        /// <summary>
        /// What the substitution is not for, and declines at once rather than searching: no
        /// radical to remove and no exponential of the inverse function -- <c>x arcsin(x)</c> is
        /// by parts' and its answer stays in <c>x</c> -- a radical the construction does not
        /// reach, two inverse functions, and an inverse function of something other than the
        /// bare variable. Each is either answered correctly by another rule or declined; a wrong
        /// answer is what is pinned against.
        /// </summary>
        [Theory]
        [InlineData("x*arcsin(x)", new[] { -0.7, -0.3, 0.2, 0.55, 0.85 })]
        [InlineData("x^3*arcsin(x)/sqrt(1 - x^4)", new[] { -0.7, -0.3, 0.2, 0.55, 0.85 })]
        [InlineData("arcsin(x)*arctan(x)", new[] { -0.7, -0.3, 0.2, 0.55, 0.85 })]
        [InlineData("e^(arcsin(2*x))*x/sqrt(1 - 4*x^2)", new[] { -0.4, -0.2, 0.1, 0.3, 0.45 })]
        public void WhatItIsNotFor(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("NaN", integral.Stringize());
            if (integral.Stringize().Contains("integral("))
                return;   // unanswered is a legitimate verdict; a wrong answer is not
            DifferentiatesBack(integrand, points);
        }

        /// <summary>
        /// A power of the inverse function above the first, with no radical: parts on
        /// <c>x^3 arccsc(x)^2</c> leaves <c>x^2 arccsc(x)/sqrt(x^2 - 1)</c> and stalls, where under
        /// <c>x = csc(u)</c> it is <c>-u^2 csc(u)^4 cot(u)</c>; Timofeev's <c>arccos(x)^2/x^5</c>,
        /// <c>arcsec(x)^4/x^5</c> and <c>x^3 arctan(x)^2/(1 + x^2)^3</c> the same, and the
        /// cosecant and the cotangent are read now beside the four. The trigonometric
        /// functions of the inverse are written back in <c>x</c> on the principal branch:
        /// <c>tan(arcsec(x))</c> is <c>x sqrt(1 - 1/x^2)</c>, and no <c>sec(arcsec</c> is left.
        /// </summary>
        [Theory]
        [InlineData("arccos(x)^2/x^5", new[] { -0.7, -0.3, 0.2, 0.55, 0.85 })]
        [InlineData("x^3*arccsc(x)^2", new[] { 1.3, 1.8, 2.5, 3.7, 5.0 })]
        [InlineData("arcsec(x)^4/x^5", new[] { 1.3, 1.8, 2.5, 3.7, 5.0 })]
        [InlineData("x^3*arctan(x)^2/(1 + x^2)^3", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("arccsc(x)^4/(x^2*sqrt(x^2 - 1))", new[] { 1.3, 1.8, 2.5, 3.7, 5.0 })]
        [InlineData("x*arccotan(x)^2", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        public void APowerOfTheInverseAboveTheFirst(string integrand, double[] points)
        {
            DifferentiatesBack(integrand, points);
            var integral = integrand.ToEntity().Integrate("x").Stringize();
            foreach (var composition in new[] { "sin(arc", "cos(arc", "tan(arc", "sec(arc", "csc(arc", "cotan(arc" })
                Assert.DoesNotContain(composition, integral);
        }

        /// <summary>
        /// <c>x arcsin(x)</c> is answered in <c>x</c>, not in <c>sin(2 arcsin(x))</c>: the
        /// substitution declines it, and by parts gives the answer the reader expects.
        /// </summary>
        [Fact]
        public void APolynomialTimesTheInverseStaysInX()
        {
            var integral = "x*arcsin(x)".ToEntity().Integrate("x").Stringize();
            Assert.DoesNotContain("integral(", integral);
            Assert.DoesNotContain("sin(arcsin", integral);
            Assert.DoesNotContain("cos(arcsin", integral);
        }

        /// <summary>
        /// The argument may be a linear <c>c x + d</c>, the radical a constant multiple of
        /// <c>1 - (c x + d)^2</c> (or a whole power of it from the second up), and the power
        /// one of <c>a + b arcsin(c x)</c>: Rubi's <c>x arcsin(a x)^2</c>, which parts in x
        /// stalled on, <c>x^4 (a + b arcsin(c x))/(d - c^2 d x^2)^(5/2)</c> and
        /// <c>x (d - c^2 d x^2)^3 (a + b arcsin(c x))</c>.
        /// </summary>
        [Theory]
        [InlineData("x*arcsin(2*x)^2", new[] { -0.4, -0.2, 0.1, 0.3, 0.45 })]
        [InlineData("x^2*(3 + 2*arcsin(2*x))^3", new[] { -0.4, -0.2, 0.1, 0.3, 0.45 })]
        [InlineData("x^4*(1 + 2*arcsin(3*x))/(5 - 45*x^2)^(5/2)", new[] { -0.3, -0.15, 0.05, 0.2, 0.31 })]
        [InlineData("x*(2 - 8*x^2)^3*(1 + arcsin(2*x))", new[] { -0.4, -0.2, 0.1, 0.3, 0.45 })]
        [InlineData("arcsin(2*x + 1/2)^3/sqrt(3/4 - 2*x - 4*x^2)", new[] { -0.7, -0.5, -0.3, -0.1, 0.2 })]
        [InlineData("x*arccos(2*x)^2", new[] { -0.4, -0.2, 0.1, 0.3, 0.45 })]
        [InlineData("x^2*arctan(3*x)^2/(2 + 18*x^2)^(5/2)", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("e^(arccotan(x))/(3 + 3*x^2)^3", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        public void ALinearArgumentAndAMultipleOfTheQuadratic(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// An exponential of <c>i</c> times an inverse trigonometric function is algebraic:
        /// <c>e^(i arctan(a x))</c> is <c>(1 + i a x)/sqrt(1 + a^2 x^2)</c>, so Rubi's
        /// <c>e^(i arctan(a x))/sqrt(c + a^2 c x^2)</c> is a rational function, where under
        /// <c>x = tan(u)/a</c> it was <c>e^(i u) sec(u)</c>.
        /// </summary>
        [Theory]
        [InlineData("e^(i*arctan(2*x))*x^4", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("e^(i*arctan(2*x))/sqrt(3 + 12*x^2)", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("x/e^(2*i*arctan(1 + 2*x))", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        public void AnExponentialOfTheInverseIsAlgebraic(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// To a power that is not whole, <c>e^(n i arctan(L))</c> is one power of a quotient of
        /// linears, <c>((1 + i L)/(1 - i L))^(n/2)</c> for a real <c>L</c>, which the substitution
        /// for such a power reads, where <c>(1 + i L)^n (1 + L^2)^(-n/2)</c> is two radicals of
        /// different orders: Rubi's 5.3.6.
        /// </summary>
        [Theory]
        [InlineData("e^(3/2*i*arctan(2*x))*x^2", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("e^(3/2*i*arctan(2*x))/x^4", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("x^3/e^(3/2*i*arctan(2*x))", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("e^(1/3*i*arctan(x))*x", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        [InlineData("e^(3/2*i*arctan(1 + 2*x))*x^2", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        public void ToAPowerThatIsNotWhole(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);
    }
}
