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
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A quotient of two logarithms that both diverge is the limit of the quotient of their logarithmic derivatives,
    /// and that was asked as a quotient of quotients, <c>(u'/u) / (v'/v)</c>. For <c>ln(x - 1) / ln(ln(x))</c> at 1 that
    /// is <c>(1/(x - 1)) / ((1/x) / ln(x))</c>, whose limit took twelve seconds from the left and six from the right,
    /// where <c>x ln(x) / (x - 1)</c>, the same function, takes a fraction of one. Found by limitcheck, which gave each
    /// side five seconds; no issue was filed. The cap here is far from both: the fixed limit takes about 0.2 s.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AQuotientOfLogarithmsIsAskedAsOneQuotientTest
    {
        [Theory]
        [InlineData("ln(x - 1) / ln(ln(x))", ApproachFrom.Left)]
        [InlineData("ln(x - 1) / ln(ln(x))", ApproachFrom.Right)]
        [InlineData("ln(x - 1) / ln(ln(x))", ApproachFrom.BothSides)]
        [InlineData("log(ln(x), x - 1)", ApproachFrom.Left)]
        [InlineData("ln(1 - x) / ln(-ln(x))", ApproachFrom.Left)]
        public void IsOneAtOne(string input, ApproachFrom side)
        {
            var task = Task.Factory.StartNew(() => MathS.FromString(input).Limit("x", 1, side), TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(4)), $"the limit of {input} at 1, {side}, did not finish in four seconds");
            Assert.Equal(Number.Integer.One, task.Result);
        }
    }
}
