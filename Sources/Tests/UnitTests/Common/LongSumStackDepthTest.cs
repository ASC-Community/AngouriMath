//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using System.Threading;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A sum built one term at a time is nested as deep as it is long, and a sum simplifies its
    /// left operand first, so it was simplified by a recursion as deep as the sum. Expanding a
    /// product of two sums of 30 terms collects 900, and that overflowed a 256 KB stack in a
    /// Release build. In C# Test's Debug build on Windows, the integral of
    /// <c>(q + p x)/(c + b x + a x^2)^3</c> did the same to a 1 MB stack.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2166">#2166</a>
    /// </summary>
    /// <remarks>
    /// Run on a thread of a fixed stack size, because the test runner's threads have whatever
    /// stack the platform gives them: 8 MB on Linux, 512 KB on macOS. A regression here is a
    /// stack overflow, which takes the test host down rather than failing one test.
    /// </remarks>
    [Trait("Area", "Common")]
    public sealed class LongSumStackDepthTest
    {
        [Fact]
        public void ASumOf900TermsIsCollectedOnASmallStack()
        {
            var left = string.Join(" + ", Enumerable.Range(1, 30).Select(i => "a_" + i)).ToEntity();
            var right = string.Join(" + ", Enumerable.Range(1, 30).Select(i => "b_" + i)).ToEntity();
            Entity? expanded = null;
            var worker = new Thread(() => expanded = (left * right).Expand(), 256 * 1024);
            worker.Start();
            worker.Join();
            Assert.NotNull(expanded);
            Assert.Equal(900, Entity.Sumf.LinearChildren(expanded!).Count());
        }
    }
}
