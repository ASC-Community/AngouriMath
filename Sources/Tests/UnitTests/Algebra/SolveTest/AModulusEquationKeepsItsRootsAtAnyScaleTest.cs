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
    /// An equation with a modulus in it, with <c>x</c> written as <c>10^-30 x</c>, has the roots of
    /// the equation in <c>x</c> times <c>10^30</c>. The system it is solved by, for the parts of x,
    /// is solved a variable at a time, and a radical root put into the equations left their
    /// constant terms as what the evaluation of terms near <c>1e120</c> rounded to, read as a
    /// rational. A later root then left that rational, decided exactly to be no zero, and was taken
    /// for no root: <c>3x^2 - 2x - 2|2x - 3| = 0</c> lost <c>(sqrt(19) - 1) 10^30 / 3</c>. Found by
    /// solving generated equations as written and scaled; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AModulusEquationKeepsItsRootsAtAnyScaleTest
    {
        [Theory]
        [InlineData("3 * x^2 - 2 * x - 2 * abs(2 * x - 3)", 2)]
        [InlineData("-2 + x - x^2 + 2 * abs(x^2 - 2 * x - 3)", 4)]
        [InlineData("(x^2 - 2) / abs(1 + 3 * x - 2 * x^2) - 3", 2)]
        public void TheRootsAreThoseInXTimesTheScale(string equation, int count)
        {
            var asWritten = Assert.IsType<FiniteSet>($"{equation} = 0".ToEntity().Solve("x"));
            Assert.Equal(count, asWritten.Count);
            var scaled = Assert.IsType<FiniteSet>(
                $"{equation.Replace("x", "(10^(-30) * x)")} = 0".ToEntity().Solve("x"));
            Assert.Equal(count, scaled.Count);
            foreach (var root in scaled)
                Assert.Contains(asWritten, other =>
                    ((Number.Complex)(root / "10^30".ToEntity() - other).EvalNumerical()).Abs() < 1e-20);
            Assert.All(scaled, root => Assert.False(root.Nodes.Any(node => node is Number.Real and not Number.Rational)));
        }
    }
}
