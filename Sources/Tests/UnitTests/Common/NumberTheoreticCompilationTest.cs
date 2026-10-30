//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The functions of a factorisation, the valuation, the n-th prime and the prime-counting function
    /// compile, in FastExpression and in the LINQ compiler, to what evaluation gives: exactly at a whole
    /// argument, and <c>NaN</c> at any other. Each was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class NumberTheoreticCompilationTest
    {
        private static readonly string[] wholes =
            new[] { "-3", "0", "97", "1000003", "1099511627776", "600851475143", "5/2", "1 + i" }
                .Concat(Enumerable.Range(1, 60).Select(n => n.ToString())).ToArray();

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        // Both NaN, or equal to a part in 1e12.
        private static bool Agree(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
               ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
               : double.IsInfinity(expected.Real) ? expected == actual
               : Complex.Abs(expected - actual) <= 1e-12 * System.Math.Max(1, Complex.Abs(expected));

        private static void Compare(string expression, string[] xs, string[] ys)
        {
            var fast = expression.ToEntity().Compile("x", "y");
            var linq = expression.ToEntity().Compile<Complex, Complex, Complex>("x", "y");
            var real = expression.ToEntity().Compile<double, double, double>("x", "y");
            var compared = 0;
            foreach (var a in xs)
                foreach (var b in ys)
                {
                    if (expression.ToEntity().Substitute("x", a).Substitute("y", b).Evaled is not Entity.Number.Complex evaluated)
                        continue;
                    var expected = evaluated.ToNumerics();
                    var (x, y) = (Numeric(a), Numeric(b));
                    Assert.True(Agree(expected, fast.Call(x, y)), $"{expression} at ({a}, {b}): {expected} evaluated, {fast.Call(x, y)} by FastExpression");
                    Assert.True(Agree(expected, linq(x, y)), $"{expression} at ({a}, {b}): {expected} evaluated, {linq(x, y)} by LINQ");
                    if (x.Imaginary == 0 && y.Imaginary == 0 && expected.Imaginary == 0)
                        Assert.True(Agree(expected, real(x.Real, y.Real)), $"{expression} at ({a}, {b}): {expected} evaluated, {real(x.Real, y.Real)} over double");
                    compared++;
                }
            Assert.True(compared > 0, $"nothing of {expression} evaluated");
        }

        [Theory]
        [InlineData("mobius(x) + 0 y")]
        [InlineData("primenu(x) + 0 y")]
        [InlineData("primeomega(x) + 0 y")]
        public void AFunctionOfAFactorisationIsWhatEvaluationSays(string expression)
            => Compare(expression, wholes, new[] { "0" });

        [Fact]
        public void TheDivisorSigmaIsWhatEvaluationSays()
            => Compare("divisorsigma(y, x)", wholes, new[] { "0", "1", "2", "-1", "1/2", "i" });

        [Fact]
        public void TheValuationIsWhatEvaluationSays()
            => Compare("valuation(x, y)", new[] { "0", "12", "-12", "1024", "7", "5/2", "600851475143" }, new[] { "2", "3", "4", "7", "1/2", "71" });

        [Fact]
        public void TheNthPrimeIsWhatEvaluationSays()
            => Compare("prime(x) + 0 y", new[] { "1", "2", "10", "1000", "10000", "0", "-2", "5/2", "i" }, new[] { "0" });

        [Fact]
        public void ThePrimeCountIsWhatEvaluationSays()
            => Compare("primepi(x) + 0 y", new[] { "-3", "0", "2", "5/2", "100", "100000", "i" }, new[] { "0" });
    }
}
