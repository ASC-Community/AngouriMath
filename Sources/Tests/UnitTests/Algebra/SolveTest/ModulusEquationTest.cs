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
        /// <c>x</c> inside a modulus, a conjugate, a real or an imaginary part and outside it too,
        /// solved in the real and imaginary parts of <c>x</c>, exactly. Newton's method assumes a
        /// holomorphic function, and its near-copies of a root came back as roots:
        /// <c>x^2/|1 - x^2| = x</c> had <c>1.618 + 1.7e-9 i</c> beside <c>1.618</c>. Nor does a
        /// split by the sign find every root, since over the complex numbers <c>|u| = +-u</c> only
        /// where <c>u</c> is real: <c>x^2 + 1 = |x|</c> is solved by <c>+-0.618 i</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2139">#2139</a>
        /// </summary>
        [Theory]
        [InlineData("x^2/abs(1 - x^2) = x", "0", "(sqrt(5) - 1) / 2", "(sqrt(5) + 1) / 2")]
        [InlineData("abs(x^2 - 4) = 3 x", "1", "4")]
        [InlineData("x * abs(x) = 4", "2")]
        [InlineData("x^2 + 1 = abs(x)", "i * (sqrt(5) - 1) / 2", "-i * (sqrt(5) - 1) / 2")]
        [InlineData("x * (abs(x) - x^2) = 0", "0", "1", "-1")]
        [InlineData("1 / abs(x) = x^2", "1", "-1")]
        [InlineData("abs(1 / (x - 1)) = x", "(sqrt(5) + 1) / 2")]
        [InlineData("conj(x) = x^2", "0", "1", "-1/2 + i * sqrt(3) / 2", "-1/2 - i * sqrt(3) / 2")]
        [InlineData("x + Re(x) = 3 + i", "3/2 + i")]
        [InlineData("Im(x)^2 + x = 1", "1")]
        [InlineData("x + conj(x) = 2 + i")]
        [InlineData("abs(x)^2 = x + i")]
        [InlineData("x + sgn(x) = 3", "2")]
        [InlineData("x * sgn(x) = 2", "2", "-2")]
        [InlineData("x^2 = sgn(x)", "0", "1")]
        [InlineData("sgn(x^2 - 1) = x")]
        public void OverTheComplexNumbersAnEquationInAndOutOfAModulusIsSolvedInThePartsOfX(string equation, params string[] roots)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(roots.Length, answer.Count);
            foreach (var root in roots)
                Assert.Contains(answer, found => Entity.Number.IsZero((found - root.ToEntity()).EvalNumerical()));
        }

        /// <summary>
        /// Left unsolved where the roots are not finitely many, as the ray <c>|x| = Re x</c>; where
        /// one is not found exactly, as the real root of <c>x^5 - x - 1</c>; where the system is
        /// too large to solve in the time an answer is worth; and with an argument, which is not a
        /// polynomial in the parts of <c>x</c>.
        /// </summary>
        [Theory]
        [InlineData("abs(x) = Re(x)")]
        [InlineData("x^5 = abs(x) + 1")]
        [InlineData("x^7 = abs(x) + 1")]
        [InlineData("x + arg(x) = 1")]
        public void OverTheComplexNumbersTheRestIsLeftUnsolved(string equation)
            => Assert.IsType<ConditionalSet>(equation.ToEntity().Solve("x"));

        /// <summary>The variable only inside the modulus, or not in one at all, is answered as before.</summary>
        [Theory]
        [InlineData("abs(x) = 2")]
        [InlineData("x^2 = 4")]
        public void OverTheComplexNumbersTheOthersAreStillAnswered(string equation)
            => Assert.IsNotType<ConditionalSet>(equation.ToEntity().Solve("x"));

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
