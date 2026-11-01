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
    /// An equation in <c>sqrt(x)</c> and <c>x</c> is solved by raising the radical's side to a power.
    /// Cleared of its denominator, <c>sqrt(x) - 1/x - 1 - i = 0</c> holds <c>x * sqrt(x)</c>, which
    /// the power rules write as <c>x ^ (1/2 + 1)</c>; read with its exponent a sum, that was no
    /// fractional power, and the equation went to Newton's method, which answered one decimal root
    /// and not the other, -1. A power past one is read as a whole power times a proper fractional
    /// one, so <c>x^(3/2)</c> shares its radical with <c>sqrt(x)</c>. Found by a check of systems
    /// built from known solutions, which met this equation after an elimination; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARadicalTimesAPowerTest
    {
        [Theory]
        [InlineData("sqrt(x) - 1/x - 1 - i = 0", "sqrt(x) - 1/x - 1 - i", 2, "-1")]
        [InlineData("x*sqrt(x) - 1 - (1 + i)*x = 0", "x*sqrt(x) - 1 - (1 + i)*x", 2, "-1")]
        // 3 sqrt(x) - x^(3/2) is the one term (3 - x) sqrt(x). Read as sqrt(x) and sqrt(x^3), two
        // radicals, it cannot be raised to a power, and Newton's method finds no root 0.
        [InlineData("(3 - x)*sqrt(x) - x = 0", "(3 - x)*sqrt(x) - x", 2, "0")]
        [InlineData("x^2*sqrt(x) + 3*sqrt(x) + 2*x = 0", "x^2*sqrt(x) + 3*sqrt(x) + 2*x", 3, "0")]
        // s^3 + 1 = 0 in s = sqrt(x): the two roots with a principal square root, and not s = -1.
        [InlineData("sqrt(x) + 1/x = 0", "sqrt(x) + 1/x", 2, "-1/2 + sqrt(3)/2 * i")]
        // s^3 - s^2 - 1 = 0: the real root only, as the other two have a negative real part.
        [InlineData("sqrt(x) - 1/x - 1 = 0", "sqrt(x) - 1/x - 1", 1, null)]
        public void EveryRootIsExactAndSatisfiesTheEquation(string equation, string residual, int count, string? among)
        {
            var roots = Assert.IsType<Set.FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(count, roots.Count);
            foreach (var root in roots)
            {
                Assert.True(root.Nodes.OfType<Complex>().All(number => number.RealPart is Rational && number.ImaginaryPart is Rational),
                    $"{root} is in decimals");
                var value = residual.ToEntity().Substitute("x", root).EvalNumerical();
                Assert.True(Complex.Abs(value).EDecimal.CompareTo(EDecimal.FromString("1E-20")) < 0, $"{root} leaves {value}");
            }
            if (among is { })
                Assert.Contains(roots, root => (root - among.ToEntity()).Simplify() == 0);
        }
    }
}
