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

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A factor that every term of a quotient's numerator holds, as it is or to a whole power, is taken out of the
    /// numerator and paired off against the denominator. A term holding it only to a power was not read as holding it,
    /// so <c>(x (y + 1) + (y + 1)^2) / (y + 1)^4</c> was left as written while <c>(x (y + 1) + (y + 1)) / (y + 1)^2</c>
    /// cancelled. <a href="https://github.com/asc-community/AngouriMath/issues/2301">#2301</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AFactorHeldToAPowerCancelsTest
    {
        [Theory]
        [InlineData("(x * (y + 1) + (y + 1)^2) / (y + 1)^4", "(x + y + 1) / (y + 1) ^ 3")]
        [InlineData("(x * (x^2 + 1) + (x^2 + 1)^2) / (x^2 + 1)^4", "(x ^ 2 + 1 + x) / (x ^ 2 + 1) ^ 3")]
        [InlineData("(x * (y + 1) + (y + 1)) / (y + 1)^2", "(x + 1) / (y + 1)")]
        public void TheFactorCancels(string quotient, string expected)
        {
            var simplified = quotient.ToEntity().Simplify();
            // As printed: the answer's sums are grouped as Simplify builds them, not as the parser does.
            Assert.Equal(expected, simplified.Stringize());
            foreach (var (x, y) in new[] { (2.0, 0.5), (-1.5, 3.0), (0.25, -0.75) })
            {
                var before = quotient.ToEntity().Substitute("x", x).Substitute("y", y).EvalNumerical();
                var after = simplified.Substitute("x", x).Substitute("y", y).EvalNumerical();
                Assert.True((before - after).Abs() < 1e-9, $"{after} where the quotient is {before} at x = {x}, y = {y}");
            }
        }

        /// <summary>
        /// The quotient rule squares the denominator at each derivative, and what the new numerator shares with it was
        /// not taken back: the seventh derivative of <c>e^x / (1 + x^2)</c>, simplified at each step, had 5,117 nodes,
        /// and has 149 once the factor cancels.
        /// </summary>
        [Fact]
        public void TheDerivativesOfAQuotientStaySmall()
        {
            Entity derivative = "e^x / (1 + x^2)";
            for (var i = 0; i < 7; i++)
                derivative = derivative.Differentiate("x").Simplify();
            Assert.True(derivative.Nodes.Count() < 500, $"{derivative.Nodes.Count()} nodes: {derivative}");
        }
    }
}
