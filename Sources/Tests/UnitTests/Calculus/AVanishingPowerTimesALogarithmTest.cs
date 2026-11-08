//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A product tending to 0 times an infinity, read inside something else, is asked as a whole once.
    /// <c>x^pi ln(x)</c> at 0 from the right is 0, and its own limit says so, but read as a part of a
    /// quotient or a sine it came back as <c>0 * (-oo)</c>, a form with no value, and the whole was left
    /// unevaluated. Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AVanishingPowerTimesALogarithmTest
    {
        [Theory]
        [InlineData("x^pi * ln(x) / 2", "0")]
        [InlineData("sin(x^pi * ln(x))", "0")]
        [InlineData("sin(x^(3/2) * ln(x))", "0")]
        public void IsAnsweredFromTheRight(string input, string expected)
        {
            var limit = Task.Run(() => MathS.FromString(input).Limit("x", 0, ApproachFrom.Right));
            Assert.True(limit.Wait(TimeSpan.FromSeconds(30)), $"{input} did not finish");
            Assert.Equal(MathS.FromString(expected), limit.Result);
        }
    }
}
