//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A quotient whose divisor is 0 throughout the approach has no value there and no limit.
    /// abs(x) - x is 0 for every positive x, and l'Hopital's rule, whose premise is a divisor that
    /// is not 0 near the point, read (abs(x) - x) / (abs(x) - x) at 1 as 1. Found while probing a
    /// cancellation in l'Hopital's rule; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ADivisorThatIsZeroNearThePointTest
    {
        [Theory]
        [InlineData("(abs(x) - x) / (abs(x) - x)", "1", ApproachFrom.BothSides)]
        [InlineData("(abs(x) - x) / (abs(x) - x)", "1", ApproachFrom.Left)]
        [InlineData("(abs(x) - x) * x / ((abs(x) - x) * sin(x))", "0", ApproachFrom.Right)]
        public void HasNoLimit(string input, string destination, ApproachFrom side)
            => Assert.Equal(MathS.NaN, MathS.FromString(input).Limit("x", destination, side));

        // Where the divisor is not 0, or is 0 on one side only of a two-sided limit, the limit stands.
        [Theory]
        [InlineData("(abs(x) - x) / (abs(x) - x)", "-1", ApproachFrom.BothSides, "1")]
        [InlineData("(abs(x) - x) * x / ((abs(x) - x) * sin(x))", "0", ApproachFrom.Left, "1")]
        [InlineData("(abs(x) - x) * x / ((abs(x) - x) * sin(x))", "0", ApproachFrom.BothSides, "1")]
        [InlineData("x / (abs(x) - x)", "0", ApproachFrom.Left, "-1/2")]
        public void Stands(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));
    }
}
