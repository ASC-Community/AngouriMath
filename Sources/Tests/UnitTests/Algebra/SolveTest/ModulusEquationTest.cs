//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation in moduli. Over the complex numbers <c>|x - 2| = |x - 3|</c> holds on the line
    /// <c>Re x = 5/2</c>, and Newton's method answered it with three points of that line. Over the
    /// reals it is <c>x = 5/2</c>, found by cases between the kinks.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1573">#1573</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ModulusEquationTest
    {
        /// <summary>
        /// Real for every complex <c>x</c>, so a curve solves it, and no list of points is its
        /// solution set: left as the set of the <c>x</c> satisfying it.
        /// </summary>
        [Theory]
        [InlineData("abs(x - 2) = abs(x - 3)")]
        [InlineData("abs(x - 1) + abs(x - 2) = 5")]
        [InlineData("abs(2 x - 1) - abs(x) = 3")]
        public void OverTheComplexNumbersACurveIsNotAListOfPoints(string equation)
            => Assert.IsType<ConditionalSet>(equation.ToEntity().Solve("x"));

        /// <summary>
        /// Sullivan and Mackey's Prob 1.5.16, and moduli between which a whole interval solves.
        /// </summary>
        [Theory]
        [InlineData("abs(x - 2) = abs(x - 3)", "{ 5/2 }")]
        [InlineData("abs(2 x - 1) = abs(2 x - 3)", "{ 1 }")]
        [InlineData("abs(2 x - 2) = abs(3 x - 3)", "{ 1 }")]
        [InlineData("abs(x + 1) = abs(x - 5)", "{ 2 }")]
        [InlineData("abs(x - 1) + abs(x - 2) = abs(x - 3)", "{ 0, 2 }")]
        [InlineData("abs(x - 1) + abs(x - 2) = 5", "{ -1, 4 }")]
        [InlineData("abs(x) = 2", "{ -2, 2 }")]
        [InlineData("abs(x) = -1", "{ }")]
        [InlineData("abs(x - 1) + abs(x - 2) = 1", "[1; 2]")]
        public void OverTheRealsTheKinksCutItIntoCases(string equation, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal(expected.ToEntity(), equation.ToEntity().Solve("x"));
        }

        /// <summary>
        /// A modulus of a polynomial keeps one sign between the polynomial's neighbouring real
        /// roots, so those are its kinks. <c>x^2/|1 - x^2| = x</c> was answered by a numerical
        /// search, in decimals and with two non-real numbers that solve nothing.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2139">#2139</a>
        /// </summary>
        [Theory]
        [InlineData("x^2/abs(1 - x^2) = x", "{ 0, (-1 + sqrt(5)) / 2, (1 + sqrt(5)) / 2 }")]
        [InlineData("abs(x^2 - 4) = 3 x", "{ 1, 4 }")]
        [InlineData("abs(x^2 - 1) + abs(x - 2) = 3", "{ -1, 0, 2 }")]
        [InlineData("abs(x^2 - 2) = 1", "{ -sqrt(3), 1, -1, sqrt(3) }")]
        [InlineData("abs(x^2 + 1) = 2", "{ 1, -1 }")]
        [InlineData("x^2 + 1 = abs(x)", "{ }")]
        public void OverTheRealsAPolynomialIsCutAtItsRealRoots(string equation, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal(expected.ToEntity(), equation.ToEntity().Solve("x"));
        }
    }
}
