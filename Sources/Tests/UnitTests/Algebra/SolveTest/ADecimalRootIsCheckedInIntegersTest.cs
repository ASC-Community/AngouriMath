//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core.Budgets;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation with a sign in it is solved as a system in the parts of x, whose roots in
    /// decimals are checked against an equation with rational coefficients, exactly. Added up in
    /// rationals, the denominators were multiplied together at every step, so their digits
    /// doubled with each power of the root, and with x written as <c>10^-12 x</c> the equation
    /// below took longer than ten minutes. Found by solving generated equations as written and
    /// scaled; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ADecimalRootIsCheckedInIntegersTest
    {
        [Fact]
        public void TheRootsAreThoseInXTimesTheScale()
        {
            const string equation = "(-3) + (-3) * x + (2 + i) * x^2 + (i - x) * x * sgn((-3 + i) - x)";
            // The Gröbner basis bounds itself by five seconds of wall clock, which a loaded
            // runner can spend on the equation as written; a budget of two minutes keeps the
            // answer from depending on the machine, and the scaled solve is still bounded below.
            // The setting is thread-static, so the task sets its own.
            var budget = new WorkBudget { Time = TimeSpan.FromMinutes(2) };
            FiniteSet asWritten;
            using (MathS.Settings.Budget.Set(budget))
                asWritten = Assert.IsType<FiniteSet>($"{equation} = 0".ToEntity().Solve("x"));
            Assert.Equal(2, asWritten.Count);
            var solving = Task.Run(() =>
            {
                using var _ = MathS.Settings.Budget.Set(budget);
                return $"{equation.Replace("x", "(10^(-12) * x)")} = 0".ToEntity().Solve("x");
            });
            Assert.True(solving.Wait(TimeSpan.FromMinutes(2)), "the scaled equation is solved");
            var scaled = Assert.IsType<FiniteSet>(solving.Result);
            Assert.Equal(2, scaled.Count);
            foreach (var root in scaled)
                Assert.Contains(asWritten, other =>
                    ((Number.Complex)(root / "10^12".ToEntity() - other).EvalNumerical()).Abs() < 1e-20);
        }
    }
}
