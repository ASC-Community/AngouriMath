//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A two-sided limit compares its sides by descending into each, and the descent asks the whole expression's limit
    /// from that side. That ask used the one level of whole limits a descent may ask, so the side could not ask the
    /// regrouping that answers it: x^pi / log(x, 2) at 0 took ten seconds two-sided, against a fifth of one from the
    /// right alone. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AComparedSideIsItsOwnQuestionTest
    {
        [Theory]
        [InlineData("x^pi / log(x, 2)", "0")]
        [InlineData("x^pi / log(x, 2) - 2", "-2")]
        [InlineData("x^e / log(x, 2)", "0")]
        public void ATwoSidedLimitAtZeroFinishes(string input, string expected)
        {
            Entity? limit = null;
            var task = Task.Factory.StartNew(() => limit = input.ToEntity().Limit("x", 0), TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), "the limit did not finish in 30 seconds");
            Assert.Equal(expected.ToEntity(), limit);
        }
    }
}
