//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using AngouriMath.Functions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The identities of the reference chapter of Modenov's <i>Problems in Geometry</i> that
    /// <c>Simplify</c> did not prove: each needs an angle opened, doubled or halved and the
    /// Pythagorean pair taken, in that order, and the rules take them a step at a time.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2142">#2142</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TrigonometricIdentityTest
    {
        private static Entity Bare(Entity expr)
        {
            while (expr is Providedf(var inner, _))
                expr = inner;
            return expr;
        }

        /// <summary>
        /// The tangent addition formula, the half-angle formulas, the Weierstrass forms and
        /// sum-to-product, each written as a difference.
        /// </summary>
        [Theory]
        [InlineData("tan(s + t) - (tan(s) + tan(t)) / (1 - tan(s) * tan(t))")]
        [InlineData("tan(s - t) - (tan(s) - tan(t)) / (1 + tan(s) * tan(t))")]
        [InlineData("sin(a/2)^2 - (1 - cos(a)) / 2")]
        [InlineData("cos(a/2)^2 - (1 + cos(a)) / 2")]
        [InlineData("tan(a/2) - (1 - cos(a)) / sin(a)")]
        [InlineData("tan(a/2) - sin(a) / (1 + cos(a))")]
        [InlineData("sin(a) - 2 * tan(a/2) / (1 + tan(a/2)^2)")]
        [InlineData("cos(a) - (1 - tan(a/2)^2) / (1 + tan(a/2)^2)")]
        [InlineData("tan(a) - 2 * tan(a/2) / (1 - tan(a/2)^2)")]
        [InlineData("sin(a) + sin(b) - 2 * sin((a + b)/2) * cos((a - b)/2)")]
        [InlineData("sin(a) - sin(b) - 2 * cos((a + b)/2) * sin((a - b)/2)")]
        [InlineData("cos(a) + cos(b) - 2 * cos((a + b)/2) * cos((a - b)/2)")]
        [InlineData("cos(a) - cos(b) + 2 * sin((a + b)/2) * sin((a - b)/2)")]
        [InlineData("cot(a/2) - sin(a) / (1 - cos(a))")]
        [InlineData("sec(x)^2 - 1 / (1 - sin(x)^2)")]
        public void TheIdentityIsZero(string identity)
            => Assert.Equal(Integer.Zero, Bare(identity.Simplify()));

        /// <summary>
        /// The same sum written two ways, one of which the rules finished and the other not:
        /// <c>(cos(2x) - 1)^2 + sin(2x)^2 - 4 sin(x)^2</c> stopped at
        /// <c>2 + (-2) cos(2x) + (-4) sin(x)^2</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2133">#2133</a>
        /// </summary>
        [Fact]
        public void TheValueDoesNotDependOnHowTheSumIsWritten()
            => Assert.Equal(Integer.Zero, "(cos(2*x) - 1)^2 + sin(2*x)^2 - 4*sin(x)^2".Simplify());

        /// <summary>
        /// A difference with no quotient has a value everywhere, so its zero has no condition.
        /// </summary>
        [Theory]
        [InlineData("sin(a/2)^2 - (1 - cos(a)) / 2")]
        [InlineData("sin(a) + sin(b) - 2 * sin((a + b)/2) * cos((a - b)/2)")]
        public void WithoutAQuotientTheZeroHasNoCondition(string identity)
            => Assert.Equal(Integer.Zero, identity.Simplify());

        /// <summary>
        /// A difference with a quotient has none where the quotient has none, and its zero keeps
        /// that: <c>tan(a/2) - sin(a)/(1 + cos(a))</c> is undefined at <c>a = pi</c>.
        /// </summary>
        [Fact]
        public void WithAQuotientTheZeroKeepsWhereItHasNoValue()
        {
            var zero = "tan(a/2) - sin(a) / (1 + cos(a))".Simplify();
            Assert.IsType<Providedf>(zero);
            Assert.Equal(Integer.Zero, Bare(zero));
        }

        /// <summary>
        /// A constant other than zero: <c>tan(a/2)^2 = (1 - cos a)/(1 + cos a)</c> as a quotient.
        /// </summary>
        [Fact]
        public void AQuotientOfTwoSidesIsOne()
            => Assert.Equal(Integer.One, Bare("(1 - cos(a)) / (1 + cos(a)) / tan(a/2)^2".Simplify()));

        /// <summary>
        /// One sign changed in each identity, so none of them is a constant.
        /// </summary>
        [Theory]
        [InlineData("tan(s + t) - (tan(s) + tan(t)) / (1 + tan(s) * tan(t))")]
        [InlineData("sin(a/2)^2 - (1 + cos(a)) / 2")]
        [InlineData("tan(a/2) - (1 + cos(a)) / sin(a)")]
        [InlineData("sin(a) + sin(b) - 2 * sin((a + b)/2) * sin((a - b)/2)")]
        public void ANearMissIsNotAConstant(string expression)
        {
            Assert.False(TrigonometricIdentity.TryConstant(expression.ToEntity(), out _));
            Assert.IsNotType<Integer>(Bare(expression.Simplify()));
        }

        /// <summary>
        /// The decision needs the only relation between the sines and cosines of the base angles
        /// to be the Pythagorean one, so an angle with a constant term or a named constant is
        /// declined rather than decided wrongly: <c>sin(pi)</c> is 0, which the relation does not
        /// know. The rules settle these.
        /// </summary>
        [Theory]
        [InlineData("sin(x + 1)^2 + cos(x + 1)^2")]
        [InlineData("sin(pi - x) - sin(x)")]
        [InlineData("sin(2) - 2 * sin(1) * cos(1)")]
        [InlineData("sin(x^2)^2 + cos(x^2)^2")]
        [InlineData("sin(sqrt(2) * x)^2 + cos(sqrt(2) * x)^2")]
        public void AnAngleThatIsNotARationalCombinationOfVariablesIsDeclined(string expression)
            => Assert.False(TrigonometricIdentity.TryConstant(expression.ToEntity(), out _));
    }
}
