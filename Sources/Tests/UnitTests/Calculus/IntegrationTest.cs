//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using Xunit;
using AngouriMath.Extensions;

namespace AngouriMath.Tests.Calculus
{
    [Trait("Area", "Calculus")]
    public sealed class IntegrationTest
    {
        // TODO: add more tests
        [Theory]
        [InlineData("2x", "C + x ^ 2")]
        [InlineData("x2", "x ^ 3 / 3 + C")]
        [InlineData("x2 + x", "x ^ 3 / 3 + x ^ 2 / 2 + C")]
        [InlineData("x2 - x", "C + x ^ 3 / 3 - x ^ 2 / 2")]
        [InlineData("a / x", "a * ln(abs(x)) + C")]
        [InlineData("x cos(x)", "cos(x) + x * sin(x) + C")]
        [InlineData("sin(x)cos(x)", "sin(x) ^ 2 / 2 + C")]
        [InlineData("ln(x)", "x * (ln(x) - 1) + C")]
        [InlineData("log(a, x)", "C + x * (ln(x) - 1) / ln(a)")]
        [InlineData("e ^ x", "C + e ^ x")]
        [InlineData("a ^ x", "a ^ x / ln(a) + C")]
        [InlineData("sec(a x + b)", "1/2 * ln((1 + sin(a * x + b)) / (1 - sin(a * x + b))) / a + C")]
        [InlineData("csc(a x + b)", "ln(abs(tan((a * x + b) / 2))) / a + C")]
        [InlineData("C", "C_1 + C * x")]
        [InlineData("C C_1", "C_2 + C * C_1 * x")]
        [InlineData("integral(x, x)", "C_1 + C * x + x ^ 3 / 6")]
        [InlineData("e^e^e^x", "integral(e ^ e ^ e ^ x, x)")] // don't recurse infinitely: e^e^x is Ei(e^x)
        public void TestIndefinite(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            Assert.Equal(MathS.Boolean.True, initial.Integrate("x").EqualTo(expected).Simplify());
            Assert.Equal(expected, MathS.Integral(initial, "x").Simplify().Stringize());
        }

        /// <summary>
        /// https://github.com/asc-community/AngouriMath/issues/946 -- <c>abs</c> is not
        /// holomorphic, so <c>ln(abs(f))</c> is an antiderivative on the real line and nowhere
        /// else. The default codomain is the complex plane, so the default answer is the plain
        /// logarithm.
        /// </summary>
        [Theory]
        [InlineData("1/x", "ln(x) + C")]
        [InlineData("a / x", "a * ln(x) + C")]
        [InlineData("tan(x)", "-ln(cos(x)) + C")]
        [InlineData("cos(x) / sin(x)", "ln(sin(x)) + C")]
        [InlineData("1 / (2*x + 5)", "ln(2 * x + 5) / 2 + C")]
        [InlineData("arctan(x)", "x * arctan(x) - ln(1 + x ^ 2) / 2 + C")]
        public void TheDefaultCodomainIntegratesWithoutAbs(string initial, string expected)
        {
            // Stated rather than assumed: the whole point of these rows is that they are what a
            // caller who has set nothing gets.
            Assert.Equal(AngouriMath.Core.Domain.Complex, MathS.Settings.Codomain.Value);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        /// <summary>
        /// And the real-line form is still reachable, which is the other half of #946: the
        /// absolute value was not wrong, it was unconditional.
        /// </summary>
        [Theory]
        [InlineData("1/x", "ln(abs(x)) + C")]
        [InlineData("tan(x)", "-ln(abs(cos(x))) + C")]
        [InlineData("cos(x) / sin(x)", "ln(abs(sin(x))) + C")]
        public void TheRealCodomainKeepsTheAbsoluteValue(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }
        [Theory]
        [InlineData("2x * e ^ (x2)", "e ^ (x2) + C")]
        [InlineData("x * e ^ (x2)", "1/2 * e ^ (x2) + C")]
        [InlineData("3 * x2 * e ^ (x3)", "e ^ (x3) + C")]
        [InlineData("2 * x * e ^ (x ^ 2) + x ^ 2", "e^x^2 + x^3/3 + C")]
        public void TestExponentialSubstitution(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("sin(x) * cos(x)", "1/2 * sin(x)2 + C")]
        [InlineData("2x * sin(x2)", "-cos(x2) + C")]
        [InlineData("cos(x) / sin(x)", "ln(abs(sin(x))) + C")]
        public void TestTrigonometricSubstitution(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("x / (x2 + 1)", "1/2 * ln(abs(x2 + 1)) + C")]
        [InlineData("2x / (x2 + 5)", "ln(abs(x2 + 5)) + C")]
        [InlineData("x / (1 + x2)", "1/2 * ln(abs(1 + x2)) + C")]
        public void TestRationalFunctionSubstitution(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        // Both were the expanded polynomial, or in the second case an answer that was not
        // an antiderivative at all. u-substitution reaches the closed form now that the
        // variable it substitutes with can no longer collide with the one being integrated.
        [InlineData("x * (x2 + 1) ^ 3", "C + (x ^ 2 + 1) ^ 4 / 8")]
        [InlineData("3 * x2 * (x3 + 2) ^ 2", "C + (x ^ 3 + 2) ^ 3 / 3")]
        public void TestPowerSubstitution(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }


        [Theory]
        [InlineData("sin(2x + 3)", "-1/2 * cos(2x + 3) + C")]
        [InlineData("cos(3x - 1)", "1/3 * sin(3x - 1) + C")]
        [InlineData("e ^ (2x)", "1/2 * e ^ (2x) + C")]
        public void TestLinearSubstitution(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("x / sqrt(x2 + 1)", "sqrt(x2 + 1) + C")]
        [InlineData("x * sqrt(x2 + 1)", "1/3 * (x2 + 1) ^ (3/2) + C")]
        public void TestSquareRootSubstitution(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("x2 * e ^ (x3)", "1/3 * e ^ (x3) + C")]
        [InlineData("sin(x) ^ 2 * cos(x)", "1/3 * sin(x) ^ 3 + C")]
        public void TestCompositeSubstitution(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("1 / (x * ln(x))", "ln(abs(ln(x))) + C")]
        public void TestLogarithmicSubstitution(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("tan(x)", "-ln(abs(cos(x))) + C")]
        [InlineData("sec(x) * tan(x)", "sec(x) + C")]
        public void TestAdvancedTrigSubstitution(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        /// <summary>
        /// Substitutions under a sine, cosine, tangent or secant of a linear that the search's
        /// repeat test keeps: each integrand over the candidate's derivative is a function of the
        /// candidate, so it agrees where the candidate repeats -- at pi - theta for a sine, at
        /// -theta for a cosine, a period on for a tangent -- and is written in it.
        /// </summary>
        [Theory]
        [InlineData("sin(3*x + 1)^4 * cos(3*x + 1)")]
        [InlineData("cos(2*x)^5 * sin(2*x)")]
        [InlineData("tan(2*x)^3 * sec(2*x)^2")]
        [InlineData("cos(x)/(1 + sin(x)^2)")]
        [InlineData("sec(a*x + b)^2 * tan(a*x + b)^5")]
        [InlineData("sin(x) * cos(x)^3/(1 + cos(x)^4)")]
        public void ASubstitutionTheRepeatTestKeeps(string integrand)
        {
            var integral = integrand.Integrate("x").Substitute("C", 0);
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pin(Entity e) => e.Substitute("a", 0.7).Substitute("b", 0.3);
            var derivative = Pin(integral).Differentiate("x");
            var original = Pin(integrand.ToEntity());
            foreach (var at in new[] { 0.2, 0.5, 0.9, 1.3 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = (double)((Entity.Number.Real)(got - want).Abs()).EDecimal.ToDouble();
                Assert.True(difference < 1e-9, $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        [Theory]
        [InlineData("abs(x)", "signum(x) * x^2 / 2 + C")]
        [InlineData("abs(x + 0)", "signum(x) * x^2 / 2 + C")]
        [InlineData("abs(2x)", "signum(2x) * (2x)^2 / 4 + C")]
        [InlineData("abs(x + 3)", "signum(x + 3) * (x + 3)^2 / 2 + C")]
        [InlineData("abs(3x - 1)", "signum(3x - 1) * (3x - 1)^2 / 6 + C")]
        [InlineData("signum(x)", "abs(x) + C")]
        [InlineData("signum(2x)", "abs(2x) / 2 + C")]
        [InlineData("signum(x + 5)", "abs(x + 5) + C")]
        [InlineData("signum(4x - 2)", "abs(4x - 2) / 4 + C")]
        public void TestAbsAndSignumIntegration(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("ln(abs(x))", "x * (ln(abs(x)) - 1) + C")] // Direct pattern: x * (ln|x| - 1)
        [InlineData("ln(abs(2x))", "2 * x / 2 * (ln(abs(2 * x)) - 1) + C")] // Direct pattern with linear arg
        [InlineData("ln(abs(x + 3))", "(x + 3) * (ln(abs(x + 3)) - 1) + C")] // Direct pattern with linear arg
        [InlineData("ln(abs(3x - 1))", "(3 * x - 1) / 3 * (ln(abs(3 * x - 1)) - 1) + C")] // Direct pattern with linear arg
        [InlineData("ln(abs(x + 5))", "(x + 5) * (ln(abs(x + 5)) - 1) + C")] // Direct pattern with linear arg
        [InlineData("ln(abs(2x + 4))", "(2 * x + 4) / 2 * (ln(abs(2 * x + 4)) - 1) + C")] // Direct pattern with linear arg
        // Elementary, and the note that used to sit here said it was not: it named Li(x), which
        // is the antiderivative of 1/ln(x) and a different integrand. This one is ln|x|^2/2. The
        // form below is that up to a constant -- with ln(x) = ln|x| + i*pi on the negative axis
        // it is ln|x|^2/2 + pi^2/2, and differentiating it gives back ln|x|/x either side of zero.
        [InlineData("ln(abs(x)) / x", "ln(abs(x)) * ln(x) - ln(x) ^ 2 / 2 + C")]
        [InlineData("ln(abs(x)) * ln(abs(x))", "ln(abs(x)) * x * (ln(abs(x)) - 1) - (x * (ln(abs(x)) - 1) + -x) + C")]
        [InlineData("x * ln(abs(x^2))", "x ^ 2 * (ln(abs(x ^ 2)) - 1) / 2 + C")] // Solvable via u-substitution
        [InlineData("ln(abs(sin(x))) * cos(x)", "sin(x) * (ln(abs(sin(x))) - 1) + C")] // Solvable via u-substitution
        [InlineData("abs(x) * ln(abs(x))", "integral(abs(x) * ln(abs(x)), x)")] // Unsolvable - requires piecewise handling and integration by parts
        [InlineData("signum(x) * ln(abs(x))", "abs(x) * ln(abs(x)) - abs(x) + C")] // Solvable! sgn(x) = x/|x|, reduces to clever substitution
        [InlineData("ln(abs(abs(x)))", "x * (ln(abs(x)) - 1) + C")] // Simplifies abs(abs(x)) → abs(x), then integrates
        [InlineData("abs(ln(abs(x)))", "integral(abs(ln(abs(x))), x)")] // Unsolvable - nested absolute value with logarithm, no elementary form
        public void TestLnAbsIntegration(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            Assert.Equal(expected, result.Stringize());
        }

        [Theory]
        [InlineData("x * abs(x^2)", "signum(x^2) * (x^2)^2 / 4 + C")]
        [InlineData("2x * abs(x^2 + 1)", "signum(x^2 + 1) * (x^2 + 1)^2 / 2 + C")]
        [InlineData("x * signum(x^2)", "abs(x^2) / 2 + C")]
        [InlineData("2x * signum(x^2 + 3)", "abs(x^2 + 3) + C")]
        [InlineData("cos(x) * abs(sin(x))", "signum(sin(x)) * sin(x)^2 / 2 + C")]
        [InlineData("cos(x) * signum(sin(x))", "abs(sin(x)) + C")]
        [InlineData("sin(x) * abs(cos(x))", "signum(cos(x)) * cos(x)^2 / (-2) + C")]
        [InlineData("sin(x) * signum(cos(x))", "abs(cos(x)) / (-1) + C")]
        public void TestAbsAndSignumWithSubstitution(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        // This is the closed form the old expectation's comment said it differed from.
        [InlineData("x * (x ^ 2 + 1) ^ 10", "C + (x ^ 2 + 1) ^ 11 / 22")]
        [InlineData("x * (x ^ 2 + 1) ^ n", "1/(2(n+1)) * (x^2 + 1) ^ (n+1) + C")]
        public void TestHighPowerSubstitution(string initial, string expected)
        {
            var result = initial.Integrate("x").Simplify();
            var expectedResult = expected.ToEntity().Simplify();
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        [InlineData("1", "0", "1", "1")]
        [InlineData("x", "0", "1", "1/2")]
        [InlineData("sin(x)", "-1", "1", "0")]
        [InlineData("cos(x)", "0", "pi", "0")]
        [InlineData("1/(x^2+1)", "-oo", "+oo", "pi")]
        [InlineData("e^e^e^x", "0", "1", "integral(e ^ e ^ e ^ x, x, 0, 1)")] // don't recurse infinitely: e^e^x is Ei(e^x)
        public void TestDefinite(string initial, string from, string to, string expected)
        {
            Assert.Equal(expected, initial.Integrate("x", from, to).Simplify().Stringize());
            Assert.Equal(expected, MathS.Integral(initial, "x", from, to).Simplify().Stringize());
        }

        [Theory] // TODO: Some of these results can be further simplified, e.g. (4 + 2 * x) / 2
        [InlineData("1 / (x^2 + 1)", "arctan(x) + C")] // no linear term
        [InlineData("1 / (x^2 + 4)", "1/2 * arctan(x/2) + C")] // no linear term
        [InlineData("1 / (4*x^2 + 1)", "1/2 * arctan(2*x) + C")] // no linear term
        [InlineData("2 / (x^2 + 1)", "2 * arctan(x) + C")] // no linear term
        [InlineData("1 / (x^2 - 1)", "1/2 * ln(abs((x - 1) / (x + 1))) + C")] // no linear term
        [InlineData("1 / (x^2 - 4)", "ln(abs(1 + (-8) / (4 + 2 * x))) / 4 + C")] // no linear term
        [InlineData("1 / (9 - x^2)", "ln(abs(1 + (-12) / (6 + (-2) * x))) / 6 + C")] // no linear term
        [InlineData("1 / (x^2 + 2*x + 1)", "-1 / (x + 1) + C")] // perfect square
        [InlineData("1 / (x^2 + 4*x + 4)", "(-2) / (2 * x + 4) + C")] // perfect square
        [InlineData("2 / (x^2 - 6*x + 9)", "(-4) / (2 * x - 6) + C")] // perfect square
        [InlineData("1 / (x^2 + 2*x + 2)", "arctan(x + 1) + C")] // complete the square via arctan
        [InlineData("1 / (x^2 + 4*x + 5)", "arctan((4 + 2 * x) / 2) + C")] // complete the square via arctan
        [InlineData("1 / (x^2 - 2*x + 2)", "arctan(x - 1) + C")] // complete the square via arctan
        [InlineData("3 / (x^2 + 6*x + 10)", "3 * arctan((6 + 2 * x) / 2) + C")] // complete the square via arctan
        [InlineData("1 / (x^2 + 2*x - 3)", "ln(abs(1 + (-8) / (6 + 2 * x))) / 4 + C")] // complete the square via log
        [InlineData("1 / (x^2 - 2*x - 3)", "1/4 * ln(abs((x - 3) / (x + 1))) + C")] // complete the square via log
        [InlineData("2 / (x^2 + 4*x - 5)", "1/3 * ln(abs(1 + (-12) / (10 + 2 * x))) + C")] // complete the square via log
        [InlineData("1 / (2*x^2 + 3*x + 1)", "ln(abs(1 + -1/2 / (x + 1))) + C")] // factorable
        [InlineData("1 / (3*x^2 + 5*x + 2)", "ln(abs(1 + -1/3 / (x + 1))) + C")] // factorable
        public void TestQuadraticDenominator(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Fact]
        public void TestLnAbsSquared()
        {
            // Test that ln(abs(x))^2 can be integrated without stack overflow
            // ∫ln²(abs(x)) dx = x·ln²(abs(x)) - 2x·ln(abs(x)) + 2x + C
            //                 = x(ln²(abs(x)) - 2ln(abs(x)) + 2) + C
            
            var expr = "ln(abs(x)) ^ 2".ToEntity();
            var result = expr.Integrate("x").Simplify(1);
            // (ln|x| - 1)^2 + 1 = ln^2|x| - 2ln|x| + 2, so this is the antiderivative
            // written above. Term collection used to stop short, leaving the repeated
            // ln(abs(x)) uncombined and the 2 * x outside the bracket.
            // (ln|x| - 1)^2 + 1 multiplied out is ln^2|x| - 2ln|x| + 2, which is what
            // Expand now returns, having collected the like terms it used to leave apart.
            Assert.Equal("C + x * (2 + ln(abs(x)) ^ 2 + (-2) * ln(abs(x)))", result.Stringize());

            // Verify the result by differentiation
            var derivative = result.Differentiate("x"); // TODO: Make this simplify to expr with Simplify()
            foreach (var point in new[] { -3, 7 }) // TODO: Implement and test for "provided x in R" in result
            {
                // Compared as a difference rather than digit for digit: the two are equal
                // algebraically, but evaluating each to 100 digits separately need not
                // agree in the last of them, and which form the antiderivative takes is
                // not what this test is about.
                var difference = (expr.Substitute(x, point) - derivative.Substitute(x, point)).EvalNumerical();
                Assert.True(difference.Abs().EDecimal.ToDouble() < 1e-90,
                    $"at x = {point} the derivative is off by {difference.Stringize()}");
            }
        }

        // These five no longer need integration by parts at all. e^x*sin(x) and its
        // cosine twin cycle under by parts, so they are solved as the closed form that
        // cycle resolves to; the inverse trigonometric ones are by parts against 1, which
        // has no product for the by-parts solver to split, so they are table entries.
        [Theory]
        [InlineData("e^x * sin(x)", "-1/2 * cos(x) * e ^ x + 1/2 * sin(x) * e ^ x + C")]
        [InlineData("e^x * cos(x)", "1/2 * cos(x) * e ^ x + 1/2 * sin(x) * e ^ x + C")]
        [InlineData("arctan(x)", "x * arctan(x) - 1/2 * ln(abs(x ^ 2 + 1)) + C")]
        [InlineData("arcsin(x)", "x * arcsin(x) + sqrt(1 - x ^ 2) + C")]
        [InlineData("arccos(x)", "x * arccos(x) - sqrt(1 - x ^ 2) + C")]
        public void TestIntegrationByPartsNonPolynomial(string initial, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        // Still open: this one wants by parts applied three times over.
        [Theory(Skip = "TODO: integration by parts multiple times")]
        [InlineData("ln(abs(x)) ^ 3", "C + x * (ln(abs(x)) ^ 3 - ln(abs(x)) ^ 2 - ln(abs(x)) ^ 2 - ln(abs(x)) ^ 2) + 6 * (x * (ln(abs(x)) - 1) + -x)")]
        public void TestTripleIntegrationByParts(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory(Skip = "TODO: integration by parts multiple times")]
        [InlineData("sin(ln(abs(x)))", "x / 2 * (sin(ln(abs(x))) - cos(ln(abs(x)))) + C")] // Integration by parts twice
        [InlineData("cos(ln(abs(x)))", "x / 2 * (sin(ln(abs(x))) + cos(ln(abs(x)))) + C")] // Integration by parts twice
        [InlineData("ln(abs(x)) * sin(x)", "-ln(abs(x)) * cos(x) + sin(x) + C")] // Integration by parts
        [InlineData("ln(abs(x)) * cos(x)", "ln(abs(x)) * sin(x) + cos(x) + C")] // Integration by parts
        public void TestIntegrationByPartsMixed(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        [Theory]
        // The polynomial coefficient now comes out written out rather than factored --
        // `x^2 - 2x + 2` where this used to say `x^2 - 2(x - 1)`, which is the same polynomial.
        // The rule that answers it is the closed one for a polynomial times an exponential times
        // a trigonometric function, and it collects its coefficients as a sum. Their difference
        // simplifies to exactly 0; `EqualTo(...).Simplify()` does not settle it either way, which
        // is why the form is pinned here at all.
        [InlineData("e^x * x^2", "e ^ x * (x ^ 2 + (-2) * x + 2) + C")] // Polynomial times exponential (should use recursive polynomial IBP)
        [InlineData("x^3 * sin(x)", "-cos(x) * x ^ 3 + 6 * cos(x) * x + (-6) * sin(x) + 3 * sin(x) * x ^ 2 + C")] // Polynomial times trig (should use recursive polynomial IBP)
        // [InlineData("x * ln(abs(x)) ^ 2", "x ^ 2 / 2 * (ln(abs(x)) ^ 2 - ln(abs(x)) - ln(abs(x))) + x ^ 2 + C")] // TODO: ln(abs(x)) ^ 2 needs integration by parts
        public void TestPolynomialIntegrationByParts(string initial, string expected)
        {
            var result = initial.Integrate("x").InnerSimplified;
            var expectedResult = expected.ToEntity().InnerSimplified;
            Assert.Equal(MathS.Boolean.True, result.EqualTo(expectedResult).Simplify());
        }

        static readonly Entity.Variable x = nameof(x);
        [Fact]
        public void Test1()
        {
            var expr = x;

            Assert.True((expr.DefiniteIntegral(x, 0, 1).RealPart - 1.0 / 2).Abs() < 0.1);
        }
        [Fact]
        public void Test2()
        {
            var expr = MathS.Sin(x);
            // Quadrature over a symmetric interval leaves a residual around 1e-17, which
            // downcasting used to round away. It no longer does, because the same rounding
            // was destroying every legitimate small number (see IssueRegressionTest, and
            // https://github.com/asc-community/AngouriMath/issues/602).
            // Asserted the way Test1 and Test3 assert, against the method's own accuracy.
            Assert.True(expr.DefiniteIntegral(x, -1, 1).Abs() < 1e-15);
        }
        [Fact]
        public void Test3()
        {
            var expr = MathS.Sin(x);
            Assert.True(expr.DefiniteIntegral(x, 0, 3).RealPart > 1.5);
        }
    }
}
