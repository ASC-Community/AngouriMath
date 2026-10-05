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
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A constant with the imaginary unit among its coefficients, in lowest terms over the
    /// symbols: its real and imaginary parts over a real denominator, the three without a common
    /// factor. The gcd reads coefficients in the rationals, and a constant with <c>i</c> in it was
    /// left as its expansion gave it, so nothing in it cancelled.
    /// https://github.com/asc-community/AngouriMath/issues/1788
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class LowestTermsOverTheGaussianRationalsTest
    {
        private static Entity InLowestTerms(string constant)
            => Functions.PartialFractions.InLowestTermsOverTheSymbols(constant.ToEntity());

        /// <summary>A constant that is zero comes out zero, since the form is unique.</summary>
        [Theory]
        [InlineData("1/(c + i*d) - (c - i*d)/(c^2 + d^2)")]
        [InlineData("(c + i*d)*(c - i*d)/(c^2 + d^2) - 1")]
        [InlineData("2*i*d/((c + i*d) - (c - i*d)) - 1")]
        public void IsZero(string constant) => Assert.Equal(Integer.Zero, InLowestTerms(constant));

        [Theory]
        [InlineData("(c^2 + d^2)/(c + i*d)", "c - i*d")]
        [InlineData("1/(2*i*d)^3", "i/(8*d^3)")]
        [InlineData("(c + i*d)^3/(c^2 + d^2)^2", "(c + i*d)/(c - i*d)^2")]
        [InlineData("(1 + 2*i*a)/(a - i)", "(-a + i*(2*a^2 + 1))/(a^2 + 1)")]
        public void IsOverARealDenominator(string constant, string value)
        {
            var lowest = InLowestTerms(constant);
            var (_, below) = Functions.SingleQuotient.Of(lowest);
            Assert.DoesNotContain(below.Nodes, node => node is Complex number && number is not Real);
            foreach (var (first, second) in new[] { (1.37, 0.83), (-2.1, 0.4) })
            {
                Entity Pinned(Entity e) => e.Substitute("a", first).Substitute("c", first).Substitute("d", second);
                var got = Pinned(lowest).EvalNumerical();
                var want = Pinned(value.ToEntity()).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                Assert.True(difference < 1e-12, $"{constant} is {lowest}, which is {got} where {value} is {want}");
            }
        }
    }
}
