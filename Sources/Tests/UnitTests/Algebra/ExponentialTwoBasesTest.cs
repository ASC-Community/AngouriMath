//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using System.Text.RegularExpressions;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation between two powers of numeric bases is answered exactly.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1007">#1007</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The multiplicative solver reaches these by dividing one exponent by the other, and for two
    /// different integer bases that ratio is <c>ln(3)/ln(2)</c> — irrational — so it settles to a
    /// decimal and everything after it is numeric. The answer then agreed with the exact one to
    /// about seventeen figures and diverged, which is a <c>double</c> promoted to a decimal.
    /// </para>
    /// <para>
    /// Asserted by <b>substituting the root back</b> rather than by comparing printed text, which
    /// is what the issue asks for: a decimal that is right to seventeen figures passes any test
    /// that reads the answer and fails the equation.
    /// </para>
    /// </remarks>
    [Trait("Area", "Algebra")]
    public sealed class ExponentialTwoBasesTest
    {
        [Theory]
        [InlineData("3^(x+1) - 2^(x-1)")]
        [InlineData("3^(x+1) - 2^x")]
        [InlineData("5^(2*x) - 7^(x+3)")]
        [InlineData("2^(3*x) - 5^(x+1)")]
        public void ARootOfTwoNumericPowersSatisfiesItsEquation(string equation)
        {
            // Over the complex numbers each root is a family, e^u = e^v being u = v + 2 pi i n,
            // and the members either side of the real one are roots too.
            // https://github.com/asc-community/AngouriMath/issues/2319
            var finite = equation.ToEntity().SolveEquation("x").Members(-2, -1, 0, 1, 2);
            Assert.NotEmpty(finite);
            foreach (var root in finite)
            {
                var residual = equation.ToEntity().Substitute("x", root).EvalNumerical();
                Assert.True(residual.Abs().RealPart.EDecimal.ToDouble() < 1e-8,
                    $"{equation} at {root.Stringize()} is {residual.Stringize()}, not 0");
            }
        }

        /// <summary>
        /// And no decimal literal in the printed form — the exactness is the point, and a root
        /// that satisfies the equation to eight figures would pass the check above either way.
        /// </summary>
        [Theory]
        [InlineData("3^(x+1) - 2^(x-1)")]
        [InlineData("3^(x+1) - 2^x")]
        [InlineData("5^(2*x) - 7^(x+3)")]
        public void TheAnswerCarriesNoDecimal(string equation)
            => Assert.DoesNotMatch(@"\d\.\d{6,}",
                equation.ToEntity().SolveEquation("x").Stringize());

        /// <summary>
        /// The cases that were exact before stay exact — this branch is asked before the
        /// multiplicative solver, so it must not take equations that one already answers.
        /// </summary>
        [Theory]
        [InlineData("3^x - 2^x", "{ 0 }")]
        [InlineData("3^x - 2", "{ log(3, 2) }")]
        [InlineData("2^x - 8", "{ 3 }")]
        [InlineData("2^(2*x) - 5*2^x + 4", "{ 0, 2 }")]
        [InlineData("ln(x)^2 - 3*ln(x) + 2", "{ e, e ^ 2 }")]
        [InlineData("6^x - 2", "{ log(6, 2) }")]
        public void WhatWasExactStaysExact(string equation, string expected)
            // The principal member of each family, n = 0.
            => Assert.Equal(expected, equation.ToEntity().SolveEquation("x").Principal().Stringize());
    }
}
