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
using static AngouriMath.Entity.Boolean;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// <c>a divides b</c>: the statement that <c>b</c> is a whole multiple of <c>a</c>, as a node
    /// of its own. https://github.com/asc-community/AngouriMath/issues/1212
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class DividesTest
    {
        [Theory]
        [InlineData("3 divides 12", true)]
        [InlineData("5 divides 12", false)]
        [InlineData("-3 divides 12", true)]
        [InlineData("3 divides -12", true)]
        [InlineData("12 divides 3", false)]
        [InlineData("1 divides 7", true)]
        [InlineData("7 divides 7", true)]
        [InlineData("7 divides 0", true)]
        [InlineData("0 divides 0", true)]
        [InlineData("0 divides 5", false)]
        [InlineData("2 divides 2 ^ 100", true)]
        [InlineData("2 divides 3 ^ 100", false)]
        public void AnIntegerStatementIsDecided(string statement, bool expected)
            => Assert.Equal(expected ? True : False, statement.ToEntity().Evaled);

        // A statement about integers: over anything else it does not exist, the way an
        // inequality over a non-real number does not.
        [Theory]
        [InlineData("3 divides 5/2")]
        [InlineData("1/2 divides 3")]
        [InlineData("2 divides 2.5")]
        [InlineData("i divides 4")]
        public void OverANonIntegerItIsNaN(string statement)
            => Assert.Equal(MathS.NaN, statement.ToEntity().Evaled);

        [Fact]
        public void ASymbolicStatementIsCarried()
        {
            var statement = "a divides b".ToEntity();
            Assert.IsType<Dividesf>(statement.Simplify());
            Assert.Equal(True, statement.Substitute("a", 3).Substitute("b", 9).Evaled);
            Assert.Equal(False, statement.Substitute("a", 3).Substitute("b", 10).Evaled);
        }

        [Theory]
        [InlineData("2 divides x + 4", "2 divides x + 4")]
        [InlineData("a divides b", "a divides b")]
        [InlineData("(a divides b) and (c divides d)", "a divides b and c divides d")]
        [InlineData("not a divides b", "not a divides b")]
        [InlineData("(a divides b) divides c", "a divides b divides c")]
        [InlineData("2 divides 3 x", "2 divides 3 * x")]
        public void ItParsesAndPrintsAsWritten(string written, string printed)
        {
            var entity = written.ToEntity();
            Assert.Equal(printed, entity.ToString());
            Assert.Equal(entity, printed.ToEntity());
        }

        // Parsed at the level of `in`: below the comparisons and the connectives, above the
        // arithmetic, so the operands are arithmetic and the result is a statement.
        [Fact]
        public void ItBindsLikeMembership()
        {
            Assert.Equal(new Dividesf(2, "x + 4".ToEntity()), "2 divides x + 4".ToEntity());
            Assert.Equal(new Andf(new Dividesf(2, "x".ToEntity()), "x > 0".ToEntity()), "2 divides x and x > 0".ToEntity());
            Assert.Equal(new Notf(new Dividesf("a".ToEntity(), "b".ToEntity())), "not a divides b".ToEntity());
        }

        /// <summary>
        /// <c>|</c> is the same operator as <c>divides</c>, at the same precedence, and it used to
        /// be disjunction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It was the one spelling in the grammar that means something else in mathematics than we
        /// read it as: <c>|</c> is divides, "such that", "given", and the delimiter in <c>|x|</c>,
        /// and none of those is disjunction, which is written <c>∨</c>. Decided on
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1212">#1212</a>.
        /// </para>
        /// <para>
        /// <c>or</c> is unaffected and always was the primary spelling — it is what the library
        /// prints, so a round-tripped expression never held a <c>|</c> to begin with.
        /// </para>
        /// </remarks>
        [Fact]
        public void TheBarIsTheSameOperator()
        {
            Assert.Equal("2 divides 6".ToEntity(), "2 | 6".ToEntity());
            Assert.Equal(new Dividesf(2, "x + 4".ToEntity()), "2 | x + 4".ToEntity());
            Assert.Equal(Boolean.True, "2 | 6".ToEntity().Evaled);
            Assert.Equal(Boolean.False, "4 | 6".ToEntity().Evaled);
            // Same precedence as the word, so the two spellings group identically.
            Assert.Equal("2 divides x and x > 0".ToEntity(), "2 | x and x > 0".ToEntity());
            // And `or` still means what it always did.
            Assert.IsType<Orf>("a or b".ToEntity());
        }

        [Fact]
        public void ItPrintsAsTheBarInLatex()
        {
            Assert.Equal(@"2 \mid x+4", "2 divides x + 4".ToEntity().Latexize());
            Assert.Equal(@"a \mid b", MathS.NumberTheory.Divides("a", "b").Latexize());
        }

        [Fact]
        public void TheConvenienceSpellingsAgreeWithTheParser()
        {
            Entity a = "a", b = "b";
            Assert.Equal("a divides b".ToEntity(), a.Divides(b));
            Assert.Equal("a divides b".ToEntity(), MathS.NumberTheory.Divides(a, b));
        }

        [Fact]
        public void ItGoesToSympyAsAModulusEquation()
            => Assert.Contains("sympy.Eq(sympy.Mod(b, a), 0)", MathS.ToSympyCode("a divides b".ToEntity()));

        [Fact]
        public void ItRoundTripsThroughJson()
        {
            var statement = "a divides b + 1".ToEntity();
            var json = System.Text.Json.JsonSerializer.Serialize(statement);
            Assert.Equal(statement, System.Text.Json.JsonSerializer.Deserialize<Entity>(json));
        }
    }
}
