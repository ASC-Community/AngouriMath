//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The sign of an expression that tends to 0 at an infinity was read through 1/x, at 0 from the right, and
    /// sinh(e^(-1/x)) is flat there: every derivative of e^(-1/x) vanishes at 0, so sgn(sinh(e^x)) at -oo was left
    /// unevaluated. Where the variable is only inside one part u, the expression is h(u), and its sign is that of h(t)
    /// as t tends to the limit of u from the side u arrives from: (e^t - e^-t) / 2 at 0 from the right is positive.
    /// Found by limitcheck; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASignThroughAChangeOfVariableTest
    {
        [Theory]
        [InlineData("sgn(sinh(e^x))", "-oo", "1")]
        [InlineData("sgn(-sinh(e^x))", "-oo", "-1")]
        [InlineData("sgn(sin(e^x))", "-oo", "1")]
        [InlineData("sgn(sin(e^(-x)))", "+oo", "1")]
        [InlineData("sgn(cos(e^x) - 1)", "-oo", "-1")]
        [InlineData("sgn(1 - cosh(1/x))", "+oo", "-1")]
        // ln(1 + u) - u is -u^2 / 2 and less near u = 0.
        [InlineData("sgn(ln(1 + e^x) - e^x)", "-oo", "-1")]
        [InlineData("floor(cos(e^x))", "-oo", "0")]
        [InlineData("floor(-sin(e^x))", "-oo", "-1")]
        public void IsTheSignOfTheOuterFunctionNearTheInnerLimit(string input, string destination, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", destination, ApproachFrom.BothSides));

        // The side 1 + e^x arrives at 1 from is the sign of (1 + e^x) - 1, whose own part minus its limit is the same
        // expression; asking it again overflowed the stack, which a test process does not survive.
        [Fact]
        public void APartWhoseOffsetIsTheExpressionItselfIsPassedOver()
            => Assert.Equal("1".ToEntity(), "sgn(ln(1 + e^x))".ToEntity().Limit("x", "-oo", ApproachFrom.BothSides));

        // sin(x) at +oo has no limit and keeps no sign; nothing here gives it one.
        [Fact]
        public void AnOscillatingArgumentIsStillDeclined()
            => Assert.IsType<Entity.Limitf>("sgn(sin(x))".ToEntity().Limit("x", "+oo", ApproachFrom.BothSides));
    }
}
