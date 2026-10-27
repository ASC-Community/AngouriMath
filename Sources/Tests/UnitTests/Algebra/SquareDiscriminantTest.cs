//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A quadratic whose discriminant is the square of a polynomial was answered with the square
    /// left under the root: <c>a z^2 - (a^2 - bc) z - abc</c> has discriminant
    /// <c>(a^2 + bc)^2</c> and roots <c>a</c> and <c>-bc/a</c>, and came back as
    /// <c>(a^2 - bc -/+ sqrt((a^2 - bc)^2 + 4a^2 bc))/(2a)</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2140">#2140</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class SquareDiscriminantTest
    {
        private static Entity[] Roots(string equation, string variable)
            => Assert.IsType<FiniteSet>(equation.Solve(variable)).ToArray();

        // -bc/a less itself is 0 provided a is not 0, and the condition is not what is compared.
        private static bool Equal(Entity left, Entity right)
        {
            var difference = (left - right).Simplify();
            while (difference is Providedf(var inner, _))
                difference = inner;
            return difference == Integer.Zero;
        }

        [Theory]
        [InlineData("a*z^2 - (a^2 - b*c)*z - a*b*c = 0", "z", "a", "-b*c/a")]
        [InlineData("x^2 - (p + q)*x + p*q = 0", "x", "p", "q")]
        [InlineData("-x^2 + (p - q)*x + p*q = 0", "x", "p", "-q")]
        [InlineData("x^2 + x*y - 2*y^2 = 0", "x", "y", "-2*y")]
        [InlineData("x^2 - 2*a*x + a^2 - 1 = 0", "x", "a - 1", "a + 1")]
        [InlineData("x^2 - 2*a*x + a^2 - b^2/9 = 0", "x", "a - b/3", "a + b/3")]
        public void TheRootsAreThePolynomialsTheyAre(string equation, string variable, string first, string second)
        {
            var roots = Roots(equation, variable);
            Assert.Equal(2, roots.Length);
            Assert.Contains(roots, root => Equal(root, first.ToEntity()));
            Assert.Contains(roots, root => Equal(root, second.ToEntity()));
            Assert.DoesNotContain(roots, root => root.Nodes.Any(node => node is Powf(_, Rational { Denominator: var d }) && d == 2));
        }

        /// <summary>
        /// Where the discriminant is a rational number times a square that is not a rational
        /// square, that number's root is the only radical left, and it is over a number.
        /// </summary>
        [Theory]
        [InlineData("x^2 - 2*a*x + a^2 - 2 = 0", "a - sqrt(2)", "a + sqrt(2)")]
        [InlineData("x^2 - 2*a*x + a^2 - 2*b^2 = 0", "a - sqrt(2) * b", "a + sqrt(2) * b")]
        [InlineData("x^2 - 2*a*x + a^2 + 4 = 0", "a - 2i", "a + 2i")]
        [InlineData("2*x^2 - 2*(p + q)*x + p^2 + q^2 = 0", "(p + q)/2 - i*(p - q)/2", "(p + q)/2 + i*(p - q)/2")]
        public void OnlyTheRootOfTheNumberIsLeft(string equation, string first, string second)
        {
            var roots = Roots(equation, "x");
            Assert.Equal(2, roots.Length);
            Assert.Contains(roots, root => Equal(root, first.ToEntity()));
            Assert.Contains(roots, root => Equal(root, second.ToEntity()));
            Assert.DoesNotContain(roots, root => root.Nodes.Any(node => node is Powf(var radicand, Rational { Denominator: var d }) && d == 2 && radicand is not Number));
        }
    }
}
