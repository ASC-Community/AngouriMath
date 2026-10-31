//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>round</c>, <c>min</c>, <c>max</c> and <c>gcd</c> —
    /// <a href="https://github.com/asc-community/AngouriMath/issues/809">#809</a>.
    /// </summary>
    /// <remarks>
    /// Expected values are SymPy 1.14's, measured rather than reasoned about.
    /// </remarks>
    [Trait("Area", "Convenience")]
    public sealed class RoundMinMaxGcdTest
    {
        /// <summary>
        /// Half to even — Python, SymPy, Mathematica and IEEE 754 all agree, and it is what
        /// .NET's <c>Math.Round</c> does by default. The ties are the whole point: 1/2 goes
        /// down to 0 and 5/2 goes down to 2, because the even neighbour wins.
        /// </summary>
        [Theory]
        [InlineData("round(1/2)", 0)]
        [InlineData("round(3/2)", 2)]
        [InlineData("round(5/2)", 2)]
        [InlineData("round(7/2)", 4)]
        [InlineData("round(-1/2)", 0)]
        [InlineData("round(-3/2)", -2)]
        [InlineData("round(-5/2)", -2)]
        [InlineData("round(13/10)", 1)]
        [InlineData("round(17/10)", 2)]
        [InlineData("round(2)", 2)]
        [InlineData("round(-2)", -2)]
        public void RoundGoesToTheEvenNeighbourOnATie(string input, int expected)
            => Assert.Equal(Entity.Number.Integer.Create(expected), input.ToEntity().Simplify());

        /// <summary>
        /// The obvious translation of rounding is <c>floor(x + 1/2)</c>, and it is wrong at
        /// every tie. Pinned so that nobody simplifies the node into it.
        /// </summary>
        [Theory]
        [InlineData("1/2")]
        [InlineData("5/2")]
        [InlineData("-3/2")]
        public void RoundIsNotFloorOfTheArgumentPlusAHalf(string at)
        {
            var rounded = $"round({at})".ToEntity().Simplify();
            var floored = $"floor({at} + 1/2)".ToEntity().Simplify();
            Assert.NotEqual(floored, rounded);
        }

        [Theory]
        [InlineData("min(3, 5)", 3)]
        [InlineData("max(3, 5)", 5)]
        [InlineData("min(-2, -7)", -7)]
        [InlineData("max(-2, -7)", -2)]
        [InlineData("min(3, 5, 1)", 1)]
        [InlineData("max(3, 5, 1)", 5)]
        [InlineData("min(4)", 4)]
        public void MinAndMaxCompareWhereTheArgumentsAreOrdered(string input, int expected)
            => Assert.Equal(Entity.Number.Integer.Create(expected), input.ToEntity().Simplify());

        // That thing where it is real: the complex numbers are not ordered, and min(i, i) has no value.
        // https://github.com/asc-community/AngouriMath/issues/2256
        [Theory]
        [InlineData("min(x, x)", "x provided x in RR")]
        [InlineData("max(x, x)", "x provided x in RR")]
        [InlineData("min(3, 3)", "3")]
        public void MinAndMaxOfOneThingWithItselfIsThatThing(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        // Only ordered arguments compare, so an unordered pair is left alone rather than
        // guessed at -- which is what SymPy's Min does too.
        [Theory]
        [InlineData("min(x, y)")]
        [InlineData("max(x, y)")]
        public void AnUncomparablePairIsLeftAlone(string input)
            => Assert.Equal(input.ToEntity(), input.ToEntity().Simplify());

        [Theory]
        [InlineData("gcd(12, 18)", "6")]
        [InlineData("gcd(-12, 18)", "6")]
        [InlineData("gcd(0, 5)", "5")]
        [InlineData("gcd(12, 18, 8)", "2")]
        // Rationals, not integers only: gcd(a/b, c/d) is gcd(a, c) / lcm(b, d).
        [InlineData("gcd(1/2, 1/3)", "1/6")]
        public void GcdIsWhatSymPyGives(string input, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), input.ToEntity().Simplify());

        [Fact]
        public void AGcdItCannotSettleIsLeftAsANode()
            => Assert.IsType<Entity.Gcdf>("gcd(x, y)".ToEntity().Simplify());

        // The least common multiple, by the gcd's conventions: non-negative, 0 where either
        // argument is 0, and lcm(a/b, c/d) = lcm(a, c) / gcd(b, d).
        // https://github.com/asc-community/AngouriMath/issues/1409
        [Theory]
        [InlineData("lcm(4, 6)", "12")]
        [InlineData("lcm(-4, 6)", "12")]
        [InlineData("lcm(0, 5)", "0")]
        [InlineData("lcm(2, 3, 4)", "12")]
        [InlineData("lcm(1/2, 1/3)", "1")]
        [InlineData("lcm(2/3, 4/9)", "4/3")]
        [InlineData("lcm(7, 7)", "7")]
        public void LcmIsWhatSymPyGives(string input, string expected)
            => Assert.Equal(expected.ToEntity().Simplify(), input.ToEntity().Simplify());

        [Fact]
        public void AnLcmItCannotSettleIsLeftAsANode()
        {
            Assert.IsType<Entity.Lcmf>("lcm(x, y)".ToEntity().Simplify());
            Assert.Equal("lcm(x, y)", "lcm(x, y)".ToEntity().ToString());
            Assert.Equal(@"\operatorname{lcm}\left(x, y\right)", "lcm(x, y)".ToEntity().Latexize());
        }

        [Theory]
        [InlineData("round(x)", "round(x)")]
        [InlineData("min(x, y)", "min(x, y)")]
        [InlineData("max(x, y)", "max(x, y)")]
        [InlineData("gcd(x, y)", "gcd(x, y)")]
        public void ThePrintedFormIsTheUsualSpelling(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Stringize());

        [Theory]
        [InlineData("round(x)")]
        [InlineData("min(x, y)")]
        [InlineData("max(x, y)")]
        [InlineData("gcd(x, y)")]
        [InlineData("min(x, y) + max(a, b) + gcd(p, q) + round(z)")]
        public void ThePrintedFormParsesBackToTheSameExpression(string input)
            => Assert.Equal(input.ToEntity(), input.ToEntity().Stringize().ToEntity());

        [Theory]
        [InlineData("round(x)", @"\left\lfloor{x}\right\rceil")]
        [InlineData("min(x, y)", @"\min\left(x, y\right)")]
        [InlineData("max(x, y)", @"\max\left(x, y\right)")]
        [InlineData("gcd(x, y)", @"\gcd\left(x, y\right)")]
        public void TheLatexIsTheUsualNotation(string input, string expected)
            => Assert.Equal(expected, input.ToEntity().Latexize());

        /// <summary>
        /// Round is flat between the <i>half</i>-integers and jumps at each of them, which is
        /// a different condition from floor's.
        /// </summary>
        [Fact]
        public void TheDerivativeOfRoundIsZeroAwayFromTheHalfIntegers()
        {
            var provided = Assert.IsType<Entity.Providedf>("round(x)".ToEntity().Differentiate("x"));
            Assert.Equal(Entity.Number.Integer.Create(0), provided.Expression);
        }

        /// <summary>
        /// Min and max have no derivative this library can state without a case split on
        /// which argument is smaller, so they decline rather than guess.
        /// </summary>
        [Theory]
        [InlineData("min(x, y)")]
        [InlineData("max(x, y)")]
        public void MinAndMaxDeclineToDifferentiate(string input)
            => Assert.Contains(input.ToEntity().Differentiate("x").Nodes,
                node => node is Entity.Derivativef);

        [Theory]
        [InlineData("min()")]
        [InlineData("max()")]
        [InlineData("gcd()")]
        public void TheVariadicOnesStillNeedAnArgument(string input)
            => Assert.Throws<FunctionArgumentCountException>(() => input.ToEntity());

        [Theory]
        [InlineData("round(x, y)")]
        public void RoundTakesExactlyOneArgument(string input)
            => Assert.Throws<FunctionArgumentCountException>(() => input.ToEntity());

        [Theory]
        [InlineData("round(x)", "7/2", 4)]
        [InlineData("round(x)", "5/2", 2)]
        [InlineData("min(x, 3)", "1", 1)]
        [InlineData("max(x, 3)", "1", 3)]
        [InlineData("gcd(x, 18)", "12", 6)]
        public void SubstitutingReachesTheValue(string input, string at, int expected)
            => Assert.Equal(Entity.Number.Integer.Create(expected),
                input.ToEntity().Substitute("x", at.ToEntity()).Simplify());
    }
}
