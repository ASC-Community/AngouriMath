//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using System.Linq;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// The principal power <c>z^p</c> has argument <c>p arg(z)</c>, so for <c>|p| &lt; 1</c> its
    /// values lie in a sector, and an equation with its other side outside the sector has no
    /// solution. <c>sqrt(x) = a</c> was answered <c>{ a ^ 2 }</c>, which is wrong at
    /// <c>a = -1</c>: <c>sqrt((-1)^2)</c> is <c>1</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// <para/>
    /// A negative real to a rational power with an odd denominator below ten is the real root to
    /// the numerator, which is what the evaluator gives: <c>(-8)^(1/3)</c> is <c>-2</c>. So with
    /// an odd numerator the negative reals are values too, and <c>x^(1/3) = -2</c> is
    /// <c>x = -8</c>, which the sector alone answered <c>{ }</c>; and with an even one a positive
    /// value has a negative preimage as well, <c>(-8)^(2/3) = 4</c>.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class FractionalPowerRangeTest
    {
        private static Entity OnlyRoot(string equation)
        {
            var roots = Assert.IsType<Set.FiniteSet>(equation.ToEntity().Solve("x"));
            return Assert.Single(roots);
        }

        /// <summary>
        /// The root holds exactly where its condition does, at a value inside the sector, one on
        /// its included edge, and one outside.
        /// </summary>
        [Theory]
        [InlineData("sqrt(x) = a", "2", true)]
        [InlineData("sqrt(x) = a", "0", true)]
        [InlineData("sqrt(x) = a", "i", true)]
        [InlineData("sqrt(x) = a", "-i", false)]
        [InlineData("sqrt(x) = a", "-1", false)]
        [InlineData("x^(1/3) = a", "1 + i", true)]
        [InlineData("x^(1/3) = a", "-1", true)]
        [InlineData("x^(1/3) = a", "-i", false)]
        [InlineData("x^(-1/2) = a", "-i", true)]
        [InlineData("x^(-1/2) = a", "i", false)]
        [InlineData("x^(-1/2) = a", "0", false)]
        public void TheRootHoldsWhereItsConditionDoes(string equation, string value, bool holds)
        {
            var root = Assert.IsType<Providedf>(OnlyRoot(equation));
            var at = value.ToEntity();
            Assert.Equal(Boolean.Create(holds), root.Predicate.Substitute("a", at).EvalBoolean());
            if (!holds)
                return;
            var lhs = equation.Split('=')[0].ToEntity();
            var residual = (lhs.Substitute("x", root.Expression.Substitute("a", at)) - at).EvalNumerical();
            Assert.True(Number.Abs(residual).EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-30")) < 0,
                $"{equation} at a = {value}: the residual is {residual}");
        }

        /// <summary>A number on the other side is decided, as it was.</summary>
        [Theory]
        [InlineData("sqrt(x) = 2", "{ 4 }")]
        [InlineData("sqrt(x) = -1", "{ }")]
        [InlineData("sqrt(x) = i", "{ -1 }")]
        [InlineData("sqrt(x) = -i", "{ }")]
        [InlineData("x^(1/3) = -2", "{ -8 }")]
        [InlineData("x^(1/5) = -2", "{ -32 }")]
        [InlineData("x^(-1/3) = -1/2", "{ -8 }")]
        [InlineData("x^(3/5) = -8", "{ -32 }")]
        [InlineData("x^(2/3) = 4", "{ 8, -8 }")]
        [InlineData("x^(4/3) = 16", "{ 8, -8 }")]
        [InlineData("x^(2/3) = -4", "{ }")]
        [InlineData("x^(1/11) = -1", "{ }")]
        public void ANumberIsDecided(string equation, string expected)
            => Assert.Equal(expected.ToEntity(), equation.ToEntity().Solve("x"));

        /// <summary>
        /// What the equation is provided is known while it is solved. A real value has argument 0
        /// or <c>pi</c>, so the sector is the half-line; a value known to lie on it needs nothing,
        /// and one known to lie off it has no root.
        /// </summary>
        [Theory]
        [InlineData("(sqrt(x) = a) provided a in RR", "{ a ^ 2 provided a in RR and a >= 0 }")]
        [InlineData("(sqrt(x) = a) provided a > 0", "{ a ^ 2 provided a > 0 }")]
        [InlineData("(sqrt(x) = a) provided a >= 1", "{ a ^ 2 provided a >= 1 }")]
        [InlineData("(sqrt(x) = a) provided a > -5", "{ a ^ 2 provided a >= 0 }")]
        [InlineData("(x^(-1/2) = a) provided a in RR", "{ a ^ (-2) provided a in RR and a > 0 }")]
        [InlineData("(sqrt(x) = a) provided a < 0", "{ }")]
        [InlineData("(x^(-1/2) = a) provided a <= 0", "{ }")]
        public void WhatTheEquationIsProvidedIsKnown(string equation, string expected)
            => Assert.Equal(expected.ToEntity(), equation.ToEntity().Solve("x"));

        /// <summary>A symbol declared real is the same case: <c>domain(a, RR)</c>.</summary>
        [Fact]
        public void ASymbolDeclaredRealHasTheHalfLine()
            => Assert.Equal("{ domain(a, RR) ^ 2 provided domain(a, RR) >= 0 }".ToEntity(),
                "sqrt(x) = domain(a, RR)".ToEntity().Solve("x"));

        /// <summary>
        /// For <c>|p| &gt; 1</c> the sector is the whole plane, and <c>v^(1/p)</c> is a preimage
        /// of every <c>v</c>, so there is nothing to state.
        /// </summary>
        [Fact]
        public void APowerAboveOneNeedsNoCondition()
            => Assert.IsNotType<Providedf>(OnlyRoot("x^(3/2) = a"));
    }
}
