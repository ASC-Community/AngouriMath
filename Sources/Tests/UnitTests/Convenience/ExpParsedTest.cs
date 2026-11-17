//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// exp is the ordinary name for the exponential function everywhere else -- sympy,
    /// Mathematica, MATLAB, numpy -- and the grammar did not have it. It fell through to
    /// implicit multiplication, the rule that lets a(b + c) mean a * (b + c), and came out
    /// as the product of an undeclared variable named exp with the argument. Nothing was
    /// said about it, so exp(x) - 3x = 0 answered { 0 }: a wrong answer to a question the
    /// library had misread, rather than a refusal to answer. No issue is filed for this.
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ExpParsedTest
    {
        // e^x, not a distinct node: the library already differentiates, integrates and
        // simplifies powers of e, and a new node would have to be taught all of it.
        [Theory]
        [InlineData("exp(x)", "e ^ x")]
        [InlineData("exp(2 * x)", "e ^ (2 * x)")]
        [InlineData("exp(1)", "e ^ 1")]
        [InlineData("exp(x + y)", "e ^ (x + y)")]
        public void ExpIsAPowerOfE(string written, string expected) =>
            Assert.Equal(expected, written.ToEntity().Stringize());

        [Fact]
        public void ExpEvaluatesAndDifferentiatesAsThePowerItIs()
        {
            Assert.Equal(MathS.DecimalConst.e, "exp(1)".ToEntity().EvalNumerical().RealPart.EDecimal);
            // exp(x) * exp(y) = exp(x + y), which only holds if both are really powers of e
            Assert.Equal("e ^ (x + y)", "exp(x) * exp(y)".ToEntity().Simplify().Stringize());
            Assert.Equal("2 * e ^ (2 * x)",
                "exp(2 * x)".ToEntity().Differentiate("x").Simplify().Stringize());
        }

        /// <summary>
        /// The defect as it showed: both real roots of e^x = 3x were lost, and 0 -- which
        /// is a root of the misread exp * x - 3x and of nothing else -- was returned in
        /// their place.
        /// </summary>
        [Fact]
        public void Issue_ExpEquationIsSolvedRatherThanMisread()
        {
            // Every branch of the Lambert W function is a root, and the real ones are the branches -1 and 0.
            var roots = "exp(x) - 3 * x".ToEntity().SolveEquation("x").Members(-1, 0);
            var real = roots.Select(root => root.EvalNumerical())
                            .Where(root => root.ImaginaryPart.EDecimal.Abs().ToDouble() < 1e-8)
                            .Select(root => root.RealPart.EDecimal.ToDouble())
                            .OrderBy(value => value).ToList();
            Assert.Equal(2, real.Count);
            Assert.Equal(0.6190612867359450, real[0], 9);
            Assert.Equal(1.5121345516578424, real[1], 9);
        }

        // Only the exact name followed by a bracket is the function. Everything that merely
        // begins the same way stays the implicit product it was.
        [Theory]
        [InlineData("expr(x)")]
        [InlineData("exp * x")]
        [InlineData("exp")]
        [InlineData("expo(x)")]
        [InlineData("aexp(x)")]
        public void EverythingElseParsesAsItDid(string written) => written.ToEntity();
    }
}
