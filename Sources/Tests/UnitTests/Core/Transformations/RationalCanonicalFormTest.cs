//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// <see cref="Transformation.RationalCanonicalization"/>: a canonical form for rational
    /// functions over Q, which is the part of the language where one is possible.
    /// https://github.com/asc-community/AngouriMath/issues/934
    /// </summary>
    /// <remarks>
    /// The property being asserted is that equality becomes a structural comparison — two
    /// writings of one rational function reach the identical tree. Everything here compares
    /// entities; comparing printed forms would test the printer.
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class RationalCanonicalFormTest
    {
        private static Entity? Canonical(string expression)
        {
            var result = Transformation.RationalCanonicalization.Apply(expression.ToEntity());
            return result.Succeeded ? result.Output : null;
        }

        private static Entity Required(string expression)
        {
            var canonical = Canonical(expression);
            Assert.True(canonical is not null, $"'{expression}' was refused");
            return canonical!;
        }

        /// <summary>
        /// The point of the whole thing: however it was written, one rational function has one
        /// form. The first pair is the one nothing else in the library could bring together,
        /// since no other route puts a sum of quotients over a common denominator.
        /// </summary>
        [Theory]
        [InlineData("1/x + 1/y", "(x + y) / (x * y)")]
        [InlineData("1/x - 1/y", "(y - x) / (x * y)")]
        [InlineData("1/x + 1/y", "(y + x) / (y * x)")]
        [InlineData("2 * x / (4 * y)", "x / (2 * y)")]
        [InlineData("(x + 1) / (x + 2)", "(2 * x + 2) / (2 * x + 4)")]
        [InlineData("x / y + 1", "(x + y) / y")]
        [InlineData("1 / (1 / x)", "x ^ 2 / x")]
        [InlineData("x ^ 2 / x ^ 2", "x / x")]
        [InlineData("(a / b) / (c / d)", "(a * d ^ 2) / (b * c * d)")]
        [InlineData("x ^ (-2)", "1 / x ^ 2")]
        [InlineData("(a + b) / (a * b)", "1/b + 1/a")]
        public void OneFunctionHasOneForm(string left, string right)
            => Assert.Equal(Required(left), Required(right));

        /// <summary>
        /// Two rational functions that are <i>not</i> equal must not collide, or the form
        /// decides equality wrongly, which is worse than not deciding it.
        /// </summary>
        [Theory]
        [InlineData("1/x + 1/y", "1/x - 1/y")]
        [InlineData("x / y", "y / x")]
        [InlineData("(x + 1) / (x + 2)", "(x + 2) / (x + 1)")]
        [InlineData("x / (2 * y)", "x / (3 * y)")]
        // Undefined at zero and zero there: not one function, however the value agrees elsewhere.
        // https://github.com/asc-community/AngouriMath/issues/1618
        [InlineData("1 / (1 / x)", "x")]
        [InlineData("1 / (1 + 1 / x)", "x / (x + 1)")]
        [InlineData("0 / x", "0")]
        public void DifferentFunctionsDoNotCollide(string left, string right)
            => Assert.NotEqual(Required(left), Required(right));

        /// <summary>
        /// Dividing by a quotient moves its denominator into the numerator, where it no longer
        /// stops the form having a value; the expression has none there, so the form says the
        /// denominator is nonzero. The same for a quotient raised to a negative power, and for a
        /// numerator that vanishes, which is zero only where its denominator does not.
        /// https://github.com/asc-community/AngouriMath/issues/1618
        /// </summary>
        [Theory]
        [InlineData("1 / (1 / x)", "x", "0")]
        [InlineData("1 / (1 + 1 / x)", "x", "0")]
        [InlineData("(a / b) / (c / d)", "d", "0")]
        [InlineData("(1 / x) ^ (-1)", "x", "0")]
        [InlineData("(x / y) ^ (-2)", "y", "0")]
        [InlineData("x / (y / x)", "x", "0")]
        [InlineData("0 / x", "x", "0")]
        public void WhereTheExpressionHasNoValueNeitherHasTheForm(string expression, string variable, string at)
        {
            Entity original = expression;
            var form = Required(expression);
            foreach (var name in original.Vars)
            {
                Entity value = name.Name == variable ? at : "1";
                original = original.Substitute(name, value);
                form = form.Substitute(name, value);
            }
            Assert.Equal(MathS.NaN, original.Evaled);
            Assert.Equal(MathS.NaN, form.Evaled);
        }

        /// <summary>
        /// The condition is part of the form, so it is canonical too, and it says only what the
        /// denominator does not: <c>(x^2 - 1)/(x^2 + 2x + 1) + 1/(x + 1)</c> cancels
        /// <c>(x + 1)^2</c>, and <c>x/(x + 1)</c> is already undefined at <c>-1</c>.
        /// </summary>
        [Fact]
        public void TheDenominatorsExclusionIsNotRepeated()
            => Assert.Equal(Required("x / (x + 1)"), Required("(x ^ 2 - 1) / (x ^ 2 + 2 * x + 1) + 1 / (x + 1)"));

        /// <summary>
        /// The case where "the same function" is a trap. `(x^2 - 1)/(x + 1)` is undefined at
        /// `x = -1` and `x - 1` is not, so they are *not* the same function and the form must
        /// not equate them — even though every textbook writes the cancellation. This is the
        /// assertion that makes "equal trees means equal expressions" true rather than nearly
        /// true, and it is the reason cancelling carries a condition.
        /// </summary>
        [Theory]
        [InlineData("(x ^ 2 - 1) / (x + 1)", "x - 1")]
        [InlineData("x / x", "1")]
        [InlineData("(x * y) / x", "y")]
        public void ARemovableSingularityIsNotRemoved(string quotient, string polynomial)
            => Assert.NotEqual(Required(quotient), Required(polynomial));

        /// <summary>
        /// Cancelling widens the domain, so the cancelled factor's non-vanishing is stated.
        /// `x/x` is the whole reason this cannot be skipped: it is 1 everywhere it is defined
        /// and undefined at zero, and a form that answered a bare `1` would be claiming the
        /// two are the same function.
        /// </summary>
        [Theory]
        [InlineData("x / x")]
        [InlineData("(x ^ 2 - 1) / (x - 1)")]
        [InlineData("(x * y) / (x * z)")]
        public void ACancelledFactorKeepsItsCondition(string expression)
            => Assert.True(Required(expression) is Entity.Providedf,
                $"'{expression}' cancelled a factor and dropped the condition");

        /// <summary>
        /// And where nothing is cancelled, no condition is invented — gathering over a common
        /// denominator does not widen anything, since a sum is defined exactly where its terms
        /// are.
        /// </summary>
        [Theory]
        [InlineData("1/x + 1/y")]
        [InlineData("(x + 1) / (x + 2)")]
        [InlineData("x + 1")]
        [InlineData("x / y")]
        public void NothingCancelledMeansNoCondition(string expression)
            => Assert.False(Required(expression) is Entity.Providedf,
                $"'{expression}' acquired a condition it did not need");

        /// <summary>
        /// The boundary, and it is in the signature rather than in a comment: anything that is
        /// not a rational function over Q gets no answer. A form whose value is that equal
        /// trees mean equal expressions must not hand back a normalisation that resembles one.
        /// </summary>
        [Theory]
        [InlineData("sin(x) / x")]
        [InlineData("x ^ y")]
        [InlineData("sqrt(x)")]
        [InlineData("e ^ x")]
        [InlineData("ln(x) / (x + 1)")]
        [InlineData("x ^ (1/2) / y")]
        public void WhatIsNotARationalFunctionIsRefused(string expression)
            => Assert.Null(Canonical(expression));

        /// <summary>A form is a fixed point of itself.</summary>
        [Theory]
        [InlineData("1/x + 1/y")]
        [InlineData("2 * x / (4 * y)")]
        [InlineData("(x ^ 2 - 1) / (x + 1)")]
        [InlineData("x + 1")]
        public void CanonicalizingTwiceIsCanonicalizingOnce(string expression)
        {
            var once = Required(expression);
            var twice = Transformation.RationalCanonicalization.Apply(once);
            // A condition attached on the first pass is not itself a rational function, so the
            // second pass may decline; what must not happen is a different quotient.
            if (twice.Succeeded)
                Assert.Equal(once, twice.Output);
        }

        /// <summary>
        /// It is a form and not a rewrite of the value: the difference simplifies to zero,
        /// which is how this repository checks an identity.
        /// </summary>
        [Theory]
        [InlineData("1/x + 1/y")]
        [InlineData("x / y + 1")]
        [InlineData("(x + 1) / (x + 2)")]
        [InlineData("2 * x / (4 * y)")]
        public void TheValueIsUnchanged(string expression)
        {
            Entity canonical = Required(expression);
            while (canonical is Entity.Providedf(var inner, _)) canonical = inner;
            var difference = (expression.ToEntity() - canonical).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        /// <summary>Nothing applies it behind the caller's back.</summary>
        [Fact]
        public void NothingRunsItByDefault()
            => Assert.Equal("1/x + 1/y".ToEntity(), "1/x + 1/y".ToEntity().Simplify());
    }
}
