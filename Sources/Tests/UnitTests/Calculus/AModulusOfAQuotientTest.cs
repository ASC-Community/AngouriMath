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
    /// <c>abs(1/x^pi)</c> at 0 was NaN -- the claim that the limit does not exist -- where it is +oo from both sides, as
    /// <c>1/abs(x)^pi</c> is. The modulus took its argument's limit and nothing else, and to the left of 0 <c>x^pi</c> is
    /// complex, so the sign <c>1/x^pi</c>'s divisor vanishes with could not be read: that side had no limit, and the
    /// two-sided limit was NaN. The modulus of a quotient is the quotient of the moduli, and of a product the product,
    /// for complex values too, and read that way the divisor vanishes from above. The same on 2.6.0. Found by limitcheck
    /// at <c>DEPTH=3</c>, <c>SEED=51</c>, and <c>abs(log(cos(x), -1))</c>, which is pi / abs(ln(cos(x))), at
    /// <c>SEED=41</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AModulusOfAQuotientTest
    {
        [Theory]
        [InlineData("abs(1/x^pi)")]
        [InlineData("abs(2/x^(3/2))")]
        [InlineData("abs(1/sqrt(x))")]
        [InlineData("abs(log(cos(x), -1))")]
        public void IsInfiniteFromEachSideAndBoth(string input)
        {
            foreach (var side in new[] { ApproachFrom.Left, ApproachFrom.Right, ApproachFrom.BothSides })
                Assert.Equal(MathS.FromString("+oo"), MathS.FromString(input).Limit("x", 0, side));
        }

        // A modulus that has a limit as written keeps it.
        [Theory]
        [InlineData("abs(sin(x)/x)", "0", "1")]
        [InlineData("abs(x/abs(x))", "0", "1")]
        [InlineData("abs((x^2 - 1)/(x - 1))", "1", "2")]
        public void KeepsWhatItHad(string input, string destination, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination));
    }
}
