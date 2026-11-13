//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// <c>sgn(a) * (b * a) / abs(a) -> b</c> holds for a real <c>a</c>. The library's <c>sgn(a)</c>
    /// is <c>a / abs(a)</c>, so off the real line the left side is <c>b * a^2 / abs(a)^2</c>: at
    /// <c>a = i, b = 1</c> it is <c>-1</c>. <c>Simplify</c> sorts the product before
    /// <c>Common</c> is asked, and the rule's nested product does not survive that, so the rule
    /// set is applied here as a caller of the public registry applies it.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class ASignOverAnAbsoluteValueTest
    {
        private static Entity Rewritten(string input)
            => Transformation.Rewriting(RewriteRules.Common).ApplyOrKeep(input.ToEntity());

        [Theory]
        [InlineData("i")]
        [InlineData("1 + i")]
        [InlineData("-2")]
        [InlineData("3")]
        public void TheRewriteKeepsTheValue(string at)
        {
            const string input = "sgn(x) * (y * x) / abs(x)";
            Entity At(Entity e) => e.Substitute("x", at.ToEntity()).Substitute("y", 1).Evaled;
            Assert.Equal(At(input.ToEntity()), At(Rewritten(input)));
        }

        [Fact]
        public void OverTheRealsItCancels()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal("y provided not x = 0".ToEntity(), Rewritten("sgn(x) * (y * x) / abs(x)"));
        }
    }
}
