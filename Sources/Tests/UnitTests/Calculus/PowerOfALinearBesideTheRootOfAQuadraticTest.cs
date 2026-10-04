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
    /// A polynomial over a power of a linear beside a half-odd power of a quadratic, Rubi's
    /// 1.2.1.9, by undetermined coefficients: algebraic terms times the root, and multiples of
    /// the integrals of <c>1/sqrt(Q)</c> and <c>1/((g + h x) sqrt(Q))</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// Checked by differentiating back at points where the integrand is real, never against a
    /// printed form. With symbols, each of the two integrals is a piecewise on the sign of a
    /// quantity, so each row is checked with the symbols pinned on both sides of it.
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class PowerOfALinearBesideTheRootOfAQuadraticTest
    {
        private static void DifferentiatesBack(string integrand, double[] points, params (string, double)[] pins)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());

            var derivative = integral.Substitute("C", 0).Differentiate("x");
            Entity original = integrand.ToEntity();
            foreach (var (name, value) in pins)
            {
                derivative = derivative.Substitute(name, value);
                original = original.Substitute(name, value);
            }
            var compared = 0;
            foreach (var at in points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 3, $"only {compared} points could be compared for {integrand}");
        }

        public static TheoryData<string> Symbolic => new()
        {
            "(d + k*x + f*x^2)*sqrt(a + c*x^2)/(g + h*x)^4",
            "(a + c*x^2)^(3/2)*(d + k*x + f*x^2)/(g + h*x)^3",
            "(d + k*x + f*x^2)*sqrt(a + b*x + c*x^2)/(g + h*x)^3",
            "(a + b*x + c*x^2)^(3/2)*(d + k*x + f*x^2)/(g + h*x)^2",
            "(d + k*x + f*x^2)/((g + h*x)^2*sqrt(a + b*x + c*x^2))",
            // A first power, which the rules for a linear beside the root declined.
            "1/((g + h*x)*sqrt(a + c*x^2))",
            "(d + k*x + f*x^2)*sqrt(a + c*x^2)/(g + h*x)",
            // And a power of the quadratic below the bar beside its root.
            "(d + k*x + f*x^2)/((g + h*x)^3*(a + b*x + c*x^2)^(3/2))",
            "(d + k*x + f*x^2)/((g + h*x)*(a + c*x^2)^(3/2))",
        };

        /// <summary>
        /// The leading coefficient positive, and the quadratic positive at the linear's root:
        /// both integrals are logarithms.
        /// </summary>
        [Theory]
        [MemberData(nameof(Symbolic))]
        public void BothLogarithms(string integrand)
            => DifferentiatesBack(integrand, new[] { 0.3, 0.9, 1.7, 2.6 },
                ("a", 1.3), ("b", 0.7), ("c", 0.8), ("d", 1.1), ("k", -0.6), ("f", 0.45), ("g", 1.9), ("h", 0.5));

        /// <summary>
        /// The leading coefficient negative, so the integral of <c>1/sqrt(Q)</c> is an arcsine,
        /// and the quadratic negative at the linear's root, so the other is an arctangent: the
        /// quadratic is real between its roots, near -1.8 and 2.6, and the linear's is at -3.5.
        /// </summary>
        [Theory]
        [MemberData(nameof(Symbolic))]
        public void AnArcsineAndAnArctangent(string integrand)
            => DifferentiatesBack(integrand, new[] { -1.2, 0.3, 0.9, 1.7, 2.2 },
                ("a", 2.3), ("b", 0.4), ("c", -0.5), ("d", 1.1), ("k", -0.6), ("f", 0.45), ("g", 3.5), ("h", 1));

        /// <summary>
        /// The linear sharing a root with the quadratic, where every integral over a power of
        /// the linear is algebraic: <c>(d + m x)(a m + c d x)</c> written out, as Rubi writes
        /// it, and <c>d^2 - m^2 x^2</c>, real between its roots near -1.8 and 1.8.
        /// </summary>
        [Theory]
        [InlineData("(a*d*m + (c*d^2 + a*m^2)*x + c*d*m*x^2)^(5/2)/(d + m*x)^4", new[] { 0.3, 0.9, 1.7, 2.6 })]
        [InlineData("sqrt(a*d*m + (c*d^2 + a*m^2)*x + c*d*m*x^2)/(d + m*x)^3", new[] { 0.3, 0.9, 1.7, 2.6 })]
        [InlineData("(A + B*x + F*x^2)*sqrt(d^2 - m^2*x^2)/(d + m*x)^2", new[] { -1.2, 0.3, 0.9, 1.7 })]
        [InlineData("(A + B*x + F*x^2)/((d + m*x)^3*sqrt(d^2 - m^2*x^2))", new[] { -1.2, 0.3, 0.9, 1.7 })]
        [InlineData("sqrt(1 - x^2)/(1 + x)^2", new[] { -0.6, 0.3, 0.9 })]
        public void BesideASharedRoot(string integrand, double[] points)
            => DifferentiatesBack(integrand, points,
                ("a", 1.3), ("c", 0.8), ("d", 1.1), ("m", 0.6), ("A", 0.7), ("B", -0.4), ("F", 0.9));

        /// <summary>
        /// The antiderivative is real wherever the integrand is, and not real by a constant: a
        /// differentiation back cannot see a constant. The logarithm over the linear's root is of
        /// <c>(1 + z)/(1 - z)</c> where the quadratic has no real root and of <c>(z + 1)/(z - 1)</c>
        /// where it has two, and each row is on one side, the symbolic ones pinned to it.
        /// </summary>
        [Theory]
        [InlineData("sqrt(4 - x^2)/x^3", new[] { 0.3, 0.9, 1.7 }, new string[0], new double[0])]
        [InlineData("sqrt(1 + x^2)/(1 + x)^2", new[] { -2.6, 0.3, 0.9, 1.7 }, new string[0], new double[0])]
        [InlineData("1/((g + h*x)*sqrt(a + c*x^2))", new[] { -1.2, 0.3, 0.9, 1.7 }, new[] { "a", "c", "g", "h" }, new[] { 1.3, 0.8, 1.9, 0.5 })]
        [InlineData("1/((g + h*x)*sqrt(a + c*x^2))", new[] { -2.6, 1.5, 2.0, 2.6 }, new[] { "a", "c", "g", "h" }, new[] { -1.2, 0.8, 1.9, 0.5 })]
        public void RealWhereTheIntegrandIsReal(string integrand, double[] points, string[] names, double[] values)
        {
            var integral = integrand.ToEntity().Integrate("x").Substitute("C", 0);
            Entity original = integrand.ToEntity();
            for (var i = 0; i < names.Length; i++)
            {
                integral = integral.Substitute(names[i], values[i]);
                original = original.Substitute(names[i], values[i]);
            }
            foreach (var at in points)
            {
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(!want.IsNaN && Math.Abs((double)want.ImaginaryPart) < 1e-12, $"{integrand} is not real at x = {at}");
                var got = integral.Substitute("x", at).EvalNumerical();
                Assert.True(!got.IsNaN && Math.Abs((double)got.ImaginaryPart) < 1e-9,
                    $"the antiderivative of {integrand} is {got} at x = {at}, where the integrand is real");
            }
        }

        /// <summary>
        /// Rubi's rows with numbers, which nothing answered: the rules further down take a
        /// product of distinct linears apart beside the root, and not a power of one.
        /// </summary>
        [Theory]
        [InlineData("(2 + x + 3*x^2 - x^3 + 5*x^4)*sqrt(3 - x + 2*x^2)/(5 + 2*x)^5")]
        [InlineData("(3 - x + 2*x^2)^(3/2)*(2 + x + 3*x^2 - x^3 + 5*x^4)/(5 + 2*x)^2")]
        [InlineData("(2 + x + 3*x^2 - x^3 + 5*x^4)/((5 + 2*x)^4*sqrt(3 - x + 2*x^2))")]
        [InlineData("(1 + 3*x + 4*x^2)*sqrt(2 - x + 3*x^2)/(1 + 2*x)^3")]
        [InlineData("(2 + x + 3*x^2 - x^3 + 5*x^4)/((5 + 2*x)^3*(3 - x + 2*x^2)^(5/2))")]
        [InlineData("(1 + 3*x + 4*x^2)/((1 + 2*x)^3*(2 + 3*x^2)^(5/2))")]
        public void WithNumbers(string integrand)
            => DifferentiatesBack(integrand, new[] { -1.7, 0.3, 0.9, 1.7, 2.6 });
    }
}
