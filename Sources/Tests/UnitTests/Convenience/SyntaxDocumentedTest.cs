//
// Copyright (c) 2019-2022 Angouri.
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
    /// Every example printed in <c>Docs/Usage/Syntax.md</c>, run. That page is the only statement
    /// of the language other than the grammar itself, and a rule nothing executes is a rule
    /// nothing keeps true.
    /// </summary>
    /// <remarks>
    /// Only what that page states and nothing else pins. The function synonyms are
    /// <c>SynonymFunctionTest</c>, the refused <c>arc-</c> spellings <c>ArcHyperbolicRefusedTest</c>,
    /// <c>sum</c> and <c>product</c> in general <c>SummationProductTest</c>, and the codomain the
    /// printed form drops <c>EntitySerializationTest</c>.
    /// </remarks>
    [Trait("Area", "Convenience")]
    public sealed class SyntaxDocumentedTest
    {
        // ------------------------------------------------------------ operators, loosest first

        /// <summary>
        /// The precedence table, one row per line: what is written without brackets against the
        /// same thing written with them.
        /// </summary>
        [Theory]
        [InlineData("a provided b provided c", "a provided (b provided c)")]
        [InlineData("a implies b implies c", "(a implies b) implies c")]
        [InlineData("a or b xor c", "a or (b xor c)")]
        [InlineData("a xor b and c", "a xor (b and c)")]
        [InlineData("not a and b", "(not a) and b")]
        [InlineData("a and b implies c", "(a and b) implies c")]
        [InlineData("not x = y", "not (x = y)")]
        [InlineData("x + y in RR", "(x + y) in RR")]
        [InlineData("x in RR and y in ZZ", "(x in RR) and (y in ZZ)")]
        [InlineData("A \\/ B \\ C", "(A \\/ B) \\ C")]
        [InlineData("A /\\ B \\/ C", "(A /\\ B) \\/ C")]
        [InlineData("a - b - c", "(a - b) - c")]
        [InlineData("x * y mod z", "(x * y) mod z")]
        [InlineData("a / b / c", "(a / b) / c")]
        [InlineData("-x ^ 2", "-(x ^ 2)")]
        [InlineData("2 ^ 3 ^ 2", "2 ^ (3 ^ 2)")]
        [InlineData("x! + 1", "(x!) + 1")]
        [InlineData("2 ^ x!", "2 ^ (x!)")]
        public void AnOperatorGroupsWhereTheTableSaysItDoes(string written, string bracketed) =>
            Assert.Equal(bracketed.ToEntity(), written.ToEntity());

        /// <summary><c>a &lt; b &lt; c</c> is a conjunction and not a comparison of a boolean.</summary>
        [Theory]
        [InlineData("2 <= x < 5", "2 <= x and x < 5")]
        [InlineData("a = b = c", "a = b and b = c")]
        public void AComparisonChains(string written, string meaning) =>
            Assert.Equal(meaning.ToEntity(), written.ToEntity());

        /// <summary><c>%</c> is left free to mean percent, so it is not a token at all.</summary>
        [Fact]
        public void PercentIsNotAnOperator() =>
            Assert.Throws<UnhandledParseException>(() => "5 % 2".ToEntity());

        // ------------------------------------------------------------ whitespace and comments

        [Theory]
        [InlineData("x + /* dropped */ y")]
        [InlineData("x + // dropped\n y")]
        [InlineData("x +\n y")]
        public void ACommentOrANewlineIsDropped(string written) =>
            Assert.Equal("x + y".ToEntity(), written.ToEntity());

        /// <summary>
        /// A <c>//</c> comment is ended by a newline <i>or</i> by the end of the input. It used to
        /// require the newline, so a comment on the last line never matched at all and its
        /// <c>/</c> reached the parser as an operator
        /// (<a href="https://github.com/asc-community/AngouriMath/issues/1039">#1039</a>) -- which
        /// is where a comment is most likely to be written, and which the block form never
        /// required.
        /// </summary>
        [Theory]
        [InlineData("x + 1 // done")]
        [InlineData("x + 1 // done\n")]
        [InlineData("x + 1 // done\r\n")]
        [InlineData("x + 1 //")]
        [InlineData("x + 1 // a // b")]
        [InlineData("x + 1 /* done */")]
        public void ACommentMayEndTheInput(string written) =>
            Assert.Equal("x + 1".ToEntity(), written.ToEntity());

        /// <summary>
        /// Skipping the comment leaves the input empty, which is not an expression -- so this is a
        /// parse error for the same reason <c>""</c> is, not because the comment failed to match.
        /// </summary>
        [Fact]
        public void ACommentIsNotAnExpressionOnItsOwn() =>
            Assert.Throws<UnhandledParseException>(() => "// nothing".ToEntity());

        // ------------------------------------------------------------ numbers and names

        [Theory]
        [InlineData(".5", "1/2")]
        [InlineData("1.", "1")]
        [InlineData("1.5e3", "1500")]
        [InlineData("1e-9", "0.000000001")]
        [InlineData("true", "True")]
        [InlineData("false", "False")]
        [InlineData("True", "true")]
        [InlineData("False", "false")]
        public void ANumberOrABooleanIsWrittenTheseWays(string written, string same) =>
            Assert.Equal(same.ToEntity(), written.ToEntity());

        /// <summary>A trailing <c>i</c> makes the literal imaginary, exponent and all.</summary>
        [Theory]
        [InlineData("3i", 3)]
        [InlineData("1.5e3i", 1500)]
        public void ATrailingIMakesTheLiteralImaginary(string written, int magnitude) =>
            Assert.Equal((magnitude * MathS.i).Evaled, written.ToEntity());

        /// <summary>The lexer takes the longest match, so only the exact word is the value.</summary>
        [Theory]
        [InlineData("NaNx")]
        [InlineData("NaN_1")]
        public void ALongerNameContainingNaNIsAVariable(string written) =>
            Assert.IsType<Entity.Variable>(written.ToEntity());

        [Theory]
        [InlineData("x")]
        [InlineData("xy")]
        [InlineData("x_1")]
        [InlineData("x_a")]
        [InlineData("α")]
        [InlineData("ω_1")]
        [InlineData("Θ_1")]
        [InlineData("альфа")]
        [InlineData("x_ω")]
        [InlineData("sinx")]
        // A subscript may have a subscript. https://github.com/asc-community/AngouriMath/issues/524
        [InlineData("x_1_2")]
        [InlineData("X_a_beta")]
        public void AName(string written)
        {
            var variable = Assert.IsType<Entity.Variable>(written.ToEntity());
            Assert.Equal(written, variable.Name);
        }

        /// <summary>
        /// The four shapes the page lists as looking like names and not being them. The first two
        /// are lexer errors; the last two are expressions, which is what makes them worth writing
        /// down — nothing tells the caller they did not get the name they typed.
        /// </summary>
        [Theory]
        [InlineData("_x")]
        [InlineData("x_")]
        [InlineData("x__1")]
        [InlineData("ﬁ")]
        public void WhatIsNotAName(string written) =>
            Assert.Throws<UnhandledParseException>(() => written.ToEntity());

        [Fact]
        public void ADigitEndsAName() => Assert.Equal("x ^ 1".ToEntity(), "x1".ToEntity());

        // ------------------------------------------------------------ juxtaposition

        /// <summary>Second is a name, a function or a bracket: the inserted operator is a product.</summary>
        [Theory]
        [InlineData("2x", "2 * x")]
        [InlineData("x y", "x * y")]
        [InlineData("a(b + c)", "a * (b + c)")]
        [InlineData("x sin(x)", "x * sin(x)")]
        [InlineData("x(2)", "x * 2")]
        [InlineData("(x + 1)(x + 2)", "(x + 1) * (x + 2)")]
        [InlineData("2i x", "2i * x")]
        public void JuxtapositionBeforeANameIsAProduct(string written, string meaning) =>
            Assert.Equal(meaning.ToEntity(), written.ToEntity());

        /// <summary>Second is a number: the inserted operator is a power, which is the surprise.</summary>
        [Theory]
        [InlineData("x2", "x ^ 2")]
        [InlineData("3 2", "3 ^ 2")]
        [InlineData("(x + 1)2", "(x + 1) ^ 2")]
        [InlineData("x i", "x ^ i")]
        public void JuxtapositionBeforeANumberIsAPower(string written, string meaning) =>
            Assert.Equal(meaning.ToEntity(), written.ToEntity());

        /// <summary>
        /// <see cref="MathS.Settings.ExplicitParsingOnly"/> refuses to insert either one, and says
        /// which two tokens it will not join.
        /// </summary>
        [Theory]
        [InlineData("2x")]
        [InlineData("x2")]
        [InlineData("a(b + c)")]
        public void ExplicitParsingOnlyInsertsNothing(string written) =>
            MathS.Settings.ExplicitParsingOnly.As(true, () =>
                Assert.Throws<MissingOperatorParseException>(() => written.ToEntity()));

        [Fact]
        public void ExplicitParsingOnlyStillTakesWhatIsWrittenOut() =>
            MathS.Settings.ExplicitParsingOnly.As(true, () =>
                Assert.Equal(2 * MathS.Var("x"), "2 * x".ToEntity()));

        // ------------------------------------------------------------ sets, vectors, abs

        /// <summary>
        /// A column vector and its transpose, with the shapes the page gives. The empty set is a
        /// value and the empty vector is not
        /// (<a href="https://github.com/asc-community/AngouriMath/issues/1028">#1028</a>);
        /// asserted as "does not answer" rather than by exception type, since that issue is about
        /// which exception it ought to be.
        /// </summary>
        [Fact]
        public void AVectorAndItsTranspose()
        {
            var column = Assert.IsType<Entity.Matrix>("[1, 2, 3]".ToEntity());
            Assert.Equal((3, 1), (column.RowCount, column.ColumnCount));
            var row = Assert.IsType<Entity.Matrix>("[1, 2, 3]T".ToEntity());
            Assert.Equal((1, 3), (row.RowCount, row.ColumnCount));

            Assert.Empty(Assert.IsType<Entity.Set.FiniteSet>("{}".ToEntity()));
            Assert.ThrowsAny<System.Exception>(() => "[]".ToEntity());
        }

        [Fact]
        public void AbsoluteValueBrackets()
        {
            Assert.Equal(MathS.Abs("x - 5".ToEntity()), "(|x - 5|)".ToEntity());
            Assert.Equal("abs(x)", "(|x|)".ToEntity().Stringize());
        }

        /// <summary>
        /// The page says there is no universal set and no literal for one, and that a set
        /// constraining nothing is written <c>{ x : True }</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/996">#996</a>
        /// </summary>
        [Fact]
        public void ThereIsNoUniversalSetLiteral()
        {
            Assert.IsType<Entity.Variable>("AA".ToEntity());
            Assert.IsType<Entity.Variable>("UU".ToEntity());

            var unconstrained = Assert.IsType<Entity.Set.ConditionalSet>("{ x : true }".ToEntity().Simplify());
            Assert.True(unconstrained.TryContains(3, out var number) && number);
            Assert.True(unconstrained.TryContains(Entity.Boolean.True, out var truth) && truth);
        }

        /// <summary>
        /// The page says <c>domain(...)</c> takes the eight special sets or the keyword
        /// <c>Any</c>, that the annotation reads back, and that <c>Any</c> is a keyword in that
        /// one position rather than a set.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1048">#1048</a>,
        /// <a href="https://github.com/asc-community/AngouriMath/issues/996">#996</a>
        /// </summary>
        [Fact]
        public void DomainTakesTheEightSpecialSetsOrTheKeywordAny()
        {
            foreach (var (name, domain) in new[]
                     {
                         ("CC", AngouriMath.Core.Domain.Complex), ("RR", AngouriMath.Core.Domain.Real),
                         ("QQ", AngouriMath.Core.Domain.Rational), ("ZZ", AngouriMath.Core.Domain.Integer),
                         ("ZZ*", AngouriMath.Core.Domain.NonNegativeInteger), ("ZZ+", AngouriMath.Core.Domain.PositiveInteger),
                         ("BB", AngouriMath.Core.Domain.Boolean), ("PP", AngouriMath.Core.Domain.Prime),
                     })
                Assert.Equal(domain, $"domain(x, {name})".ToEntity().Codomain);

            Assert.Equal(AngouriMath.Core.Domain.Any, "domain(abs(x), Any)".ToEntity().Codomain);
            Assert.Equal(AngouriMath.Core.Domain.Real, "abs(x)".ToEntity().Codomain);
            Assert.IsType<Entity.Variable>("Any".ToEntity());
            Assert.Equal("Any + 1".ToEntity(), MathS.Var("Any") + 1);
        }

        // ------------------------------------------------------------ functions

        /// <summary>One argument to <c>log</c> is base 10, and two more names say a base outright.</summary>
        [Theory]
        [InlineData("log(100)", "2")]
        [InlineData("log10(100)", "2")]
        [InlineData("log2(1024)", "10")]
        [InlineData("exp(2)", "e ^ 2")]
        public void ALogarithmOrAnExponential(string written, string expected) =>
            Assert.Equal(expected.ToEntity(), written.ToEntity().Simplify());

        /// <summary>
        /// Each row of the inverse hyperbolic table: every accepted spelling of one function is
        /// the same expression. They are rewritten as they are parsed, so this compares what each
        /// one became and not a node named after it.
        /// </summary>
        [Theory]
        [InlineData("arsinh(x)", "asinh(x)", "arsh(x)")]
        [InlineData("arcosh(x)", "acosh(x)", "arch(x)")]
        [InlineData("artanh(x)", "atanh(x)", "arth(x)")]
        [InlineData("arcotanh(x)", "acotanh(x)", "arcoth(x)", "acoth(x)", "arcth(x)")]
        [InlineData("arsech(x)", "asech(x)", "arsch(x)")]
        [InlineData("arcosech(x)", "acosech(x)", "arcsch(x)", "acsch(x)")]
        public void OneInverseHyperbolicFunctionUnderEverySpelling(params string[] spellings)
        {
            var first = spellings[0].ToEntity();
            foreach (var spelling in spellings)
                Assert.Equal(first, spelling.ToEntity());
        }

        /// <summary>Rewritten on the way in, so none of these is a node of its own.</summary>
        [Theory]
        [InlineData("sinh(x)", "(e ^ x - e ^ (-x)) / 2")]
        [InlineData("arsinh(x)", "ln(x + sqrt(x ^ 2 + 1))")]
        [InlineData("cbrt(x)", "x ^ (1/3)")]
        [InlineData("sqr(x)", "x ^ 2")]
        [InlineData("exp(x)", "e ^ x")]
        public void AFunctionThatIsRewrittenAsItIsParsed(string written, string what) =>
            Assert.Equal(what.ToEntity(), written.ToEntity());

        /// <summary>
        /// Three arguments name neither form of <c>integral</c>, where they are an order for
        /// <c>derivative</c>.
        /// </summary>
        [Fact]
        public void IntegralTakesNoOrder()
        {
            Assert.Throws<FunctionArgumentCountException>(() => "integral(f, x, 2)".ToEntity());
            Assert.IsType<Entity.Derivativef>("derivative(f, x, 2)".ToEntity());
        }

        /// <summary>
        /// Short enough to be somebody's variable, so read as a product and not refused. <c>re</c> and
        /// <c>im</c> were the examples until they became the real and the imaginary part.
        /// </summary>
        [Theory]
        [InlineData("beta(x)")]
        [InlineData("norm(x)")]
        public void ANameTheLibraryDoesNotRefuseIsAProduct(string written) =>
            Assert.IsType<Entity.Mulf>(written.ToEntity());

        // ------------------------------------------------------------ sum and product

        /// <summary>
        /// The second argument is the name and the first is the body, which is the whole
        /// difference between 55 and something else.
        /// </summary>
        [Theory]
        [InlineData("sum(i, i, 1, 10)", "55")]
        [InlineData("sum(k, k, 1, 10)", "55")]
        [InlineData("product(k, k, 1, 5)", "120")]
        public void TheSecondArgumentIsTheDeclaredName(string written, string expected) =>
            Assert.Equal(expected.ToEntity(), written.ToEntity().Simplify());

        /// <summary>Inclusive bounds, step one, and the identity of the operator on an empty range.</summary>
        [Theory]
        [InlineData("sum(k, k, 5, 1)", "0")]
        [InlineData("product(k, k, 5, 1)", "1")]
        [InlineData("sum(k, k, 1, 1)", "1")]
        [InlineData("sum(k, k, -2, 2)", "0")]
        public void AnInclusiveRangeAndItsIdentity(string written, string expected) =>
            Assert.Equal(expected.ToEntity(), written.ToEntity().Simplify());

        /// <summary>
        /// A bound that is a number and not a whole one is left as written. The index runs over
        /// the integers, so <c>sum(k, k, 1, 5/2)</c> is <c>1 + 2</c>; the closed form continued
        /// to <c>5/2</c> would be <c>35/8</c>, which answers a different question.
        /// </summary>
        [Theory]
        // `sum(k, k, 1, +oo)` used to be here and is now `+oo`, the terms not tending to zero.
        [InlineData("sum(k, k, 1, 5/2)")]
        [InlineData("sum(k ^ k, k, 1, n)")]
        public void ARangeThatIsNotAnIntegerOneStaysAsWritten(string written) =>
            Assert.Equal(written.ToEntity(), written.ToEntity().Simplify());

        /// <summary>
        /// A symbolic bound is read as standing for a whole number, which is what the index of a
        /// summation ranges over — so a polynomial summand is answered in closed form rather
        /// than left as written.
        /// </summary>
        [Fact]
        public void ASymbolicBoundOverAPolynomialIsAnswered() =>
            Assert.NotEqual("sum(k, k, 1, n)".ToEntity(), "sum(k, k, 1, n)".ToEntity().Simplify());

        /// <summary>
        /// The declared name means the name inside the operator, and what it usually means
        /// outside it. Both halves matter: the first alone would be a name that leaks.
        /// </summary>
        [Theory]
        [InlineData("sum(2i, i, 1, 3)", "12")]
        [InlineData("product(pi, pi, 1, 4)", "24")]
        [InlineData("sum(e, e, 1, 3)", "6")]
        [InlineData("sum(i, i, 1, 3) + i", "6 + i")]
        [InlineData("sum(sqrt(-1) * i, i, 1, 10)", "55i")]
        public void ADeclaredNameShadowsWhatItWouldOtherwiseMean(string written, string expected) =>
            Assert.Equal(expected.ToEntity().Evaled, written.ToEntity().Simplify().Evaled);

        /// <summary>
        /// A name that outlives the operator that declared it is renamed, since the parser cannot
        /// produce a variable called <c>pi</c>.
        /// </summary>
        [Fact]
        public void ANameThatOutlivesItsOperatorIsRenamed() =>
            Assert.Equal("2 * pi_1".ToEntity(), "derivative(pi ^ 2, pi)".ToEntity().Simplify());

        /// <summary><c>lambda</c> is the one binder whose parameter has to be a name.</summary>
        [Fact]
        public void OnlyALambdaInsistsOnAName()
        {
            Assert.IsType<Entity.Summationf>("sum(k, 2, 1, 3)".ToEntity());
            Assert.Throws<InvalidArgumentParseException>(() => "lambda(2, x)".ToEntity());
        }

        /// <summary>A set builder declares the name before its colon, the same way.</summary>
        [Fact]
        public void ASetBuilderDeclaresItsNameToo()
        {
            var set = Assert.IsType<Entity.Set.ConditionalSet>("{ i : i > 0 }".ToEntity());
            Assert.True(set.Contains(5));
            Assert.False(set.Contains(-5));
        }
    }
}
