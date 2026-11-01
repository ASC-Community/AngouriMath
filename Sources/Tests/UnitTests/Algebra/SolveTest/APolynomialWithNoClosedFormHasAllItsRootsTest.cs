//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A polynomial with rational coefficients and no roots in closed form has all its roots, found
    /// by the Durand-Kerner iteration, and is not first written in x to the least of its powers,
    /// which lost roots of its own. Newton's method was started inside a bounded region and
    /// answered the roots it found there as though they were all: <c>x^5 - 30x^4 + 1 = 0</c> has three
    /// real roots, one of them near 30, and two off the real line, and was answered with one of them.
    /// Found following an equation in <c>x^(1/3)</c> that cubes to a polynomial of degree 7 with a
    /// root near 23; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class APolynomialWithNoClosedFormHasAllItsRootsTest
    {
        [Theory]
        [InlineData("x^5 - 30x^4 + 1", 5, "29.9")]
        [InlineData("x^7 - 30x^6 + 1", 7, "29.9")]
        [InlineData("x^7 - 5x^6 + 1", 7, "4.9")]
        [InlineData("(x^2 + 3)^3 * x - (3x^2 - 3x + 1)^3", 7, "23.3")]
        public void EveryRootIsThere(string polynomial, int degree, string farRoot)
        {
            var roots = Assert.IsType<Set.FiniteSet>($"{polynomial} = 0".ToEntity().Solve("x"));
            Assert.Equal(degree, roots.Count);
            foreach (var root in roots)
            {
                var at = root.EvalNumerical();
                var size = EDecimal.Max(EDecimal.One, Complex.Abs(at).EDecimal);
                var residual = Complex.Abs(polynomial.ToEntity().Substitute("x", at).EvalNumerical()).EDecimal;
                Assert.True(residual.CompareTo(EDecimal.FromString("1E-17").Multiply(size.Pow(degree))) < 0,
                    $"{root} leaves {residual}");
            }
            Assert.Contains(roots, root => root.EvalNumerical() is Real real && real.EDecimal.CompareTo(EDecimal.FromString(farRoot)) > 0);
        }

        /// <summary>
        /// An equation in powers of x is solved through x to the greatest common divisor of the
        /// powers. Through the least of them, x^4 - 3x^(5/2) + 2 was written in t = x^(5/2) with
        /// x^4 as t^(8/5), which is not x^4 off the positive reals, and two of its four roots were lost.
        /// </summary>
        [Fact]
        public void AnEquationInPowersIsSolvedThroughTheirCommonDivisor()
        {
            var roots = Assert.IsType<Set.FiniteSet>("x^4 - 3*x^(5/2) + 2 = 0".ToEntity().Solve("x"));
            Assert.Equal(4, roots.Count);
            foreach (var root in roots)
            {
                var residual = Complex.Abs("x^4 - 3*x^(5/2) + 2".ToEntity().Substitute("x", root.EvalNumerical()).EvalNumerical()).EDecimal;
                Assert.True(residual.CompareTo(EDecimal.FromString("1E-20")) < 0, $"{root} leaves {residual}");
            }
        }

        /// <summary>Where the polynomial has a closed form, it is still answered in it.</summary>
        [Fact]
        public void APolynomialInACubeIsStillExact()
        {
            var roots = Assert.IsType<Set.FiniteSet>("x^6 + x^3 + 1 = 0".ToEntity().Solve("x"));
            Assert.Equal(6, roots.Count);
            Assert.All(roots, root => Assert.True(root.Nodes.OfType<Complex>().All(number => number.RealPart is Rational && number.ImaginaryPart is Rational),
                $"{root} is in decimals"));
        }
    }
}
