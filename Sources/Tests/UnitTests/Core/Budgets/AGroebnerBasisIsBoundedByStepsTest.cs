//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Core.Budgets;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Budgets
{
    /// <summary>
    /// A Gröbner computation was bounded by five seconds of wall clock, so whether it answered
    /// depended on the machine and on what else it was doing. The equation below, with x written
    /// as 10^-12 x, spends more than five seconds in the computation on a desktop, mostly in
    /// checking the roots it back-substitutes, and its outcome was "gave up on time" -- a
    /// different machine would have got further. Bounded by steps, it runs to its own end.
    /// Found by solving generated equations as written and scaled; no issue was filed.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AGroebnerBasisIsBoundedByStepsTest
    {
        [Fact]
        public void TheOutcomeDoesNotDependOnTheMachine()
        {
            const string equation = "((-3) + (1 + (-2)*i)*x^2) + ((-3 + (-1)*i))*abs(((-3) + (3)*x)) = 0";
            using var recording = BudgetRecording.Start();
            _ = equation.Replace("x", "(10^(-12) * x)").ToEntity().Solve("x");
            var groebner = recording.Outcomes.Where(outcome => outcome.Where == "Gröbner").ToList();
            Assert.NotEmpty(groebner);
            Assert.All(groebner, outcome => Assert.True(outcome.IsDeterministic, outcome.ToString()));
        }
    }
}
