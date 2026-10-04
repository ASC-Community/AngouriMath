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
    /// An integrand holding an inverse hyperbolic function of a linear in the variable,
    /// integrated by the substitution that undoes it: <c>x = sinh(u)/c</c> for
    /// <c>arsinh(c x)</c>, written here as <c>ln(c x + sqrt(c^2 x^2 + 1))</c>, and likewise
    /// the cosine and the tangent. What is left is a function of <c>u</c> and of exponentials
    /// of it, which the exponential rules read: <c>x arsinh(a x)</c> is <c>u sinh(2u)/(2a^2)</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class InverseHyperbolicSubstitutionTest
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

        private static readonly double[] Anywhere = { -1.6, -0.4, 0.3, 1.1, 2.4 };
        private static readonly double[] PastOneHalf = { 0.6, 0.9, 1.3, 2.2, 3.5 };
        private static readonly double[] InsideTheHalf = { -0.45, -0.2, 0.1, 0.3, 0.45 };

        /// <summary>The sine: real everywhere. Rubi's, with symbolic coefficients pinned to numbers.</summary>
        [Theory]
        [InlineData("x*arsinh(2*x)")]
        [InlineData("arsinh(2*x)^2")]
        [InlineData("x^2*arsinh(2*x)")]
        [InlineData("x*arsinh(2*x)^2")]
        [InlineData("x^4*arsinh(2*x)")]
        [InlineData("arsinh(1 + 2*x)^3")]
        [InlineData("x*(3 + 2*arsinh(2*x))/sqrt(5 + 20*x^2)")]
        [InlineData("x*(5 + 20*x^2)^3*(3 + 2*arsinh(2*x))")]
        [InlineData("e^(arsinh(1 + 2*x))/x^4")]
        public void TheSine(string integrand) => DifferentiatesBack(integrand, Anywhere);

        /// <summary>The cosine, past <c>c x = 1</c> where it is real.</summary>
        [Theory]
        [InlineData("arcosh(2*x)")]
        [InlineData("x^2*arcosh(2*x)^2")]
        [InlineData("x*(3 + 2*arcosh(2*x))/sqrt(20*x^2 - 5)")]
        public void TheCosine(string integrand) => DifferentiatesBack(integrand, PastOneHalf);

        /// <summary>The tangent, inside <c>(-1/2, 1/2)</c> where it is real.</summary>
        [Theory]
        [InlineData("x*artanh(2*x)^2")]
        [InlineData("x^2*artanh(2*x)")]
        [InlineData("artanh(2*x)/(1 - 4*x^2)^(3/2)")]
        public void TheTangent(string integrand) => DifferentiatesBack(integrand, InsideTheHalf);

        /// <summary>
        /// The tangent's, where the integrand is real inside <c>(-1/2, 1/2)</c> only: the answer
        /// holds there and not past it, where the integrand is not real either, and is given
        /// with the condition that says where it holds. Rubi's 7.3.4.
        /// </summary>
        [Theory]
        [InlineData("1/((1 - 4*x^2)^(5/2)*artanh(2*x))")]
        [InlineData("1/((1 - 4*x^2)^(3/2)*artanh(2*x))")]
        [InlineData("x/((1 - 4*x^2)^(3/2)*artanh(2*x)^2)")]
        [InlineData("x*artanh(2*x)^3/(1 - 4*x^2)^(3/2)")]
        public void TheTangentOnItsDomain(string integrand)
        {
            DifferentiatesBack(integrand, InsideTheHalf);
            Assert.True(SaysItHoldsInsideTheHalfOnly(integrand.ToEntity().Integrate("x")));
        }

        private static bool SaysItHoldsInsideTheHalfOnly(Entity integral)
            => integral.Nodes.OfType<Entity.Providedf>().Any(provided =>
                provided.Predicate.Substitute("x", 0.3).Evaled == Entity.Boolean.True
                && provided.Predicate.Substitute("x", 0.7).Evaled == Entity.Boolean.False);

        /// <summary>
        /// A power of a constant multiple of the radicand the inverse cosine holds, written over
        /// that radicand: <c>arcosh(c x)</c> is <c>ln(c x + sqrt(c^2 x^2 - 1))</c>, and
        /// <c>(d - c^2 d x^2)^(k/2)</c> beside it is <c>K^k (c^2 x^2 - 1)^(k/2)</c> for
        /// <c>K = sqrt(d - c^2 d x^2)/sqrt(c^2 x^2 - 1)</c>, which is constant on each interval
        /// where it is defined and stands in front of the answer; a whole power is the product
        /// of the powers. These are answered now, not only right when answered: the first is
        /// real on <c>(-1/2, 1/2)</c>, where <c>arcosh(2x)</c> is <c>i arccos(2x)</c> and its
        /// square real, and is checked there. Rubi's 7.2.4 and 7.2.5, 130 rows.
        /// </summary>
        [Theory]
        [InlineData("arcosh(2*x)^2/sqrt(1 - 4*x^2)", new[] { -0.45, -0.2, 0.1, 0.3, 0.45 })]
        [InlineData("x*(5 - 20*x^2)^(3/2)*(3 + 2*arcosh(2*x))", new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })]
        [InlineData("(3 + 2*arcosh(2*x))/(5 - 20*x^2)^(5/2)", new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })]
        [InlineData("x*(3 + 2*arcosh(2*x))/(5 - 20*x^2)^3", new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })]
        public void AConstantMultipleOfTheRadicandIsWrittenOverIt(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>
        /// The radicand written as two linear factors: <c>(5 + 10 i x)(7 - 14 i x)</c> is
        /// <c>35 (1 + 4 x^2)</c>, the radicand of <c>arsinh(2x)</c>, so <c>L1^p L2^q</c> is
        /// <c>L1^(p - q) K^k (1 + 4 x^2)^(k/2)</c> with <c>K = sqrt(L1) sqrt(L2)/sqrt(1 + 4 x^2)</c>
        /// in front, constant wherever it is defined. Rubi's 7.1.4 and 7.1.5, 39 rows; the
        /// integrands are complex everywhere, and compared so.
        /// </summary>
        [Theory]
        [InlineData("(3 + 2*arsinh(2*x))*sqrt(5 + 10*i*x)*sqrt(7 - 14*i*x)", new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })]
        [InlineData("(5 + 10*i*x)^(5/2)*(3 + 2*arsinh(2*x))*sqrt(7 - 14*i*x)", new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })]
        [InlineData("(5 + 10*i*x)^(3/2)*(7 - 14*i*x)^(3/2)*(3 + 2*arsinh(2*x))", new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })]
        [InlineData("x*(3 + 2*arsinh(2*x))*sqrt(5 + 10*i*x)*sqrt(7 - 14*i*x)", new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })]
        public void ARadicandWrittenAsTwoLinearFactorsIsWrittenAsOne(string integrand, double[] points)
            => DifferentiatesBack(integrand, points);

        /// <summary>The same with every coefficient a symbol, pinned only after integrating.</summary>
        [Theory]
        [InlineData("(a+b*asinh(c*x))*sqrt(d+i*c*d*x)*sqrt(f-i*c*f*x)")]
        [InlineData("(d+i*c*d*x)^(5/2)*(a+b*asinh(c*x))*sqrt(f-i*c*f*x)")]
        public void TwoLinearFactorsWithSymbolicCoefficients(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 0.9).Substitute("b", 1.7).Substitute("c", 0.6).Substitute("d", -1.3).Substitute("f", 0.8);
            var derivative = Pin(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var at in new[] { -0.4, 0.1, 0.3, 0.7, 1.2 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9, $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// A root of a *negative* multiple of the quadratic is not the multiple's root times
        /// the quadratic's: <c>arcosh(a x)^2/sqrt(1 - a^2 x^2)</c> came back with <c>i a</c>
        /// below where the integrand is real, and another function of <c>x</c> beside the
        /// inverse is not this route's. Both are declined here or answered right.
        /// </summary>
        [Theory]
        [InlineData("arcosh(2*x)^2/sqrt(1 - 4*x^2)", new[] { -0.45, -0.2, 0.1, 0.3, 0.45 })]
        [InlineData("x*arctan(x)*arsinh(x)/sqrt(1 + x^2)", new[] { -1.6, -0.4, 0.3, 1.1, 2.4 })]
        public void DeclinedOrRight(string integrand, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x");
            if (integral.Stringize().Contains("integral("))
                return;
            DifferentiatesBack(integrand, points);
        }
    }
}
