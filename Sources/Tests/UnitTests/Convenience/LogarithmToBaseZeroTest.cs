//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// A logarithm to base 0 has no value, as <c>log</c>'s condition says, where <c>ln(x) / ln(0)</c>, which is
    /// <c>ln(x) / (-oo)</c>, made it 0 in evaluation and in the compilers.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2255">#2255</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class LogarithmToBaseZeroTest
    {
        [Theory]
        [InlineData("log(0, 2)")]
        [InlineData("log(0, 1/2)")]
        [InlineData("log(0, -3)")]
        [InlineData("log(0, i)")]
        [InlineData("log(0, 0)")]
        [InlineData("log(0, 1)")]
        public void ItHasNoValue(string expression)
        {
            Assert.True(expression.ToEntity().Evaled.IsNaN, $"{expression} evaluates to {expression.ToEntity().Evaled}");
            Assert.True(expression.ToEntity().Simplify().IsNaN, $"{expression} simplifies to {expression.ToEntity().Simplify()}");
        }

        [Theory]
        [InlineData(2)]
        [InlineData(0.5)]
        [InlineData(-3)]
        [InlineData(1)]
        public void CompiledItHasNoValue(double antilogarithm)
        {
            var expression = $"log(x, {antilogarithm})".ToEntity();
            var fast = expression.Compile("x").Call(0);
            var linq = expression.Compile<Complex, Complex>("x")(0);
            Assert.True(double.IsNaN(fast.Real) || double.IsNaN(fast.Imaginary), $"{expression} at 0: {fast} by FastExpression");
            Assert.True(double.IsNaN(linq.Real) || double.IsNaN(linq.Imaginary), $"{expression} at 0: {linq} by LINQ");
            Assert.True(double.IsNaN(expression.Compile<double, double>("x")(0)), $"{expression} at 0 over double");
        }

        /// <summary>A base that is small and not 0 has its logarithm.</summary>
        [Theory]
        [InlineData("log(2, 8)", 3)]
        [InlineData("log(1/2, 4)", -2)]
        [InlineData("log(10^(-100), 10^(-200))", 2)]
        public void AnotherBaseHasItsLogarithm(string expression, double value)
        {
            var evaluated = ((Entity.Number.Complex)expression.ToEntity().Evaled).ToNumerics();
            Assert.True(Complex.Abs(evaluated - value) < 1e-12, $"{expression} evaluates to {evaluated}");
        }
    }
}
