//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using System.Numerics;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A polynomial whose roots have no closed form is solved by the Durand-Kerner iteration,
    /// which started its approximations on a circle of radius at least 1 and settled them to an
    /// absolute tolerance. So a polynomial with every root near <c>10^-12</c> did not settle, and
    /// one with every root near <c>10^-30</c> settled on NaN, its steps having divided by numbers
    /// below the decimal range. The iteration now runs on the roots scaled to about unit size.
    /// Found by solving generated equations with <c>x</c> written as <c>10^12 x</c>; no issue
    /// was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class RootsOfAnySizeAreFoundNumericallyTest
    {
        /// <summary>The coefficients of an octic whose eight roots are near 1, highest power first.</summary>
        private static readonly (int Numerator, int Denominator)[] Octic =
        {
            (1, 1), (-7, 4), (-137, 216), (869, 432), (-12377, 23328),
            (-2557, 15552), (-6815, 93312), (961, 23328), (961, 93312),
        };

        /// <summary>The coefficients of <c>x^5 - 3x + 1</c>, highest power first.</summary>
        private static readonly (int Numerator, int Denominator)[] Quintic =
            { (1, 1), (0, 1), (0, 1), (0, 1), (-3, 1), (1, 1) };

        /// <summary>
        /// The polynomial with the coefficients <paramref name="coefficients"/> and its roots
        /// times <c>10^shift</c>, with each coefficient written out as the rational it is.
        /// </summary>
        private static Entity Scaled((int Numerator, int Denominator)[] coefficients, int shift)
        {
            var degree = coefficients.Length - 1;
            var x = MathS.Var("x");
            Entity? polynomial = null;
            for (var i = 0; i <= degree; i++)
            {
                var (numerator, denominator) = coefficients[i];
                if (numerator == 0)
                    continue;
                // The coefficient of x^k is c_k 10^(shift (n - k)).
                var power = shift * i;
                var scale = BigInteger.Pow(10, System.Math.Abs(power));
                var top = power >= 0 ? numerator * scale : numerator;
                var bottom = power >= 0 ? denominator : denominator * scale;
                Entity coefficient = Number.Rational.Create(
                    PeterO.Numbers.EInteger.FromString(top.ToString()),
                    PeterO.Numbers.EInteger.FromString(bottom.ToString()));
                // Written as the polynomial is: x^0 is 1 only where x is not 0, which is a condition
                // on the answer, and the constant term is the coefficient alone.
                var k = degree - i;
                var term = k == 0 ? coefficient : k == 1 ? coefficient * x : coefficient * MathS.Pow(x, k);
                polynomial = polynomial is null ? term : polynomial + term;
            }
            return polynomial!;
        }

        [Theory]
        [InlineData(-12)]
        [InlineData(-30)]
        [InlineData(-45)]
        [InlineData(12)]
        [InlineData(0)]
        public void EveryRootOfTheOcticIsFound(int shift) => EveryRootIsFound(Octic, shift);

        [Theory]
        [InlineData(-30)]
        [InlineData(-45)]
        [InlineData(30)]
        [InlineData(0)]
        public void EveryRootOfTheQuinticIsFound(int shift) => EveryRootIsFound(Quintic, shift);

        private static void EveryRootIsFound((int Numerator, int Denominator)[] coefficients, int shift)
        {
            var answer = Assert.IsType<FiniteSet>(Scaled(coefficients, shift).SolveEquation("x"));
            Assert.Equal(coefficients.Length - 1, answer.Count);
            var unscaled = Scaled(coefficients, 0);
            var back = MathS.Pow(10, -shift);
            foreach (var root in answer)
            {
                // A root of the scaled polynomial, scaled back, is a root of the unscaled one.
                var residual = Assert.IsAssignableFrom<Number.Complex>(unscaled.Substitute("x", root * back).EvalNumerical());
                Assert.True(residual.Abs() < 1e-40, $"{root} leaves {residual}");
            }
            // And they are distinct, each to far more than the digits it is checked to.
            var values = answer.Select(root => (root * back).EvalNumerical()).Cast<Number.Complex>().ToList();
            for (var i = 0; i < values.Count; i++)
                for (var j = i + 1; j < values.Count; j++)
                    Assert.True((values[i] - values[j]).Abs() > 1e-3, $"{values[i]} and {values[j]}");
        }

        /// <summary>
        /// A root far smaller than the others is a root, not a residual of cancellation: the root
        /// near <c>1/(3 10^90)</c> beside four of size <c>1.3e-15</c> was rounded onto 0, which the
        /// polynomial leaves at <c>10^-150</c>, and 0 was answered among the roots.
        /// </summary>
        [Theory]
        [InlineData("x^5 - 3 * 10^(-60) * x + 10^(-150) = 0", 90)]
        [InlineData("x^5 - 3 * x + 10^(-60) = 0", 60)]
        public void ARootFarSmallerThanTheOthersIsNotZero(string equation, int digits)
        {
            var answer = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(5, answer.Count);
            Assert.DoesNotContain(answer, root => root.EvalNumerical() is Number.Complex { IsZero: true });
            var small = MathS.Pow(3 * MathS.Pow(10, digits), -1);
            Assert.Contains(answer, root => ((root - small) / small).EvalNumerical() is Number.Complex relative && relative.Abs() < 1e-20);
        }

        /// <summary>
        /// Solved for the parts of x, a system whose last univariate equation is the octic above with
        /// its roots near <c>10^-12</c>: it kept one root of the two. Near <c>10^-30</c> the decimals
        /// of each root flushed every term but the constant, and the system read as having no
        /// solution answered <c>{ }</c>; it is declined then, and another route answers.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        [InlineData(30)]
        public void BothRootsOfAnEquationInAModulusAreFound(int scale)
        {
            var x = $"(10^{scale} * x)";
            var equation = $"(1 + i) + 2 * {x} - 3 * {x}^2 + {x} * abs(3 - {x}) = 0".ToEntity();
            var answer = Assert.IsType<FiniteSet>(equation.Solve("x"));
            Assert.Equal(2, answer.Count);
            var unscaled = "(1 + i) + 2 * x - 3 * x^2 + x * abs(3 - x)".ToEntity();
            foreach (var root in answer)
            {
                var residual = Assert.IsAssignableFrom<Number.Complex>(unscaled.Substitute("x", root * MathS.Pow(10, scale)).EvalNumerical());
                Assert.True(residual.Abs() < 1e-30, $"{root} leaves {residual}");
            }
        }
    }
}
