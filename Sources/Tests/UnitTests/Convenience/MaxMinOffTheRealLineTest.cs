//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>max</c> and <c>min</c> are defined where both arguments are real, as their values are: the complex numbers are not
    /// ordered, and <c>max(i, i)</c> is <c>NaN</c>. Their condition said <c>True</c>, so <c>max(x, x)</c> simplified to
    /// <c>x</c>, which has a value at <c>x = i</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2256">#2256</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class MaxMinOffTheRealLineTest
    {
        [Theory]
        [InlineData("max(x, 2)", "i", false)]
        [InlineData("min(x, 2)", "1/2 + 6/5 * i", false)]
        [InlineData("max(x, 2)", "-3", true)]
        [InlineData("min(x, x)", "pi", true)]
        public void TheConditionIsWhereTheValueIs(string expression, string at, bool defined)
        {
            var condition = expression.ToEntity().DomainCondition.Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False, condition);
            // Where it is not defined, evaluation gives NaN or leaves the node, as max(i, 2) is left: no number.
            var value = expression.ToEntity().Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined, value is Entity.Number && !value.IsNaN);
        }

        [Theory]
        [InlineData("max(i, i)")]
        [InlineData("min(1 + i, 1 + i)")]
        public void OfAComplexNumberWithItselfItHasNoValue(string expression)
        {
            Assert.True(expression.ToEntity().Evaled.IsNaN, $"{expression} evaluates to {expression.ToEntity().Evaled}");
            Assert.True(expression.ToEntity().Simplify().IsNaN, $"{expression} simplifies to {expression.ToEntity().Simplify()}");
        }

        [Theory]
        [InlineData("max(x, x)")]
        [InlineData("min(x, x)")]
        public void ASimplificationHasNoValueWhereTheOriginalHasNone(string expression)
        {
            var simplified = expression.ToEntity().Simplify();
            Assert.True(simplified.Substitute("x", "i".ToEntity()).Evaled.IsNaN, $"{simplified} at i");
            Assert.Equal(Entity.Number.Integer.Create(3), simplified.Substitute("x", 3).Evaled);
        }
    }
}
