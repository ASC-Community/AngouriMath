//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// Limits at infinity that compare exponentials whose bases have logarithms in an
    /// irrational ratio, such as <c>2^x</c> and <c>3^x</c>. Gruntz's algorithm writes
    /// <c>3^x</c> in <c>w = 2^(-x)</c> as <c>w^(-ln(3)/ln(2))</c>. These were left unevaluated
    /// while the series it expands in took rational exponents only.
    /// https://github.com/asc-community/AngouriMath/issues/2597
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ExponentialsOfIrrationallyRelatedBasesLimitTest
    {
        [Theory]
        [InlineData("(3^x + 2^x)^(1/x)", "+oo", "3")]
        [InlineData("(3^x + 2^x)^(1/x)", "-oo", "2")]
        [InlineData("(2^x + 3^x)^(2/x)", "+oo", "9")]
        [InlineData("(2^x + 3^x + 5^x)^(1/x)", "+oo", "5")]
        [InlineData("ln(2^x + 3^x) / x", "+oo", "ln(3)")]
        [InlineData("(4^x + 3^x) / (4^x - 3^x)", "+oo", "1")]
        [InlineData("(3^x - 2^x) / (3^x + 2^x)", "+oo", "1")]
        [InlineData("(2^x + 3^x) / (3^x + 4^x)", "+oo", "0")]
        [InlineData("(2^x + 3^x) / (3^x + x)", "+oo", "1")]
        [InlineData("x^10 * 2^x / 3^x", "+oo", "0")]
        [InlineData("(2^x + x^5) / 3^x", "+oo", "0")]
        [InlineData("(2^x + e^x) / 3^x", "+oo", "0")]
        [InlineData("3^x - 2^x", "+oo", "+oo")]
        [InlineData("2^x - 3^x", "+oo", "-oo")]
        public void IsAnswered(string expression, string destination, string expected)
        {
            var limit = expression.ToEntity().Limit("x", destination.ToEntity());
            Assert.DoesNotContain(limit.Nodes, node => node is Entity.Limitf);
            Assert.Equal(expected.ToEntity().InnerSimplified, limit.InnerSimplified);
            // The answer agrees with the expression far out, where it has settled.
            if (limit.Evaled is Entity.Number.Real { IsFinite: true } value)
            {
                var far = destination == "+oo" ? 400 : -400;
                var near = (double)((Entity.Number.Real)expression.ToEntity().Substitute("x", far).Evaled).EDecimal.ToDouble();
                Assert.True(Math.Abs(near - value.EDecimal.ToDouble()) < 0.01, $"{near} at x = {far} against {value}");
            }
        }

        /// <summary>
        /// Exponents written over different irrational constants are compared by value, and
        /// <c>ln(6)/ln(2)</c> is <c>1 + ln(3)/ln(2)</c>. Where two of them cannot be told apart
        /// the series is not read, so an answer is either right or not given.
        /// </summary>
        [Theory]
        [InlineData("(6^x - 2^x * 3^x + 1)", "1")]
        [InlineData("(2^x * 3^x + 2^x) / 6^x", "1")]
        public void RationallyDependentBasesAreNotAnsweredWrongly(string expression, string expected)
        {
            var limit = expression.ToEntity().Limit("x", "+oo".ToEntity());
            if (!limit.Nodes.Any(node => node is Entity.Limitf))
                Assert.Equal(expected.ToEntity().InnerSimplified, limit.InnerSimplified);
        }
    }
}
