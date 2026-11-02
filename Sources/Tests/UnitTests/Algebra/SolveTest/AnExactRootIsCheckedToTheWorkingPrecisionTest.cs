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
    /// A root in exact numbers is put back into an equation in exact numbers to the working
    /// precision, not to a percent of its largest term. <c>(3x - 4) sqrt(x) + 3 + 3x - 4x^2 = 0</c>,
    /// squared, is a quartic whose roots come back in radicals, and one of them, 1.3233, is the
    /// quartic's and not the equation's: it leaves 0.069 against a largest term of 7, under the
    /// percent that a root found numerically is allowed. Found by a check of generated equations in
    /// <c>sqrt(x)</c> beside powers of <c>x</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AnExactRootIsCheckedToTheWorkingPrecisionTest
    {
        [Fact]
        public void ARootOfTheSquaredEquationOnlyIsNotAnswered()
        {
            const string residual = "((-4) + 3x) * sqrt(x) + 3 + 3x - 4x^2";
            var roots = Assert.IsType<Set.FiniteSet>($"{residual} = 0".ToEntity().Solve("x"));
            Assert.Equal(3, roots.Count);
            foreach (var root in roots)
            {
                var left = Complex.Abs(residual.ToEntity().Substitute("x", root).EvalNumerical()).EDecimal;
                Assert.True(left.CompareTo(EDecimal.FromString("1E-30")) < 0, $"{root} leaves {left}");
            }
            Assert.DoesNotContain(roots, root => root.EvalNumerical() is Real real
                && (real.EDecimal - EDecimal.FromString("1.3233")).Abs().CompareTo(EDecimal.FromString("0.001")) < 0);
        }
    }
}
