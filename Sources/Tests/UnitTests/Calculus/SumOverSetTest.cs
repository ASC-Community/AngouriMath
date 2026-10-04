//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// <c>sum(f(x), x in S)</c>: the sum of an expression over the members of a set, each counted
    /// once, as a binder. https://github.com/asc-community/AngouriMath/issues/1285
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class SumOverSetTest
    {
        [Fact]
        public void ItIsWrittenAsItIsRead()
        {
            var sum = "sum(x^2, x in {1, 2, 3})".ToEntity();
            Assert.IsType<SumOverSetf>(sum);
            Assert.Equal("sum(x ^ 2, x in { 1, 2, 3 })", sum.Stringize());
            Assert.Equal(sum, sum.Stringize().ToEntity());
        }

        /// <summary>The range form keeps its meaning, and two arguments that are not a range are the error they were.</summary>
        [Fact]
        public void TheRangeFormIsUnchanged()
        {
            Assert.IsType<Summationf>("sum(k, k, 1, 10)".ToEntity());
            Assert.Equal(Integer.Create(55), "sum(k, k, 1, 10)".ToEntity().Simplify());
            Assert.ThrowsAny<AngouriMath.Core.Exceptions.ParseException>(() => "sum(x, y)".ToEntity());
        }

        [Theory]
        [InlineData("sum(x^2, x in {1, 2, 3})", "14")]
        [InlineData("sum(x, x in {})", "0")]
        [InlineData("sum(7, x in {1, 2, 3})", "21")]
        // An integer range is listed, so the set form agrees with the range form.
        [InlineData("sum(k, k in ZZ intersect [1; 10])", "55")]
        [InlineData("sum(w^2, w in { w : w^3 - 2w + 1 = 0 })", "4")]
        [InlineData("sum(w, w in { w : w^2 + 1 = 0 })", "0")]
        [InlineData("sum(1/w, w in { w : w^2 - 3w + 2 = 0 })", "3/2")]
        // Over the reals only the real roots count: w^4 - 1 has 1 and -1 there, and i and -i besides.
        [InlineData("sum(w^2, w in { w in RR : w^4 - 1 = 0 })", "2")]
        [InlineData("sum(w^2, w in { w : w^4 - 1 = 0 })", "0")]
        public void TheTermsAreAddedUp(string sum, string value)
            => Assert.Equal(value.ToEntity(), sum.ToEntity().Simplify());

        /// <summary>A set counts a member once, so a double root is one term.</summary>
        [Fact]
        public void ARepeatedRootIsOneMember()
            => Assert.Equal(Integer.Create(3), "sum(w, w in { w : (w - 1)^2 (w - 2) = 0 })".ToEntity().InnerSimplified);

        /// <summary>
        /// Left as written: members that may coincide, a set that is not finite, and roots the
        /// solver cannot write, which are not added up symbolically.
        /// </summary>
        [Theory]
        [InlineData("sum(x, x in {a, b})")]
        [InlineData("sum(x, x in [0; 1])")]
        [InlineData("sum(k, k in ZZ)")]
        [InlineData("sum(w, w in { w : w^5 + w + 3 = 0 })")]
        // An irreducible cubic: Cardano would write its roots in complex radicals, which read no
        // more simply than the sum.
        [InlineData("sum(w^2, w in { w : w^3 + w + 1 = 0 })")]
        public void WhatItCannotListIsLeftAsWritten(string sum)
            => Assert.IsType<SumOverSetf>(sum.ToEntity().InnerSimplified);

        /// <summary>
        /// Evaluated to a number, the roots the solver cannot write are found numerically, all of
        /// them, and the sum agrees with the exact value the coefficients give: for
        /// <c>w^5 + w + 3</c> the roots add to <c>0</c>, their reciprocals to <c>-1/3</c>, and
        /// their fifth powers to <c>-15</c>, since each root has <c>w^5 = -w - 3</c>.
        /// </summary>
        [Theory]
        [InlineData("sum(w, w in { w : w^5 + w + 3 = 0 })", "0")]
        [InlineData("sum(1/w, w in { w : w^5 + w + 3 = 0 })", "-1/3")]
        [InlineData("sum(w^5, w in { w : w^5 + w + 3 = 0 })", "-15")]
        // The cubic kept as a sum still has its value: the squares of its roots add to -2.
        [InlineData("sum(w^2, w in { w : w^3 + w + 1 = 0 })", "-2")]
        public void NumericallyOverRootsTheSolverCannotWrite(string sum, string exact)
        {
            AssertClose(exact.ToEntity().Evaled, sum.ToEntity().Evaled);
            // And evaluated with downcasting off, as a numerical check evaluates an answer that was
            // built as usual: parsed afresh, since a cached entity would hand back the value it was
            // evaluated to with downcasting on, and parsed outside the scope, since there a literal
            // 3 is a decimal and nothing is a polynomial with rational coefficients.
            var (fresh, reference) = (MathS.FromString(sum, useCache: false), MathS.FromString(exact, useCache: false));
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            AssertClose(reference.Evaled, fresh.Evaled);
        }

        private static void AssertClose(Entity expected, Entity actual)
        {
            var e = Assert.IsAssignableFrom<Complex>(expected);
            var a = Assert.IsAssignableFrom<Complex>(actual);
            var tolerance = EDecimal.FromString("1E-50");
            Assert.True(e.RealPart.EDecimal.Subtract(a.RealPart.EDecimal).Abs().CompareTo(tolerance) < 0, $"{a} is not {e}");
            Assert.True(e.ImaginaryPart.EDecimal.Subtract(a.ImaginaryPart.EDecimal).Abs().CompareTo(tolerance) < 0, $"{a} is not {e}");
        }

        /// <summary>The name ranges over the set, so the sum is not a function of it.</summary>
        [Fact]
        public void TheNameIsBound()
        {
            var sum = "sum(w * a, w in { w : w^5 + w + 3 = 0 })".ToEntity();
            Assert.Equal(new[] { MathS.Var("a") }, sum.FreeVariables);
            Assert.Equal(sum, sum.Substitute("w", 5));
            Assert.Equal("sum(w * 2, w in { w : w ^ 5 + w + 3 = 0 })".ToEntity(), sum.Substitute("a", 2));
        }

        /// <summary>
        /// Term by term where the set does not depend on the variable, and zero in the bound name:
        /// the derivative of <c>y^2</c> times the fifth powers of the roots, which add to
        /// <c>-15</c>, is a sum over the same set with the value <c>-30 y</c>.
        /// </summary>
        [Fact]
        public void ItIsDifferentiatedTermByTerm()
        {
            var sum = "sum(w^5 * y^2, w in { w : w^5 + w + 3 = 0 })".ToEntity();
            var derivative = sum.Differentiate("y");
            Assert.IsType<SumOverSetf>(derivative);
            AssertClose(Integer.Create(-30), derivative.Substitute("y", 1).Evaled);
            Assert.Equal(Integer.Zero, sum.Differentiate("w"));
        }

        [Fact]
        public void ItPrintsAsASumUnderItsRange()
            => Assert.Equal(@"\sum_{x \in \left\{ 1, 2, 3 \right\}} {x}^{2}", "sum(x^2, x in {1, 2, 3})".ToEntity().Latexize());
    }
}
