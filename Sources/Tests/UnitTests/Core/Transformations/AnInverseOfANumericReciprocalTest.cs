//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// <c>arcsin(n / d) -> arccsc(d / n)</c> for a numeric <c>n</c>, and its three siblings, read
    /// <c>n / d</c> as <c>1 / (d / n)</c>, which holds for a nonzero <c>n</c>. At <c>n = 0</c> it
    /// took <c>arcsin(0 / x)</c>, which is <c>0</c> wherever <c>x</c> is not, to
    /// <c>arccsc(x / 0)</c>, which has no value. <c>Simplify</c> folds <c>0 / x</c> before the rule
    /// is asked, so the rule set is applied here as a caller of the public registry applies it.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AnInverseOfANumericReciprocalTest
    {
        private static Entity Rewritten(string input)
            => Transformation.Rewriting(RewriteRules.Trigonometric).ApplyOrKeep(input.ToEntity());

        [Theory]
        [InlineData("arcsin(0 / x)")]
        [InlineData("arccos(0 / x)")]
        public void AZeroNumeratorKeepsItsValue(string input)
            => Assert.Equal(input.ToEntity().Substitute("x", 2).Evaled,
                Rewritten(input).Substitute("x", 2).Evaled);

        [Theory]
        [InlineData("arcsin(2 / x)", "arccsc(x / 2)")]
        [InlineData("arccos(2 / x)", "arcsec(x / 2)")]
        public void ANonzeroNumeratorStillTurnsTheFunction(string input, string expected)
            => Assert.Equal(expected.ToEntity(), Rewritten(input));
    }
}
