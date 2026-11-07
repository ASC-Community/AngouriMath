//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A product whose factors tend to 0 and to an infinity, one of them a quotient, from one side.
    /// Read as a product nothing settled the form, and x * (ln(x) / ln(2 + x)) at 0 from the right
    /// did not finish in thirty seconds, nor did x * log(2 + x, x), where x ln(x) / ln(2 + x), the
    /// same thing over one divisor, was 0 at once; from the left each took twelve seconds. Found
    /// by limitcheck at <c>DEPTH=3</c>, <c>SEED=59</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AProductWithAQuotientFactorTest
    {
        [Theory]
        [InlineData("x * (ln(x) / ln(2 + x))", "0", ApproachFrom.Right)]
        [InlineData("(ln(x) / ln(2 + x)) * x", "0", ApproachFrom.Right)]
        [InlineData("x * log(2 + x, x)", "0", ApproachFrom.Right)]
        [InlineData("x * log(2 + x, x)", "0", ApproachFrom.Left)]
        [InlineData("sin(x) * (ln(x) / ln(3 - x))", "0", ApproachFrom.Right)]
        [InlineData("x * (ln(-x) / ln(2 + x))", "0", ApproachFrom.Left)]
        public void IsZero(string expr, string at, ApproachFrom side)
        {
            var asked = Task.Run(() => expr.ToEntity().Limit("x", at, side));
            Assert.True(asked.Wait(TimeSpan.FromSeconds(20)), $"the limit of {expr} at {at} from the {side} did not finish");
            var limit = asked.Result;
            Assert.DoesNotContain(limit.Nodes, node => node is Entity.Limitf);
            Assert.Equal(Entity.Number.Integer.Zero, limit.Simplify());
        }
    }
}
