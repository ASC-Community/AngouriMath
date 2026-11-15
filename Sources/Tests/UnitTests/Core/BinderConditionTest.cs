//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// What a binder's body says about the name it binds stays inside the binder: it is required
    /// at every value the name ranges over, and never left free outside, nor left out.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1632">#1632</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class BinderConditionTest
    {
        private const string LogarithmsOverRoots = "sum(ln(x - w), w in { w : w^3 + w + 1 = 0 })";

        /// <summary>
        /// The product rule leaves <c>0 * sum(...)</c>, which keeps the sum's condition: that was
        /// <c>not x - w = 0</c> with <c>w</c> free, and the derivative could not be evaluated.
        /// </summary>
        [Fact]
        public void ADerivativeKeepsNoBoundName()
        {
            var derivative = ("2 * " + LogarithmsOverRoots).ToEntity().Differentiate("x");
            Assert.DoesNotContain(derivative.FreeVariables, v => v.Name == "w");
            Assert.True(derivative.Substitute("x", 0.3).EvalNumerical() is Number.Complex);
        }

        /// <summary>
        /// Over the roots of <c>p</c>, "<c>x - w != 0</c> at every root" is the resultant
        /// <c>p(x) != 0</c>. With <c>p = w^2 - 1</c> that is false at the roots 1 and -1 and true
        /// elsewhere, and it mentions <c>x</c> alone. At 0 too: the zeroth power in the resultant
        /// was written as a power, which is 1 only where its base is not zero.
        /// </summary>
        [Theory]
        [InlineData("1", false)]
        [InlineData("-1", false)]
        [InlineData("2", true)]
        [InlineData("1/2", true)]
        [InlineData("0", true)]
        public void OverRootsTheConditionIsTheResultant(string at, bool defined)
        {
            var condition = "sum(ln(x - w), w in { w : w^2 - 1 = 0 })".ToEntity().DomainCondition;
            Assert.Equal(new[] { "x" }, condition.Vars.Select(v => v.Name).ToArray());
            Assert.Equal(defined ? MathS.Boolean.True : MathS.Boolean.False, condition.Substitute("x", at.ToEntity()).Simplify());
        }

        /// <summary>
        /// The root polynomial itself as the condition: every term is undefined, so the sum is
        /// defined nowhere, and the resultant of <c>p</c> with itself is zero.
        /// </summary>
        [Fact]
        public void ASumOfTermsUndefinedAtEveryRootIsDefinedNowhere()
            => Assert.Equal(MathS.Boolean.False,
                "sum(1/(w^3 + w + 1), w in { w : w^3 + w + 1 = 0 })".ToEntity().DomainCondition);

        /// <summary>
        /// Not left out: <c>0 * sum(1/(k - x), k, 1, n)</c> is undefined wherever the sum is, and a
        /// plain 0 would give it a value at <c>x = 1</c>.
        /// </summary>
        [Fact]
        public void AZeroTimesASumKeepsTheSumsCondition()
        {
            var product = "0 * sum(1/(k - x), k, 1, n)".ToEntity().InnerSimplified;
            Assert.IsType<Providedf>(product);
            Assert.DoesNotContain(product.FreeVariables, v => v.Name == "k");
            Assert.True(product.Substitute("x", 1).Substitute("n", 3).Simplify().IsNaN);
            Assert.Equal(0, product.Substitute("x", "1/2".ToEntity()).Substitute("n", 3).Simplify());
        }

        /// <summary>
        /// A definite integral's condition holds between its limits in either order: from 1 to 0
        /// ranges over [0; 1] as well, where <c>[1; 0]</c> is empty and would make any condition
        /// over it true.
        /// </summary>
        [Theory]
        [InlineData("integral(1/(t - x), t, 0, 1)", "1/2", false)]
        [InlineData("integral(1/(t - x), t, 0, 1)", "2", true)]
        [InlineData("integral(1/(t - x), t, 1, 0)", "1/2", false)]
        [InlineData("integral(1/(t - x), t, 1, 0)", "2", true)]
        public void AnIntegralsConditionHoldsBetweenItsLimits(string integral, string at, bool defined)
        {
            var condition = integral.ToEntity().DomainCondition;
            Assert.DoesNotContain(condition.FreeVariables, v => v.Name == "t");
            Assert.Equal(defined ? MathS.Boolean.True : MathS.Boolean.False, condition.Substitute("x", at.ToEntity()).Simplify());
        }

        /// <summary>Between symbolic limits, whichever is larger: no name of the integral's is left free.</summary>
        [Fact]
        public void BetweenSymbolicLimitsNoBoundNameIsLeft()
            => Assert.DoesNotContain("integral(1/(t - x), t, a, b)".ToEntity().DomainCondition.FreeVariables, v => v.Name == "t");

        /// <summary>
        /// Sufficient for a value and not necessary: <c>ln(t)</c> over <c>[0; 1]</c> converges, to
        /// -1, and is read as undefined, since the integrand has no value at 0; over <c>[1; 2]</c>
        /// it is defined throughout. That is the direction in which nothing is given a value it does
        /// not have.
        /// </summary>
        [Theory]
        [InlineData("integral(ln(t), t, 0, 1)", false)]
        [InlineData("integral(ln(t), t, 1, 2)", true)]
        public void AnIntegralUndefinedAtAPointOfItsRangeIsReadAsUndefined(string integral, bool defined)
            => Assert.Equal(defined ? MathS.Boolean.True : MathS.Boolean.False, integral.ToEntity().DomainCondition.Simplify());

        /// <summary>A few whole numbers are the conjunction over them, and none is true.</summary>
        [Theory]
        [InlineData("sum(1/(k - x), k, 1, 3)", "2", false)]
        [InlineData("sum(1/(k - x), k, 1, 3)", "5", true)]
        [InlineData("sum(1/(k - x), k, 3, 1)", "2", true)]
        public void AFiniteRangeIsTheConjunctionOverIt(string sum, string at, bool defined)
        {
            var condition = sum.ToEntity().DomainCondition;
            Assert.DoesNotContain(condition.Vars, v => v.Name == "k");
            Assert.Equal(defined ? MathS.Boolean.True : MathS.Boolean.False, condition.Substitute("x", at.ToEntity()).Simplify());
        }

        /// <summary>
        /// A set builder admits the members its predicate is defined at: its condition on its own
        /// name is whom it admits, not where the set is defined.
        /// </summary>
        [Fact]
        public void ASetBuildersOwnConditionIsMembership()
            => Assert.DoesNotContain("{ w : 1/w > a }".ToEntity().DomainCondition.Vars, v => v.Name == "w");

        /// <summary>
        /// The set builder's condition is of the predicate it publishes, in which its name is
        /// renamed, so the test above held on a condition that mentioned <c>%1</c> instead:
        /// <c>1 / %1 in RR and a in RR and not %1 = 0</c>. No name of the builder's, written or
        /// invented, is left.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2458">#2458</a>
        /// </summary>
        [Theory]
        [InlineData("{ w : 1/w > a }")]
        [InlineData("{ w in ZZ : 1/w > a }")]
        [InlineData("x in { w : w > a }")]
        public void Issue2458_ASetBuilderLeavesNoPlaceholderInItsCondition(string exprRaw)
            => Assert.Equal(new[] { "a" }, exprRaw.ToEntity().DomainCondition.Vars.Select(v => v.Name).ToArray());

        /// <summary>
        /// A lambda's condition on its parameter is where the function it denotes is defined,
        /// and outside it that name is another one.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2458">#2458</a>
        /// </summary>
        [Fact]
        public void Issue2458_ALambdaLeavesNoParameterInItsCondition()
            => Assert.DoesNotContain("lambda(k, 1/(k - y))".ToEntity().DomainCondition.Vars, v => v.Name == "k");

        /// <summary>
        /// An indexed union or intersection is defined where every set of the family is, as a
        /// sum is where every term is: over the index set, not at a free name of the same spelling.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2458">#2458</a>
        /// </summary>
        [Theory]
        [InlineData("union({ 1/k }, k in ZZ)", "0", false)]
        [InlineData("union({ 1/k }, k in ZZ+)", "0", true)]
        [InlineData("intersection([-1/k; 1/k], k in ZZ+)", "0", true)]
        [InlineData("union({ 1/(k - x) }, k in { 1, 2 })", "1", false)]
        [InlineData("union({ 1/(k - x) }, k in { 1, 2 })", "3", true)]
        public void Issue2458_AnIndexedFamilysConditionHoldsOverItsIndexSet(string family, string at, bool defined)
        {
            var condition = family.ToEntity().DomainCondition;
            Assert.DoesNotContain(condition.FreeVariables, v => v.Name == "k");
            Assert.Equal(defined ? MathS.Boolean.True : MathS.Boolean.False, condition.Substitute("x", at.ToEntity()).Simplify());
        }
    }
}
