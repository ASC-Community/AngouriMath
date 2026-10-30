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

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The polygamma functions and the harmonic numbers compile, in FastExpression and in the LINQ
    /// compiler, to what evaluation gives: at its poles, either side of them, left of <c>1/2</c> where
    /// the reflection is taken, far out, and off the axis. Each was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class PolygammaCompilationTest
    {
        private static readonly string[] orders = { "0", "1", "2", "3", "7", "1/2" };
        private static readonly string[] arguments =
            { "1", "2", "1/2", "-1/2", "-5/2", "0", "-3", "3/10", "-7/10", "25", "1000", "i", "1 + i", "-2 - 3i", "1/2 + 10i" };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        // Both NaN, or within a part in a billion.
        private static bool Agree(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
               ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
               : Complex.Abs(expected - actual) <= 1e-9 * System.Math.Max(1, Complex.Abs(expected));

        [Theory]
        [InlineData("polygamma(n, z)")]
        [InlineData("harmonicnumber(z, n)")]
        public void ItIsWhatEvaluationSays(string expression)
        {
            var fast = expression.ToEntity().Compile("n", "z");
            var linq = expression.ToEntity().Compile<Complex, Complex, Complex>("n", "z");
            var real = expression.ToEntity().Compile<double, double, double>("n", "z");
            var compared = 0;
            foreach (var order in orders)
                foreach (var argument in arguments)
                {
                    if (expression.ToEntity().Substitute("n", order).Substitute("z", argument).Evaled is not Entity.Number.Complex evaluated)
                        continue;
                    var expected = evaluated.ToNumerics();
                    var (n, z) = (Numeric(order), Numeric(argument));
                    Assert.True(Agree(expected, fast.Call(n, z)), $"{expression} at ({order}, {argument}): {expected} evaluated, {fast.Call(n, z)} by FastExpression");
                    Assert.True(Agree(expected, linq(n, z)), $"{expression} at ({order}, {argument}): {expected} evaluated, {linq(n, z)} by LINQ");
                    if (z.Imaginary == 0)
                    {
                        var overTheReals = System.Math.Abs(expected.Imaginary) <= 1e-12 * System.Math.Max(1, Complex.Abs(expected))
                            ? new Complex(expected.Real, 0) : new Complex(double.NaN, 0);
                        Assert.True(Agree(overTheReals, real(n.Real, z.Real)), $"{expression} at ({order}, {argument}): {overTheReals} over the reals, {real(n.Real, z.Real)} over double");
                    }
                    compared++;
                }
            Assert.True(compared > orders.Length * arguments.Length * 3 / 4, $"only {compared} of {orders.Length * arguments.Length} evaluated");
        }

        [Fact]
        public void TheDigammaFunctionCompiles()
            => Assert.Equal(-0.5772156649015329, "digamma(x)".ToEntity().Compile<double, double>("x")(1), 14);
    }
}
