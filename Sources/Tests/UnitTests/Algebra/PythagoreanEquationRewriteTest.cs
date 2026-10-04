//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation mixing <c>sin(u)</c> and <c>cos(u)</c> went to the exponential solver,
    /// which rewrites both in terms of <c>e^(i u)</c> and so turns
    /// <c>cos(a x)^2 + sin(a x) + c = 0</c> into a *quartic* in <c>e^(i a x)</c>. The answer
    /// was pages of nested radicals where there are two arcsines.
    /// https://github.com/asc-community/AngouriMath/issues/270
    /// </summary>
    /// <remarks>
    /// Writing <c>cos(u)^2</c> as <c>1 - sin(u)^2</c> makes it a polynomial in
    /// <c>sin(u)</c> alone, which the replacement machinery already solves --
    /// <c>sin(x)^2 + sin(x) = 0</c> has always answered in arcsines. The mixed form simply
    /// never became one.
    /// </remarks>
    [Trait("Area", "Algebra")]
    public sealed class PythagoreanEquationRewriteTest
    {
        /// <summary>
        /// Every root is substituted back into the equation and required to satisfy it.
        /// That is the property worth asserting: a solver rewrite can just as easily lose a
        /// root or invent one as shorten the answer, and the printed form says neither.
        /// </summary>
        private static void AssertRootsSatisfy(string equation, (string Symbol, double Value)[] constants)
        {
            Entity specialised = equation.ToEntity().Solve("x");
            // The residual, not the equation: substituting into `lhs = rhs` gives a boolean,
            // which EvalNumerical refuses -- and a helper that swallows that exception counts
            // every root as unverifiable and passes without checking anything.
            Entity truth = equation.ToEntity() is Entity.Equalsf(var lhs, var rhs)
                ? lhs - rhs
                : equation.ToEntity();
            foreach (var (symbol, value) in constants)
            {
                specialised = specialised.Substitute(symbol, value);
                truth = truth.Substitute(symbol, value);
            }
            var roots = Assert.IsType<Entity.Set.FiniteSet>(specialised.InnerSimplified);
            Assert.NotEmpty(roots);

            var checkedRoots = 0;
            foreach (var root in roots)
            {
                // The periodic parameter is a free integer; 0 is a root whatever it is.
                var point = root;
                foreach (var free in point.Vars.Where(v => v.Name.StartsWith("n_")))
                    point = point.Substitute(free, 0);
                if (point.Vars.Any()) continue;
                double residual;
                try
                {
                    var value = truth.Substitute("x", point).EvalNumerical();
                    residual = Math.Abs(value.RealPart.EDecimal.ToDouble())
                             + Math.Abs(value.ImaginaryPart.EDecimal.ToDouble());
                }
                catch { continue; }
                if (double.IsNaN(residual)) continue;
                checkedRoots++;
                Assert.True(residual < 1e-6,
                    $"{equation}: {root.Stringize()} is not a root, residual {residual}");
            }
            Assert.True(checkedRoots > 0, $"{equation}: no root could be checked, so nothing was verified");
        }

        /// <summary>
        /// The answer must be written in arc functions rather than as radicals in
        /// <c>e^(i a x)</c>, which is what #270 asks for. Measured as size, because the
        /// quartic answer runs to thousands of nodes and the arcsine one to tens.
        /// </summary>
        [Theory]
        [InlineData("cos(a * x) ^ 2 + sin(a * x) + c = 0")]
        [InlineData("cos(x) ^ 2 + sin(x) = 0")]
        [InlineData("cos(x) ^ 2 + sin(x) + 1 = 0")]
        [InlineData("sin(x) ^ 2 + cos(x) = 0")]
        [InlineData("2 * cos(x) ^ 2 - 3 * sin(x) = 0")]
        public void TheAnswerIsSmallAndInArcFunctions(string equation)
        {
            var solved = equation.ToEntity().Solve("x");
            Assert.True(solved.Complexity < 400,
                $"{equation} answered with {solved.Complexity} nodes, which is the quartic route");
            Assert.Contains(solved.Nodes, node => node is Entity.Arcsinf or Entity.Arccosf);
        }

        [Theory]
        [InlineData("cos(x) ^ 2 + sin(x) = 0")]
        [InlineData("cos(x) ^ 2 + sin(x) + 1 = 0")]
        [InlineData("sin(x) ^ 2 + cos(x) = 0")]
        [InlineData("2 * cos(x) ^ 2 - 3 * sin(x) = 0")]
        public void EveryRootSatisfiesTheEquation(string equation)
            => AssertRootsSatisfy(equation, Array.Empty<(string, double)>());

        [Fact]
        public void TheIssuesOwnEquationIsSolved()
            => AssertRootsSatisfy("cos(a * x) ^ 2 + sin(a * x) + c = 0",
                new[] { ("a", 2.0), ("c", -0.5) });

        /// <summary>
        /// An equation with an odd power of cosine is not a polynomial in sine, and the
        /// rewrite must decline rather than produce a square root of a square. These are the
        /// cases that must keep whatever answer they had.
        /// </summary>
        [Theory]
        [InlineData("sin(x) + cos(x) = 0")]
        [InlineData("sin(x) * cos(x) = 0")]
        public void AnOddPowerIsLeftToTheOtherSolvers(string equation)
        {
            var solved = equation.ToEntity().Solve("x");
            Assert.False(solved is Entity.Set.FiniteSet { IsSetEmpty: true },
                $"{equation} lost its answer: {solved.Stringize()}");
        }
    }
}
