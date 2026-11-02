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
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// Newton's method starts from each bracket where the expression changes sign along the real
    /// line, and started from the bracket's midpoint. <c>(x^2 - 3x - 3)^(1/3) + sqrt(3x - 4) = 3</c>
    /// has its one real root at 3.79617, just past the cube root's zero at 3.79129, where the
    /// slope is unbounded; from the midpoint 3.7 the iteration left the real line, and the root
    /// was not answered, so over the reals the answer was <c>{ }</c>. No issue was filed; found
    /// by a cross-check of random radical equations against mpmath.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ARootPastARadicandsZeroTest
    {
        // mpmath's findroot at 40 digits.
        private static readonly Real Root = Real.Create(PeterO.Numbers.EDecimal.FromString("3.7961670056822382211489100492838228303"));

        private static bool IsTheRoot(Entity candidate)
            => candidate.Evaled is Complex value && ((Complex)(value - Root)).Abs().EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-12")) < 0;

        [Fact]
        public void IsAnswered()
        {
            var roots = Assert.IsType<Entity.Set.FiniteSet>("(x^2 - 3x - 3)^(1/3) + sqrt(3x - 4) = 3".ToEntity().Solve("x"));
            Assert.Contains(roots, IsTheRoot);
        }

        [Fact]
        public void IsTheOneRealRoot()
        {
            var roots = Assert.IsType<Entity.Set.FiniteSet>("(x^2 - 3x - 3)^(1/3) + sqrt(3x - 4) = 3 provided x in RR".ToEntity().Solve("x"));
            Assert.True(IsTheRoot(Assert.Single(roots)));
        }
    }
}
