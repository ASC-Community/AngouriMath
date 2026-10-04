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
    /// A square root of a perfect square is the modulus, <c>sgn(P) P</c> for a real <c>P</c>, at
    /// every depth, and the rules that read a root of a quadratic must not see one:
    /// <c>1/sqrt(1 + csch(x)^2)</c> under <c>u = tanh(x)</c> is <c>sqrt(u^2)/(u^2 - 1)</c>, which
    /// the table rule for a root of a quadratic beside a linear answered with a logarithm of zero.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class RootOfAPerfectSquareIntegralTest
    {
        private static readonly double[] Points = { -2.3, -1.7, -0.4, 0.3, 0.7, 1.1, 1.9 };

        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            var derivative = integral.Differentiate("x");
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
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
            Assert.True(compared >= 5, $"only {compared} of {Points.Length} points were comparable for {integrand}");
        }

        [Theory]
        [InlineData("1/sqrt(1+csch(x)^2)")]
        [InlineData("sqrt(x^2)/(x^2-1)")]
        [InlineData("sqrt(x^2)/(x+2)")]
        [InlineData("(x^2+2*x+1)^(3/2)/(x+3)")]
        [InlineData("(4*x^2-4*x+1)^(1/2)*e^x")]
        [InlineData("sqrt(sinh(x)^2)")]
        public void ARootOfAPerfectSquareIsTheModulus(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A root of a square inside a sum, where its sign is a factor of nothing:
        /// <c>1/(1 + (x^2)^(3/2))</c> is <c>1/(1 + x^3)</c> for a positive <c>x</c> and
        /// <c>1/(1 - x^3)</c> for a negative one, each integrated on its own side of zero. With the
        /// sign taken out in front, the answer was the first's on both sides. The same with a
        /// square factor taken out of a root: <c>x/(x + sqrt(x^6))</c> is <c>1/(1 + x^2)</c> and
        /// <c>1/(1 - x^2)</c>. Rubi's 1.1.3.2 and 1.3.2.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
        /// </summary>
        [Theory]
        [InlineData("1/(1+(x^2)^(3/2))")]
        [InlineData("1/(1+sqrt(x^2))")]
        [InlineData("x/(1+(x^2)^(3/2))")]
        [InlineData("(x^2)^(3/2)/(1+(x^2)^(3/2))")]
        [InlineData("1/(2+sqrt(x^2+2*x+1))")]
        [InlineData("x/(x+sqrt(x^6))")]
        [InlineData("(x-sqrt(x^6))/(x*(1-x^4))")]
        public void ARootOfASquareInsideASumIsIntegratedOnEachSide(string integrand) => DifferentiatesBack(integrand);

        /// <summary>
        /// A square written with symbols: <c>a^2 + 2 a b x + b^2 x^2</c> is <c>(a + b x)^2</c>,
        /// whose discriminant <c>4a^2b^2 - 4a^2b^2</c> <see cref="Entity.InnerSimplified"/> does
        /// not collect, and whose leading coefficient <c>b^2</c> is not a number -- it is
        /// positive for a real parameter, which is what the answer's <c>provided b^2 &gt; 0</c>
        /// says. At the top the power may be any half-odd one, since the sign written there is
        /// not a factor some substitution below has to carry. Rubi's 1.2.1.9.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
        /// </summary>
        [Theory]
        [InlineData("(a^2 + 2*a*b*x + b^2*x^2)^(5/2)")]
        [InlineData("(A + B*x)*(d + pe*x)/(a^2 + 2*a*b*x + b^2*x^2)^(5/2)")]
        [InlineData("1/(a^2 + 2*a*b*x + b^2*x^2)^(3/2)")]
        public void ASquareWrittenWithSymbols(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 0.9).Substitute("b", 1.7).Substitute("A", 0.7)
                .Substitute("B", 1.3).Substitute("d", 0.4).Substitute("pe", 1.1);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
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
            Assert.True(compared >= 5, $"only {compared} points were comparable for {integrand}");
        }

        /// <summary>
        /// The same square in a power of the variable: <c>a^2 + 2 a b x^2 + b^2 x^4</c> is
        /// <c>(a + b x^2)^2</c>, and its root is <c>sqrt(b^2) sgn(x^2 + a/b) (x^2 + a/b)</c>. Pinned
        /// with <c>a</c> and <c>b</c> of opposite signs, so that the sign changes among the points.
        /// Rubi's 1.2.2.7 and 1.2.3.2.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
        /// </summary>
        [Theory]
        [InlineData("sqrt(c + pe*x + d*x^2)*sqrt(a^2 + 2*a*b*x^2 + b^2*x^4)")]
        [InlineData("x*sqrt(c + pe*x + d*x^2)*sqrt(a^2 + 2*a*b*x^2 + b^2*x^4)")]
        [InlineData("x/sqrt(a^2 + 2*a*b*x^3 + b^2*x^6)")]
        public void ASquareInAPowerOfTheVariable(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", -0.9).Substitute("b", 1.7).Substitute("c", 2.1)
                .Substitute("pe", 0.3).Substitute("d", 1.3);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
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
            Assert.True(compared >= 5, $"only {compared} points were comparable for {integrand}");
        }

        /// <summary>
        /// A whole power of the same square: <c>(a^2 + 2 a b x^2 + b^2 x^4)^(-2)</c> is
        /// <c>(b^2)^(-2) (x^2 + a/b)^(-4)</c>, exactly and with no sign, a power of a quadratic in
        /// <c>x^2</c> the rational integrator reads. Pinned with <c>a/b</c> positive, so that the
        /// square has no real zero among the points.
        /// </summary>
        [Theory]
        [InlineData("1/(a^2 + 2*a*b*x^2 + b^2*x^4)^2")]
        [InlineData("x/(a^2 + 2*a*b*x^2 + b^2*x^4)^3")]
        [InlineData("x^2*(a^2 + 2*a*b*x^3 + b^2*x^6)^(-2)")]
        public void AWholePowerOfASquareInAPowerOfTheVariable(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Assert.DoesNotContain("NaN", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 0.9).Substitute("b", 1.7);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
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
            Assert.True(compared >= 5, $"only {compared} points were comparable for {integrand}");
        }

        /// <summary>
        /// The table's arcsine and logarithm for a root of a quadratic are not for a square: the
        /// arcsine divides by the root of the discriminant, and the logarithm is of zero beyond
        /// the root, so <c>1/sqrt(x^2 + 2x + 1)</c> was <c>ln(0)</c> for every x below -1 and
        /// <c>1/sqrt(-a^2 - 2 a b x - b^2 x^2)</c> NaN everywhere. Compared at every point where
        /// the integrand has a value, on both sides of each root -- an answer with none there is
        /// wrong, not incomparable -- with the leading coefficient of either sign: <c>-b^2</c> is
        /// negative for a real <c>b</c>, and the root of its square is imaginary. Rubi's
        /// 1.2.1.2 #2737 among them.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1670">#1670</a>
        /// </summary>
        [Theory]
        [InlineData("1/sqrt(x^2 + 2*x + 1)")]
        [InlineData("sqrt(x^2 + 2*x + 1)")]
        [InlineData("3/sqrt(9*x^2 - 6*x + 1)")]
        [InlineData("1/sqrt(-4 - 4*x - x^2)")]
        [InlineData("sqrt(-4 - 4*x - x^2)")]
        [InlineData("1/sqrt(-a^2 - 2*a*b*x - b^2*x^2)")]
        [InlineData("x/sqrt(-a^2 - 2*a*b*x - b^2*x^2)")]
        [InlineData("1/(x*sqrt(-a^2 - 2*a*b*x - b^2*x^2))")]
        [InlineData("1/(x*sqrt(a^2 + 2*a*b*x + b^2*x^2))")]
        [InlineData("1/((d + h*x)*sqrt(a^2 + 2*a*b*x + b^2*x^2))")]
        [InlineData("1/((d + h*x)*sqrt(-a^2 - 2*a*b*x - b^2*x^2))")]
        public void TheTableDoesNotReadASquareAsAQuadratic(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            HoldsWhereverTheIntegrandHasAValue(integrand, integral);
        }

        /// <summary>
        /// A square with a symbol of no known sign in front, <c>c (a + b x)^2</c>, is not read as
        /// one, and the table's forms gave no value between its roots: declined, or answered
        /// everywhere the integrand has a value.
        /// </summary>
        [Fact]
        public void ASquareOfUnknownSignIsNotAnsweredWrongly()
        {
            var integrand = "1/(x*sqrt(c*(a + b*x)^2))";
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            if (!integral.Stringize().Contains("integral("))
                HoldsWhereverTheIntegrandHasAValue(integrand, integral);
        }

        private static void HoldsWhereverTheIntegrandHasAValue(string integrand, Entity integral)
        {
            Entity Pin(Entity e) => e.Substitute("a", 1.3).Substitute("b", 0.7).Substitute("c", 0.6)
                .Substitute("d", 1.9).Substitute("h", 1.1);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            var compared = 0;
            foreach (var at in new[] { -3.7, -2.6, -1.8, -1.2, -0.6, 0.4, 1.9 })
            {
                var want = original.Substitute("x", at).EvalNumerical();
                if (want.IsNaN)
                    continue;
                var got = derivative.Substitute("x", at).EvalNumerical();
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}: {integral}");
            }
            Assert.True(compared >= 6, $"only {compared} points were comparable for {integrand}");
        }

        /// <summary>
        /// A square in a fractional power of x, Rubi's 1.2.3.2 <c>(a^2 + b^2/x^(2/5) + 2 a b/x^(1/5))^(5/2)</c>:
        /// read in <c>w = x^k</c>, every power of x a whole power of w, and real for a positive x,
        /// which the answer says. Differentiated back where it says, with <c>a b</c> of either
        /// sign, so that <c>w + a/b</c> changes sign among the points for the first two.
        /// </summary>
        [Theory]
        [InlineData("(a^2 + b^2/x^(2/5) + 2*a*b/x^(1/5))^(5/2)")]
        [InlineData("sqrt(a^2 + 2*a*b*x^(1/3) + b^2*x^(2/3))")]
        [InlineData("(1 + 2*x^(1/2) + x)^(3/2)")]
        [InlineData("sqrt(4 + 4*x^(1/4) + x^(1/2))/x")]
        public void ASquareInAFractionalPowerOfTheVariable(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            foreach (var b in new[] { 0.7, -0.7 })
            {
                Entity Pin(Entity e) => e.Substitute("a", 1.3).Substitute("b", b);
                var derivative = Pin(integral).Differentiate("x");
                var original = Pin(integrand.ToEntity());
                foreach (var at in new[] { 0.01, 0.31, 0.83, 2.41, 9.0 })
                {
                    var got = derivative.Substitute("x", at).EvalNumerical();
                    var want = original.Substitute("x", at).EvalNumerical();
                    var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                    Assert.True(difference / Math.Max(1.0, Math.Abs((double)want.RealPart)) < 1e-9,
                        $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, b = {b}, where the integrand is {want}: {integral}");
                }
            }
        }

        [Fact]
        public void TheSignIsTheSignOfTheLinearFactor()
        {
            var integral = "sqrt(x^2)/(x+2)".ToEntity().Integrate("x").Substitute("C", 0);
            Assert.Contains(integral.Nodes, node => node is Entity.Signumf(var argument) && argument == "x".ToEntity());
            // sgn(x) (x - 2 ln(x + 2)), compared as a value on each side of the sign change.
            var difference = integral - "sgn(x) * (x - 2 * ln(x + 2))".ToEntity();
            foreach (var at in new[] { -1.5, 0.5, 3.0 })
                Assert.True(Math.Abs((double)difference.Substitute("x", at).EvalNumerical().RealPart) < 1e-12, $"at x = {at}: {integral}");
        }
    }
}
