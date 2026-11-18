//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation between two inverse trigonometric functions is solved by a polynomial equation
    /// in their squared sines and cosines, whose roots are put back, since it also has those where
    /// the two differ by a multiple of pi or are opposite. Newton's method answered these with the
    /// roots it found: <c>arcsin(-2x) = arccos(3x + 2)</c> has two complex roots and neither was
    /// among them. Found by a cross-check of equations in inverse functions against a Newton search;
    /// no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class InverseTrigonometricPairEquationTest
    {
        private static FiniteSet AssertRoots(string equation, params string[] roots)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(roots.Length, answer.Count);
            foreach (var root in roots)
                Assert.Contains(answer, found => Number.IsZero((found - root.ToEntity()).EvalNumerical()));
            // Exact, not a decimal from a numerical search.
            Assert.DoesNotContain(answer, found => found is Number.Real and not Number.Rational);
            return answer;
        }

        [Theory]
        [InlineData("arcsin(-2x) = arccos(3x + 2)", "(-6 - sqrt(3) * i) / 13", "(-6 + sqrt(3) * i) / 13")]
        // At -sqrt(2)/2 the arcsine is -pi/4 and the arccosine 3 pi / 4.
        [InlineData("arcsin(x) = arccos(x)", "sqrt(2) / 2")]
        // -3x^2 + 4x - 1 = 0; at 1 the two arcsines add up to pi.
        [InlineData("arcsin(x) = -arcsin(2x - 1)", "1/3")]
        // The sum is pi/2 wherever both have a value, so neither root of 2x^2 = 1 is one.
        [InlineData("arcsin(x) + arccos(x) = 0")]
        public void SolvedBySquares(string equation, params string[] roots)
            => AssertRoots(equation, roots);

        /// <summary>
        /// x (4x^3 - 4x^2 + 5x - 4) = 0: at its real root 0.876... the two add up to pi, and the
        /// two complex roots are 0.0619570552789533 +- 1.06658422993151887 i, by mpmath.
        /// </summary>
        [Fact]
        public void ArctangentAgainstArccosine()
        {
            var answer = Assert.IsType<FiniteSet>("arctan(x) = arccos(1 - 2x)".ToEntity().Solve("x"));
            Assert.Equal(3, answer.Count);
            var values = answer.Select(root => root.EvalNumerical()).Cast<Number.Complex>().ToList();
            Assert.Contains(values, value => Number.IsZero(value));
            foreach (var sign in new[] { 1, -1 })
            {
                var expected = Number.Complex.Create(
                    Number.Real.Create(PeterO.Numbers.EDecimal.FromString("0.0619570552789533640")),
                    Number.Real.Create(PeterO.Numbers.EDecimal.FromString("1.06658422993151887")) * sign);
                Assert.Contains(values, value => ((value - expected).Abs() < Number.Real.Create(PeterO.Numbers.EDecimal.FromString("1E-15"))));
            }
        }

        /// <summary>Over the reals the complex roots go.</summary>
        [Fact]
        public void OverTheRealsTheComplexRootsGo()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal("{ }".ToEntity(), "arcsin(-2x) = arccos(3x + 2)".ToEntity().Solve("x"));
            AssertRoots("arcsin(x) = arccos(x)", "sqrt(2) / 2");
        }
    }
}
