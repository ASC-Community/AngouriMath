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
    /// The Lambert W function compiles, in FastExpression and in the LINQ compiler, to what evaluation
    /// gives on each branch: near the branch point <c>-1/e</c>, on either side of it, on the negative
    /// axis where the branches are complex, and off the axis. It was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class LambertWCompilationTest
    {
        private static readonly string[] branches = { "-2", "-1", "0", "1", "3", "1/2" };

        private static readonly string[] arguments =
        {
            "0", "1", "2", "10", "100000", "-1/10", "-3/10", "-9/25", "-1/e", "-37/100", "-1/2", "-2",
            "i", "1 + i", "-1 - i", "i/2", "-3 + i/10", "-3/10 - i/100", "-3/10 + i/100",
        };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        // Both NaN, both the same infinity, or within a part in a billion.
        private static bool Agree(Complex expected, Complex actual)
            => double.IsNaN(expected.Real) || double.IsNaN(expected.Imaginary)
               ? double.IsNaN(actual.Real) || double.IsNaN(actual.Imaginary)
               : double.IsInfinity(expected.Real)
                 ? expected == actual
                 : Complex.Abs(expected - actual) <= 1e-9 * System.Math.Max(1, Complex.Abs(expected));

        [Fact]
        public void ItIsWhatEvaluationSaysOnEachBranch()
        {
            var fast = "lambertw(k, z)".ToEntity().Compile("k", "z");
            var linq = "lambertw(k, z)".ToEntity().Compile<Complex, Complex, Complex>("k", "z");
            var real = "lambertw(k, z)".ToEntity().Compile<double, double, double>("k", "z");
            foreach (var branch in branches)
                foreach (var argument in arguments)
                {
                    var evaluated = Assert.IsAssignableFrom<Entity.Number.Complex>($"lambertw({branch}, {argument})".ToEntity().Evaled);
                    var expected = evaluated.ToNumerics();
                    var (k, z) = (Numeric(branch), Numeric(argument));
                    Assert.True(Agree(expected, fast.Call(k, z)), $"lambertw({branch}, {argument}): {expected} evaluated, {fast.Call(k, z)} by FastExpression");
                    Assert.True(Agree(expected, linq(k, z)), $"lambertw({branch}, {argument}): {expected} evaluated, {linq(k, z)} by LINQ");
                    if (z.Imaginary == 0)
                    {
                        // Real where the imaginary part is rounding's: evaluation rounds -1/e to a decimal
                        // that is across the branch point by a digit, and leaves an imaginary part of that size.
                        var overTheReals = System.Math.Abs(expected.Imaginary) <= 1e-12 * System.Math.Max(1, Complex.Abs(expected))
                            ? new Complex(expected.Real, 0) : new Complex(double.NaN, 0);
                        Assert.True(Agree(overTheReals, real(k.Real, z.Real)), $"lambertw({branch}, {argument}): {overTheReals} over the reals, {real(k.Real, z.Real)} over double");
                    }
                }
        }

        [Fact]
        public void ThePrincipalBranchCompilesInOneArgument()
            => Assert.Equal(0.5671432904097838, "lambertw(x)".ToEntity().Compile<double, double>("x")(1), 15);
    }
}
