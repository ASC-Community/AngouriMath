//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A quartic whose depressed form <c>y^4 + alpha y^2 + gamma</c>, for <c>y = x + b/(4a)</c>, has
    /// no cubic term is solved as a quadratic in <c>y^2</c> and shifted back by <c>b/(4a)</c>. The
    /// shift was written <c>-b / 4 * a</c>, which is <c>-a b / 4</c>, so with a leading coefficient
    /// other than 1 or -1 every candidate was off and failed its check, and the quartic was
    /// answered <c>{ }</c>. Found by a cross-check of equations in square roots, whose squared
    /// forms are such quartics; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ADepressedBiquadraticQuarticTest
    {
        private static void AssertRoots(string equation, params string[] roots)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(roots.Length, answer.Count);
            foreach (var root in roots)
                Assert.Contains(answer, found => Number.IsZero((found - root.ToEntity()).EvalNumerical()));
        }

        /// <summary>Irreducible over the rationals, so that no factoring answers it first.</summary>
        [Fact]
        public void FourRealRoots()
            => AssertRoots("31x^4 - 62x^3 + 33x^2 - 2x - 1 = 0",
                "1/2 + sqrt((27/62 + 8/31 * sqrt(2)) / 2)", "1/2 - sqrt((27/62 + 8/31 * sqrt(2)) / 2)",
                "1/2 + sqrt((27/62 - 8/31 * sqrt(2)) / 2)", "1/2 - sqrt((27/62 - 8/31 * sqrt(2)) / 2)");

        /// <summary><c>2(x - 1)^4 - 2(x - 1)^2 - 1</c>: two real roots and two that are not.</summary>
        [Fact]
        public void TwoRealRootsAndTwoComplex()
            => AssertRoots("2x^4 - 8x^3 + 10x^2 - 4x - 1 = 0",
                "1 + sqrt((1 + sqrt(3)) / 2)", "1 - sqrt((1 + sqrt(3)) / 2)",
                "1 + sqrt((1 - sqrt(3)) / 2)", "1 - sqrt((1 - sqrt(3)) / 2)");

        /// <summary>
        /// Equations in a square root whose squared form is such a quartic, each answered
        /// <c>{ }</c>. The first has the one real root its squaring keeps; the second, four
        /// complex ones.
        /// </summary>
        [Fact]
        public void TheEquationsInARootWhoseSquareIsOne()
        {
            AssertRoots("x^2 - x - 1 = 4x * sqrt(2x^2 - 4x + 2)", "(1 - sqrt(1 + 4 / (1 + 4 * sqrt(2)))) / 2");
            AssertRoots("(2x^2 + 4x - 2) / sqrt(-2x - x^2) = -2",
                "-1 + sqrt((3 + sqrt(-3)) / 2)", "-1 - sqrt((3 + sqrt(-3)) / 2)",
                "-1 + sqrt((3 - sqrt(-3)) / 2)", "-1 - sqrt((3 - sqrt(-3)) / 2)");
        }
    }
}
