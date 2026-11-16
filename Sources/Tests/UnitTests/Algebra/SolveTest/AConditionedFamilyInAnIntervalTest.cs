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
    /// A family of roots whose members carry a condition, as an equation with a denominator has,
    /// listed inside a bounded interval: the index is put into the whole member, condition
    /// included, and a member whose condition fails is left out.
    /// https://github.com/asc-community/AngouriMath/issues/2502
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AConditionedFamilyInAnIntervalTest
    {
        [Theory]
        [InlineData("sin(x)/x = 0 and x in [-10; 10]", "-3pi", "-2pi", "-pi", "pi", "2pi", "3pi")]
        [InlineData("sin(x)^2/x = 0 and x in [-5; 5]", "-pi", "pi")]
        [InlineData("cos(x)/(x - pi/2) = 0 and x in [-5; 5]", "-3pi/2", "-pi/2", "3pi/2")]
        [InlineData("sin(x) = 0 and x in [-5; 5]", "-pi", "0", "pi")]
        public void TheRootsAreListed(string equation, params string[] roots)
        {
            var answer = equation.ToEntity().Solve("x");
            var listed = Assert.IsType<FiniteSet>(answer);
            var values = listed.Select(root => (double)root.EvalNumerical().RealPart).OrderBy(v => v).ToArray();
            var expected = roots.Select(root => (double)root.ToEntity().EvalNumerical().RealPart).OrderBy(v => v).ToArray();
            Assert.Equal(expected.Length, values.Length);
            for (var i = 0; i < expected.Length; i++)
                Assert.Equal(expected[i], values[i], 12);
        }

        [Fact]
        public void TheFamilyMeetsAnIntervalWithoutTheEquation()
            => Assert.Equal(
                MathS.Sets.Finite("-2pi", "2pi").Simplify(),
                "union({ 2 pi n provided not n = 0 }, n in ZZ) /\\ [-10; 10]".ToEntity().Simplify());

        [Theory]
        [InlineData("2pi", true)]
        [InlineData("0", false)]
        [InlineData("1", false)]
        public void AMemberIsDecided(string element, bool expected)
        {
            var family = (Set)"union({ 2 * pi * n provided not 2 * pi * n = 0 }, n in ZZ)".ToEntity();
            Assert.True(family.TryContains(element.ToEntity(), out var contains));
            Assert.Equal(expected, contains);
        }
    }
}
