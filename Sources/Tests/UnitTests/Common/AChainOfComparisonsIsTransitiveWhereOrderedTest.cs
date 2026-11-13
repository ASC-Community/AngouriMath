//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>(a &gt; b and b &gt; c) implies a &gt; c</c> is true where the three operands are
    /// ordered, and need not have a truth value where one is not: at <c>a = i, b = 1, c = 0</c>
    /// it is <c>NaN</c>. It was simplified to a bare <c>True</c> for any operands. It cannot carry
    /// <c>provided a in RR</c> as the rules beside it do
    /// (https://github.com/asc-community/AngouriMath/issues/876), because at
    /// <c>a = i, b = 0, c = 1</c> the conjunction is already false and the implication is
    /// <c>True</c>; so it is decided only where the operands are known to be ordered.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class AChainOfComparisonsIsTransitiveWhereOrderedTest
    {
        [Theory]
        [InlineData("((a > b) and (b > c)) implies (a > c)", "i", "1", "0")]
        [InlineData("((a > b) and (b > c)) implies (a > c)", "i", "0", "1")]
        [InlineData("((a > b) and (b > c)) implies (a > c)", "3", "2", "1")]
        [InlineData("((a < b) and (b < c)) implies (a < c)", "i", "0", "1")]
        [InlineData("((a < b) and (b < c)) implies (a < c)", "i", "1", "0")]
        [InlineData("((a < b) and (b < c)) implies (a < c)", "1", "2", "3")]
        public void SimplifyingKeepsTheTruthValue(string input, string a, string b, string c)
        {
            Entity At(Entity e) => e.Substitute("a", a.ToEntity())
                .Substitute("b", b.ToEntity()).Substitute("c", c.ToEntity()).Evaled;
            var original = input.ToEntity();
            Assert.Equal(At(original), At(original.Simplify()));
        }

        // Where every operand is ordered, transitivity decides it.
        [Theory]
        [InlineData("((a > b) and (b > c)) implies (a > c)")]
        [InlineData("((a < b) and (b < c)) implies (a < c)")]
        public void OverTheRealsItIsTrue(string input)
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            Assert.Equal(Entity.Boolean.True, input.ToEntity().Simplify());
        }
    }
}
