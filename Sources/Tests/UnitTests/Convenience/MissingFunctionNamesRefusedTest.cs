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
    /// A name the grammar does not know, followed by a bracket, falls through to the implicit
    /// multiplication that lets <c>a(b + c)</c> mean <c>a * (b + c)</c>, and for a
    /// one-argument call that never fails. So every CAS spelling the library does not have
    /// came back as a product of an undeclared variable with its argument, silently:
    /// <c>floor(x)</c> was <c>floor * x</c>, and <c>(floor(x) - 3 = 0).Solve("x")</c> answered
    /// <c>{ 3 / floor }</c>, which is a root of nothing.
    /// Refusing every unknown name is not an option -- it is what makes <c>a(b + c)</c> work --
    /// so these are refused one at a time, as <c>arcsinh</c> already is.
    /// https://github.com/asc-community/AngouriMath/issues/733
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class MissingFunctionNamesRefusedTest
    {
        // floor, ceil, ceiling, round, min, max, gcd and lcm were all on this list until they
        // were implemented: https://github.com/asc-community/AngouriMath/issues/809, and erf
        // until https://github.com/asc-community/AngouriMath/issues/1501.
        // conj and arg since https://github.com/asc-community/AngouriMath/issues/2136: arg(z)
        // was arg * z. det was refused there too, until
        // https://github.com/asc-community/AngouriMath/issues/2147.
        [Theory]
        [InlineData("trunc(x)")]
        [InlineData("conjugate(x)")]
        [InlineData("conj(z)")]
        [InlineData("arg(z)")]
        public void ANameTheLibraryDoesNotHaveIsRefused(string written) =>
            Assert.Throws<UnrecognizedFunctionParseException>(() => written.ToEntity());

        /// <summary>
        /// Only the call is refused. The same name as a variable, or followed by an operator,
        /// is read as it was: a name is refused with its bracket, never on its own.
        /// </summary>
        [Theory]
        [InlineData("re * (x + 1)", "re * (x + 1)")]
        [InlineData("im + arg", "im + arg")]
        [InlineData("rect(x)", "rect * x")]
        public void TheSameNameWithoutTheBracketIsStillAVariable(string written, string expected) =>
            Assert.Equal(expected, written.ToEntity().Stringize());

        /// <summary>
        /// The message has to name the function, since the whole point is that the caller
        /// cannot otherwise tell what happened to their expression.
        /// </summary>
        [Theory]
        [InlineData("trunc(x)", "trunc")]
        [InlineData("conjugate(x)", "conjugate")]
        [InlineData("arg(z)", "arg")]
        public void TheRefusalNamesTheFunction(string written, string name) =>
            Assert.Contains(name,
                Assert.Throws<UnrecognizedFunctionParseException>(() => written.ToEntity()).Message);

        /// <summary>
        /// The argument count must not decide whether a missing name is reported. Before this,
        /// a one-argument call was silent and a two-argument one raised a bare
        /// "no viable alternative at input '*('" from the parser generator -- so the same
        /// absence was invisible or cryptic according to how the caller happened to write it.
        /// </summary>
        [Theory]
        [InlineData("conjugate(x)")]
        [InlineData("conjugate(x, y)")]
        [InlineData("conjugate(x, y, z)")]
        [InlineData("trunc(x)")]
        [InlineData("trunc(x, y)")]
        public void TheArgumentCountDoesNotDecideWhetherItIsReported(string written) =>
            Assert.Throws<UnrecognizedFunctionParseException>(() => written.ToEntity());

        /// <summary>
        /// A name the library <i>does</i> have, called with the wrong number of arguments, is
        /// a different complaint and says so -- the count is the problem, not the name.
        /// </summary>
        [Theory]
        [InlineData("floor(x, y)", "floor")]
        [InlineData("ceil(x, y)", "ceil")]
        [InlineData("erf(x, y)", "erf")]
        public void AnImplementedNameWithTheWrongArgumentCountSaysThat(string written, string name)
            => Assert.Contains(name,
                Assert.Throws<FunctionArgumentCountException>(() => written.ToEntity()).Message);

        /// <summary>
        /// What the defect actually cost, and the reason this is worth a parse error rather
        /// than being left as a feature request: the solver answered confidently and wrongly.
        /// </summary>
        [Fact]
        public void TheEquationThatUsedToBeAnsweredWithNonsenseNowSaysWhyItCannotBe()
        {
            var thrown = Assert.Throws<UnrecognizedFunctionParseException>(
                () => "trunc(x) - 3 = 0".ToEntity().Solve("x"));
            Assert.Contains("trunc", thrown.Message);
        }

        /// <summary>
        /// And the original expression from #733 is now answered rather than refused, since
        /// floor exists. <c>floor(x) = 3</c> holds on the whole of <c>[3, 4)</c>, so the
        /// answer is that interval carried as a parameter and not a single point -- what it
        /// must never be again is <c>{ 3 / floor }</c>.
        /// https://github.com/asc-community/AngouriMath/issues/809
        /// </summary>
        [Fact]
        public void TheEquationFromTheOriginalReportIsAnsweredNow()
        {
            var solutions = "floor(x) - 3 = 0".ToEntity().Solve("x");
            Assert.NotNull(solutions);
            Assert.DoesNotContain(solutions!.Nodes, node => node is Entity.Variable { Name: "floor" });
            Assert.Contains(solutions.Nodes, node => node is Entity.Providedf);
        }

        /// <summary>
        /// Only the exact name followed by a bracket is refused. On its own each is still an
        /// ordinary variable, and a longer name beginning the same way is still the implicit
        /// product that <c>a(b + c)</c> means -- which is the rule the refusals must not break.
        /// </summary>
        [Theory]
        [InlineData("min + 1", "min + 1")]
        [InlineData("max * 2", "max * 2")]
        [InlineData("floor", "floor")]
        [InlineData("floor + floor", "floor + floor")]
        [InlineData("minimum(x)", "minimum * x")]
        [InlineData("rounded(x)", "rounded * x")]
        [InlineData("maxx(y)", "maxx * y")]
        [InlineData("a(b + c)", "a * (b + c)")]
        public void EverythingElseParsesAsItDid(string written, string expected) =>
            Assert.Equal(expected.ToEntity(), written.ToEntity());

        /// <summary>
        /// The names the library does have are unaffected -- including the ones that were
        /// added by the same sweep this came out of.
        /// </summary>
        [Theory]
        [InlineData("sgn(x)")]
        [InlineData("sign(x)")]
        [InlineData("signum(x)")]
        [InlineData("abs(x)")]
        [InlineData("gamma(x)")]
        [InlineData("factorial(x)")]
        [InlineData("exp(x)")]
        [InlineData("log10(x)")]
        [InlineData("log2(x)")]
        [InlineData("sin(x)")]
        [InlineData("arsinh(x)")]
        [InlineData("phi(x)")]
        [InlineData("floor(x)")]
        [InlineData("ceil(x)")]
        [InlineData("ceiling(x)")]
        [InlineData("round(x)")]
        [InlineData("min(x, y)")]
        [InlineData("max(x, y)")]
        [InlineData("gcd(x, y)")]
        public void TheNamesTheLibraryHasStillParse(string written) =>
            Assert.NotNull(written.ToEntity());

        /// <summary>
        /// Deliberately not refused. <c>re</c> and <c>im</c> are the sympy spellings for the
        /// real and imaginary parts and the library has neither, so they misparse the same
        /// way -- but two letters is short enough that a caller may reasonably have a variable
        /// of that name, and refusing would break their expression to fix nobody's. They are
        /// left, and pinned here so the choice is visible rather than an oversight.
        /// </summary>
        [Theory]
        [InlineData("re(x)", "re * x")]
        [InlineData("im(x)", "im * x")]
        public void TheTwoLetterNamesAreLeftAsProducts(string written, string expected) =>
            Assert.Equal(expected.ToEntity(), written.ToEntity());
    }
}
