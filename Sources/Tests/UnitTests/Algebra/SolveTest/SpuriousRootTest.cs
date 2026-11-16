//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// An equation and the same equation written out as <c>= 0</c> have to be answered
    /// alike, and neither answer may contain something that is not a root. No issue is
    /// filed for this; it was found while checking whether
    /// https://github.com/asc-community/AngouriMath/issues/214 could be closed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class SpuriousRootTest
    {
        /// <summary>
        /// Whether every root returned satisfies the equation it came from. Checked
        /// numerically, because the point is what the answers are worth as numbers and not
        /// what form they are written in.
        /// </summary>
        private static void AssertEveryRootSatisfies(string expression, params string[] variables)
        {
            var expr = expression.ToEntity();
            foreach (var variable in variables)
            {
                // A family is checked at the members either side of the principal one as well.
                // https://github.com/asc-community/AngouriMath/issues/2319
                foreach (var root in expr.SolveEquation(variable).Members(-2, -1, 0, 1, 2))
                {
                    if (root.Vars.Any())
                        continue;
                    var residual = expr.Substitute(variable, root).EvalNumerical();
                    Assert.True(
                        residual.Abs().EDecimal.ToDouble() < 1e-6,
                        $"{root.Stringize()} is not a root of {expression}; it leaves {residual.Stringize()}");
                }
            }
        }

        /// <summary>
        /// The exponential solver substitutes t = e^x and inverts what it finds with
        /// t = v^(1/p). Composing that with x = ln(t) is only the same number while the
        /// imaginary part stays inside the principal branch, and for a negative v it does
        /// not: ln((-3) ^ (1 / ln(2))) is 1.58 - 1.75i, a full turn away from the root
        /// ln(-3) / ln(2) = 1.58 + 4.53i, and satisfies nothing.
        /// </summary>
        [Theory]
        [InlineData("2 ^ x + 2 ^ (2 * x) - 6")]
        [InlineData("2 ^ (x ^ 2 + 1) + 2 ^ (2 * x ^ 2 - 1) - 3")]
        [InlineData("3 ^ x + 3 ^ (2 * x) - 12")]
        [InlineData("e ^ x + e ^ (2 * x) - 2")]
        [InlineData("2 ^ (x + 1) + 2 ^ (2 * x ^ 2 - 1) + 1")]
        public void AnExponentialEquationReturnsOnlyRoots(string expression) =>
            AssertEveryRootSatisfies(expression, "x");

        // The answer must not depend on whether the caller wrote the equation out.
        [Theory]
        [InlineData("2 ^ x + 2 ^ (2 * x) - 6")]
        [InlineData("ln(x) + ln(x + 1)")]
        [InlineData("x ^ 2 - 4")]
        [InlineData("sin(x)")]
        [InlineData("x ^ 3 - 1")]
        public void WritingOutTheEqualsSignChangesNothing(string expression)
        {
            var bare = new Entity.Set.FiniteSet(expression.ToEntity().SolveEquation("x").Members(-1, 0, 1));
            var written = new Entity.Set.FiniteSet($"{expression} = 0".ToEntity().Solve("x").Members(-1, 0, 1));
            // Compared as numbers rather than as trees: the two arrive at the same roots by
            // different routes and write them differently, sqrt(3/4) * (-1) against
            // (-1/2) * sqrt(3) and -1/2 against -0.5, while the claim here is only about
            // which numbers are answered.
            Assert.Equal(AsNumbers(written), AsNumbers(bare));
        }

        private static List<string> AsNumbers(Entity.Set.FiniteSet roots)
        {
            var numbers = roots
                .Where(root => !root.Vars.Any())
                .Select(root => root.EvalNumerical())
                .Select(value => $"{value.RealPart.EDecimal.ToDouble():G8} {value.ImaginaryPart.EDecimal.ToDouble():G8}")
                .ToList();
            numbers.Sort(System.StringComparer.Ordinal);
            return numbers;
        }

        /// <summary>
        /// A root found numerically is only as accurate as the search that found it, so the
        /// terms of the equation cancel at it to within that accuracy rather than exactly.
        /// This quartic's four roots come back as decimals and leave 5.2e-6 against terms of
        /// about 1.2e-2, and judging that residual on its own size answered the whole
        /// equation with the empty set.
        /// </summary>
        [Theory]
        [InlineData("1/210 - (17*x)/210 + (101*x^2)/210 - (247*x^3)/210 + x^4", 4)]
        [InlineData("x^4 - 10*x^3 + 35*x^2 - 50*x + 24", 4)]
        public void RootsThatOnlyCancelToTheAccuracyTheyWereFoundWithAreKept(string expression, int count)
        {
            var bare = Assert.IsType<Entity.Set.FiniteSet>(expression.ToEntity().SolveEquation("x"));
            var written = Assert.IsType<Entity.Set.FiniteSet>($"{expression} = 0".ToEntity().Solve("x"));
            Assert.Equal(count, bare.Count);
            Assert.Equal(count, written.Count);
        }

        // Nothing is dropped from the equations that were already answered correctly, and
        // a root carrying a parameter is kept rather than judged.
        [Theory]
        [InlineData("x ^ 2 - 4", 2)]
        [InlineData("x ^ 2 + 1", 2)]
        [InlineData("x ^ 3 - 1", 3)]
        [InlineData("2 ^ x - 8", 1)]
        [InlineData("ln(x) ^ 2 - 3 * ln(x) + 2", 2)]
        [InlineData("sin(x)", 2)]
        public void TheRootsThatWereRightAreStillThere(string expression, int count)
        {
            // A family counts once, by its principal member.
            Assert.Equal(count, expression.ToEntity().SolveEquation("x").Members(0).Count);
        }
    }
}
