//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// Every standard-integral rule reads a linear rate out of its integrand's argument and
    /// divides by it. An argument that mentions <c>x</c> without depending on it has a rate
    /// of zero, and the division was written into the answer unguarded:
    /// <code>
    ///     e ^ (x + -x)  ->  e ^ (x + -x) / (0 * ln(e))    -- NaN, where it is x
    /// </code>
    /// https://github.com/asc-community/AngouriMath/issues/785
    /// Found while fixing https://github.com/asc-community/AngouriMath/issues/781, whose
    /// gathering of two powers of one base turns <c>e^x * e^(-x)</c> into exactly this
    /// shape -- but the defect predates it and is reachable by writing the exponent out.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ConstantRateIntegrandTest
    {
        /// <summary>
        /// A zero rate means the integrand is a constant, so the antiderivative is that
        /// constant times x. Checked by differentiating back rather than by its printed
        /// form, which still carries the unreduced argument.
        /// </summary>
        [Theory]
        [InlineData("e ^ (x + -x)", "1")]
        [InlineData("sin(x + -x)", "0")]
        [InlineData("cos(x - x)", "1")]
        [InlineData("2 ^ (x - x)", "1")]
        public void AZeroRateIntegrandIsAConstant(string integrand, string value)
        {
            var antiderivative = integrand.Integrate("x");
            Assert.False(antiderivative is Entity.Integralf,
                $"{integrand} was declined: {antiderivative.Stringize()}");
            var difference = (antiderivative.Differentiate("x") - value.ToEntity()).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        /// <summary>
        /// The guard must fire on a zero rate and on nothing else -- a rate of one or of
        /// any other constant is what the rules below it are for, and reading those as
        /// constants would replace every one of these answers with the integrand times x.
        /// </summary>
        [Theory]
        [InlineData("e ^ (2 * x)")]
        [InlineData("sin(3 * x + 1)")]
        [InlineData("cos(x)")]
        [InlineData("ln(2 * x)")]
        public void ANonZeroRateStillIntegratesByItsRule(string integrand)
        {
            var antiderivative = integrand.Integrate("x");
            Assert.False(antiderivative is Entity.Integralf,
                $"{integrand} was declined: {antiderivative.Stringize()}");
            var difference = (antiderivative.Differentiate("x") - integrand.ToEntity()).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        /// <summary>
        /// <c>abs</c> takes the guard too, and is checked on the antiderivative rather than
        /// by differentiating it back: the library differentiates <c>abs(u)</c> as
        /// <c>sgn(u) * u'</c> and answers <c>NaN</c> at <c>u = 0</c>, so the round trip
        /// says nothing about this rule either way.
        /// </summary>
        [Fact]
        public void AZeroRateAbsIntegrandIsZero()
        {
            var antiderivative = "abs(x + -x)".Integrate("x");
            Assert.False(antiderivative is Entity.Integralf);
            Assert.Equal(Entity.Number.Integer.Create(0),
                (antiderivative - "C".ToEntity()).Simplify());
        }

        /// <summary>
        /// A symbolic rate is not decidably zero, so the guard must leave it to the rules --
        /// answering <c>sin(a*x) * x</c> here would be wrong for every non-zero a.
        /// </summary>
        [Fact]
        public void ASymbolicRateIsLeftToTheRules()
        {
            var antiderivative = "sin(a * x)".Integrate("x");
            Assert.False(antiderivative is Entity.Integralf);
            var difference = (antiderivative.Differentiate("x") - "sin(a * x)".ToEntity()).Simplify();
            while (difference is Entity.Providedf(var inner, _)) difference = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }
    }
}
