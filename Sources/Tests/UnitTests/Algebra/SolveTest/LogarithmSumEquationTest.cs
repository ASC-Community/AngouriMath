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
    /// An equation linear in logarithms of polynomials to one base is solved by exponentiating:
    /// every root is one of the polynomial equation's, and each of those is put back, since over the
    /// complex numbers <c>ln(u) + ln(v)</c> is <c>ln(u v)</c> only up to <c>2 pi i</c>. Newton's
    /// method answered these in decimals, and where it did not reach a root it left it out:
    /// <c>ln(1 + 3x^2) - ln(4x) = 1</c> was <c>{ 3.53 }</c>, without <c>0.0944</c>. Found by a
    /// cross-check of equations in logarithms against a Newton search; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class LogarithmSumEquationTest
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
        // (-1 - sqrt(5)) / 2 makes x(x + 1) = 1 too, and there both logarithms carry i pi.
        [InlineData("ln(x) + ln(x + 1) = 0", "(sqrt(5) - 1) / 2")]
        [InlineData("ln(1 + 3x^2) - ln(4x) = 1", "(4e - sqrt(16e^2 - 12)) / 6", "(4e + sqrt(16e^2 - 12)) / 6")]
        // Both arguments negative: their i pi cancel.
        [InlineData("ln(-1 - 3x^2) - ln(4x) = 1", "-(4e - sqrt(16e^2 - 12)) / 6", "-(4e + sqrt(16e^2 - 12)) / 6")]
        [InlineData("ln(x^2 + 1) - ln(x) = 1", "(e - sqrt(e^2 - 4)) / 2", "(e + sqrt(e^2 - 4)) / 2")]
        // x = -2 makes x(x - 1) = 6, where the sum is ln(6) + 2 pi i.
        [InlineData("ln(x) + ln(x - 1) = ln(6)", "3")]
        [InlineData("ln(x - 2) + ln(x - 3) = ln(2)", "4")]
        // x = -1 makes x^2 = x + 2, where 2 ln(x) is 2 pi i and ln(1) is 0.
        [InlineData("2 * ln(x) = ln(x + 2)", "2")]
        [InlineData("log(2, x) + log(2, x - 2) = 3", "4")]
        [InlineData("ln(x) / 2 + ln(x + 1) = 1",
            "-(2 + ((-2 - 27 e^2 + sqrt((-2 - 27 e^2)^2 - 4)) / 2)^(1/3) + 1 / ((-2 - 27 e^2 + sqrt((-2 - 27 e^2)^2 - 4)) / 2)^(1/3)) / 3")]
        public void SolvedByExponentiating(string equation, params string[] roots)
            => AssertRoots(equation, roots);

        /// <summary>Over the reals a logarithm of a negative number has no value, and the root that needs one goes.</summary>
        [Fact]
        public void OverTheRealsTheArgumentsArePositive()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            AssertRoots("ln(1 + 3x^2) - ln(4x) = 1", "(4e - sqrt(16e^2 - 12)) / 6", "(4e + sqrt(16e^2 - 12)) / 6");
            Assert.Equal("{ }".ToEntity(), "ln(-1 - 3x^2) - ln(4x) = 1".ToEntity().Solve("x"));
        }
    }
}
