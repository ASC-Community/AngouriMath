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
    /// A congruence <c>a = b (mod n)</c> compiles, in FastExpression and in the LINQ compiler, to the truth value
    /// evaluation gives: between whole numbers whether <c>n</c> divides <c>a - b</c>, a modulus of 0 meaning equality,
    /// and none for any other number. It was refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class CongruenceCompilationTest
    {
        private static readonly string[] numbers =
            { "-4", "0", "1", "3", "6", "11", "5/2", "i", "+oo", "100000000000000000000" };

        private static readonly string[] moduli = { "5", "-5", "0", "1", "2", "5/2", "i" };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        private static bool? Truth(Complex value) => value == 1 ? true : value == 0 ? false : null;

        [Fact]
        public void ACongruenceIsWhatEvaluationSays()
        {
            var statement = "x = y (mod z)".ToEntity();
            var fast = statement.Compile("x", "y", "z");
            var linq = statement.Compile<Complex, Complex, Complex, bool?>("x", "y", "z");
            var real = statement.Compile<double, double, double, bool?>("x", "y", "z");
            var compared = 0;
            foreach (var a in numbers)
                foreach (var b in numbers)
                    foreach (var n in moduli)
                    {
                        var evaluated = statement.Substitute("x", a).Substitute("y", b).Substitute("z", n).Evaled;
                        if (evaluated is not Entity.Boolean && !evaluated.IsNaN)
                            continue;
                        bool? expected = evaluated is Entity.Boolean truth ? (bool)truth : null;
                        var (x, y, z) = (Numeric(a), Numeric(b), Numeric(n));
                        Assert.True(expected == Truth(fast.Call(x, y, z)), $"{a} = {b} (mod {n}): {expected?.ToString() ?? "none"} evaluated, {fast.Call(x, y, z)} by FastExpression");
                        Assert.True(expected == linq(x, y, z), $"{a} = {b} (mod {n}): {expected?.ToString() ?? "none"} evaluated, {linq(x, y, z)?.ToString() ?? "none"} by LINQ");
                        if (x.Imaginary == 0 && y.Imaginary == 0 && z.Imaginary == 0)
                            Assert.True(expected == real(x.Real, y.Real, z.Real), $"{a} = {b} (mod {n}): {expected?.ToString() ?? "none"} evaluated, {real(x.Real, y.Real, z.Real)?.ToString() ?? "none"} over double");
                        compared++;
                    }
            Assert.True(compared >= numbers.Length * numbers.Length * moduli.Length * 3 / 4, $"only {compared} decided");
        }

        /// <summary>
        /// Past 2^53 the difference is taken in whole numbers: <c>2^60 + 256</c> less 1 is 3 modulo 4, where the
        /// difference in doubles rounds back to <c>2^60 + 256</c>, which 4 divides.
        /// </summary>
        [Fact]
        public void PastTwoToTheFiftyThreeTheDifferenceIsExact()
        {
            Assert.Equal(Entity.Boolean.False, "1152921504606847232 = 1 (mod 4)".ToEntity().Evaled);
            Assert.Equal(new Complex(0, 0), "x = 1 (mod 4)".ToEntity().Compile("x").Call(1152921504606847232.0));
            Assert.Equal(false, "x = 1 (mod 4)".ToEntity().Compile<double, bool?>("x")(1152921504606847232.0));
        }

        [Fact]
        public void AChainOfCongruencesCompiles()
            => Assert.Equal(new Complex(1, 0), "x = 1 = 11 (mod 5)".ToEntity().Compile("x").Call(6));
    }
}
