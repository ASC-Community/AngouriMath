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
    /// <c>e^(k ln(q))</c> is <c>q^k</c>, the definition of the principal power, and that is the
    /// spelling the parser gives every inverse hyperbolic function: <c>acoth(a x)</c> is
    /// <c>1/2 ln((a x + 1)/(a x - 1))</c>. So <c>e^acoth(a x) x^3</c> arrives as an exponential
    /// of a logarithm, which no exponential rule reads, and is <c>x^3 sqrt((a x + 1)/(a x - 1))</c>,
    /// which the radical substitution answers. Rubi's 7.4.2, exponentials of the inverse
    /// hyperbolic cotangent, where every row was declined.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialOfALogarithmIntegralTest
    {
        /// <summary>Beyond <c>a x = 1</c> for <c>a = 2</c>, where <c>acoth(2x)</c> is real.</summary>
        private static readonly double[] Points = { 0.6, 0.8, 1.1, 1.5, 2.2 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
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
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        [Theory]
        [InlineData("e^(2*acoth(2*x))*(3 - 4*3*x^2)^2")]
        [InlineData("e^acoth(2*x)/(3 - 3/(2*x))")]
        [InlineData("e^(1/3*acoth(x))*x^2")]
        [InlineData("(3 - 3/(4*x^2))^3/e^(2*acoth(2*x))")]
        public void AnExponentialOfAnInverseHyperbolicCotangent(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// With symbols for the coefficients, the substitution leaves coefficients that are zero
        /// as values and not as written. Under the root of the quotient,
        /// <c>e^(3 acoth(a x))/(c - a c x)^3</c> is <c>u^4 (a - a u^2)</c> over
        /// <c>(-(a - a u^2) - (u^2 + 1) a)^3</c>, which is <c>-8 a^3</c>, and the long division
        /// read it as a sextic with <c>-3 a^3 + 3 a^3</c> for its leading coefficient. Divided
        /// by that, the answer was NaN. <c>e^(2 acoth(a x)) sqrt(c - a c x)/x</c> leaves
        /// <c>((c - u^2)/c - 1)(u^2 - c)</c>, whose constant term <c>-(c/c - 1) c</c> is zero;
        /// read as a biquadratic, one of its roots in <c>u^2</c> was too, and the answer divided
        /// by its root. Rubi's 7.4.2, #225-#227 and #364-#365.
        /// https://github.com/asc-community/AngouriMath/issues/1665
        /// </summary>
        [Theory]
        [InlineData("e^(3*acoth(a*x))/(c-a*c*x)^3", "a=1.37,c=2.11", new[] { 1.1, 2.3, -1.4, -3.2 })]
        [InlineData("e^(3*acoth(a*x))/(c-a*c*x)^3", "a=0.71,c=-1.9", new[] { 1.6, -2.5 })]
        [InlineData("e^(3*acoth(a*x))/(c-a*c*x)^4", "a=1.37,c=2.11", new[] { 1.1, 2.3, -1.4 })]
        [InlineData("e^(2*acoth(a*x))*sqrt(c-a*c*x)/x", "a=1.37,c=2.11", new[] { 0.3, -0.5, -2.0 })]
        [InlineData("e^(2*acoth(a*x))*sqrt(c-a*c*x)/x", "a=1.37,c=-2.11", new[] { 1.5, 3.0 })]
        [InlineData("e^(2*acoth(a*x))*sqrt(c-a*c*x)/x^2", "a=1.37,c=2.11", new[] { 0.3, -0.5, -2.0 })]
        public void ACoefficientThatIsZeroAsAValueIsReadAsZero(string integrand, string pins, double[] points)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e)
            {
                foreach (var pin in pins.Split(','))
                {
                    var parts = pin.Split('=');
                    e = e.Substitute(parts[0], double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
                }
                return e;
            }
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.False(want.IsNaN, $"the integrand {integrand} has no value at x = {at}");
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        /// <summary>
        /// The quotient of linears under the root leaves <c>(a (1 - u^2))^5</c> below the bar in
        /// <c>u</c>, which the rational reader does not read as <c>a^5 (1 - u^2)^5</c> until it
        /// is written so: <c>x^3 sqrt((a x + 1)/(a x - 1))</c> took nine seconds through the
        /// split terms and takes half of one as a single quotient; <c>e^acoth(a x) x^3</c>
        /// eighteen seconds and a tenth. Pinned as solved, not as timed -- a wall clock in a
        /// test measures the runner.
        /// </summary>
        [Theory]
        [InlineData("x^3*sqrt((2*x+1)/(2*x-1))")]
        [InlineData("e^acoth(2*x)*x^3")]
        [InlineData("1/(e^(3*acoth(2*x))*x^4)")]
        [InlineData("e^acoth(2*x)*sqrt(3-3/(2*x))/x^4")]
        public void APowerOfAProductBelowTheBarIsReadDistributed(string integrand) => DifferentiatesBack(integrand);

        /// <summary>The fold is the identity it is: <c>e^(k ln q)</c> with any multiplier, nested or not.</summary>
        [Theory]
        [InlineData("e^(3*ln(x + 1))")]
        [InlineData("e^(ln(x^2 + 1)/2) * x")]
        [InlineData("e^(2*(1/2*ln(x + 2)))")]
        public void AnExponentialOfAMultipleOfALogarithm(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// The exponent read structurally, since <c>e^(u + v)</c> is <c>e^u e^v</c> and
        /// <c>e^(k u)</c> is <c>(e^u)^k</c>: a hyperbolic function of a logarithm is written
        /// with <c>e^(a + b ln(q))</c> above the bar and <c>e^(-(a + b ln(q)))</c> below it,
        /// and both are powers of <c>q</c> times a constant. Rubi's 6.5.3 and 6.6.3.
        /// </summary>
        [Theory]
        [InlineData("sinh(2 + 3*ln(x))")]
        [InlineData("cosh(1 + ln(x^2 + 1))")]
        [InlineData("sech(3 + 2*ln(2/x^(1/2)))^3")]
        [InlineData("e^(1 + ln(x + 2)/2)")]
        [InlineData("tanh(ln(x))")]
        public void AHyperbolicFunctionOfALogarithm(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A power of the variable times a sine or cosine of a logarithm, in closed form: two
        /// rounds of parts close on the integrand, since the sine's remainder is the cosine's
        /// integral and the cosine's is the sine's, and the pair is solved rather than iterated.
        /// <c>int x^m sin(L)</c> is <c>x^(m+1)((m+1) sin(L) - B cos(L))/((m+1)^2 + B^2)</c>
        /// wherever <c>L' = B/x</c>. The hyperbolic twin needs no rule, `sinh` being written as
        /// exponentials that fold against the logarithm; the sine and cosine are nodes.
        /// Rubi's 4.7.5.
        /// </summary>
        [Theory]
        [InlineData("x^2*sin(a + b*ln(c*x^n))", "a=0.4,b=1.3,c=1.7,n=2.1")]
        [InlineData("cos(a + b*ln(c*x^n))", "a=0.4,b=1.3,c=1.7,n=2.1")]
        [InlineData("x^m*cos(a + b*ln(c*x^n))", "a=0.4,b=1.3,c=1.7,n=2.1,m=1.4")]
        [InlineData("sin(2 + 3*ln(x))", "")]
        [InlineData("x^2*sin(a + b*ln(x))/x", "a=0.4,b=1.3")]
        public void APowerTimesATrigonometricOfALogarithm(string integrand, string pins)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e)
            {
                foreach (var pin in pins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pin.Split('=');
                    e = e.Substitute(parts[0], double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
                }
                return e;
            }
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.7, 1.1, 1.9, 2.6 })
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
        /// A fractional or symbolic power of a monomial, distributed: <c>(c x^n)^b</c> is
        /// <c>c^b x^(n b)</c> on the <c>x &gt; 0</c> where a symbolic <c>n</c> leaves the
        /// integrand real, for a positive <c>c</c> -- which the answer says.
        /// </summary>
        [Fact]
        public void APowerOfAMonomialIsDistributed()
        {
            var integral = "(c*x^n)^b".ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.Contains(integral.Nodes, node => node is Entity.Providedf(_, var predicate) && predicate == "c > 0".ToEntity());
            var pinned = integral.Substitute("c", 1.7).Substitute("n", 2.1).Substitute("b", 0.6);
            var derivative = pinned.Differentiate("x");
            var original = "(c*x^n)^b".ToEntity().Substitute("c", 1.7).Substitute("n", 2.1).Substitute("b", 0.6);
            foreach (var at in new[] { 0.3, 0.7, 1.1, 1.9, 2.6 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) < 1e-9, $"at x = {at}: {got} for {want}");
            }
        }

        /// <summary>
        /// <c>(c f)^b</c> is <c>c^b f^b</c> for a positive real <c>c</c> and any <c>f</c>: both
        /// sides take the same branch where <c>f</c> is negative, so the rewrite is exact and
        /// not conditional on the sign of <c>f</c>. It is worth doing where the rest of the base
        /// is a single factor, because the power it leaves can then meet another power of the
        /// same function beside it -- <c>sqrt(b sec x)/sec(x)^(7/2)</c> is the secant to the
        /// power <c>-3</c>, which the rules for those answer and cannot see while one of the two
        /// powers is written over <c>b sec(x)</c>; and <c>sqrt(a + a sin x)</c> is
        /// <c>sqrt(a) sqrt(1 + sin x)</c>, a root the half-angle rules read. Over a product of
        /// several factors it would only rewrite the question into one no easier, at the price
        /// of a whole descent, and over anything in which <c>x</c> enters other than through a
        /// trigonometric function -- a hyperbolic function, written in exponentials -- it
        /// multiplied the search at every level below it.
        /// Rubi's 4.1.0, 4.1.2, 4.1.7, 4.2.0, 4.3.0 and 4.5.0.
        /// </summary>
        [Theory]
        [InlineData("sqrt(b*sec(x))/sec(x)^(7/2)", "b=1.7")]
        [InlineData("sec(x)^(3/2)/(b*sec(x))^(5/2)", "b=1.7")]
        [InlineData("(b*sec(x))^(3/2)*sec(x)^(1/2)", "b=1.7")]
        [InlineData("(3*sin(x))^(5/2)/sin(x)^(3/2)", "")]
        [InlineData("(a+a*sin(x))^(3/2)/(c+d*sin(x))^(5/2)", "a=1.7,c=2.3,d=0.4")]
        [InlineData("sqrt(a-a*sin(x)^2)*tan(x)^6", "a=1.7")]
        public void AConstantComesOutOfAPowerOfASingleFactor(string integrand, string pins)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e)
            {
                foreach (var pin in pins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pin.Split('=');
                    e = e.Substitute(parts[0], double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
                }
                return e;
            }
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var at in new[] { 0.3, 0.7, 1.1, 1.9, 2.6 })
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
