//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>tanh</c> and <c>coth</c> are spelled <c>(e^(2x) - 1) / (e^(2x) + 1)</c> and its reciprocal, and past the range
    /// of the decimals, or of a double, both sides are infinite: <c>tanh(5568)</c> evaluated to NaN and <c>tanh(400)</c>
    /// compiled to NaN, where both are 1. The negative side was right, since <c>e^(-11136)</c> is 0.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2254">#2254</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class HyperbolicTangentPastTheRangeTest
    {
        [Theory]
        [InlineData("tanh(5568)", 1)]
        [InlineData("tanh(-5568)", -1)]
        [InlineData("coth(332640)", 1)]
        [InlineData("coth(-332640)", -1)]
        [InlineData("tanh(+oo)", 1)]
        [InlineData("tanh(-oo)", -1)]
        [InlineData("coth(+oo)", 1)]
        public void EvaluatedPastTheRangeItIsOne(string expression, int expected)
        {
            Assert.Equal(Entity.Number.Integer.Create(expected), expression.ToEntity().Evaled);
            Assert.Equal(Entity.Number.Integer.Create(expected), expression.ToEntity().EvalNumerical());
        }

        [Theory]
        [InlineData("tanh(x)", 400, 1)]
        [InlineData("tanh(x)", -400, -1)]
        [InlineData("coth(x)", 400, 1)]
        [InlineData("coth(x)", -400, -1)]
        [InlineData("tanh(x)", double.PositiveInfinity, 1)]
        [InlineData("tanh(x)", double.NegativeInfinity, -1)]
        public void CompiledPastTheRangeItIsOne(string expression, double at, double expected)
        {
            Assert.Equal(expected, expression.Compile<double, double>("x")(at));
            Assert.Equal(new Complex(expected, 0), expression.Compile<Complex, Complex>("x")(at));
            Assert.Equal(new Complex(expected, 0), expression.Compile("x").Call(at));
        }

        /// <summary>Inside the range the compiled values are still the hyperbolic tangent's, near 0 as well.</summary>
        [Theory]
        [InlineData(1e-6)]
        [InlineData(-0.3)]
        [InlineData(2)]
        [InlineData(19)]
        public void CompiledInsideTheRangeItIsTheHyperbolicTangent(double at)
        {
            var compiled = "tanh(x)".Compile<double, double>("x")(at);
            Assert.True(Math.Abs(compiled - Math.Tanh(at)) <= 1e-9 * Math.Abs(Math.Tanh(at)), $"{compiled} against {Math.Tanh(at)}");
            var coth = "coth(x)".Compile<double, double>("x")(at);
            Assert.True(Math.Abs(coth - 1 / Math.Tanh(at)) <= 1e-9 * Math.Abs(1 / Math.Tanh(at)), $"{coth} against {1 / Math.Tanh(at)}");
        }
    }
}
