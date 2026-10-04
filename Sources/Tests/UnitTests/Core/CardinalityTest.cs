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

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// <c>card(S)</c>: the number of elements of a set, as a node of its own.
    /// https://github.com/asc-community/AngouriMath/issues/1212
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class CardinalityTest
    {
        [Theory]
        [InlineData("card({ 1, 2, 3 })", "3")]
        [InlineData("card({ })", "0")]
        [InlineData("card({ 1, 1, 2 })", "2")]
        [InlineData("card({ 1/2, 0.5, 2 })", "2")]
        [InlineData("card({ 1, 2 } \\/ { 2, 3 })", "3")]
        [InlineData("card({ 1, 2, 3 } /\\ { 2, 3, 4 })", "2")]
        [InlineData("card({ 1, 2, 3 } \\ { 2 })", "2")]
        [InlineData("card({ pi, e, sqrt(2) })", "3")]
        [InlineData("card([1; 1])", "1")]
        [InlineData("card([2; 1])", "0")]
        [InlineData("card((1; 1))", "0")]
        [InlineData("card([1; 1))", "0")]
        [InlineData("card({ 1 } \\/ { 1 })", "1")]
        public void AFiniteSetOfNumbersIsCounted(string card, string expected)
            => Assert.Equal(expected.ToEntity(), card.ToEntity().Evaled);

        // {x, 1} has two elements unless x is 1: a set with a symbol in it is not counted until
        // its elements are known to be distinct.
        [Fact]
        public void ASetWithASymbolInItIsNotCountedUntilItsElementsAreKnown()
        {
            var card = "card({ x, 1 })".ToEntity();
            Assert.IsType<Cardf>(card.Simplify());
            Assert.Equal("2".ToEntity(), card.Substitute("x", 2).Evaled);
            Assert.Equal("1".ToEntity(), card.Substitute("x", 1).Evaled);
        }

        /// <summary>
        /// An infinite set's size is an aleph, or a power of 2 of one: the reals are
        /// <c>2^aleph(0)</c>, and not <c>aleph(1)</c>, which would be the continuum hypothesis.
        /// </summary>
        [Theory]
        [InlineData("card(ZZ)", "aleph(0)")]
        [InlineData("card(QQ)", "aleph(0)")]
        [InlineData("card(PP)", "aleph(0)")]
        [InlineData("card(ZZ+)", "aleph(0)")]
        [InlineData("card(RR)", "2^aleph(0)")]
        [InlineData("card(CC)", "2^aleph(0)")]
        [InlineData("card([0; 1])", "2^aleph(0)")]
        [InlineData("card(powerset(ZZ+))", "2^aleph(0)")]
        [InlineData("card(powerset(RR))", "2^2^aleph(0)")]
        [InlineData("card(ZZ \\/ [0; 1])", "2^aleph(0)")]
        [InlineData("card(BB)", "2")]
        public void AnInfiniteSetsSizeIsAnAleph(string card, string expected)
            => Assert.Equal(expected.ToEntity(), card.ToEntity().Simplify());

        // A set builder is not enumerated, and a set with symbolic ends or no set at all has no
        // size read: each stays as written rather than being answered with anything.
        [Theory]
        [InlineData("card((a; b))")]
        [InlineData("card({ x : x > 0 })")]
        [InlineData("card(S)")]
        public void WhatIsNotReadIsLeftAsWritten(string card)
            => Assert.IsType<Cardf>(card.ToEntity().Simplify());

        /// <summary>
        /// A sum or a product of sizes, one of them infinite, is the larger: the Hilbert hotel,
        /// Sullivan and Mackey's §7.6.3.
        /// </summary>
        [Theory]
        [InlineData("aleph(0) + 1", "aleph(0)")]
        [InlineData("aleph(0) + aleph(0)", "aleph(0)")]
        [InlineData("3 * aleph(0)", "aleph(0)")]
        [InlineData("aleph(0) * aleph(1)", "aleph(1)")]
        [InlineData("2^aleph(0) + aleph(0)", "2^aleph(0)")]
        [InlineData("0 * aleph(0)", "0")]
        [InlineData("card(ZZ) + 1 = card(ZZ)", "True")]
        public void ASumOrAProductOfSizesIsTheLarger(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Simplify());

        /// <summary>
        /// A difference or a quotient of sizes has no value, and neither has a size with a
        /// negative or a fractional number added or multiplied: taking the even numbers from the
        /// whole numbers leaves <c>aleph(0)</c> of them, and taking all of them leaves none. No
        /// value is <c>NaN</c>, as for <c>log(0)</c>.
        /// </summary>
        [Theory]
        [InlineData("aleph(0) - aleph(0)")]
        [InlineData("aleph(1) - aleph(0)")]
        [InlineData("aleph(0) - 1")]
        [InlineData("aleph(0) / 2")]
        [InlineData("aleph(0) / aleph(0)")]
        [InlineData("aleph(0) + 1/2")]
        [InlineData("-aleph(0)")]
        public void ArithmeticWithoutAValueIsNaN(string input)
        {
            var result = input.ToEntity().Simplify();
            Assert.True(result.IsNaN, $"{input} simplified to {result}");
        }

        [Theory]
        [InlineData("aleph(-1)")]
        [InlineData("aleph(1/2)")]
        public void AnAlephIsIndexedByAWholeNumberFromZero(string input)
            => Assert.True(input.ToEntity().Simplify().IsNaN);

        [Fact]
        public void AnAlephPrintsAsItIsWrittenAndInLatexAsAnAleph()
        {
            Assert.Equal("aleph(0)", "aleph(0)".ToEntity().Stringize());
            Assert.Equal(@"\aleph_{0}", "aleph(0)".ToEntity().Latexize());
            Assert.Equal("aleph(k)".ToEntity(), "aleph(k)".ToEntity().Stringize().ToEntity());
            Assert.Equal("aleph(0)".ToEntity(), MathS.Aleph(0));
        }

        /// <summary>
        /// Sizes that are not numbers compare as sizes, Sullivan and Mackey's §7.6: the whole
        /// numbers, the positive ones and the rationals are one size, the reals and every interval
        /// a larger one, and the power set of <c>ZZ+</c> is the size of the reals. Equality is
        /// never read off the difference of the two expressions, which is not zero.
        /// </summary>
        [Theory]
        [InlineData("card(ZZ) = card(QQ)", "True")]
        [InlineData("card(ZZ+) = card(ZZ)", "True")]
        [InlineData("card(PP) = card(ZZ*)", "True")]
        [InlineData("card(ZZ) < card(RR)", "True")]
        [InlineData("card(RR) > card(QQ)", "True")]
        [InlineData("card(ZZ) <= card(QQ)", "True")]
        [InlineData("card(RR) <= card(ZZ)", "False")]
        [InlineData("card(RR) = card(CC)", "True")]
        [InlineData("card([0; 1]) = card(RR)", "True")]
        [InlineData("card((0; 1)) = card([0; 2])", "True")]
        [InlineData("card(RR) = card(powerset(ZZ+))", "True")]
        [InlineData("card(RR) < card(powerset(RR))", "True")]
        [InlineData("card(RR \\ QQ) = card(RR)", "True")]
        [InlineData("card(ZZ \\/ [0; 1]) = card(RR)", "True")]
        [InlineData("card(ZZ) > 1000", "True")]
        [InlineData("card(ZZ) = 5", "False")]
        [InlineData("card({ 1, 2 }) < card(ZZ)", "True")]
        [InlineData("card({ x, 1 }) < card(ZZ)", "True")]
        // Appendix A.6.3 says every interval [a, b] is uncountable; with a = b it has one member.
        [InlineData("card([1; 1]) < card(RR)", "True")]
        // What ZFC proves of alephs and powers of 2: 2^aleph(0) is past aleph(0), so at least
        // aleph(1); a power of 2 of a larger aleph is no smaller; a tower is past its base.
        [InlineData("aleph(1) < aleph(2)", "True")]
        [InlineData("2^aleph(0) >= aleph(1)", "True")]
        [InlineData("2^aleph(0) > aleph(0)", "True")]
        [InlineData("2^aleph(0) <= 2^aleph(1)", "True")]
        [InlineData("2^2^aleph(0) > 2^aleph(0)", "True")]
        [InlineData("aleph(0) < 2^aleph(0)", "True")]
        public void SizesThatAreNotNumbersCompare(string statement, string expected)
            => Assert.Equal(expected.ToEntity(), statement.ToEntity().Simplify());

        /// <summary>
        /// Cantor's theorem, Thm 7.6.5: a set is smaller than its power set, whatever the set.
        /// </summary>
        [Theory]
        [InlineData("card(S) < card(powerset(S))", "True")]
        [InlineData("card(powerset(S)) <= card(S)", "False")]
        [InlineData("card(S) = card(powerset(S))", "False")]
        public void ASetIsSmallerThanItsPowerSet(string statement, string expected)
            => Assert.Equal(expected.ToEntity(), statement.ToEntity().Simplify());

        /// <summary>
        /// Lemma 7.6.11: a subset is no larger than its superset, which settles one side and
        /// leaves the other open.
        /// </summary>
        [Theory]
        [InlineData("card(A /\\ B) <= card(A)", "True")]
        [InlineData("card(A) <= card(A \\/ B)", "True")]
        [InlineData("card(A /\\ B) > card(A)", "False")]
        public void ASubsetIsNoLargerThanItsSuperset(string statement, string expected)
            => Assert.Equal(expected.ToEntity(), statement.ToEntity().Simplify());

        /// <summary>
        /// Two sizes nothing is known about stay written, and so does a comparison with an
        /// infinity, which is not a size -- the book refuses <c>|S| = oo</c> -- and one ZFC does
        /// not decide.
        /// </summary>
        [Theory]
        [InlineData("card(A) < card(B)")]
        [InlineData("card(A /\\ B) < card(A)")]
        [InlineData("card(ZZ) = +oo")]
        // The continuum hypothesis, and its generalisation one aleph up: ZFC decides neither.
        [InlineData("2^aleph(0) = aleph(1)")]
        [InlineData("2^aleph(1) = aleph(2)")]
        [InlineData("2^aleph(0) < 2^aleph(1)")]
        public void ASizeThatIsNotReadIsLeftAsWritten(string statement)
            => Assert.IsNotType<Boolean>(statement.ToEntity().Simplify());

        // Canonical is the prefix `#`; `card( )` is accepted on input and prints as `#`.
        [Theory]
        [InlineData("card({ 1, 2 })", "#{ 1, 2 }")]
        [InlineData("#{ 1, 2 }", "#{ 1, 2 }")]
        [InlineData("card(S) + 1", "#S + 1")]
        [InlineData("#S + 1", "#S + 1")]
        [InlineData("card(A \\/ B)", "#(A \\/ B)")]
        [InlineData("#(A \\/ B)", "#(A \\/ B)")]
        [InlineData("#S", "#S")]
        public void ItParsesAndPrintsAsWritten(string written, string printed)
        {
            var entity = written.ToEntity();
            Assert.Equal(printed, entity.ToString());
            Assert.Equal(entity, printed.ToEntity());
        }

        // `#` binds as tightly as a function call: `#S + 1` is `(#S) + 1`, and a set expression
        // takes parentheses.
        [Fact]
        public void TheHashBindsLikeAFunctionCall()
        {
            Assert.Equal(new Sumf(new Cardf("S".ToEntity()), 1), "#S + 1".ToEntity());
            Assert.Equal("card(S)".ToEntity(), "#S".ToEntity());
            Assert.Equal("3".ToEntity(), "#{ 1, 2, 3 }".ToEntity().Evaled);
        }

        [Fact]
        public void ItPrintsAsAHashInLatex()
        {
            Assert.Equal(@"\#S", "card(S)".ToEntity().Latexize());
            Assert.Equal(@"\#S", "#S".ToEntity().Latexize());
        }

        [Fact]
        public void TheConvenienceSpellingAgreesWithTheParser()
        {
            Entity s = "S";
            Assert.Equal("card(S)".ToEntity(), MathS.Sets.Card(s));
            Assert.Equal("card({ 1, 2 })".ToEntity(), MathS.Sets.Card(MathS.Sets.Finite(1, 2)));
        }

        [Fact]
        public void ItRoundTripsThroughJson()
        {
            var card = "card({ 1, 2 } \\/ S)".ToEntity();
            var json = System.Text.Json.JsonSerializer.Serialize(card);
            Assert.Equal(card, System.Text.Json.JsonSerializer.Deserialize<Entity>(json));
        }

        // A count is a number, so it can be used as one.
        [Fact]
        public void ACountIsANumber()
            => Assert.Equal("6".ToEntity(), "card({ 1, 2, 3 }) * 2".ToEntity().Evaled);
    }
}
