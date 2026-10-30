//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A membership and a divisibility compile, in FastExpression and in the LINQ compiler, to the truth value
    /// evaluation gives: 1 or <see langword="true"/> where it holds, 0 or <see langword="false"/> where it does
    /// not, and NaN or <see langword="null"/> where it has none. Both were refused.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class MembershipCompilationTest
    {
        private static readonly string[] points =
            { "-3", "-1", "-1/2", "0", "1/2", "1", "2", "5/2", "3", "5", "7", "10", "15", "97", "100", "i", "1 + i", "2i", "+oo", "-oo" };

        private static Complex Numeric(string value) => ((Entity.Number.Complex)value.ToEntity().Evaled).ToNumerics();

        // The truth value evaluation gives, or nothing where it leaves the statement as written.
        private static bool? Evaluated(Entity statement, out bool decided)
        {
            var evaluated = statement.Evaled;
            decided = evaluated is Entity.Boolean || evaluated.IsNaN;
            return evaluated is Entity.Boolean truth ? (bool)truth : null;
        }

        private static bool? Truth(Complex value) => value == 1 ? true : value == 0 ? false : null;

        private static void Compare(string statement, string[] xs, string[] ys)
        {
            var fast = statement.ToEntity().Compile("x", "y");
            var linq = statement.ToEntity().Compile<Complex, Complex, bool?>("x", "y");
            var real = statement.ToEntity().Compile<double, double, bool?>("x", "y");
            var compared = 0;
            foreach (var a in xs)
                foreach (var b in ys)
                {
                    var expected = Evaluated(statement.ToEntity().Substitute("x", a).Substitute("y", b), out var decided);
                    if (!decided)
                        continue;
                    var (x, y) = (Numeric(a), Numeric(b));
                    Assert.True(expected == Truth(fast.Call(x, y)), $"{statement} at ({a}, {b}): {expected?.ToString() ?? "none"} evaluated, {fast.Call(x, y)} by FastExpression");
                    Assert.True(expected == linq(x, y), $"{statement} at ({a}, {b}): {expected?.ToString() ?? "none"} evaluated, {linq(x, y)?.ToString() ?? "none"} by LINQ");
                    if (x.Imaginary == 0 && y.Imaginary == 0)
                        Assert.True(expected == real(x.Real, y.Real), $"{statement} at ({a}, {b}): {expected?.ToString() ?? "none"} evaluated, {real(x.Real, y.Real)?.ToString() ?? "none"} over double");
                    compared++;
                }
            Assert.True(compared >= xs.Length * ys.Length * 3 / 4, $"only {compared} of {xs.Length * ys.Length} of {statement} decided");
        }

        [Theory]
        [InlineData("[0; 1]")]
        [InlineData("(0; 1]")]
        [InlineData("[-2; 3)")]
        [InlineData("(-oo; 0)")]
        [InlineData("[0; +oo)")]
        [InlineData("{ 1, 2, 3 }")]
        [InlineData("{ }")]
        [InlineData("{ i, 2 }")]
        [InlineData("RR")]
        [InlineData("CC")]
        [InlineData("ZZ")]
        [InlineData("ZZ+")]
        [InlineData("ZZ*")]
        [InlineData("PP")]
        [InlineData("[0; 1] \\/ { 5 }")]
        [InlineData("ZZ /\\ [0; 10]")]
        [InlineData("RR \\ ZZ")]
        [InlineData("PP \\/ [10; 20]")]
        [InlineData("(PP /\\ [0; 100]) \\ { 2 }")]
        [InlineData("{ t : abs(t) < 2 }")]
        [InlineData("{ t : t in ZZ and t > 2 }")]
        public void AMembershipIsWhatEvaluationSays(string set)
            => Compare($"x in ({set}) and y = 0", points, new[] { "0" });

        [Fact]
        public void ADivisibilityIsWhatEvaluationSays()
            => Compare("x divides y",
                new[] { "-3", "-2", "0", "1", "2", "3", "7", "1/2", "i", "5/2" },
                new[] { "0", "6", "-6", "7", "12", "5/2", "2i", "100000000000000000000", "+oo" });

        [Theory]
        [InlineData("iverson(x in ZZ) + y")]
        [InlineData("piecewise(x provided x in [0; 1], -x) + y")]
        [InlineData("x in [0; 1] or 3 divides x + y")]
        [InlineData("not x in PP and y = 0")]
        public void AMembershipInAnExpressionIsWhatEvaluationSays(string expression)
        {
            var fast = expression.ToEntity().Compile("x", "y");
            foreach (var a in points)
            {
                var evaluated = expression.ToEntity().Substitute("x", a).Substitute("y", 0).Evaled;
                var value = fast.Call(Numeric(a), 0);
                if (evaluated is Entity.Boolean truth)
                    Assert.True(Truth(value) == (bool)truth, $"{expression} at {a}: {truth} evaluated, {value} by FastExpression");
                else if (evaluated is Entity.Number.Complex number)
                {
                    var expected = number.ToNumerics();
                    Assert.True(double.IsNaN(expected.Real) ? double.IsNaN(value.Real) : expected == value,
                        $"{expression} at {a}: {expected} evaluated, {value} by FastExpression");
                }
            }
        }

        /// <summary>
        /// Every finite double is a rational, and <c>pi in QQ</c> is <c>False</c>, so a membership in <c>QQ</c> has no
        /// compiled form; nor has one in <c>BB</c>, whose members are not numbers.
        /// </summary>
        [Theory]
        [InlineData("x in QQ")]
        [InlineData("x in BB")]
        [InlineData("x in ([0; 1] \\/ QQ)")]
        [InlineData("x in (QQ /\\ [0; 1])")]
        public void AMembershipInAnyOtherSetIsRefused(string statement)
        {
            Assert.Throws<UncompilableNodeException>(() => statement.ToEntity().Compile("x"));
            Assert.Throws<UncompilableNodeException>(() => statement.ToEntity().Compile<Complex, bool?>("x"));
        }
    }
}
