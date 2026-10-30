//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// A negation of something printed with a sign of its own is bracketed, so it reads back as itself: <c>-(-2/3)</c>
    /// printed as <c>--2/3</c>, read back as <c>-(-2) / 3</c>, and <c>-(+oo)</c> as <c>-+oo</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2264">#2264</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class NegationOfASignedTermPrintsTest
    {
        private static Entity Negated(Entity e) => new Mulf(Integer.Create(-1), e);

        public static TheoryData<Entity, string> Negations => new()
        {
            { Negated(Rational.Create(-2, 3)), "-(-2/3)" },
            { Negated(Real.PositiveInfinity), "-(+oo)" },
            { Negated(Negated(MathS.Var("x"))), "-(-x)" },
            { Negated(Integer.Create(-3)), "-(-3)" },
            { Negated(MathS.Var("x")), "-x" },
            { Negated(Rational.Create(2, 3)), "-2/3" },
        };

        [Theory]
        [MemberData(nameof(Negations))]
        public void ItPrintsWithOneSignAndReadsBackToItsValue(Entity negation, string printed)
        {
            Assert.Equal(printed, negation.Stringize());
            // The parser folds a negated number into the number, so -(-2/3) reads back as 2/3: the same value, and for
            // a symbol the same tree.
            var x = "7/10".ToEntity();
            Assert.Equal(negation.Substitute("x", x).Evaled, negation.Stringize().ToEntity().Substitute("x", x).Evaled);
        }

        [Fact]
        public void AHyperbolicSineOfANegativeRationalHasNoDoubleSign()
            => Assert.DoesNotContain("--", "sinh(-5/4)".ToEntity().Stringize());
    }
}
