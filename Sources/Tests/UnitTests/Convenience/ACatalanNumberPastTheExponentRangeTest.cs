//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// A Catalan number of a number that is not whole is <c>4^n Γ(n + 1/2) / (√π Γ(n + 2))</c>, and each gamma
    /// was computed in the evaluation's exponent range: from <c>n = 449.5</c>, whose number is 2.5e266, both were
    /// <c>+oo</c> and the quotient <c>NaN</c>, and <c>catalannumber(-60.3)</c> was <c>NaN</c> as well. And a quotient
    /// of factorials under the downcasting tolerance was rounded onto zero as though it were what a cancellation left:
    /// <c>fallingfactorial(0.5, -100.5)</c>, 9.4e-161, was <c>0</c>. Found by compilesweep, on
    /// <c>catalannumber(Ei(sinh(x)))</c> at 6; no issue was filed. The values are mpmath's at 120 digits.
    /// </summary>
    [Trait("Area", "Discrete")]
    public sealed class ACatalanNumberPastTheExponentRangeTest
    {
        [Theory]
        [InlineData("catalannumber(10.5)", "31375.760336503176813184117125078909721937218359811")]
        [InlineData("catalannumber(449.5)", "2.4958055762434131441628313609834440544021408905592e+266")]
        [InlineData("catalannumber(500.5)", "1.0773809195193076398539141613347340467488925064819e+297")]
        [InlineData("catalannumber(1600.25)", "2.462290001548975528409180780699097156132733259886e+958")]
        [InlineData("catalannumber(-60.3)", "-8.3875179942165737514658221112507292321206364599213e-40")]
        // A quotient of factorials under the downcasting tolerance, which is not a residual.
        [InlineData("fallingfactorial(0.5, -100.5)", "9.4019927548161528235486105066852002441716212361456e-161")]
        [InlineData("risingfactorial(100.5, -60.5)", "2.1883878150169778143163818514630537992056282322519e-111")]
        public void IsItsValue(string input, string expected)
        {
            var value = Assert.IsAssignableFrom<Entity.Number.Real>(Within(input));
            var reference = EDecimal.FromString(expected);
            var error = value.EDecimal.Subtract(reference).Abs().Divide(reference.Abs(), EContext.Binary64);
            Assert.True(error.CompareTo(EDecimal.FromString("1e-25")) < 0, $"{input} evaluated to {value}");
        }

        [Theory]
        // 1.7e1199, and about 10^(6e39): past the range, where a real is +oo.
        [InlineData("catalannumber(2000.5)")]
        [InlineData("catalannumber(1e40 + 0.2)")]
        public void IsPositiveInfinityPastTheRange(string input)
            => Assert.Equal(Entity.Number.Real.PositiveInfinity, Within(input));

        private static Entity Within(string input)
        {
            var task = Task.Factory.StartNew(() => input.ToEntity().EvalNumerical(), TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(30)), $"{input} did not finish in thirty seconds");
            return task.Result;
        }
    }
}
