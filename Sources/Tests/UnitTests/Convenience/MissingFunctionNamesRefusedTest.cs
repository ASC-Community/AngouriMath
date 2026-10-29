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
        // conj, arg and det were refused too, from
        // https://github.com/asc-community/AngouriMath/issues/2136, since arg(z) was arg * z;
        // conjugate, conj and arg until https://github.com/asc-community/AngouriMath/issues/2145,
        // and det until https://github.com/asc-community/AngouriMath/issues/2147.
        // And the long names of the functions to come, reserved until each is implemented, and the
        // names that will not be functions, refused with the spelling to use:
        // https://github.com/asc-community/AngouriMath/issues/2182. zeta(s) was zeta * s. mobius,
        // moebiusmu, divisorsigma, primenu, primeomega and primepi, laplacian, jacobian and hessian,
        // fallingfactorial, factorialpower and risingfactorial, multinomial, catalannumber,
        // harmonicnumber, forwarddifference, and lambertw and productlog, were among them until they
        // were implemented, in the same issue.
        [Theory]
        [InlineData("trunc(x)")]
        [InlineData("zeta(s)")]
        [InlineData("zeta(s, a)")]
        [InlineData("riemannzeta(s)")]
        [InlineData("hurwitzzeta(s, a)")]
        [InlineData("heaviside(x)")]
        [InlineData("unitstep(x)")]
        [InlineData("isprime(n)")]
        [InlineData("pochhammer(x, k)")]
        [InlineData("doublefactorial(n)")]
        [InlineData("factorial2(n)")]
        [InlineData("fractionalpart(x)")]
        [InlineData("quotient(a, b)")]
        public void ANameTheLibraryDoesNotHaveIsRefused(string written) =>
            Assert.Throws<UnrecognizedFunctionParseException>(() => written.ToEntity());

        /// <summary>
        /// Only the call is refused. The same name as a variable, or followed by an operator,
        /// is read as it was: a name is refused with its bracket, never on its own.
        /// </summary>
        [Theory]
        [InlineData("re * (x + 1)", "re * (x + 1)")]
        [InlineData("im + arg", "im + arg")]
        [InlineData("zeta + quotient * 2", "zeta + quotient * 2")]
        [InlineData("rect(x)", "rect * x")]
        public void TheSameNameWithoutTheBracketIsStillAVariable(string written, string expected) =>
            Assert.Equal(expected, written.ToEntity().Stringize());

        /// <summary>
        /// The message has to name the function, since the whole point is that the caller
        /// cannot otherwise tell what happened to their expression.
        /// </summary>
        [Theory]
        [InlineData("trunc(x)", "trunc")]
        [InlineData("zeta(s)", "zeta")]
        [InlineData("isprime(n)", "n in PP")]
        [InlineData("heaviside(x)", "iverson(x >= 0)")]
        [InlineData("fractionalpart(x)", "x mod 1")]
        [InlineData("pochhammer(x, k)", "risingfactorial")]
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
        [InlineData("trunc(x)")]
        [InlineData("trunc(x, y)")]
        [InlineData("trunc(x, y, z)")]
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
        /// <c>re</c> and <c>im</c> were deliberately not refused while the library had neither: two
        /// letters is short enough to be somebody's variable, and refusing them would have broken an
        /// expression to fix nobody's. They are the real and the imaginary part now, and the name
        /// without a bracket is still a variable.
        /// https://github.com/asc-community/AngouriMath/issues/2145
        /// </summary>
        [Theory]
        [InlineData("re(x)", typeof(Entity.Ref))]
        [InlineData("im(x)", typeof(Entity.Imf))]
        [InlineData("re * x", typeof(Entity.Mulf))]
        [InlineData("im + x", typeof(Entity.Sumf))]
        public void TheTwoLetterNamesAreTheRealAndTheImaginaryPart(string written, System.Type node) =>
            Assert.IsType(node, written.ToEntity());
    }
}
