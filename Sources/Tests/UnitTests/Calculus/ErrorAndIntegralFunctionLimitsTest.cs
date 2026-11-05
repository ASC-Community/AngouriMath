//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The error functions and the exponential and trigonometric integrals had no limit at all: <c>erf(x)</c> at +oo,
    /// which is 1, was left unevaluated, and so was <c>erf(x)</c> at 1. Their limits at the infinities are now their
    /// values there, and a finite limit of the argument is put in where the function is continuous: anywhere for the
    /// entire ones, only at a positive real point for Ci, Chi, Ei and li, which are singular at 0 and cut below it, and
    /// li singular at 1 too. The same on 2.6.0. Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=53</c>; no issue was
    /// filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ErrorAndIntegralFunctionLimitsTest
    {
        [Theory]
        [InlineData("erf(x)", "+oo", "1")]
        [InlineData("erf(x)", "-oo", "-1")]
        [InlineData("erfc(x)", "+oo", "0")]
        [InlineData("erfc(x)", "-oo", "2")]
        [InlineData("erfi(x)", "+oo", "+oo")]
        [InlineData("Si(x)", "+oo", "pi/2")]
        [InlineData("Si(x)", "-oo", "-pi/2")]
        [InlineData("Ci(x)", "+oo", "0")]
        [InlineData("Shi(x)", "-oo", "-oo")]
        [InlineData("Chi(x)", "+oo", "+oo")]
        [InlineData("Ei(x)", "+oo", "+oo")]
        [InlineData("Ei(x)", "-oo", "0")]
        [InlineData("li(x)", "+oo", "+oo")]
        [InlineData("erf(x)", "1", "erf(1)")]
        [InlineData("Ci(x)", "2", "Ci(2)")]
        [InlineData("erf(x)/x + 1", "0", "2/sqrt(pi) + 1")]
        public void HasItsLimit(string input, string destination, string expected)
            => Assert.Equal(MathS.FromString(expected).Simplify(), MathS.FromString(input).Limit("x", destination).Simplify());

        [Fact]
        public void IsOneSidedWhereItsArgumentDiverges()
        {
            Assert.Equal(MathS.FromString("1"), MathS.FromString("erf(1/x)").Limit("x", 0, ApproachFrom.Right));
            Assert.Equal(MathS.FromString("-pi/2").Simplify(), MathS.FromString("Si(1/x)").Limit("x", 0, ApproachFrom.Left).Simplify());
            Assert.Equal(MathS.NaN, MathS.FromString("erf(1/x)").Limit("x", 0));
        }

        // Nothing is claimed on Ci's cut or at li's pole.
        [Theory]
        [InlineData("Ci(x)", "-2")]
        [InlineData("Ci(x)", "-oo")]
        public void IsNotAnsweredOnACut(string input, string destination)
            => Assert.IsType<Entity.Limitf>(MathS.FromString(input).Limit("x", destination));
    }
}
