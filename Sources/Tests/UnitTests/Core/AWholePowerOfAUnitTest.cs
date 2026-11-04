//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Threading;
using Xunit;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A whole power was built by a recursion on the half of the exponent, one frame for each of its bits. A power of
    /// -1, i or -i is never too large to build, so <c>(-1)^(2^20000)</c> took 20000 frames and overflowed the stack of a
    /// thread-pool thread, 1.5 MB on Linux, which ends the process rather than throwing. Found by limitcheck at
    /// <c>DEPTH=3</c>, <c>SEED=28</c>, evaluating a sample point; no issue was filed.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AWholePowerOfAUnitTest
    {
        // A quarter of a megabyte: smaller than any thread the library is run on, and enough that the recursion
        // overflowed on these exponents, of 20000 bits and more, and ended the test host.
        private const int SmallStack = 256 * 1024;

        [Theory]
        [InlineData("(-1)^(2^20000)", "1")]
        [InlineData("(-1)^(2^20000 + 1)", "-1")]
        [InlineData("i^(3^20000)", "i")]
        [InlineData("(-i)^(-(2^20001) - 3)", "-i")]
        [InlineData("(-1)^(-(2^20000) - 1)", "-1")]
        public void IsItsCycleOnASmallStack(string input, string expected)
        {
            Entity? value = null;
            var thread = new Thread(() => value = MathS.FromString(input).EvalNumerical(), SmallStack);
            thread.Start();
            thread.Join();
            Assert.Equal(MathS.FromString(expected).EvalNumerical(), value);
        }

        // The multiplications are the recursion's, in its order, so a decimal power keeps every digit it had.
        [Theory]
        [InlineData("(1.1 + 0.2i)^(-13)", "-0.16276672053742730437722112 - 0.16876781705731410143215616i")]
        [InlineData("(0.3 + 0.4i)^(-9)", "-241.716953088 - 451.350102016i")]
        [InlineData("2.5^(-37)", "137438953472/72759576141834259033203125")]
        [InlineData("i^(-7)", "i")]
        public void IsWhatItWas(string input, string expected)
            => Assert.Equal(MathS.FromString(expected).EvalNumerical(), MathS.FromString(input).EvalNumerical());
    }
}
