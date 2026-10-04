//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// Integrating a quadratic in <c>x^2</c> answered <c>NaN</c> as soon as the <c>x^2</c>
    /// coefficient was a symbol, in both the rational and the radical family -- so the
    /// library said the antiderivative does not exist for a table integral.
    /// https://github.com/asc-community/AngouriMath/issues/771
    /// </summary>
    /// <remarks>
    /// <para>
    /// The answer is a <c>piecewise</c> split on the sign of the symbolic coefficient, so
    /// checking it means checking that the branch chosen for a given sign is the right one.
    /// Every case below is therefore specialised to concrete coefficients *after* the
    /// symbolic integration, then differentiated back and sampled -- specialising before
    /// integrating would measure the numeric-coefficient path, which was never broken and
    /// is what hid this for so long.
    /// </para>
    /// <para>
    /// Sampled rather than compared symbolically: these antiderivatives are logarithms and
    /// arctangents of surds in the coefficients, and asserting a symbolic zero would be
    /// testing the simplifier's reach rather than the integrator's answer.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class SymbolicQuadraticCoefficientTest
    {
        private const double RelativeTolerance = 1e-8;

        /// <summary>
        /// Differentiates the symbolic antiderivative back and compares it with the
        /// integrand at points where both are real and finite.
        /// </summary>
        private static void AssertAntiderivativeOf(
            string integrand, (string Symbol, double Value)[] coefficients, double[] points)
        {
            var antiderivative = integrand.ToEntity().Integrate("x");
            Assert.False(antiderivative is Entity.Integralf,
                $"{integrand} was declined: {antiderivative.Stringize()}");

            var specialised = antiderivative;
            var specialisedIntegrand = integrand.ToEntity();
            foreach (var (symbol, value) in coefficients)
            {
                specialised = specialised.Substitute(symbol, value);
                specialisedIntegrand = specialisedIntegrand.Substitute(symbol, value);
            }
            specialised = specialised.Substitute("C", 0).Simplify();
            Assert.False(specialised.Nodes.Any(node => node == MathS.NaN),
                $"{integrand} at {Describe(coefficients)} carries a NaN: {specialised.Stringize()}");

            var derivative = specialised.Differentiate("x");
            var compared = 0;
            foreach (var point in points)
            {
                if (!TryEval(derivative, point, out var actual)) continue;
                if (!TryEval(specialisedIntegrand, point, out var expected)) continue;
                compared++;
                var scale = Math.Max(Math.Max(Math.Abs(expected), Math.Abs(actual)), 1e-12);
                Assert.True(Math.Abs(expected - actual) <= RelativeTolerance * scale,
                    $"{integrand} at {Describe(coefficients)}, x = {point}: "
                    + $"differentiated back to {actual}, integrand is {expected}");
            }
            Assert.True(compared > 0,
                $"{integrand} at {Describe(coefficients)}: no point was comparable, so nothing was checked");
        }

        private static string Describe((string Symbol, double Value)[] coefficients)
            => string.Join(", ", coefficients.Select(c => $"{c.Symbol} = {c.Value}"));

        /// <summary>
        /// A point counts only where both sides are real and finite -- these integrands
        /// leave the reals over part of the line, and a skipped point is not a failure.
        /// </summary>
        private static bool TryEval(Entity expr, double point, out double value)
        {
            value = 0;
            try
            {
                var evaluated = expr.Substitute("x", point).EvalNumerical();
                var real = evaluated.RealPart.EDecimal.ToDouble();
                if (Math.Abs(evaluated.ImaginaryPart.EDecimal.ToDouble()) > 1e-9 * Math.Max(1, Math.Abs(real)))
                    return false;
                if (double.IsNaN(real) || double.IsInfinity(real)) return false;
                value = real;
                return true;
            }
            catch { return false; }
        }

        private static readonly double[] AwayFromTheOrigin = { -3.7, -2.1, 0.9, 1.6, 2.8, 4.3 };

        /// <summary>Both signs of the symbolic x^2 coefficient, and both signs of the constant.</summary>
        [Theory]
        [InlineData("1/(1 + b*x^2)", 2.0, 0.0)]
        [InlineData("1/(1 + b*x^2)", -0.5, 0.0)]
        [InlineData("1/(a + b*x^2)", 2.0, 3.0)]
        [InlineData("1/(a + b*x^2)", 2.0, -3.0)]
        [InlineData("1/(a + b*x^2)", -2.0, 3.0)]
        public void ARationalQuadraticWithASymbolicCoefficient(string integrand, double b, double a)
            => AssertAntiderivativeOf(integrand,
                a == 0.0 ? new[] { ("b", b) } : new[] { ("b", b), ("a", a) },
                AwayFromTheOrigin);

        [Theory]
        [InlineData("1/(a^2 + b^2*x^2)", 2.0, 3.0)]
        [InlineData("1/(a^2 - b^2*x^2)", 2.0, 3.0)]
        public void ASquaredSymbolicCoefficient(string integrand, double b, double a)
            => AssertAntiderivativeOf(integrand, new[] { ("b", b), ("a", a) }, AwayFromTheOrigin);

        /// <summary>The radical family, whose degenerate branch used to divide by a literal 0.</summary>
        [Theory]
        [InlineData("1/sqrt(b*x^2 - a^2)", 2.0, 1.0)]
        [InlineData("1/sqrt(b*x^2 + a^2)", 2.0, 1.0)]
        public void ARadicalQuadraticWithASymbolicCoefficient(string integrand, double b, double a)
            => AssertAntiderivativeOf(integrand, new[] { ("b", b), ("a", a) }, AwayFromTheOrigin);

        /// <summary>
        /// The symptom as the issue reports it, which the cases above do not quite cover:
        /// they specialise before simplifying, and the complaint is that simplifying the
        /// *symbolic* answer collapses it to <c>NaN</c>. One degenerate branch dividing by
        /// a literal zero is enough to take the whole piecewise with it, so this is checked
        /// on the answer as a caller would receive it.
        /// </summary>
        [Theory]
        [InlineData("1/(1 + b*x^2)")]
        [InlineData("1/(a + b*x^2)")]
        [InlineData("1/(a^2 + b^2*x^2)")]
        [InlineData("1/(a^2 - b^2*x^2)")]
        [InlineData("1/sqrt(b*x^2 - a^2)")]
        [InlineData("1/sqrt(b*x^2 + a^2)")]
        [InlineData("cos(x)/(a^2 + b^2*sin(x)^2)")]
        public void TheSymbolicAnswerCarriesNoNaN(string integrand)
        {
            var antiderivative = integrand.ToEntity().Integrate("x");
            Assert.False(antiderivative is Entity.Integralf,
                $"{integrand} was declined: {antiderivative.Stringize()}");
            foreach (var answer in new[] { antiderivative, antiderivative.Simplify() })
                Assert.False(answer.Nodes.Any(node => node == MathS.NaN),
                    $"{integrand} answered with a NaN in it: {answer.Stringize()}");
        }

        /// <summary>
        /// The same shape reached through a substitution rather than written in x directly,
        /// which is the form the Rubi corpus that found this uses.
        /// </summary>
        [Theory]
        [InlineData("cos(x)/(a^2 + b^2*sin(x)^2)", 2.0, 3.0)]
        [InlineData("cos(x)/(a^2 - b^2*sin(x)^2)", 2.0, 3.0)]
        public void ASubstitutedQuadraticWithASymbolicCoefficient(string integrand, double b, double a)
            => AssertAntiderivativeOf(integrand, new[] { ("b", b), ("a", a) },
                new[] { -1.1, -0.4, 0.3, 0.8, 1.2, 2.4 });
    }
}
