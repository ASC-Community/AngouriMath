//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The limit of a difference quotient at a symbolic point is the derivative there. The
    /// numerator's limit came back as <c>u - u</c>, which the inner simplification leaves
    /// written, so the quotient was read as a constant over a vanishing divisor and answered
    /// NaN, the claim that there is no limit.
    /// https://github.com/asc-community/AngouriMath/issues/2628
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ADifferenceQuotientAtASymbolicPointTest
    {
        [Theory]
        [InlineData("(sqrt(a + x) - sqrt(a)) / x", "0", "1 / (2 * sqrt(a))")]
        [InlineData("(e^(a + x) - e^a) / x", "0", "e^a")]
        [InlineData("(sin(a + x) - sin(a)) / x", "0", "cos(a)")]
        [InlineData("(ln(a + x) - ln(a)) / x", "0", "1 / a")]
        [InlineData("((a + x)^(1/3) - a^(1/3)) / x", "0", "1 / (3 * a^(2/3))")]
        [InlineData("(sqrt(a + x) - sqrt(a - x)) / x", "0", "1 / sqrt(a)")]
        [InlineData("(sqrt(x) - sqrt(b)) / (x - b)", "b", "1 / (2 * sqrt(b))")]
        public void IsTheDerivative(string quotient, string point, string derivative)
        {
            foreach (var side in new[] { ApproachFrom.BothSides, ApproachFrom.Left, ApproachFrom.Right })
            {
                var limit = quotient.ToEntity().Limit("x", point.ToEntity(), side);
                Assert.DoesNotContain(limit.Nodes, node => node is Entity.Limitf);
                Assert.NotEqual(MathS.NaN, limit.Evaled);
                // Compared by value at points on either side of 0 for each symbol, since the
                // forms the limit and the derivative are written in differ.
                foreach (var value in new[] { 0.7, 2.5, 9 })
                {
                    var got = (Entity.Number.Complex)limit.Substitute("a", value).Substitute("b", value).Evaled;
                    var expected = (Entity.Number.Complex)derivative.ToEntity().Substitute("a", value).Substitute("b", value).Evaled;
                    Assert.True(((Entity.Number.Complex)(got - expected).Evaled).Abs() < 1e-20,
                        $"{quotient} at {point} from {side}: {limit} = {got} at {value}, against {expected}");
                }
            }
        }

        /// <summary>A difference whose terms tend to one constant tends to 0.</summary>
        [Theory]
        [InlineData("e^(a + x) - e^a")]
        [InlineData("sqrt(a + x) - sqrt(a)")]
        [InlineData("ln(a + x) - ln(a)")]
        public void ADifferenceOfOneLimitIsZero(string difference)
            => Assert.Equal(Entity.Number.Integer.Zero,
                difference.ToEntity().Limit("x", 0).Evaled switch
                {
                    Entity.Providedf(var inner, _) => inner,
                    var bare => bare,
                });
    }
}
