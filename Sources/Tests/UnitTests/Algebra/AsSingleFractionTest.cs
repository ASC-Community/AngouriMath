//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An expression written as a single fraction: one numerator over one denominator, nothing
    /// divided inside either, nothing cancelled or multiplied out.
    /// https://github.com/asc-community/AngouriMath/issues/1239
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AsSingleFractionTest
    {
        /// <summary>The printed form is the point of the operation, so it is what these assert.</summary>
        [Theory]
        [InlineData("a + b/c", "(a * c + b) / c")]
        [InlineData("1 + 2/(1+t^2)", "(t ^ 2 + 3) / (t ^ 2 + 1)")]
        [InlineData("1/(t^2+1) + 1/(t+1)", "(2 + t + t ^ 2) / ((t ^ 2 + 1) * (t + 1))")]
        [InlineData("sin(x) + 1/cos(x)", "(1 + cos(x) * sin(x)) / cos(x)")]
        [InlineData("2/3 + x/2", "(4 + 3 * x) / 6")]
        [InlineData("x^(-2) + 1", "(x ^ 2 + 1) / x ^ 2")]
        public void WrittenAsOneFraction(string written, string fraction)
            => Assert.Equal(fraction, written.AsSingleFraction().Stringize());

        /// <summary>
        /// Over the least common multiple of the denominators as they are written, as it is done
        /// by hand: a factor two terms share is not multiplied in twice. Nothing is factorised to
        /// find it, so <c>x^2 - 1</c> and <c>x + 1</c> share nothing.
        /// </summary>
        [Theory]
        [InlineData("1/x + 1/x^2", "(x + 1) / x ^ 2")]
        [InlineData("x/2 + x/4", "x * 3 / 4")]
        [InlineData("1/6 + 1/4", "5 / 12")]
        [InlineData("1/(x + 1) + 1/(x + 1)^2", "(x + 2) / (x + 1) ^ 2")]
        [InlineData("a/(b*c) + d/(b*e)", "(a * e + c * d) / (b * c * e)")]
        [InlineData("1/(x^2 - 1) + 1/(x + 1)", "(x + x ^ 2) / ((x ^ 2 - 1) * (x + 1))")]
        public void OverTheLeastCommonDenominator(string written, string fraction)
            => Assert.Equal(fraction, written.AsSingleFraction().Stringize());

        /// <summary>
        /// Dividing by a fraction moves its denominator into the numerator, where it no longer
        /// stops the answer having a value, so the answer says it is nonzero -- unless the new
        /// denominator still says so itself.
        /// </summary>
        [Theory]
        [InlineData("1/(1/x)", "x provided not x = 0")]
        [InlineData("(a/b)/(c/d)", "a * d / (b * c) provided not d = 0")]
        [InlineData("(x/y)^(-2)", "y ^ 2 / x ^ 2 provided not y = 0")]
        [InlineData("(1/x)/(1/x)", "x / x")]
        public void TurningAFractionOverSaysItsDenominatorIsNonzero(string written, string fraction)
            => Assert.Equal(fraction, written.AsSingleFraction().Stringize());

        /// <summary>
        /// Undefined where the expression is: at zero for the variable named, and one for every
        /// other, each of these has no value, and neither has its single fraction.
        /// </summary>
        [Theory]
        [InlineData("1/(1/x)", "x")]
        [InlineData("(a/b)/(c/d)", "d")]
        [InlineData("(1/x)^(-1)", "x")]
        [InlineData("(x/y)^(-2)", "y")]
        [InlineData("x/(y/x)", "x")]
        [InlineData("(1/x)/(1/x)", "x")]
        [InlineData("1/x + 1/x^2", "x")]
        public void TheDomainIsKept(string written, string atZero)
        {
            var original = written.ToEntity();
            var fraction = original.AsSingleFraction();
            foreach (var variable in original.Vars)
            {
                Entity value = variable.Name == atZero ? 0 : 1;
                original = original.Substitute(variable, value);
                fraction = fraction.Substitute(variable, value);
            }
            Assert.Equal(MathS.NaN, original.Evaled);
            Assert.Equal(MathS.NaN, fraction.Evaled);
        }

        /// <summary>Nothing cancels, and a function's argument is not gathered.</summary>
        [Theory]
        [InlineData("x/x", "x / x")]
        [InlineData("sin(x/2) + 1/x", "(1 + sin(x / 2) * x) / x")]
        public void NothingIsCancelledAndArgumentsAreLeft(string written, string fraction)
            => Assert.Equal(fraction, written.AsSingleFraction().Stringize());

        /// <summary>With no division in it, or a number, the expression comes back as it was.</summary>
        [Theory]
        [InlineData("x")]
        [InlineData("x^2 + 1")]
        [InlineData("1/2")]
        public void WithNothingToGatherItComesBack(string written)
            => Assert.Equal(written.ToEntity(), written.ToEntity().AsSingleFraction());

        /// <summary>The same value everywhere both are defined.</summary>
        [Theory]
        [InlineData("a + b/c")]
        [InlineData("1/(t^2+1) + 1/(t+1)")]
        [InlineData("sin(x) + 1/cos(x)")]
        [InlineData("2/3 + x/2")]
        [InlineData("(a/b)/(c/d)")]
        [InlineData("x/(y/x)")]
        [InlineData("1/x + 1/x^2")]
        [InlineData("a/(b*c) + d/(b*e)")]
        [InlineData("1/(x + 1) + 1/(x + 1)^2")]
        public void TheValueIsKept(string written)
        {
            var original = written.ToEntity();
            var fraction = original.AsSingleFraction();
            foreach (var (name, value) in new[] { ("a", 2), ("b", -3), ("c", 5), ("d", 11), ("e", 17), ("t", 3), ("x", 7), ("y", 13) })
            {
                original = original.Substitute(name, value);
                fraction = fraction.Substitute(name, value);
            }
            Assert.Equal(original.EvalNumerical().RealPart.EDecimal.RoundToPrecision(PeterO.Numbers.EContext.ForPrecision(30)),
                fraction.EvalNumerical().RealPart.EDecimal.RoundToPrecision(PeterO.Numbers.EContext.ForPrecision(30)));
        }

        [Fact]
        public void TheTransformationIsTheMethod()
        {
            Entity written = "1 + 2/(1+t^2)";
            Assert.Equal(written.AsSingleFraction(), Transformation.AsSingleFraction.ApplyOrKeep(written));
            Assert.Equal("single-fraction", Transformation.AsSingleFraction.Name);
        }
    }
}
