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
    /// <c>arctan</c> and <c>arccot</c> are undefined at their poles, <c>i</c> and <c>-i</c>, where they evaluate to an
    /// infinity, as <c>ln(0)</c> does and as the tangent is where its cosine is 0. Their condition said <c>True</c>, and
    /// <c>arctan(x) - arctan(x)</c> simplified to <c>0</c>, which has a value at <c>i</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2256">#2256</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ArctangentPolesTest
    {
        [Theory]
        [InlineData("arctan(x)", "i", false)]
        [InlineData("arctan(x)", "-i", false)]
        [InlineData("arctan(x)", "2", true)]
        [InlineData("arctan(x)", "0", true)]
        [InlineData("arctan(x)", "1/2 + 6/5 * i", true)]
        [InlineData("arccot(x)", "i", false)]
        [InlineData("arccot(x)", "-i", false)]
        [InlineData("arccot(x)", "0", true)]
        [InlineData("arccot(x)", "-3", true)]
        public void ItIsDefinedOffItsPoles(string expression, string at, bool defined)
        {
            var condition = expression.ToEntity().DomainCondition.Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined ? Entity.Boolean.True : Entity.Boolean.False, condition);
            var value = expression.ToEntity().Substitute("x", at.ToEntity()).Evaled;
            Assert.Equal(defined, value is Entity.Number number && number.IsFinite);
        }

        [Theory]
        [InlineData("arctan(x) - arctan(x)")]
        [InlineData("arccot(x) - arccot(x)")]
        public void ASimplificationHasNoValueAtAPole(string expression)
        {
            var simplified = expression.ToEntity().Simplify();
            Assert.True(simplified.Substitute("x", "i".ToEntity()).Evaled.IsNaN, $"{simplified} at i");
            Assert.Equal(Entity.Number.Integer.Zero, simplified.Substitute("x", 2).Evaled);
        }
    }
}
