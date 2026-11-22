//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// <c>i x + x^2 - 2 conj(x) = 0</c> with x written as <c>10^-30 x</c> has the four roots of the
    /// equation in x times <c>10^30</c>. Three are Cardano roots of a cubic whose terms are near
    /// <c>1e90</c>, and the interval their residual was enclosed in to decide it is zero was worked
    /// to more digits than the two hundred held of <c>pi</c>, which were read as exact: the true
    /// zero was enclosed as a point beside it, and two roots were dropped. Found by solving
    /// generated equations as written and scaled; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnIntervalHoldsAConstantToItsDigitsTest
    {
        [Fact]
        public void TheRootsAreThoseInXTimesTheScale()
        {
            const string equation = "i * x + x^2 - 2 * conj(x)";
            var asWritten = Assert.IsType<FiniteSet>($"{equation} = 0".ToEntity().Solve("x"));
            Assert.Equal(4, asWritten.Count);
            var scaled = Assert.IsType<FiniteSet>(
                $"{equation.Replace("x", "(10^(-30) * x)")} = 0".ToEntity().Solve("x"));
            Assert.Equal(4, scaled.Count);
            foreach (var root in scaled)
                Assert.Contains(asWritten, other =>
                    ((Number.Complex)(root / "10^30".ToEntity() - other).EvalNumerical()).Abs() < 1e-20);
        }
    }
}
