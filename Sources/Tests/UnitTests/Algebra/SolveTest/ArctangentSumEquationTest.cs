//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation linear in arctangents of polynomials is solved by taking the tangent: every root
    /// is one of a polynomial equation's, and each of those is put back, since the tangent of the
    /// sum agrees at every other multiple of pi as well. Newton's method answered these in
    /// decimals, and <c>arctan(x) - arctan(2x) = 1</c>, whose roots are complex, not at all. Found
    /// by a cross-check of equations in inverse functions against a Newton search; no issue was
    /// filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ArctangentSumEquationTest
    {
        private static void AssertRoots(string equation, params string[] roots)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(roots.Length, answer.Count);
            foreach (var root in roots)
                Assert.Contains(answer, found => Number.IsZero((found - root.ToEntity()).EvalNumerical()));
            // Exact, not a decimal from a numerical search.
            Assert.DoesNotContain(answer, found => found is Number.Real and not Number.Rational);
        }

        [Theory]
        // -(3 + sqrt(17)) / 4 makes the tangents agree too, where the sum is -3 pi / 4.
        [InlineData("arctan(x) + arctan(2x) = pi/4", "(sqrt(17) - 3) / 4")]
        // cos(pi/2) is 0, so the equation is the real part, 1 - 2x^2 = 0; at -sqrt(2)/2 the sum is -pi/2.
        [InlineData("arctan(x) + arctan(2x) = pi/2", "sqrt(2) / 2")]
        [InlineData("arctan(x) - arctan(2x) = 1",
            "(-1 - sqrt(1 - 8 tan(1)^2)) / (4 tan(1))", "(-1 + sqrt(1 - 8 tan(1)^2)) / (4 tan(1))")]
        [InlineData("arctan(x) + arctan(2x) = 3", "(-3 - sqrt(9 + 8 tan(3)^2)) / (4 tan(3))")]
        [InlineData("arctan(x) + arctan(x / 2) = -3", "(-3/2 - sqrt(9/4 + 2 tan(3)^2)) / tan(-3)")]
        // x^3 + x^2 + x - 1 = 0; at its two complex roots the difference is -pi.
        [InlineData("2 arctan(x) - arctan(x + 1) = 0", "(-1 + (17 + 3 sqrt(33))^(1/3) - 2 / (17 + 3 sqrt(33))^(1/3)) / 3")]
        // Two principal arctangents add up to a real part of at most pi.
        [InlineData("arctan(x) + arctan(2x) = 4")]
        public void SolvedByTheTangent(string equation, params string[] roots)
            => AssertRoots(equation, roots);

        /// <summary>Over the reals the complex roots go.</summary>
        [Fact]
        public void OverTheRealsTheComplexRootsGo()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal("{ }".ToEntity(), "arctan(x) - arctan(2x) = 1".ToEntity().Solve("x"));
            AssertRoots("arctan(x) + arctan(2x) = pi/4", "(sqrt(17) - 3) / 4");
        }

        /// <summary>
        /// Where the tangent of the sum is constant the polynomial is identically zero, and the
        /// equation holds on a region rather than at points: arctan(x) + arctan(1/x) is pi/2 for
        /// every x with a positive real part. Not answered with a finite set.
        /// </summary>
        [Theory]
        [InlineData("arctan(x) + arctan(1 / x) = pi/2")]
        [InlineData("arctan(x) + arctan(-x) = 0")]
        public void AnIdentityIsNotAFiniteSet(string equation)
            => Assert.IsNotType<FiniteSet>(equation.ToEntity().Solve("x"));
    }
}
