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
    /// A product of powers of quotients of linears in which one linear stands in two of them,
    /// written over each linear once with a constant <c>K</c> in front: <c>e^atanh(a x) sqrt(c - c/(a x))</c>
    /// is <c>sqrt((1 + a x)/(1 - a x)) sqrt(c (a x - 1)/(a x))</c>, and written once it is
    /// <c>K sqrt(a x + 1)/sqrt(x)</c>. Rubi's 7.3.6 and 7.4.2, which ran out of time.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class EachLinearWrittenOnceIntegralTest
    {
        /// <summary>
        /// On both sides of every root, so that <c>K</c> is checked wherever it takes another
        /// value: the derivative of the answer against the integrand wherever the integrand is
        /// real, with <c>a</c> and <c>c</c> pinned.
        /// </summary>
        private static readonly double[] Points = { -3.1, -1.7, -0.9, -0.45, -0.2, 0.15, 0.4, 0.95, 1.6, 2.9 };

        [Theory]
        [InlineData("e^atanh(a*x)*sqrt(c-c/(a*x))", "a=1.37,c=2.11")]
        [InlineData("e^atanh(a*x)*sqrt(c-c/(a*x))", "a=1.37,c=-2.11")]
        [InlineData("e^(3*atanh(a*x))*(c-c/(a*x))^(5/2)", "a=0.71,c=1.9")]
        [InlineData("(c-c/(a*x))^(3/2)/e^atanh(a*x)", "a=1.37,c=2.11")]
        [InlineData("e^atanh(a*x)*sqrt(c-c/(a*x))/x^2", "a=1.37,c=2.11")]
        [InlineData("sqrt(c-c/(a*x))/(e^(2*atanh(a*x))*x^2)", "a=1.37,c=2.11")]
        [InlineData("e^(2*acoth(a*x))*sqrt(c-c/(a^2*x^2))", "a=1.37,c=2.11")]
        [InlineData("e^(2*acoth(a*x))/(c-c/(a^2*x^2))^(3/2)", "a=1.37,c=-2.11")]
        [InlineData("e^atanh(x)*(1-x)^(1/2)", "")]
        [InlineData("e^atanh(a*x)*sqrt(c-a*c*x)", "a=1.37,c=2.11")]
        [InlineData("x*sqrt(c-a*c*x)/e^(3*atanh(a*x))", "a=3.1,c=0.7")]
        [InlineData("sqrt(x)/(sqrt(1/x + 1)*sqrt(1 - x^2))", "")]
        [InlineData("sqrt((x - 1)/(x + 1))/x", "")]
        [InlineData("x*sqrt(c - a*c*x)/(1 + a*x)^(3/2)*(1 - a*x)^(-1/2)", "a=3.1,c=0.7")]
        [InlineData("1/(e^atanh(a*x)*(c-c/(a*x))^(3/2))", "a=1.37,c=2.11")]
        [InlineData("x*sqrt(c-c/(a*x))/e^(3*atanh(a*x))", "a=1.37,c=2.11")]
        [InlineData("x*sqrt(c-c/(a*x))/e^(3*atanh(a*x))", "a=1.37,c=-2.11")]
        [InlineData("e^(2*acoth(a*x))/(c-c/(a^2*x^2))^2", "a=1.37,c=2.11")]
        [InlineData("1/(e^(2*atanh(a*x))*(c-c/(a^2*x^2))^4)", "a=1.37,c=-2.11")]
        // A quadratic that shares a root with a linear beside it, Rubi's 1.2.1.2 and 1.2.1.4:
        // `a d e + (c d^2 + a e^2) x + c d e x^2` is `(d + e x)(a e + c d x)`, and comes apart
        // only through the root it shares, where the linear stands under a root as well.
        [InlineData("(d+h*x)^(7/2)/(a*d*h+(c*d^2+a*h^2)*x+c*d*h*x^2)^(3/2)", "a=1.37,c=2.11,d=0.83,h=1.9")]
        [InlineData("(d+h*x)^(3/2)*(a*d*h+(c*d^2+a*h^2)*x+c*d*h*x^2)^(1/2)", "a=1.37,c=2.11,d=0.83,h=1.9")]
        [InlineData("sqrt(d+h*x)/sqrt(a*d*h+(c*d^2+a*h^2)*x+c*d*h*x^2)", "a=1.37,c=2.11,d=0.83,h=1.9")]
        public void IsAnsweredWithEachLinearOnce(string integrand, string pins)
            => DifferentiatesBack(integrand, pins);

        /// <summary>
        /// Beside a third linear, a quadratic is not taken apart through the root it shares. That
        /// would leave a product of three linears, which the rules for two do not answer, and
        /// written so <c>x sqrt(a d h + ...)/(d + h x)</c> grows past what fits in memory. Read
        /// whole, it is answered.
        /// </summary>
        [Theory]
        [InlineData("x*sqrt(a*d*h+(c*d^2+a*h^2)*x+c*d*h*x^2)/(d+h*x)", "a=1.37,c=2.11,d=0.83,h=1.9")]
        [InlineData("sqrt(a*d*h+(c*d^2+a*h^2)*x+c*d*h*x^2)/(x*(d+h*x))", "a=1.37,c=2.11,d=0.83,h=1.9")]
        public void BesideAThirdLinearTheQuadraticIsReadWhole(string integrand, string pins)
            => DifferentiatesBack(integrand, pins);

        private static void DifferentiatesBack(string integrand, string pins)
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
            var compared = 0;
            foreach (var at in Points)
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN || Math.Abs((double)want.ImaginaryPart) > 1e-12 * Math.Max(1, Math.Abs((double)want.RealPart)))
                    continue;
                var got = derivative.Substitute("x", at).EvalNumerical();
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                Assert.True(difference < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 2, $"only {compared} points where {integrand} is real");
        }
    }
}
