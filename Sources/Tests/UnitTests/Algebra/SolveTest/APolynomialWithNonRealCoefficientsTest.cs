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
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A polynomial whose coefficients are not all real, with a repeated root and no closed form,
    /// has each of its distinct roots once. Newton's method crawls to a repeated root, and every
    /// start that crawled there came back as a root of its own: a polynomial of degree 6 was
    /// answered with 17 numbers, 13 of them within 1e-6 of i.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class APolynomialWithNonRealCoefficientsTest
    {
        [Theory]
        // (x - i) (x^5 - i): i twice, and the four other fifth roots of i
        [InlineData("x^6 - i x^5 - i x - 1 = 0", 5)]
        // (x^5 + i x + 1)^2
        [InlineData("1 + 2i x - x^2 + 2 x^5 + 2i x^6 + x^10 = 0", 5)]
        // (x - i)^2 (x^5 + 2x + i)
        [InlineData("-i - 3i x^2 + 2 x^3 - x^5 - 2i x^6 + x^7 = 0", 6)]
        public void EachDistinctRootIsAnsweredOnce(string equation, int distinct)
        {
            var roots = Assert.IsType<FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(distinct, roots.Count);
            var difference = equation.ToEntity() is Equalsf(var left, var right) ? left - right : equation.ToEntity();
            foreach (var root in roots)
            {
                var value = difference.Substitute("x", root).EvalNumerical();
                Assert.True(value.Abs().EDecimal.Abs().CompareTo(PeterO.Numbers.EDecimal.FromString("1e-30")) < 0,
                    $"{root} leaves {value}");
            }
        }

        [Fact]
        public void TheFamilyHasFiveMembersAPeriod()
        {
            // sin(3x) - cos(2x) is a polynomial in e^(ix) of the first case above
            var answer = Assert.IsType<IndexedUnionf>("sin(3x) = cos(2x)".ToEntity().Solve("x"));
            Assert.Equal(5, Assert.IsType<FiniteSet>(answer.Body).Count);
        }
    }
}
