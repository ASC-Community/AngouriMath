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
    /// <c>atanh(tanh(a + b x))</c> and <c>acoth(tanh(a + b x))</c> under an integral: the
    /// parser spells them <c>1/2 ln((1 + T)/(1 - T))</c> with <c>T = (E - 1)/(E + 1)</c> and
    /// <c>E = e^(2(a + b x))</c>, a nested quotient no rule read; cancelled with <c>E</c> for
    /// an indeterminate it is <c>1/2 ln(E)</c>, and that logarithm's derivative is the constant
    /// <c>2b</c>, so it is <c>2b x + c</c> for a constant <c>c</c> the answer writes back as
    /// <c>ln(E) - 2b x</c>. Rubi's 7.3.7 and 7.4.1, whose answers carry
    /// <c>b x - atanh(tanh(a + b x))</c> the same way.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class InverseHyperbolicOfAHyperbolicIntegralTest
    {
        private static readonly double[] Points = { 0.3, 0.7, 1.1, 1.9, 2.6 };

        /// <summary>
        /// The antiderivative differentiated back against the integrand at real points, with
        /// <c>a = 1/3</c> and <c>b = 3/2</c>, where <c>atanh(tanh(a + b x))</c> is <c>a + b x</c>
        /// and <c>acoth(tanh(a + b x))</c> is <c>a + b x + i pi/2</c>.
        /// </summary>
        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            var bare = integral.Substitute("C", 0).Substitute("a", "1/3".ToEntity()).Substitute("b", "3/2".ToEntity());
            var derivative = bare.Differentiate("x");
            var original = integrand.ToEntity().Substitute("a", "1/3".ToEntity()).Substitute("b", "3/2".ToEntity());
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
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 4, $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        [Theory]
        [InlineData("atanh(tanh(a+b*x))^2/sqrt(x)")]
        [InlineData("x^m*atanh(tanh(a+b*x))^3")]
        [InlineData("1/(x*atanh(tanh(a+b*x)))")]
        [InlineData("acoth(tanh(a+b*x))/x^2")]
        [InlineData("acoth(tanh(a+b*x))^3/x")]
        [InlineData("1/(x*acoth(tanh(a+b*x))^2)")]
        public void AnInverseHyperbolicOfAHyperbolicIsLinearInTheVariable(string integrand)
            => DifferentiatesBack(integrand.Replace("x^m", "x^2"));

        /// <summary>
        /// The two steps on their own: the cancelled logarithm, and a logarithm whose
        /// derivative is a constant named as a linear.
        /// </summary>
        [Theory]
        [InlineData("ln((1 + (e^(2*x) - 1)/(e^(2*x) + 1))/(1 - (e^(2*x) - 1)/(e^(2*x) + 1)))/(2*x)")]
        [InlineData("1/(x*ln(e^(2*(a+b*x))))")]
        [InlineData("x/ln(e^(a+b*x))")]
        public void ALogarithmOfACancellingQuotientAndALogarithmLinearInTheVariable(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A name that is zero on the real line: <c>ln(e^x) - x</c> is zero for every real
        /// <c>x</c>, and the answer for <c>1/(x (x + c))</c> divides by <c>c</c>, so it had no
        /// value at any real point. The arm for <c>c = 0</c> is asked as well. Rubi's 3.5,
        /// #192 and #226. https://github.com/asc-community/AngouriMath/issues/1666
        /// </summary>
        [Theory]
        [InlineData("1/(x*ln(e^x))")]
        [InlineData("cot(x)/ln(e^sin(x))")]
        [InlineData("1/(x*ln(e^(2*x)))")]
        public void ANameThatIsZeroOnTheRealLineIsAskedAboutAsZero(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// ...and only there: <c>ln(e^(2(a + b x))) - 2 b x</c> is <c>2a</c> on the real line, and
        /// the answer for <c>atanh(tanh(x))^2</c> has a value at <c>c = 0</c>.
        /// </summary>
        [Theory]
        [InlineData("1/(x*atanh(tanh(a+b*x)))")]
        [InlineData("atanh(tanh(x))^2")]
        public void ANameThatIsNotZeroOrIsHarmlessGetsNoArm(string integrand)
            => Assert.DoesNotContain("piecewise", integrand.ToEntity().Integrate("x").Stringize());
    }
}
