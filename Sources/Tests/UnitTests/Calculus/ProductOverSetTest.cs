//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// <c>product(f(x), x in S)</c>: the product of an expression over the members of a set, each
    /// counted once, as a binder that mirrors the sum over a set.
    /// https://github.com/asc-community/AngouriMath/issues/2449
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ProductOverSetTest
    {
        [Fact]
        public void ItIsWrittenAsItIsRead()
        {
            var product = "product(x^2, x in {1, 2, 3})".ToEntity();
            Assert.IsType<ProductOverSetf>(product);
            Assert.Equal("product(x ^ 2, x in { 1, 2, 3 })", product.Stringize());
            Assert.Equal(product, product.Stringize().ToEntity());
            Assert.Equal(product, MathS.Product("x^2", "x", "{1, 2, 3}"));
        }

        /// <summary>The range form keeps its meaning, and two arguments that are not a range are the error they were.</summary>
        [Fact]
        public void TheRangeFormIsUnchanged()
        {
            Assert.IsType<Productf>("product(k, k, 1, 5)".ToEntity());
            Assert.Equal(Integer.Create(120), "product(k, k, 1, 5)".ToEntity().Simplify());
            Assert.ThrowsAny<ParseException>(() => "product(x, y)".ToEntity());
        }

        /// <summary>
        /// The primes up to 30 multiply to the primorial 29# = 6469693230, and the Euler factors
        /// at 2, 3 and 5 of 1/zeta(2) to (3/4)(8/9)(24/25) = 16/25. The empty product is 1.
        /// </summary>
        [Theory]
        [InlineData("product(p, p in PP /\\ [1; 30])", "6469693230")]
        [InlineData("product(1 - 1/p^2, p in {2, 3, 5})", "16/25")]
        [InlineData("product(x, x in {})", "1")]
        [InlineData("product(7, x in {1, 2, 3})", "343")]
        // An integer range is listed, so the set form agrees with the range form.
        [InlineData("product(k, k in ZZ intersect [1; 6])", "720")]
        // The product of the roots of a monic polynomial is (-1)^n times its constant term.
        [InlineData("product(w, w in { w : w^2 - 3 = 0 })", "-3")]
        [InlineData("product(w - 1, w in { w : w^2 - 3w + 2 = 0 })", "0")]
        [InlineData("product(w^2, w in { w in RR : w^4 - 1 = 0 })", "1")]
        // A set counts a member once, so a double root is one factor.
        [InlineData("product(w, w in { w : (w - 1)^2 (w - 2) = 0 })", "2")]
        public void TheFactorsAreMultipliedOut(string product, string value)
            => Assert.Equal(value.ToEntity(), product.ToEntity().Simplify());

        [Fact]
        public void TheEmptyProductIsOneAsTheEmptySumIsZero()
        {
            Assert.Equal(Integer.One, "product(x, x in {})".ToEntity().Evaled);
            Assert.Equal(Integer.Zero, "sum(x, x in {})".ToEntity().Evaled);
        }

        /// <summary>Left as written wherever the sum over the same set is.</summary>
        [Theory]
        [InlineData("x, x in {a, b}")]
        [InlineData("x, x in [0; 1]")]
        [InlineData("k, k in ZZ")]
        [InlineData("p, p in PP")]
        [InlineData("w, w in { w : w^5 + w + 3 = 0 }")]
        [InlineData("w^2 + 1, w in { w : w^3 + w + 1 = 0 }")]
        public void WhatTheSumLeavesAsWrittenTheProductDoesToo(string arguments)
        {
            Assert.IsType<SumOverSetf>($"sum({arguments})".ToEntity().InnerSimplified);
            Assert.IsType<ProductOverSetf>($"product({arguments})".ToEntity().InnerSimplified);
        }

        /// <summary>
        /// Evaluated to a number over roots the solver cannot write: the roots of
        /// <c>w^5 + w + 3</c> multiply to <c>-3</c>, and over the roots of <c>p = w^3 + w + 1</c>,
        /// <c>w^2 + 1 = (w - i)(w + i)</c> multiplies to <c>p(i) p(-i) = 1</c>.
        /// </summary>
        [Theory]
        [InlineData("product(w, w in { w : w^5 + w + 3 = 0 })", "-3")]
        [InlineData("product(w^2 + 1, w in { w : w^3 + w + 1 = 0 })", "1")]
        public void NumericallyOverRootsTheSolverCannotWrite(string product, string exact)
            => AssertClose(exact.ToEntity().Evaled, product.ToEntity().Evaled);

        /// <summary>The name ranges over the set, so the product is not a function of it.</summary>
        [Fact]
        public void TheNameIsBound()
        {
            var product = "product(w * a, w in { w : w^5 + w + 3 = 0 })".ToEntity();
            Assert.Equal(new[] { MathS.Var("a") }, product.FreeVariables);
            Assert.Equal(product, product.Substitute("w", 5));
            Assert.Equal("product(w * 2, w in { w : w ^ 5 + w + 3 = 0 })".ToEntity(), product.Substitute("a", 2));
        }

        /// <summary>
        /// The product rule, which holds where a factor is zero: over <c>{ 1, 2, 3 }</c> the
        /// derivative of <c>(y + 1)(y + 2)(y + 3)</c>, 26 at 1; over the roots of
        /// <c>w^3 + w + 1</c> the product of <c>w + y</c> is <c>y^3 + y - 1</c>, whose derivative
        /// is 4 at 1; and zero in the bound name.
        /// </summary>
        [Theory]
        [InlineData("product(w + y, w in {1, 2, 3})", "(y + 1)(y + 2)(y + 3)")]
        [InlineData("product(w + y, w in { w : w^2 - 2 = 0 })", "y^2 - 2")]
        [InlineData("product(w + y, w in { w : w^3 + w + 1 = 0 })", "y^3 + y - 1")]
        [InlineData("product(w - y, w in {1, 2})", "(1 - y)(2 - y)")]
        public void ItIsDifferentiatedByTheProductRule(string product, string expanded)
        {
            var derivative = product.ToEntity().Differentiate("y");
            foreach (var at in new[] { "1", "2", "-3/2" })
                AssertClose(expanded.ToEntity().Differentiate("y").Substitute("y", at).Evaled,
                    derivative.Substitute("y", at).Evaled);
            Assert.Equal(Integer.Zero, product.ToEntity().Differentiate("w"));
        }

        [Fact]
        public void ItsConditionIsRequiredAtEveryMember()
        {
            var condition = "product(1/(w - x), w in {1, 2})".ToEntity().DomainCondition;
            Assert.DoesNotContain(condition.Vars, v => v.Name == "w");
            Assert.Equal(MathS.Boolean.False, condition.Substitute("x", 2).Simplify());
            Assert.Equal(MathS.Boolean.True, condition.Substitute("x", 3).Simplify());
        }

        [Fact]
        public void ItPrintsAsAProductUnderItsRange()
        {
            Assert.Equal(@"\prod_{x \in \left\{ 1, 2, 3 \right\}} {x}^{2}", "product(x^2, x in {1, 2, 3})".ToEntity().Latexize());
            Assert.Equal(@"\prod_{x \in S} \left(x+1\right)", "product(x + 1, x in S)".ToEntity().Latexize());
        }

        /// <summary>
        /// A member taken out of a set of roots is found as it was written, and a number that is
        /// none of them takes nothing out: the product rule writes <c>S \ { m }</c>.
        /// </summary>
        [Theory]
        [InlineData("sum(x, x in { w : w^2 - 2 = 0 } \\ { sqrt(2) })", "-sqrt(2)")]
        [InlineData("sum(x, x in { w : w^2 - 3w + 2 = 0 } \\ { 1 })", "2")]
        [InlineData("product(x, x in { w : w^2 - 3w + 2 = 0 } \\ { 5 })", "2")]
        public void AMemberTakenOutOfASetOfRootsIsFound(string expression, string value)
            => Assert.Equal(value.ToEntity(), expression.ToEntity().Simplify());

        [Fact]
        public void ASymbolTakenOutIsUndecided()
            => Assert.IsType<SumOverSetf>("sum(x, x in { w : w^2 - 3w + 2 = 0 } \\ { a })".ToEntity().Simplify());

        private static void AssertClose(Entity expected, Entity actual)
        {
            var e = Assert.IsAssignableFrom<Complex>(expected);
            var a = Assert.IsAssignableFrom<Complex>(actual);
            var tolerance = EDecimal.FromString("1E-50");
            Assert.True(e.RealPart.EDecimal.Subtract(a.RealPart.EDecimal).Abs().CompareTo(tolerance) < 0, $"{a} is not {e}");
            Assert.True(e.ImaginaryPart.EDecimal.Subtract(a.ImaginaryPart.EDecimal).Abs().CompareTo(tolerance) < 0, $"{a} is not {e}");
        }
    }
}
